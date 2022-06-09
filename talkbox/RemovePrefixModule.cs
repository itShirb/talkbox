using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using MySql.Data.MySqlClient;

namespace talkbox;

public class RemovePrefixModule : ModuleBase<SocketCommandContext>
{
	[RequireUserPermission(GuildPermission.Administrator, Group = "P")]
	[RequireOwner(Group = "P")]
	[Command("removeprefix")]
	[Summary("Removes the custom server prefix.")]
	[Alias("rp")]
	public async Task RemovePrefixAsync()
	{
		if (!(bool)(DbHandler.CheckExists(0, "guild_id", "guild_data", "guild_id", Context.Guild.Id) ?? throw new InvalidOperationException()) ||
		    (string)DbHandler.CheckExists(1, "guild_prefix", "guild_data", "guild_id", Context.Guild.Id)! is null)
		{
			await ReplyAsync("There is no custom prefix set.");
			return;
		}
		using var dbRemove =
			new MySqlCommand($"UPDATE guild_data SET guild_prefix=null WHERE guild_id={Context.Guild.Id}");
		dbRemove.Connection = Program.SqlCon;
		dbRemove.ExecuteScalar();
		await ReplyAsync("The server prefix has been removed. The default prefix is `tb$`.");
	}
}