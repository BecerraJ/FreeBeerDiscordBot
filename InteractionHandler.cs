using Aspose.Words.Fields;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using GoogleSheetsData;
using PlayerData;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using CommandModule;
using System.Linq;
using static System.Net.WebRequestMethods;
using Aspose.Imaging.AsyncTask;
using Aspose.Imaging.ProgressManagement;
using DiscordBot;
using DNet_V3_Tutorial;
using DiscordBot.Services;

namespace InteractionHandlerService
{
  public class InteractionHandler
  {
    private readonly DiscordSocketClient _client;
    private readonly InteractionService _commands;
    private readonly IServiceProvider _services;
    private ulong HQMiniMarketChannelID = ulong.Parse(System.Configuration.ConfigurationManager.AppSettings.Get("HQMiniMarketChannelID"));
    private ulong LootSplitChannelID = ulong.Parse(System.Configuration.ConfigurationManager.AppSettings.Get("LootSplitChannelID"));
    private ulong FreeBeerGuildID = ulong.Parse(System.Configuration.ConfigurationManager.AppSettings.Get("FreeBeerDiscordGuildID"));
    private ulong AllianceGuildID = ulong.Parse(System.Configuration.ConfigurationManager.AppSettings.Get("AllianceGuildID"));
    private ulong ChannelThreadId { get; set; }
    // Using constructor injection
    public InteractionHandler(DiscordSocketClient client, InteractionService commands, IServiceProvider services)
    {
      _client = client;
      _commands = commands;
      _services = services;
    }

    public async Task InitializeAsync()
    {
      // Add the public modules that inherit InteractionModuleBase<T> to the InteractionService
      await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);

      // Process the InteractionCreated payloads to execute Interactions commands
      _client.Ready += ClientReady;
      _client.InteractionCreated += HandleInteraction;
      _client.ThreadCreated += ThreadCreationExecuted;
      _client.UserJoined += UserJoinedGuildExecuted;
      _client.UserLeft += UserLeftGuildExecuted;
      _client.ButtonExecuted += ButtonExecuted;
      _client.ModalSubmitted += ModalSubmittedExecuted;
      _client.SelectMenuExecuted += MenuHandler;



      // Process the command execution results 
      _commands.SlashCommandExecuted += SlashCommandExecuted;
      _commands.ContextCommandExecuted += ContextCommandExecuted;
      _commands.ComponentCommandExecuted += ComponentCommandExecuted;
      _commands.ModalCommandExecuted += ModalCommandExecuted;


    }
    private Task ClientReady()
    {
      return Task.CompletedTask;
    }

    private async Task HandleInteraction(SocketInteraction arg)
    {
      try
      {
        // Create an execution context that matches the generic type parameter of your InteractionModuleBase<T> modules
        var ctx = new SocketInteractionContext(_client, arg);
        await _commands.ExecuteCommandAsync(ctx, _services);
      }
      catch (Exception ex)
      {
        Console.WriteLine(ex);

        // If a Slash Command execution fails it is most likely that the original interaction acknowledgement will persist. It is a good idea to delete the original
        // response, or at least let the user know that something went wrong during the command execution.
        if (arg.Type == InteractionType.ApplicationCommand)
          await arg.GetOriginalResponseAsync().ContinueWith(async (msg) => await msg.Result.DeleteAsync());
      }
    }
    private async Task ThreadCreationExecuted(SocketThreadChannel arg)
    {
      string? sUserNickname = (arg.Owner.DisplayName != null) ? arg.Owner.DisplayName : arg.Owner.Username;

      if (sUserNickname.Contains("!sl"))
      {
        sUserNickname = new PlayerDataLookUps().CleanUpShotCallerName(sUserNickname);
      }

      if (arg.ParentChannel.Id == HQMiniMarketChannelID && ChannelThreadId != arg.Owner.Thread.Id)
      {
        ChannelThreadId = arg.Owner.Thread.Id;
        string miniMarketCreditsTotal = GoogleSheetsDataWriter.GetMiniMarketCredits(sUserNickname);
        await arg.SendMessageAsync($"{sUserNickname} Mini market credits balance: {miniMarketCreditsTotal}");
      }
      else if (arg.ParentChannel.Id == LootSplitChannelID && ChannelThreadId != arg.Owner.Thread.Id)
      {
        ChannelThreadId = arg.Owner.Thread.Id;
        await arg.SendMessageAsync($"Don't forget to post info first before you run /Split-Loot");
      }
    }

