#nullable enable
using System.Threading.Tasks;
using Discord;
using Discord.Audio;
using Discord.Commands;

namespace talkbox;

public class ConnectModule : ModuleBase<SocketCommandContext>
{
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
}