using Discord;
using Discord.Audio;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;

namespace talkbox;

public class AudioService
{
	public IAudioClient? AudioClient { get; set; }
	public IVoiceChannel? VoiceChannel { get; set; }
}

public class Setup
{
	public IServiceProvider BuildProvider() => new ServiceCollection()
		.AddSingleton<AudioService>()
		.BuildServiceProvider();
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
			audioClient = await channel.ConnectAsync();
			_audioService.AudioClient = audioClient;
			_audioService.VoiceChannel = channel;
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
		if (_audioService.VoiceChannel != null)
		{
			await _audioService.VoiceChannel.DisconnectAsync();
			await ReplyAsync("Disconnected from voice channel.");
		}
		else await ReplyAsync("I'm not in a voice channel!");
	}
}