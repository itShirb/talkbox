using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using DSharpPlus.Lavalink.EventArgs;
using MySql.Data.MySqlClient;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Net.Http.Headers;

namespace talkbox;

public class TextToSpeech
{
	public static async Task<List<Database.Users.Voice>> GetVoices()
    {
        using (HttpClient c = new())
        {
            using (HttpRequestMessage req = new())
            {
				req.Method = HttpMethod.Get;
				req.RequestUri = new Uri("https://texttospeech.googleapis.com/v1/voices?languageCode=en-*");
				req.Headers.Add("X-Goog-Api-Key", Program.gcpKey);
                using (HttpResponseMessage r = await c.SendAsync(req))
                {
					r.EnsureSuccessStatusCode();
					JObject res = JObject.Parse(await r.Content.ReadAsStringAsync());
					List<Database.Users.Voice> voices = new();
					JArray rawVoices = (JArray)res.GetValue("voices");
					IEnumerable<JToken> filteredVoices =
						from vo in rawVoices.Children()
						where vo["name"].Value<string>().Contains("WaveNet")
						select vo;
					
					foreach (JToken voice in filteredVoices)
                    {
						Debug.Print(voice["name"].Value<string>());
						voices.Add(new(
								voice["name"].Value<string>(),
                                voice["languageCodes"][0].Value<string>(),
								voice["ssmlGender"].Value<string>()
							));
                    }
					return voices;
				}
			}
        }
    }

	public static async Task<HttpContent?> ApiRequest(CommandContext ctx, string text)
	{
		//" , content
		var client = new HttpClient();
		JObject requestBody = new();
#pragma warning disable CS8602 // Dereference of a possibly null reference.
		requestBody["audioConfig"] = new JObject();
        requestBody["audioConfig"]["audioEncoding"] = "OGG_OPUS";
        requestBody["audioConfig"]["sampleRateHertz"] = 48000;
        requestBody["input"] = new JObject();
		requestBody["input"]["text"] = text;
		var voice = await Database.Users.GetVoice(ctx.User.Id);
		requestBody["voice"] = JObject.FromObject(voice);
		requestBody["audioConfig"] = new JObject();
		requestBody["audioConfig"]["speakingRate"] = voice.Rate;
		requestBody["audioConfig"]["pitch"] = voice.Pitch;
#pragma warning restore CS8602 // Dereference of a possibly null reference.
		var req = new HttpRequestMessage(
			HttpMethod.Post,
			new Uri("https://texttospeech.googleapis.com/v1/text:synthesize")
		);
		req.Headers.Add("X-Goog-Api-Key", Program.gcpKey);
		req.Content = new StringContent(requestBody.ToString(0));
		var response = await client.SendAsync(req);
		try
		{
			response.EnsureSuccessStatusCode();
			return response.Content;
		}catch(HttpRequestException e)
		{
			Debug.WriteLine(await response.Content.ReadAsStringAsync());
			return null;
		}
	}
}

public class AudioModule : BaseCommandModule
{
	[Command("connect")]
    [Description("Connect bot to voice channel")]
	public async Task ConnectCommand(CommandContext ctx)
	{
		// get caller's current voice channel
		DiscordMember member = ctx.Member;
		if (member.VoiceState == null)
        {
            await ctx.RespondAsync("You must be in a voice channel.");
			return;
        }
        DiscordChannel channel = member.VoiceState.Channel;
		
		// get lavalink connection
		LavalinkExtension lava = ctx.Client.GetLavalink();
		if (!lava.ConnectedNodes.Any())
        {
			await ctx.RespondAsync("Talkbox is not currently connected to its voice backend.");
			return;
		}
		// connect to node then channel
		LavalinkNodeConnection node = lava.GetIdealNodeConnection();
		// check if already in channel
		if (node.GetGuildConnection(ctx.Guild) != null && node.GetGuildConnection(ctx.Guild).Channel == channel)
        {
			await ctx.RespondAsync("Talkbox is already connected to your channel.");
			return;
		}
		Debug.WriteLine(node.IsConnected);
		// time out after 10 seconds
		LavalinkGuildConnection connecting = await node.ConnectAsync(channel).ConfigureAwait(false);
		if (connecting.IsConnected)
		{
			await ctx.RespondAsync($"Connected to {channel.Mention}");
		} else
        {
			await ctx.RespondAsync("Failed to connect to channel.");
		}
	}
	