    private async Task UserJoinedGuildExecuted(SocketGuildUser SocketGuildUser)
    {
      if (SocketGuildUser.Guild.Id == FreeBeerGuildID)
      {
        var lobbyChannel = _client.GetChannel(ulong.Parse(System.Configuration.ConfigurationManager.AppSettings.Get("LobbyChannelID"))) as IMessageChannel;

        Random rnd = new Random();

        List<string> insultList = new List<string>
        {
          $"<@{SocketGuildUser.Id}> Welcome to Free Beer.",
          //$"Sorry <@{SocketGuildUser.Id}>, if your here for the free beer we're fresh out.",
          //$"Hi <@{SocketGuildUser.Id}>! If your looking to spy on us, please submit an app in <#880611577236164628> You have 48 hours or you getting kicked",
          ////$"<@{SocketGuildUser.Id}> Dominoes pizza, you spank it, we bank it.",
          ////$"<@{SocketGuildUser.Id}> Welcome to Free Beer!",
          //$"Hello <@{SocketGuildUser.Id}>. But just in case your here to talk shit. :middle_finger:",
          //$"<@{ SocketGuildUser.Id}>. What's up homie? You have 48 hours to apply or ya getting kicked. Please see <#880611577236164628>",
          //$"<@{SocketGuildUser.Id}>. Welcome. Do you ever feel like a plastic bag?",
          //$"<@{ SocketGuildUser.Id}>. Hi <@{SocketGuildUser.Id}>! Welcome to free beer, in an attempt to keep rats out of our channel, you have 48 hours to apply. Please see <#880611577236164628>"
        };

        int r = rnd.Next(insultList.Count);
        await lobbyChannel.SendMessageAsync((string)insultList[r]);
      }
      else if (SocketGuildUser.Guild.Id == AllianceGuildID)
      {
        var lobbyChannel = _client.GetChannel(1128713631132033154) as IMessageChannel;
        await lobbyChannel.SendMessageAsync($"Welcome to PPFU Alliance server <@{SocketGuildUser.Id}>");
      }
    }

