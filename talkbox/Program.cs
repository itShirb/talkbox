using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Lavalink;
using Microsoft.Extensions.DependencyInjection;
using MySql.Data.MySqlClient;

namespace talkbox;

internal class Program
{
	public static Task Main(string[] args) => new Program().MainAsync();
	public const string defaultPrefix = "tb$";
	private DiscordClient client = null!;

	public static MySqlConnection sqlConnection = null!;
	public static Database db;
	
	private async Task MainAsync()
	{
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
		sqlConnection = new MySqlConnection();
		try
		{
			sqlConnection.ConnectionString = conStr;
			sqlConnection.Open();
			Console.WriteLine("Connected to database");
		}
		catch (MySqlException err)
		{
			Console.WriteLine(err);
		}
		db = new Database(sqlConnection);

		client = new DiscordClient(new DiscordConfiguration()
		{
			Token = token,
			TokenType = TokenType.Bot,
			Intents = DiscordIntents.AllUnprivileged
		});

		LavalinkExtension lava = client.UseLavalink();

		CommandsNextExtension commands = client.UseCommandsNext(new CommandsNextConfiguration()
        {
			StringPrefixes = new[] { defaultPrefix }
        });
		commands.RegisterCommands<AudioModule>();
		
		await client.ConnectAsync();
		await Task.Delay(-1);
	}
}