	[Command("disconnect")]
    [Description("Disconnects bot from whatever voice channel it's in.")]
	public async Task DisconnectCommand(CommandContext ctx)
	{
		LavalinkExtension lava = ctx.Client.GetLavalink();
		LavalinkNodeConnection node = lava.GetIdealNodeConnection();
		LavalinkGuildConnection connection = node.GetGuildConnection(ctx.Guild);
		if (connection == null)
        {
			await ctx.RespondAsync("I can't leave a channel that I'm not in!");
        } else
        {
			await connection.DisconnectAsync();
        }
	}

	[Command("voicelist")]
    [Description("Allows you to change your current TTS voice.")]
    [Aliases("vl")]
	public async Task VoiceListCommand(CommandContext ctx)
	{
		// defer response so voice listing can take its time
		DiscordMessage msg = await ctx.RespondAsync("Working...");
		// icon for ssmlGender
		var voices = await TextToSpeech.GetVoices();

		string m = "";
        foreach (var voice in from voice in voices where voice.Gender is "MALE" select voice)
        {
			m += $"{voice.Language} {voice.Name}\n";
		}
		string f = "";
		foreach (var voice in from voice in voices where voice.Gender is "FEMALE" select voice)
		{
			f += $"{voice.Language} {voice.Name}\n";
		}

		DiscordEmbedBuilder builder = new DiscordEmbedBuilder()
			.WithTitle("Talkbox Voices")
			.AddField("Masculine", m, true)
			.AddField("Feminine", f, true);
		
		await msg.ModifyAsync("",builder.Build());
	}

	[Command("tts")]
    [Description("Speaks text in a voice channel")]
	public async Task TtsAsync(CommandContext ctx, [RemainingText] string text)
	{
		if (ctx.Member.Id == 768310054973079592)
        {
			await ctx.RespondAsync("piss off wyatt");
			return;
        }
		if (ctx.Member.VoiceState == null)
		{
			await ctx.RespondAsync(
				$"You need to set a voice before you can use tts!\nTo view a list of available voices, run `{Program.defaultPrefix}voicelsit`.\n" +
				$"To set a voice, run `{Program.defaultPrefix}setvoice [voice]`.");
			return;
		}
		using var response = await TextToSpeech.ApiRequest(ctx, text);
		if (response is null) return;
		JObject parsedResponse = JObject.Parse(response.ReadAsStringAsync().Result);
		string path = Path.GetFullPath(ctx.Message.Id.ToString());
#pragma warning disable CS8604 // Possible null reference argument.
        await File.WriteAllBytesAsync(path, Convert.FromBase64String((string?)parsedResponse.GetValue("audioContent")));
#pragma warning restore CS8604 // Possible null reference argument.
        LavalinkExtension lava = ctx.Client.GetLavalink();
		LavalinkNodeConnection node = lava.ConnectedNodes.Values.First();
		LavalinkGuildConnection guildConnection = node.GetGuildConnection(ctx.Guild);
		if (guildConnection == null || !guildConnection.IsConnected) await ConnectCommand(ctx);
		guildConnection = node.GetGuildConnection(ctx.Guild);
		LavalinkLoadResult loadResult = await guildConnection.GetTracksAsync(path, LavalinkSearchType.Plain);
		playingTracks.Add(loadResult.Tracks.First().TrackString, ctx.Message.Id);
		guildConnection.PlaybackFinished += (LavalinkGuildConnection con, TrackFinishEventArgs arg) => new Task(()=>{
			Debug.WriteLine(playingTracks[arg.Track.TrackString].ToString());
			File.Delete(Path.GetFullPath(playingTracks[arg.Track.TrackString].ToString()));
			playingTracks.Remove(arg.Track.TrackString);
		});
		await guildConnection.PlayAsync(loadResult.Tracks.First());
	}
	Dictionary<string, ulong> playingTracks = new();
}