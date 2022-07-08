using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using MySql.Data.MySqlClient;
using Victoria;

namespace talkbox;

internal class Program
{
	public static Task Main(string[] args) => new Program().MainAsync();
	public const string DefaultPrefix = "tb$";
	private DiscordSocketClient _client = null!;
	private CommandHandler _command = null!;
	private CommandService _commandService = null!;
	private IServiceProvider? _services;

	public static MySqlConnection SqlCon = null!;
		
	private async Task MainAsync()
	{
		TextToSpeech.GetVoiceList();
		var config = new DiscordSocketConfig{ MessageCacheSize = 100 };
		_client = new DiscordSocketClient(config);
		_commandService = new CommandService();
		_services = ConfigureServices();
		_command = new CommandHandler(_client, _commandService, _services);
		await _command.InstallCommandsAsync();
		_client.Log += Log;
		
		// collect credentials from environment
		string? token = Environment.GetEnvironmentVariable("TB_TOKEN");
		string? dbServer = Environment.GetEnvironmentVariable("TB_DB_SERVER");
		string? dbUser = Environment.GetEnvironmentVariable("TB_DB_USER");
		string? dbPass = Environment.GetEnvironmentVariable("TB_DB_PASS");
		string? dbName = Environment.GetEnvironmentVariable("TB_DB_NAME");
		// fail if missing
		if (token == null)
        {
			throw new Exception("TB_TOKEN unset");
        }
		if (dbUser == null)
		{
			throw new Exception("TB_DB_USER unset");
		}
		if (dbPass == null)
		{
			throw new Exception("TB_DB_PASS unset");
		}
		if (dbServer == null)
        {
			Console.WriteLine("TB_DB_SERVER unset, assuming localhost.");
			dbServer = "localhost";
        }
		if (dbName == null)
        {
			Console.WriteLine("TB_DB_NAME unset, assuming talkbox.");
			dbName = "talkbox";
		}

		var conStr = $"server={dbServer};uid={dbUser};pwd={dbPass};database={dbName}";
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

		await _client.LoginAsync(TokenType.Bot, token);
		await _client.StartAsync();
		await _client.SetGameAsync("tb$help");
			
		_client.Ready += () =>
		{
			Console.WriteLine("talkbox is now online.");
			return Task.CompletedTask;
		};
		await Task.Delay(-1);
	}

	private static IServiceProvider ConfigureServices()
	{
		var map = new ServiceCollection()
			.AddSingleton(new AudioService())
			.AddLavaNode();
		return map.BuildServiceProvider();
	}
	private static Task Log(LogMessage msg)
	{
		Console.WriteLine(msg.ToString());
		return Task.CompletedTask;
	}
}
