using System;
using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Data.Sql;
// using System.Data.SqlClient;
using MongoDB.Bson;
using MongoDB.Driver;
using MySql.Data.MySqlClient;

namespace talkbox
{
	internal class Program
	{
		public static Task Main(string[] args) => new Program().MainAsync();
		public const string DefaultPrefix = "tb$";
		private DiscordSocketClient _client;
		private CommandHandler _command;
		private CommandService _service;
		// private static SqlConnectionStringBuilder _sqlBuilder;
		// public static SqlConnection SqlCon;
		
		// public static MongoClient MClient;
		// public static IMongoDatabase MDatabase;
		// public static IMongoCollection<BsonDocument> ServerData;

		public static MySqlConnection SqlCon;
		
		private async Task MainAsync()
		{
			var config = new DiscordSocketConfig{ MessageCacheSize = 100 };
			_client = new DiscordSocketClient(config);
			_service = new CommandService();
			_command = new CommandHandler(_client, _service);
			// MClient = new MongoClient("mongodb+srv://shirb:VtMoH1J9FnT4DL8h@talkbox.gquw3fu.mongodb.net/test");
			// MDatabase = MClient.GetDatabase("talkbox");
			await _command.InstallCommandsAsync();
			_client.Log += Log;
			
			var token = File.ReadAllText("token.txt");
			var passwd = File.ReadAllText("passwd");
			var uname = File.ReadAllText("uname");
			var conStr = $"server=localhost;uid={uname};pwd={passwd};database=talkbox";
			SqlCon = new MySqlConnection();
			try
			{
				SqlCon.ConnectionString = conStr;
				SqlCon.Open();
				Console.WriteLine("Connected to database");
			}
			catch (MySqlException err)
			{
				Console.WriteLine(err);
			}
			// _sqlBuilder = new SqlConnectionStringBuilder
			// {
			// 	UserID = uname,
			// 	Password = passwd,
			// 	["Server"] = "localhost"
			// };
			// Console.WriteLine(_sqlBuilder.ConnectionString);
			//
			// using (SqlCon = new SqlConnection(_sqlBuilder.ConnectionString))
			// {
			// 	try
			// 	{
			// 		SqlCon.Open();
			// 		Console.WriteLine("Connected to database");
			// 	}
			// 	catch (SqlException err)
			// 	{
			// 		Console.WriteLine(err);
			// 	}
			// }

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