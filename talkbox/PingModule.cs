using System.Threading.Tasks;
using Discord.Commands;

namespace talkbox;

public class PingModule : ModuleBase<SocketCommandContext>
{
	[Command("ping")]
	[Summary("Test response time for the bot")]
	[Alias("p")]
	public async Task PingAsync()
	{
		// not sure if this works right, i just felt the need to have a ping command
		await ReplyAsync($"Pong! `{Context.Client.Latency}ms`");
	}
}