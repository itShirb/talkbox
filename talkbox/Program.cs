﻿using System;
using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace talkbox
{
	internal class Program
	{
		public static Task Main(string[] args) => new Program().MainAsync();
		public static string prefix = "tb$";

		private DiscordSocketClient _client;
		private CommandHandler _command;
		private CommandService _service;
		public async Task MainAsync()
		{
			// token OTgxNzYxMDM5MTgxOTQyODI0.Gtsxhe.89n-KhGUx9c3sjccF41FcYQqEf7hkOYpjdWVW4
			// invite link https://discord.com/api/oauth2/authorize?client_id=981761039181942824&permissions=2213624128&scope=bot

			var config = new DiscordSocketConfig{ MessageCacheSize = 100 };
			_client = new DiscordSocketClient(config);
			_service = new CommandService();
			_command = new CommandHandler(_client, _service);
			await _command.InstallCommandsAsync();
			_client.Log += Log;

			var token = File.ReadAllText("token.txt");
			
			await _client.LoginAsync(TokenType.Bot, token);
			await _client.StartAsync();

			_client.Ready += () =>
			{
				Console.WriteLine("talkbox is now online.");
				return Task.CompletedTask;
			};
			await Task.Delay(-1);
		}

		private Task Log(LogMessage msg)
		{
			Console.WriteLine(msg.ToString());
			return Task.CompletedTask;
		}

	}
}