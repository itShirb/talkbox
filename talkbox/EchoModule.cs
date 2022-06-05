using System.Threading.Tasks;
using Discord.Commands;

namespace talkbox
{
	public class EchoModule : ModuleBase<SocketCommandContext>
	{
		private readonly string _prompt = $"Command usage: `{Program.prefix}echo [text]`";
		[Command("echo")]
		[Summary("Echoes a message.")]
		[Alias("e")]
		public async Task EchoAsync([Remainder] [Summary("Echoes text.")] string echo = null)
		{
			if (Context.Message.MentionedEveryone) await Context.Channel.SendMessageAsync("no");
			if (echo is null) await Context.Channel.SendMessageAsync(_prompt);
			else await ReplyAsync(echo);
		}
	}
}