    private async Task UserLeftGuildExecuted(SocketGuild SocketGuild, SocketUser SocketUser)
    {

      if (SocketGuild.Id == FreeBeerGuildID)
      {
        var lobbyChannel = _client.GetChannel(ulong.Parse(System.Configuration.ConfigurationManager.AppSettings.Get("LobbyChannelID"))) as IMessageChannel;

        SocketGuildUser user = (SocketGuildUser)SocketUser;

        if (!SocketUser.IsBot && await new DataBaseService().CheckPlayerIsExist(SocketUser.Username))
        {
          //await CommandModule.CommandModule.Unregister(user.Username.ToString(), user);
          //await new DataBaseService().unre
        }
        Random probability = new Random();
        Random gifRandom = new Random();

        List<string> GoodByeList = new List<string>
        {
          $"<@{SocketUser.Id}> / {SocketUser.Username} has left the server",
          //$"<@{SocketUser.Id}> / {SocketUser.Username} has left probably because they're sick of us",
          //$"<@{SocketUser.Id}> / {SocketUser.Username} has left. https://tenor.com/view/ok-bye-ok-bye-bye-ok-girl-bye-gif-18696870",
          //$"<@{SocketUser.Id}> / {SocketUser.Username} has left. https://tenor.com/view/peace-out-later-bye-gif-14086405",
          //$"<@{SocketUser.Id}> / {SocketUser.Username} has left. https://tenor.com/view/bye-slide-baby-later-peace-out-gif-19322436",
          //$"<@{SocketUser.Id}> / {SocketUser.Username} has left. https://tenor.com/view/chris-tucker-bye-bish-gif-13500768",
          //$"<@{SocketUser.Id}> / {SocketUser.Username} has left. https://tenor.com/view/i-wont-miss-them-at-all-i-wont-miss-them-matthew-rhys-i-dont-care-about-them-i-dont-care-gif-12663265",
          //$"<@{SocketUser.Id}> / {SocketUser.Username} has left. https://tenor.com/view/see-ya-kick-woman-gif-11295867",
          //$"<@{SocketUser.Id}> / {SocketUser.Username} has left. https://tenor.com/view/chris-tucker-bye-bish-gif-13500768",
          //$"<@{SocketUser.Id}> / {SocketUser.Username} has left. https://tenor.com/view/bye-tata-ok-by-gif-gif-18973858",
          //$"<@{SocketUser.Id}> / {SocketUser.Username} has left. https://tenor.com/view/rip-gif-19364920",
          //$"<@{SocketUser.Id}> / {SocketUser.Username} has left. https://tenor.com/view/rip-rest-in-peace-rip-bozo-pour-one-out-homie-gif-22783396",
          //$"<@{SocketUser.Id}> / {SocketUser.Username} has left. https://tenor.com/view/bye-tata-ok-by-gif-gif-18973858",
          //$"<@{SocketUser.Id}> / {SocketUser.Username} rage quitted the server",
          //$"<@{SocketUser.Id}> / {SocketUser.Username} left to avoid getting shit from us in that last fight.",

        };

        if (probability.NextDouble() < 0.3)
        {
          int r = gifRandom.Next(GoodByeList.Count);
          await lobbyChannel.SendMessageAsync((string)GoodByeList[r]);
        }
        else
        {
          await lobbyChannel.SendMessageAsync($"<@{SocketUser.Id}> / {SocketUser.Username} has left the server");
        }
      }
      //else if (SocketGuild.Id == AllianceGuildID)
      //{
      //  var lobbyChannel = _client.GetChannel(1128713631132033154) as IMessageChannel;
      //  await lobbyChannel.SendMessageAsync($"Has left the server <@{SocketUser.GlobalName}>");
      //}
    }
    private Task MenuHandler(SocketMessageComponent arg)
    {
      return Task.CompletedTask;
    }
    private Task ModalSubmittedExecuted(SocketModal a_Modal)
    {
      if(a_Modal.Data.CustomId == "update_config_settings")
      {
        List<SocketMessageComponentData> components = a_Modal.Data.Components.ToList();
        string UpdatedSettingValue = components.FirstOrDefault().Value;
        HelperMethods.WriteToJson(components.FirstOrDefault().CustomId, UpdatedSettingValue.ToString());

        a_Modal.UpdateAsync(x =>
        {
          PingModule.CreateConfiguationEmbed();

          x.Embed = PingModule.Embed.Build();
          x.Components = PingModule.Componets.Build();
        });
      }
      return Task.CompletedTask;
    }

    private Task SlashCommandExecuted(SlashCommandInfo arg1, Discord.IInteractionContext arg2, IResult arg3)
    {
      return Task.CompletedTask;
    }

    private Task ContextCommandExecuted(ContextCommandInfo arg1, Discord.IInteractionContext arg2, IResult arg3)
    {
      return Task.CompletedTask;
    }

    private Task ComponentCommandExecuted(ComponentCommandInfo arg1, Discord.IInteractionContext arg2, IResult arg3)
    {
      return Task.CompletedTask;
    }

    private async Task<Task> ButtonExecuted(SocketMessageComponent MessageComponet)
    {
      return Task.CompletedTask;
    }

    private Task ModalCommandExecuted(ModalCommandInfo arg1, Discord.IInteractionContext arg2, IResult arg3)
    {
      return Task.CompletedTask;
    }


  }
}
