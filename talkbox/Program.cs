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
		
		string token = "";
		string passwd = "";
		string uname = "";
		try{
			token = File.ReadAllText("token.txt");
			passwd = File.ReadAllText("passwd");
			uname = File.ReadAllText("uname");
		}catch{
			token = File.ReadAllText("bin/Debug/net6.0/token.txt");
			passwd = File.ReadAllText("bin/Debug/net6.0/passwd");
			uname = File.ReadAllText("bin/Debug/net6.0/uname");
		}
		//var token = File.ReadAllText("token.txt");
		//var passwd = File.ReadAllText("passwd");
		//var uname = File.ReadAllText("uname");
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
