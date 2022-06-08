#nullable enable
using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using MySql.Data.MySqlClient;

namespace talkbox
{
	public class SetPrefixModule : ModuleBase<SocketCommandContext>
	{
		[RequireUserPermission(GuildPermission.Administrator, Group = "P")]
		[RequireOwner(Group = "P")]
		[Command("setprefix")]
		[Summary("Sets a new prefix for the bot on this server")]
		[Alias("sp")]
		public async Task SetPrefixAsync([Remainder] [Summary("[prefix]")] string? newPrefix = null)
		{
			if (newPrefix is null)
			{
				await ReplyAsync(CommandHandler.ReturnCommandUsage("setprefix").Result);
				return;
			}

			if (newPrefix.Length > 5)
			{
				await ReplyAsync("Prefix cannot be more than 5 characters long.");
				return;
			}

			var hasWhiteSpace = false;
			foreach (var ch in newPrefix)
			{
				if (char.IsWhiteSpace(ch)) hasWhiteSpace = true;
			}

			if (hasWhiteSpace)
			{
				await ReplyAsync("The prefix cannot contain white space.\n" +
				                 "(White space includes characters like spaces, tabs, etc.)");
				return;
			}

			if (!(bool)DbHandler.CheckExists(0, "guild_id","guild_data", "guild_id", Context.Guild.Id))
			{
				using var dbInsert = new MySqlCommand($"INSERT guild_data SET guild_id={Context.Guild.Id}");
				dbInsert.Connection = Program.SqlCon;
				dbInsert.ExecuteScalar();
				dbInsert.CommandText = $"UPDATE guild_data SET guild_prefix='{newPrefix}' WHERE guild_id={Context.Guild.Id}";
				dbInsert.ExecuteScalar();
				await ReplyAsync($"The server prefix has been changed to `{newPrefix}`.");
			}
			else
			{
				using var dbUpdate =
					new MySqlCommand(
						$"UPDATE guild_data SET guild_prefix='{newPrefix}' WHERE guild_id={Context.Guild.Id}");
				dbUpdate.Connection = Program.SqlCon;
				dbUpdate.ExecuteScalar();
				await ReplyAsync($"The server prefix has been changed to `{newPrefix}`.");
			}
		}
	}
}