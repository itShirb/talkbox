using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace talkbox
{
	public class DisconnectModule : ModuleBase<SocketCommandContext>
	{
		[Command("disconnect")]
		[Summary("Disconnects bot from whatever voice channel it's in.")]
		[Alias("dc")]
		public async Task DisconnectAsync([Remainder][Summary("[channel]")] IVoiceChannel? channel = null)
		{
			channel = channel ?? (Context.User as IGuildUser)?.VoiceChannel;
			if (channel == null)
			{
				await ReplyAsync("Due to problems beyond my control, you either need to specify the channel I'm in," +
				                 "or you need to be in the channel that I'm in when you disconnect me.");
				return;
			}

			await channel.DisconnectAsync();
			await ReplyAsync("Disconnected from voice channel.");
		}
	}
}