using Discord;
using Discord.Audio;
using Discord.Commands;
using MySql.Data.MySqlClient;
using Newtonsoft.Json.Linq;

namespace talkbox;

public class TextToSpeech
{
	public static string[]? VoiceList;
	public static void GetVoiceList()
	{
		VoiceList = File.ReadAllLines("voicelist");
	}

	public static string? GetUserVoice(SocketCommandContext context)
	{
		return (string)DbHandler.CheckExists(1, "user_voice","user_data", "user_id", context.User.Id)!;
	}
	
	public static bool SetUserVoice(SocketCommandContext context, string voice)
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
			if (!(bool)((DbHandler.CheckExists(0, "user_id", "user_data", "user_id", context.User.Id)) ?? throw new InvalidOperationException()))
			{
				using var dbInsert = new MySqlCommand($"INSERT user_data SET user_id={context.User.Id}");
				dbInsert.Connection = Program.SqlCon;
				dbInsert.ExecuteScalar();
			}
		}catch(Exception err){Console.WriteLine(err);}
		using var dbUpdate = new MySqlCommand(
			$"UPDATE user_data SET user_voice='{voice}' WHERE user_id={context.User.Id}");
		dbUpdate.Connection = Program.SqlCon;
		dbUpdate.ExecuteScalar();
		return true;
	}

	public static async Task<HttpContent?> ApiRequest(SocketCommandContext context, string text)
	{
		var client = new HttpClient();
		var values = new Dictionary<string, string>
		{
			{ "voice", GetUserVoice(context) ?? throw new InvalidOperationException() },
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
			await context.Channel.SendMessageAsync(e.ToString());
			return null;
		}
	}
}

public class AudioModule : ModuleBase<SocketCommandContext>
{
	private readonly AudioService _audioService;
	public AudioModule(AudioService audioService) => _audioService = audioService;
	
	[Command("connect", RunMode = RunMode.Async)]
	[Summary("Connect bot to voice channel")]
	[Alias("c")]
	public async Task ConnectAsync([Remainder] [Summary("<voice_channel>")] IVoiceChannel? channel = null)
	{
		channel = channel ?? (Context.User as IGuildUser)?.VoiceChannel;
		if (channel is null)
		{
			await ReplyAsync("You must be in a voice channel, or a channel must be passed as an argument");
			return;
		}

		IAudioClient? audioClient = null;
		try
		{
			await _audioService.JoinAudio(Context.Guild, channel, Context);
		}
		catch
		{
			await ReplyAsync("Unable to connect to channel.");
		}

		if (audioClient is not null)
		{
			if (audioClient.ConnectionState == ConnectionState.Connecting)
				await ReplyAsync("Attempting to connect...");
			if (audioClient.ConnectionState == ConnectionState.Connected)
			{
				await ReplyAsync("Connected to channel.");
			}
		}
	}
	
	[Command("disconnect")]
	[Summary("Disconnects bot from whatever voice channel it's in.")]
	[Alias("dc")]
	public async Task DisconnectAsync()
	{
		await _audioService.LeaveAudio(Context.Guild, Context);
	}

	[Command("voicelist")]
	[Summary("Provides a list of available voices")]
	[Alias("vl")]
	public async Task VoiceListAsync()
	{
		var list = "";
		if (TextToSpeech.VoiceList != null)
			for (var e = 0; e < TextToSpeech.VoiceList.Length; e++)
			{
				list += TextToSpeech.VoiceList[e];
				if (e % 6 == 0 && e!=0) list += "\n";
				else list += " ";
			}

		var voice = TextToSpeech.GetUserVoice(Context);
		
		var response = $"Here's the list of available voices:\n```\n{list}\n```\n";
		if (voice is not null)
		{
			response += $"Your voice is currently set to `{voice}`";
		}
		else
		{
			response +=
				$"You don't currently have a voice set. To set a voice, run `{Program.DefaultPrefix}setvoice [voice]`";
		}
		await ReplyAsync(response);
	}
	
	[Command("setvoice")]
	[Summary("Sets the voice that TTS will use when you speak")]
	[Alias("sv")]
	public async Task SetVoiceAsync([Remainder] [Summary("[voice]")] string? voice = null)
	{
		if (voice is null)
		{
			await ReplyAsync(CommandHandler.ReturnCommandUsage("setvoice").Result);
			return;
		}

		if (TextToSpeech.SetUserVoice(Context, voice))
		{
			await ReplyAsync($"Voice was successfully set to {voice}");
			return;
		}

		await ReplyAsync(
			$"You didn't specify a valid voice option. For a list of voices, run `{Program.DefaultPrefix}voicelist`.");
	}

	[Command("tts", RunMode = RunMode.Async)]
	[Summary("Speaks text in a voice channel")]
	public async Task TtsAsync([Remainder][Summary("[text]")]string? text = null)
	{
		if (text is null)
		{
			await ReplyAsync(CommandHandler.ReturnCommandUsage("tts").Result);
			return;
		}

		if (TextToSpeech.GetUserVoice(Context) is null)
		{
			await ReplyAsync(
				$"You need to set a voice before you can use tts!\nTo view a list of available voices, run `{Program.DefaultPrefix}voicelsit`.\n" +
				$"To set a voice, run `{Program.DefaultPrefix}setvoice [voice]`.");
			return;
		}
		var response = await TextToSpeech.ApiRequest(Context, text);
		if (response is null) return;
		var parsedResponse = JObject.Parse(response.ReadAsStringAsync().Result);
		var speakUrl = "";
		foreach (var entry in parsedResponse)
		{
			if (entry.Key == "speak_url"&&entry.Value is not null) speakUrl = (string)entry.Value!;
		}

		await _audioService.SendAudioAsync(Context.Guild, Context.Channel, speakUrl);

	}
}