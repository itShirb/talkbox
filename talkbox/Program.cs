using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using DSharpPlus.Lavalink;
using DSharpPlus.Net;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.EventArgs;
using Microsoft.Extensions.DependencyInjection;
using MySql.Data.MySqlClient;
using System.Diagnostics;

namespace talkbox;

internal class Program
{
    public static Task Main(string[] args) => new Program().MainAsync();
    public const string defaultPrefix = "tb$";
	private DiscordClient client = null!;

	public static MySqlConnection sqlConnection = null!;
	public static string gcpKey;

	private async Task MainAsync()
	{
		// collect credentials from environment
		string? token = Environment.GetEnvironmentVariable("TB_TOKEN");
		string? dbServer = Environment.GetEnvironmentVariable("TB_DB_SERVER");
		string? dbUser = Environment.GetEnvironmentVariable("TB_DB_USER");
		string? dbPass = Environment.GetEnvironmentVariable("TB_DB_PASS");
		string? dbName = Environment.GetEnvironmentVariable("TB_DB_NAME");
		string? _gcpKey = Environment.GetEnvironmentVariable("TB_GCP_KEY");
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
		if (_gcpKey == null)
		{
			throw new Exception("TB_GCP_KEY unset");
		}
		gcpKey = _gcpKey;

		var conStr = $"server={dbServer};uid={dbUser};pwd={dbPass};database={dbName};";
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
		Database.Connection = sqlConnection;

		client = new DiscordClient(new DiscordConfiguration()
		{
			Token = token,
			TokenType = TokenType.Bot,
			Intents = DiscordIntents.AllUnprivileged,
			MinimumLogLevel = Microsoft.Extensions.Logging.LogLevel.Debug
		});

		ConnectionEndpoint endpoint = new()
		{
			Hostname = "127.0.0.1", // From your server configuration.
			Port = 2333 // From your server configuration
		};

		LavalinkConfiguration lavaConf = new()
		{
			Password = "youshallnotpass", // From your server configuration.
			RestEndpoint = endpoint,
			SocketEndpoint = endpoint
		};

		LavalinkExtension lava = client.UseLavalink();

		CommandsNextExtension commands = client.UseCommandsNext(new CommandsNextConfiguration()
		{
			PrefixResolver = new PrefixResolverDelegate(async (DiscordMessage msg) =>
				CommandsNextUtilities.GetStringPrefixLength(msg, await Database.Guilds.GetPrefix((ulong)msg.Channel.GuildId))
			),
			EnableMentionPrefix = false,
			EnableDms = false,
		});
		commands.CommandErrored += Commands_CommandErrored;
		commands.RegisterCommands<AudioModule>();
		commands.RegisterCommands<AdminModule>();

		await client.ConnectAsync();
		await lava.ConnectAsync(lavaConf);
		await Task.Delay(-1);
	}

    private async Task Commands_CommandErrored(CommandsNextExtension sender, CommandErrorEventArgs e)
    {
		if (e.Exception.GetType() == typeof(ChecksFailedException))
        {
			await e.Context.RespondAsync("You don't have permission to use that command.");
        } else
        {
			
			await e.Context.RespondAsync("**EXC:** ```\n" + e.Exception.ToString() + "```");
		}
	}
}
