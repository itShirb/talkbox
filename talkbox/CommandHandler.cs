using System;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace talkbox
{
	public class CommandHandler
	{
		private readonly DiscordSocketClient _client;
		public static CommandService Commands;

		public CommandHandler(DiscordSocketClient client, CommandService commands)
		{
			_client = client;
			CommandHandler.Commands = commands;
		}

		public async Task InstallCommandsAsync()
		{
			_client.MessageReceived += HandleCommandAsync;
			await Commands.AddModulesAsync(assembly: Assembly.GetEntryAssembly(), services: null);
		}

		public static Task<string> ReturnCommandUsage(string commandName)
		{
			CommandInfo cmd = null;
			foreach (var c in CommandHandler.Commands.Commands)
			{
				if (c.Name == commandName) cmd = c;
			}
			if (cmd is null) return Task.FromResult("Command could not be found for some reason");
			var par = "";
			foreach (var param in cmd.Parameters)
			{
				par += param.Summary+" ";
			}

			return Task.FromResult($"Command Usage: {Program.DefaultPrefix}{cmd.Name} {par}");
		}

		private string GetCustomPrefix(SocketUserMessage msg)
		{
			SocketGuild guild;
			guild = msg.Channel is SocketGuildChannel channel ? channel.Guild : (SocketGuild)null;
			if (guild is not null) return (string)DbHandler.CheckExists(1, "guild_prefix","guild_data", "guild_id", guild.Id);
			return null;
		}
		
		private async Task HandleCommandAsync(SocketMessage messageParam)
		{
			if (messageParam is not SocketUserMessage message) return;
			var argPos = 0;
			
			if (!(message.HasStringPrefix(Program.DefaultPrefix, ref argPos) ||
			      message.HasMentionPrefix(_client.CurrentUser, ref argPos) ||
			      (GetCustomPrefix(message) is not null && message.HasStringPrefix(GetCustomPrefix(message), ref argPos))) || message.Author.IsBot) return;
			var context = new SocketCommandContext(_client, message);
			await Commands.ExecuteAsync(
				context: context,
				argPos: argPos,
				services: null);
		}
	}
}