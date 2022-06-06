using System;
using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using MongoDB.Bson;
using MongoDB.Driver;

namespace talkbox
{
	internal class Program
	{
		public static Task Main(string[] args) => new Program().MainAsync();
		public static string prefix;

		private DiscordSocketClient _client;
		private CommandHandler _command;
		private CommandService _service;
		public static MongoClient MClient;
		public static IMongoDatabase MDatabase;
		public static IMongoCollection<BsonDocument> ServerData;

		private async Task MainAsync()
		{
			var config = new DiscordSocketConfig{ MessageCacheSize = 100 };
			_client = new DiscordSocketClient(config);
			_service = new CommandService();
			_command = new CommandHandler(_client, _service);
			MClient = new MongoClient("mongodb+srv://shirb:VtMoH1J9FnT4DL8h@talkbox.gquw3fu.mongodb.net/test");
			MDatabase = MClient.GetDatabase("talkbox");
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