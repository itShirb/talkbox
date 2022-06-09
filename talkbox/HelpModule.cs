#nullable enable
using Discord;
using Discord.Commands;

namespace talkbox;

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
				else
					foreach (var alias in c.Aliases)
					{
						if (alias == command) cmd = c;
					}
			}
			if (cmd is null)
			{
				await ReplyAsync("Command not found.");
			}
			else
			{
				_embedBuilder.Title = cmd.Name;
				_embedBuilder.Description = $"Description: `{cmd.Summary}`";
				_embedBuilder.Description += "\nAliases: `";
				foreach (var alias in cmd.Aliases)
				{
					if (alias == cmd.Name) continue;
					_embedBuilder.Description += alias+" ";
				}
				_embedBuilder.Description = _embedBuilder.Description.TrimEnd();
				_embedBuilder.Description += $"`\nUsage: `{Program.DefaultPrefix}{cmd.Name} ";
				foreach (var param in cmd.Parameters)
				{
					_embedBuilder.Description += param.Summary + " ";
				}
				_embedBuilder.Description = _embedBuilder.Description.TrimEnd();
				_embedBuilder.Description += "`";
				await ReplyAsync("Command info:", false, _embedBuilder.Build());
			}
		}
	}
}