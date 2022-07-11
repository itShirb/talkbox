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
	public static string[] VoiceList;
	public static string? GetUserVoice(CommandContext ctx)
	{
		return (string)Database.CheckExists(1, "user_voice","user_data", "user_id", ctx.User.Id)!;
	}
	
	public static bool SetUserVoice(CommandContext ctx, string voice)
	{
		var voiceExists = false;
		if (VoiceList != null)
			foreach (var entry in VoiceList)
			{
				if (entry == voice) voiceExists = true;
			}
		if (!voiceExists) return false;
		try
		{
			if (!(bool)((Database.CheckExists(0, "user_id", "user_data", "user_id", ctx.User.Id)) ?? throw new InvalidOperationException()))
			{
				using var dbInsert = new MySqlCommand($"INSERT user_data SET user_id={ctx.User.Id}");
				dbInsert.Connection = Program.sqlConnection;
				dbInsert.ExecuteScalar();
			}
		}catch(Exception err){Console.WriteLine(err);}
		using var dbUpdate = new MySqlCommand(
			$"UPDATE user_data SET user_voice='{voice}' WHERE user_id={ctx.User.Id}");
		dbUpdate.Connection = Program.sqlConnection;
		dbUpdate.ExecuteScalar();
		return true;
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
		requestBody["voice"] = new JObject();
		requestBody["voice"]["name"] = "en-GB-WaveNet-A";
		requestBody["voice"]["languageCode"] = "en-GB";
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
	[Aliases("c")]
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
	[Aliases("dc")]
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
	[Description("Provides a list of available voices")]
	[Aliases("vl")]
	public async Task VoiceListCommand(CommandContext ctx)
	{
        /* splits list in half, so instead of:
		 * alice
		 * bob
		 * jane
		 * bill
		 * 
		 * you get:
		 * alice  jane
		 * bob    bill
		 */
        try
        {
			DiscordEmbedBuilder embed = new();
			embed.Title = "Talkbox Voices";
			var chunks = TextToSpeech.VoiceList.Chunk(TextToSpeech.VoiceList.Length / 3);
            foreach (var chunk in chunks) // because it's better than figuring out how to index this thing
            {
				embed.AddField("** **",
				String.Join("\n", chunk.ToList()), true);
			}
			await ctx.RespondAsync(embed.Build());
		}
        catch (Exception e)
        {
			Debug.WriteLine(e.Message);
        }
	}
	
	[Command("setvoice")]
	[Description("Sets the voice that TTS will use when you speak")]
	[Aliases("sv")]
	public async Task SetVoiceCommand(
		CommandContext ctx,
		[Description("Voice to use")] string voice
		)
	{
		if (await Program.db.setVoice(ctx.User.Id, voice))
        {
			await ctx.RespondAsync("Set your TTS voice to " + voice);
        } else {
			await ctx.RespondAsync("Failed to change your TTS voice");
        }
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