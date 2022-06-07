#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace talkbox
{
	public class HelpModule : ModuleBase<SocketCommandContext>
	{
		private List<CommandInfo> commands = CommandHandler.Commands.Commands.ToList();
		private EmbedBuilder _embedBuilder = new EmbedBuilder();
		[Command("help")]
		[Summary("Provides command usage and information")]
		[Alias("h")]
		public async Task HelpAsync([Remainder] [Summary("[command]")] string? command = null)
		{
			_embedBuilder.Color = new Color((byte)87, (byte)14, (byte)156);
			if (command is null)
			{
				foreach (var cmd in commands)
				{
					var embedFieldText = cmd.Summary ?? "No description provided";
					_embedBuilder.AddField(cmd.Name, embedFieldText);
				}

				await ReplyAsync("", false, _embedBuilder.Build());
			}
			else
			{
				CommandInfo? cmd = null;
				foreach (var c in commands)
				{
					if (c.Name == command) cmd = c;
				}
				if (cmd is null)
				{
					await ReplyAsync("Command not found.");
				}
				else
				{
					var embedFieldText = cmd.Summary ?? "No description provided";
					_embedBuilder.Title = cmd.Name;
					_embedBuilder.Description = cmd.Summary+$"\nUsage: {Program.DefaultPrefix}{cmd.Name} ";
					foreach (var param in cmd.Parameters)
					{
						_embedBuilder.Description += param.Summary + " ";
					}
					await ReplyAsync("Command info:", false, _embedBuilder.Build());
				}
			}
		}
	}	
}