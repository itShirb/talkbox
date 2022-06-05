#nullable enable
using System.Threading.Tasks;
using Discord.Commands;

namespace talkbox
{
	public class EchoModule : ModuleBase<SocketCommandContext>
	{
		[Command("echo")]
		[Summary("Echoes a message.")]
		[Alias("e")]
		public async Task EchoAsync([Remainder] [Summary("[text]")] string? echo = null)
		{
			if (Context.Message.MentionedEveryone) await Context.Channel.SendMessageAsync("no");
			if (echo is null)
			{
				var msg = CommandHandler.ReturnCommandUsage("echo");
				await ReplyAsync(msg.Result);
			}
			else await ReplyAsync(echo);
		}
	}
}