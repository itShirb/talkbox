using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using DSharpPlus.Lavalink.EventArgs;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using DSharpPlus.SlashCommands.EventArgs;
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

	public static async Task<HttpContent?> ApiRequest(InteractionContext ctx, string text)
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

public class AudioModule : ApplicationCommandModule
{
	[SlashCommand("connect", "Connect bot to voice channel")]
	public async Task ConnectCommand(InteractionContext ctx)
	{
		// get caller's current voice channel
		DiscordMember member = ctx.Member;
		if (member.VoiceState == null)
        {
            await ctx.CreateResponseAsync("You must be in a voice channel.", true);
			return;
        }
        DiscordChannel channel = member.VoiceState.Channel;
		
		// get lavalink connection
		LavalinkExtension lava = ctx.Client.GetLavalink();
		if (!lava.ConnectedNodes.Any())
        {
			await ctx.CreateResponseAsync("Talkbox is not currently connected to its voice backend.", true);
			return;
		}
		// connect to node then channel
		LavalinkNodeConnection node = lava.GetIdealNodeConnection();
		// check if already in channel
		if (node.GetGuildConnection(ctx.Guild) != null && node.GetGuildConnection(ctx.Guild).Channel == channel)
        {
			await ctx.CreateResponseAsync("Talkbox is already connected to your channel.", true);
			return;
		}
		Debug.WriteLine(node.IsConnected);
		// time out after 10 seconds
		LavalinkGuildConnection connecting = await node.ConnectAsync(channel).ConfigureAwait(false);
		if (connecting.IsConnected)
		{
			await ctx.CreateResponseAsync($"Connected to {channel.Mention}");
		} else
        {
			await ctx.CreateResponseAsync("Failed to connect to channel.", true);
		}
	}
	
	[SlashCommand("disconnect", "Disconnects bot from whatever voice channel it's in.")]
	public async Task DisconnectCommand(InteractionContext ctx)
	{
		LavalinkExtension lava = ctx.Client.GetLavalink();
		LavalinkNodeConnection node = lava.GetIdealNodeConnection();
		LavalinkGuildConnection connection = node.GetGuildConnection(ctx.Guild);
		if (connection == null)
        {
			await ctx.CreateResponseAsync("I can't leave a channel that I'm not in!", true);
        } else
        {
			await connection.DisconnectAsync();
        }
	}

	[SlashCommand("voice", "Allows you to change your current TTS voice.")]
	public async Task VoiceListCommand(InteractionContext ctx)
	{
		// defer response so voice listing can take its time
		await ctx.DeferAsync(true);
		await ctx.CreateResponseAsync(DSharpPlus.InteractionResponseType.DeferredChannelMessageWithSource,
			new DiscordInteractionResponseBuilder().WithContent("Working...").AsEphemeral());
		// list voices

		/* Build a response looking like:
		 * Select a voice for TTS
		 * [ English (UK) - Voice A    v]
		 */
		List<DiscordSelectComponentOption> voices = new();
		var currentVoice = await Database.Users.GetVoice(ctx.User.Id);
		// icon for ssmlGender
		DiscordComponentEmoji male = new(DiscordEmoji.FromName(ctx.Client, ":male_sign:"));
		DiscordComponentEmoji female = new(DiscordEmoji.FromName(ctx.Client, ":female_sign:"));
		foreach (var voice in await TextToSpeech.GetVoices())
        {
			voices.Add(new DiscordSelectComponentOption(
					voice.name,
					JObject.FromObject(voice).ToString(Newtonsoft.Json.Formatting.None).ToString(),
					voice.languageCode,
					voice == currentVoice,
					voice.ssmlGender == "MALE"?male:female
				));
        }

		DiscordFollowupMessageBuilder builder = new DiscordFollowupMessageBuilder()
			.WithContent("You can select a voice here:")
			.AddComponents(new DiscordSelectComponent(
					"voiceSelection",
					"Voices",
					voices));
		
		await ctx.FollowUpAsync(builder);
	}

	[SlashCommand("tts", "Speaks text in a voice channel")]
	public async Task TtsAsync(InteractionContext ctx, [Option("text", "Text to be spoken.")] string text)
	{
		if (ctx.Member.Id == 768310054973079592)
        {
			await ctx.CreateResponseAsync("piss off wyatt");
			return;
        }
		if (ctx.Member.VoiceState == null)
		{
			await ctx.CreateResponseAsync(
				$"You need to set a voice before you can use tts!\nTo view a list of available voices, run `{Program.defaultPrefix}voicelsit`.\n" +
				$"To set a voice, run `{Program.defaultPrefix}setvoice [voice]`.");
			return;
		}
		using var response = await TextToSpeech.ApiRequest(ctx, text);
		if (response is null) return;
		JObject parsedResponse = JObject.Parse(response.ReadAsStringAsync().Result);
		string path = Path.GetFullPath(ctx.InteractionId.ToString());
#pragma warning disable CS8604 // Possible null reference argument.
        await File.WriteAllBytesAsync(path, Convert.FromBase64String((string?)parsedResponse.GetValue("audioContent")));
#pragma warning restore CS8604 // Possible null reference argument.
        LavalinkExtension lava = ctx.Client.GetLavalink();
		LavalinkNodeConnection node = lava.ConnectedNodes.Values.First();
		LavalinkGuildConnection guildConnection = node.GetGuildConnection(ctx.Guild);
		if (guildConnection == null || !guildConnection.IsConnected) await ConnectCommand(ctx);
		guildConnection = node.GetGuildConnection(ctx.Guild);
		LavalinkLoadResult loadResult = await guildConnection.GetTracksAsync(path, LavalinkSearchType.Plain);
		playingTracks.Add(loadResult.Tracks.First().TrackString, ctx.InteractionId);
		guildConnection.PlaybackFinished += (LavalinkGuildConnection con, TrackFinishEventArgs arg) => new Task(()=>{
			Debug.WriteLine(playingTracks[arg.Track.TrackString].ToString());
			File.Delete(Path.GetFullPath(playingTracks[arg.Track.TrackString].ToString()));
			playingTracks.Remove(arg.Track.TrackString);
		});
		await guildConnection.PlayAsync(loadResult.Tracks.First());
	}
	Dictionary<string, ulong> playingTracks = new();
}