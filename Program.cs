using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using DiscordBot.Services;
using DiscordbotLogging.Log;
using InteractionHandlerService;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading.Tasks;
using static AlbionOnlineDataParser.AlbionOnlineDataParser;
using static System.Net.Mime.MediaTypeNames;

namespace FreeBeerBot
{
  public class Program
  {
    public static DiscordSocketClient _client;
    private DataBaseService dataBaseService;
    ulong GuildID = ulong.Parse(System.Configuration.ConfigurationManager.AppSettings.Get("guildID"));
    private bool EnableMusicCommands = bool.Parse(System.Configuration.ConfigurationManager.AppSettings.Get("enableMusic"));
    private bool RegisterCommandsAtReboot = bool.Parse(System.Configuration.ConfigurationManager.AppSettings.Get("registerCommands"));
    // Program entry point
    public static Task Main(string[] args) => new Program().MainAsync();

    public async Task MainAsync()
    {
      var config = new ConfigurationBuilder()
      // this will be used more later on
      .SetBasePath(AppContext.BaseDirectory)
      //.AddJsonFile("DiscordBot.dll.config")
      .AddJsonFile("appsettings.json")
      .Build();

      using IHost host = Host.CreateDefaultBuilder()
          .ConfigureServices((_, services) => services
      // Add the configuration to the registered services
      .AddSingleton(config)
      // Add the DiscordSocketClient, along with specifying the GatewayIntents and user caching
      .AddSingleton(x => new DiscordSocketClient(new DiscordSocketConfig
      {
        //GatewayIntents = GatewayIntents.All, //Toggle intents inside the Discord Developer portal to add more security.
        GatewayIntents = (GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent | GatewayIntents.GuildMembers) ^ GatewayIntents.GuildBans,
        AlwaysDownloadUsers = true,
        UseInteractionSnowflakeDate = false
      }))
      // Adding console logging
      .AddTransient<ConsoleLogger>()
      // Used for slash commands and their registration with Discord
      .AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>()))
      // Required to subscribe to the various client events used in conjunction with Interactions
      .AddSingleton<InteractionHandler>())
      .Build();

      InitializeAlbionAPIClient();
      InitializeAlbionDataProjectCurrentPrices();
      InitializeAlbion24HourDataMarketPricesHistory();
      InitializeAlbionData24DayAveragePrices();

      //dataBaseService = new DataBaseService();
      //await dataBaseService.AddSeedingData();

      await RunAsync(host);
    }

    public async Task RunAsync(IHost host)
    {
      using IServiceScope serviceScope = host.Services.CreateScope();
      IServiceProvider provider = serviceScope.ServiceProvider;

      var commands = provider.GetRequiredService<InteractionService>();
      _client = provider.GetRequiredService<DiscordSocketClient>();
      var config = provider.GetRequiredService<IConfigurationRoot>();

      await provider.GetRequiredService<InteractionHandler>().InitializeAsync();

      // Subscribe to client log events
      _client.Log += _ => provider.GetRequiredService<ConsoleLogger>().Log(_);
      // Subscribe to slash command log events
      commands.Log += _ => provider.GetRequiredService<ConsoleLogger>().Log(_);
      //_client.ThreadCreated += AuditThreadCreated;

      _client.Ready += async () =>
      {

        if (IsDebug())
        {

          var guild = _client.GetGuild(GuildID);

          //await _client.Rest.DeleteAllGlobalCommandsAsync(); //USE TO DELETE ALL GLOBAL COMMANDS
          //await guild.DeleteApplicationCommandsAsync(); //USE TO DELETE ALL GUILD COMMANDS
          //await commands.RegisterCommandsToGuildAsync(GuildID);
        }
        else
        {
          //If not debug, register commands globally
          //await commands.RegisterCommandsGloballyAsync(true);
        }

      };

      //await _client.LoginAsync(Discord.TokenType.Bot, config["discordBotToken"]);
      await _client.LoginAsync(TokenType.Bot, System.Configuration.ConfigurationManager.AppSettings.Get("discordBotToken"));
      await _client.StartAsync();
      await Task.Delay(-1);
    }

    static bool IsDebug()
    {
#if DEBUG
      return true;
#else
            return false;
#endif
    }
  }
}