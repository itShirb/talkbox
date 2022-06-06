#nullable enable
using System.Threading.Tasks;
using Discord.Commands;

namespace talkbox
{
	public class SetPrefixModule : ModuleBase<SocketCommandContext>
	{
		[Command("setprefix")]
		[Summary("Sets a new prefix for the bot on this server")]
		[Alias("sp")]
		public async Task SetPrefixAsync([Summary("[prefix]")] string? newPrefix = null)
		{
			
		}
	}
}