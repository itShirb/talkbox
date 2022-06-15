// using Discord.Commands;
//
// namespace talkbox;
//
// public class ConfigModule : ModuleBase<SocketCommandContext>
// {
// 	[Command("config")]
// 	[Summary("Configure some options about how the bot behaves in the server.")]
// 	[Alias("c")]
// 	public async Task ConfigAsync([Summary("[set/list]")] string? mode = null, [Summary("(option)")] string? option = null,
// 		[Remainder] [Summary("(value)")] string? value = null)
// 	{
// 		switch (mode)
// 		{
// 			case null:
// 				await ReplyAsync(CommandHandler.ReturnCommandUsage("config").Result);
// 				return;
// 			case "list":
// 				// list all configs here
// 				break;
// 			case "set":
// 				if (option is null)
// 				{
// 					await ReplyAsync($"You need to specify a configuration option that you'd like to edit.\n" +
// 					                 $"To view a list of all config options as well as see their current values, do `{Program.DefaultPrefix}config list`");
// 					return;
// 				}
//
// 				var realName = option.ToLower();
// 				if ((bool)(DbHandler.CheckExists(0, realName, "guild_data", "guild_id", Context.Guild.Id) ??
// 				           throw new InvalidOperationException()))
// 				{
// 					await ReplyAsync("That is not a valid configuration option.\n" +
// 					                 $"To view a list of all config options, do `{Program.DefaultPrefix}config list`");
// 					return;
// 				}
// 				
// 				break;
// 		}
// 	}
// }