using System.Collections.Concurrent;
using System.Diagnostics;
using Discord;
using Discord.Audio;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;

namespace talkbox;

public class AudioService
{
	private readonly ConcurrentDictionary<ulong, IAudioClient?> _connectedChannels = new();

	public async Task JoinAudio(IGuild guild, IVoiceChannel target, SocketCommandContext context)
	{
		if (_connectedChannels.TryGetValue(guild.Id, out var client))
		{
			return;
		}

		if (target.Guild.Id != guild.Id)
		{
			return;
		}

		var audioClient = await target.ConnectAsync();
		await context.Channel.SendMessageAsync($"Joined {target}");
		if (_connectedChannels.TryAdd(guild.Id, audioClient))
		{
			Console.WriteLine($"Connected to channel in {guild.Name}");
		}
	}

	public async Task LeaveAudio(IGuild guild, SocketCommandContext context)
	{
		if (_connectedChannels.TryRemove(guild.Id, out var client))
		{
			if (client != null) await client.StopAsync();
			await context.Channel.SendMessageAsync($"Disconnected from channel.");
			Console.WriteLine($"Disconnected from channel in {guild.Name}");
		}
		else
		{
			await context.Channel.SendMessageAsync("I'm not in a voice channel!");
		}
	}

	public async Task SendAudioAsync(IGuild guild, IMessageChannel channel, string path)
	{
		if (!path.Contains("https://polly.streamlabs.com"))
		{
			await channel.SendMessageAsync("Failed to retrieve tts link.");
			return;
		}

		if (_connectedChannels.TryGetValue(guild.Id, out var client))
		{
			using var ffmpeg = CreateProcess(path);
			await using var stream = client.CreatePCMStream(AudioApplication.Music);
			try
			{
				await ffmpeg.StandardOutput.BaseStream.CopyToAsync(stream);
			}
			finally
			{
				await stream.FlushAsync();
			}
		}
	}

	private Process CreateProcess(string path)
	{
		try
		{
			return Process.Start(new ProcessStartInfo
			{
				FileName = "ffmpeg",
				Arguments = $"-hide_banner -loglevel panic -i \"{path}\" -ac 2 -f s16le -ar 48000 pipe:1",
				UseShellExecute = false,
				RedirectStandardOutput = true
			}) ?? throw new InvalidOperationException();
		}
		catch(Exception e)
		{
			Console.WriteLine(e);
			throw new Exception();
		}
	}
}