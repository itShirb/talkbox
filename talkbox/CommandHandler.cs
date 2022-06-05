using System;
using System.Reflection;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;

namespace talkbox
{
	public class CommandHandler
	{
		private readonly DiscordSocketClient _client;
		private readonly CommandService _commands;

		public CommandHandler(DiscordSocketClient client, CommandService commands)
		{
			_client = client;
			_commands = commands;
		}

		public async Task InstallCommandsAsync()
		{
			_client.MessageReceived += HandleCommandAsync;
			await _commands.AddModulesAsync(assembly: Assembly.GetEntryAssembly(), services: null);
		}

		private async Task HandleCommandAsync(SocketMessage messageParam)
		{
			if (messageParam is not SocketUserMessage message) return;
			var argPos = 0;
			if (!(message.HasStringPrefix(Program.prefix, ref argPos) ||
			      message.HasMentionPrefix(_client.CurrentUser, ref argPos)) || message.Author.IsBot) return;
			var context = new SocketCommandContext(_client, message);
			await _commands.ExecuteAsync(
				context: context,
				argPos: argPos,
				services: null);
		}
	}
}