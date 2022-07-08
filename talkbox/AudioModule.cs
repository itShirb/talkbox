using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using MySql.Data.MySqlClient;
using Newtonsoft.Json.Linq;

namespace talkbox;

public class TextToSpeech
{
	public static List<string> VoiceList = new(File.ReadAllLines("voicelist"));
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
		var client = new HttpClient();
		var values = new Dictionary<string, string>
		{
			{ "voice", GetUserVoice(ctx) ?? throw new InvalidOperationException() },
			{ "text", text }
		};
		var content = new FormUrlEncodedContent(values);
		try
		{
			var response = await client.PostAsync("https://streamlabs.com/polly/speak", content);
			response.EnsureSuccessStatusCode();
			return response.Content;
		}catch(HttpRequestException e)
		{
			await ctx.Channel.SendMessageAsync(e.ToString());
			return null;
		}
	}
}

public class AudioModule : BaseCommandModule
{
	[Command("connect")]
	[Description("Connect bot to voice channel")]
	[Aliases("c")]
	public async Task ConnectCommand(CommandContext ctx, DiscordMember member)
	{
		// get caller's current voice channel
		await ctx.RespondAsync("fuck you baltimore");
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
        }
		// connect to channel
		LavalinkNodeConnection node = lava.ConnectedNodes.Values.First();
		await node.ConnectAsync(channel);
		await ctx.RespondAsync($"Connected to {channel.Mention}");
	}
	
	[Command("disconnect")]
	[Description("Disconnects bot from whatever voice channel it's in.")]
	[Aliases("dc")]
	public async Task DisconnectCommand(CommandContext ctx, DiscordMember member)
	{
		LavalinkExtension lava = ctx.Client.GetLavalink();
		LavalinkNodeConnection node = lava.ConnectedNodes.Values.First();
		LavalinkGuildConnection connection = node.GetGuildConnection(ctx.Guild);
		if (!connection.IsConnected)
        {
			await ctx.RespondAsync("I can't leave a channel that I'm not in!");
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
		DiscordEmbedBuilder embed = new();
		embed.Title = "Talkbox Voices";
		embed.AddField("** **",
			String.Join("\n", TextToSpeech.VoiceList.GetRange(0, TextToSpeech.VoiceList.Count / 2)),
			true);
		embed.AddField("** **",
			String.Join("\n", TextToSpeech.VoiceList.GetRange(TextToSpeech.VoiceList.Count / 2, TextToSpeech.VoiceList.Count - 1)),
			true);
		await ctx.RespondAsync(embed.Build());
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
		string? speakUrl = parsedResponse.GetValue("speak_url").Value<string>();

		LavalinkExtension lava = ctx.Client.GetLavalink();
		LavalinkNodeConnection node = lava.ConnectedNodes.Values.First();
		LavalinkGuildConnection guildConnection = node.GetGuildConnection(ctx.Guild);
		if (!guildConnection.IsConnected) await ConnectCommand(ctx, ctx.Member);
		LavalinkLoadResult loadResult = await guildConnection.GetTracksAsync(speakUrl, LavalinkSearchType.Plain);
		await guildConnection.PlayAsync(loadResult.Tracks.First());
	}
}