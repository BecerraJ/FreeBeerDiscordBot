using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using DiscordBot.Enums;
using DiscordBot.LootSplitModule;
using DiscordBot.Models;
using DiscordBot.RegearModule;
using DiscordBot.Services;
using DiscordbotLogging.Log;
using FreeBeerBot;
using GoogleSheetsData;
using MarketData;
using Newtonsoft.Json.Linq;
using PlayerData;
using SharpLink;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DiscordBot;
using System.Globalization;
using DNet_V3_Tutorial;

namespace CommandModule
{
  public class CommandModule : InteractionModuleBase<SocketInteractionContext>
  {
    public InteractionService Commands { get; set; }
    private PlayerDataHandler.Rootobject PlayerEventData { get; set; }

    private ulong GuildID = ulong.Parse(System.Configuration.ConfigurationManager.AppSettings.Get("FreeBeerDiscordGuildID"));
    private static Logger _logger;
    private DataBaseService dataBaseService;
    private static LootSplitModule lootSplitModule;

    private int iRegearLimit = int.Parse(System.Configuration.ConfigurationManager.AppSettings.Get("RegearSubmissionCap"));
    private bool bAutomatedRegearProcessing = bool.Parse(System.Configuration.ConfigurationManager.AppSettings.Get("AutomaticRegearProcessing"));
    string AllianceGuildGuidID = System.Configuration.ConfigurationManager.AppSettings.Get("FreeBeerAllianceAPIID");

    private ulong RecruitersModChannelID = ulong.Parse(System.Configuration.ConfigurationManager.AppSettings.Get("RecruitersModChannel"));

    private ulong RecruitersRoleID = ulong.Parse(System.Configuration.ConfigurationManager.AppSettings.Get("RecruitersRoleID"));
    private ulong ManagementRoleID = ulong.Parse(System.Configuration.ConfigurationManager.AppSettings.Get("ManagementRoleID"));
    private ulong OfficerRoleID = ulong.Parse(System.Configuration.ConfigurationManager.AppSettings.Get("OfficerRoleID"));
    private ulong VeteranRoleID = ulong.Parse(System.Configuration.ConfigurationManager.AppSettings.Get("veteran"));
    private ulong MemberRoleID = ulong.Parse(System.Configuration.ConfigurationManager.AppSettings.Get("member"));
    private ulong NewRecruitRoleID = ulong.Parse(System.Configuration.ConfigurationManager.AppSettings.Get("newRecruit"));

    private ulong GoldTierRegearID = ulong.Parse(System.Configuration.ConfigurationManager.AppSettings.Get("GoldTierRegear"));
    private ulong SilverTierRegearID = ulong.Parse(System.Configuration.ConfigurationManager.AppSettings.Get("SilverTierRegear"));
    private ulong BronzeTierRegearID = ulong.Parse(System.Configuration.ConfigurationManager.AppSettings.Get("BronzeTierRegear"));
    private ulong FreeTierRegearID = ulong.Parse(System.Configuration.ConfigurationManager.AppSettings.Get("FreeTierRegear"));
    private ulong ApplicantRoleID = 335894631810334720;

    private int iMiniMartAccountCap = int.Parse(System.Configuration.ConfigurationManager.AppSettings.Get("MiniMartAccountCap"));
    private string FreeBeerAPIGuildID = System.Configuration.ConfigurationManager.AppSettings.Get("FreeBeerGuildAPIID");

    private int iRecruitmentKillFame = int.Parse(System.Configuration.ConfigurationManager.AppSettings.Get("RecruitmentKillFame"));

    public IEnumerable<IMessage> msgs { get; set; }
    public CommandModule(ConsoleLogger logger)
    {
      _logger = logger;
    }

    [SlashCommand("get-player-info", "Search for Player Info")]
    public async Task GetBasicPlayerInfo(string a_sPlayerName)
    {
      PlayerLookupInfo? playerInfo = new PlayerLookupInfo();
      PlayerDataLookUps? albionData = new PlayerDataLookUps();

      playerInfo = await albionData.GetPlayerInfo(Context, a_sPlayerName);
      var detailedplayerinfo = await albionData.GetDetailedAlbionPlayerInfo(playerInfo.Id);


      await _logger.Log(new LogMessage(LogSeverity.Info, "Get Player Info", $"User: {Context.User.Username} has used command, Command: Get-Player-Info", null));

      try
      {

        var embed = new EmbedBuilder()
      .WithTitle($"Player Search Results")
      .AddField("Player Name", (detailedplayerinfo.Name == null) ? "No info" : detailedplayerinfo.Name, true)
      .AddField("Kill Fame", (detailedplayerinfo.KillFame == 0) ? 0 : detailedplayerinfo.KillFame)
      .AddField("Guild Name ", (detailedplayerinfo.GuildName == null || detailedplayerinfo.GuildName == "") ? "No info" : detailedplayerinfo.GuildName, true)
      .AddField("Alliance Name", (detailedplayerinfo.AllianceName == null || detailedplayerinfo.AllianceName == "") ? "No info" : detailedplayerinfo.AllianceName, true)
      .AddField("Death Fame: ", (detailedplayerinfo.DeathFame == 0) ? 0 : detailedplayerinfo.DeathFame, true)
      .AddField("Crafting Fame: ", (detailedplayerinfo.LifetimeStatistics.Crafting.Total == 0) ? 0 : detailedplayerinfo.LifetimeStatistics.Crafting.Total, true)
      .AddField("Gathering Fame: ", (detailedplayerinfo.LifetimeStatistics.Gathering.All.Total == 0) ? 0 : detailedplayerinfo.LifetimeStatistics.Gathering.All.Total, true)
      .AddField("PVE Fame: ", (detailedplayerinfo.LifetimeStatistics.PvE.Total == 0) ? 0 : detailedplayerinfo.LifetimeStatistics.PvE.Total, true)
      .AddField("Sigma Info", $"https://app.sigmacomputing.com/embed/2Fb3n6osB7MZ0psRKGqR6?name={a_sPlayerName}")
      .AddField("AlbionDB Info", $"https://albiondb.net/player/{a_sPlayerName}");

        await RespondAsync(null, null, false, true, null, null, null, embed.Build());


      }
      catch (Exception ex)
      {
        await RespondAsync("Player info not found", null, false, true);
      }
    }

    [SlashCommand("register", "Register player to Free Beer guild")]
    public async Task Register(SocketGuildUser guildUserName, string ingameName)
    {
      await DeferAsync(true);
      PlayerDataHandler playerDataHandler = new PlayerDataHandler();
      PlayerLookupInfo playerInfo = new PlayerLookupInfo();
      PlayerDataLookUps albionData = new PlayerDataLookUps();
      DataBaseService dataBaseService = new DataBaseService();
      IMessageChannel RecruitersModChannel = Context.Client.GetChannel(RecruitersModChannelID) as IMessageChannel;

      string? sUserNickname = (guildUserName.DisplayName == null) ? guildUserName.Username : guildUserName.DisplayName;
      var freeBeerMainChannel = Context.Client.GetChannel(739949855195267174) as IMessageChannel;
      var newMemberRole = guildUserName.Guild.GetRole(NewRecruitRoleID);
      var freeRegearRole = guildUserName.Guild.GetRole(FreeTierRegearID);//free regear role id
      var user = guildUserName.Guild.GetUser(guildUserName.Id);

      if (!await dataBaseService.CheckPlayerIsExist(sUserNickname))
      {

        if (user.Nickname == null)
        {
          try
          {
            await user.ModifyAsync(x => x.Nickname = ingameName);
          }
          catch (Exception ex)
          {
            await FollowupAsync($"{ingameName} Error: Unable to udpate player server name. Run command again or register them by hand. <@{OfficerRoleID}> This person may need to be added to the spreadsheets", null, false, true);
            await _logger.Log(new LogMessage(LogSeverity.Info, "Register Member", $"User: {Context.User.Username} nickname couldn't be updated", null));
          }
        }

        playerInfo = await albionData.GetPlayerInfo(Context, sUserNickname);

        if (sUserNickname.ToLower() == playerInfo.Name.ToLower() && playerInfo != null)
        {
          await dataBaseService.AddPlayerInfo(new DiscordBot.Models.Player
          {
            PlayerId = playerInfo.Id,
            PlayerName = playerInfo.Name
          });

          await user.AddRoleAsync(newMemberRole);
          //await user.AddRoleAsync(freeRegearRole);

          await _logger.Log(new LogMessage(LogSeverity.Info, "Register Member", $"User: {Context.User.Username} has registered {playerInfo.Name}, Command: register", null));

          await GoogleSheetsDataWriter.RegisterUserToDataRoster(playerInfo.Name.ToString(), ingameName, null, null, null);
          await GoogleSheetsDataWriter.RegisterUserToRegearSheets(guildUserName, ingameName, null, null, null);

          var embed = new EmbedBuilder()
         .WithTitle($":beers: WELCOME TO FREE BEER :beers:")
         .WithDescription("We're glad to have you. Please read the following below.")
         .AddField($"Rules", "<#1261875306575298620>");

          List<string> questionList = new List<string>
                {
                    $"food",
                    $"weapon in Albion",
                    $"drink",
                    $"hobby",
                    $"car",
                    $"movie",
                    $"video game (Albion doesn't count)",
                    $"book",
                    $"boardgame",
                    $"TV show",
                    $"Sports team"
                };
          Random rnd = new Random();
          int r = rnd.Next(questionList.Count);
          await freeBeerMainChannel.SendMessageAsync($"<@{Context.Guild.GetUser(guildUserName.Id).Id}> Make to sure read the info below but.... We want to get to know you! Tell us... What's your favorite {(string)questionList[r]}?", false, embed.Build());

          await FollowupAsync($"{ingameName} was registered", null, false, true);

          
          await RecruitersModChannel.SendMessageAsync($"{ingameName} has been manually registered by {(Context.User as SocketGuildUser).Nickname} ");
        }
        else
        {
          await RespondAsync($"Player not found. The users discord may not match the ingame name. Please try again", null, false, true);
        }
      }
      else
      {
        await FollowupAsync($"{ingameName} has been already registered. If this isn't correct let an Officer know.", null, false, true);
      }

    }
    [SlashCommand("unregister-member", "Remove player from database and perms from discord")]
    public async Task Unregister(string InGameName, SocketGuildUser? DiscordUser = null)
    {
      PlayerDataLookUps albionData = new PlayerDataLookUps();
      PlayerLookupInfo playerInfo = new PlayerLookupInfo();
      DataBaseService dataBaseService = new DataBaseService();

      await DeferAsync(true);

      playerInfo = await albionData.GetPlayerInfo(Context, InGameName);

      if (playerInfo != null || DiscordUser != null)
      {
        try
        {
          await GoogleSheetsDataWriter.UnResgisterUserFromDataSources(InGameName, DiscordUser);
          dataBaseService.DeletePlayer(InGameName);
        }
        catch
        {
          await FollowupAsync($"Issue unregistering {InGameName}. Please notify Officer/Admin", ephemeral: true);
        }


        if (DiscordUser != null)
        {
          foreach (var roles in DiscordUser.Roles)
          {
            if (roles.Name != "@everyone")
            {
              await DiscordUser.RemoveRoleAsync(roles.Id);
            }
          }
        }

        await _logger.Log(new LogMessage(LogSeverity.Info, "Unregister ", $"User: {Context.User.Username} has used command Unregister", null));

        IMessageChannel RecruitersModChannel = Context.Client.GetChannel(RecruitersModChannelID) as IMessageChannel;
        await RecruitersModChannel.SendMessageAsync($"{InGameName} has been unregistered by {(Context.User as SocketGuildUser).Nickname} ");

        await FollowupAsync($"{InGameName} was unregistered", ephemeral: true);
      }
      else
      {
        await FollowupAsync("Member was not found while trying to unregister", null, false, true);
      }
    }

    [SlashCommand("view-roster", "View members that are reigstered to the guild")]
    public async Task ViewGuildRoster()
    {
      DataBaseService DataBaseService= new DataBaseService();

      await DeferAsync(true);

      List<Player> GuildRosterList = DataBaseService.GetRegisteredPlayerRoster();
      List<string> formattedPlayerList = new List<string>();

      foreach (Player player in GuildRosterList)
      {
        //embed.AddField("Name", player.PlayerName);
        formattedPlayerList.Add(player.PlayerName);
      }

      PingModule.WriteToCSV(formattedPlayerList);

      await FollowupWithFileAsync(@".\Files\PurgeList.csv", "PlayerRoster.csv", "Roster Generated");
    }

    [SlashCommand("give-regear", "Assign users regear roles")]
    public async Task GiveRegear(string GuildUserNames, DateTime SelectedDate, RegearTiers RegearTier)
    {
      ulong memberRoleID = 0;
      await DeferAsync(true);
      List<char> charsToRemove = new List<char>() { '@', '<', '>' };
      foreach (char c in charsToRemove)
      {
        GuildUserNames = GuildUserNames.Replace(c.ToString(), String.Empty);
      }

      List<string> CommandUsers = GuildUserNames.Split(',').ToList();

      List<SocketGuildUser> SocketUsersList = new List<SocketGuildUser>();

      foreach (var user in CommandUsers)
      {
        var userConvert = Convert.ToUInt64(user);
        SocketUsersList.Add(Context.Guild.GetUser(userConvert));
      }

      await GoogleSheetsDataWriter.UpdateRegearRole(SocketUsersList, SelectedDate, RegearTier);

      switch (RegearTier)
      {
        case RegearTiers.Bronze:
          memberRoleID = ulong.Parse(System.Configuration.ConfigurationManager.AppSettings.Get("BronzeTierRegear"));
          break;

        case RegearTiers.Silver:
          memberRoleID = ulong.Parse(System.Configuration.ConfigurationManager.AppSettings.Get("SilverTierRegear"));
          break;

        case RegearTiers.Gold:
          memberRoleID = ulong.Parse(System.Configuration.ConfigurationManager.AppSettings.Get("GoldTierRegear"));
          break;

      }

      foreach (var user in SocketUsersList)
      {
        if (!user.Roles.Any(r => r.Id == memberRoleID))
        {
          await user.AddRoleAsync(memberRoleID);
        }
      }

      await FollowupAsync("Members regear roles updated.", null, false, true);
    }


    [SlashCommand("recent-deaths", "View recent deaths")]
    public async Task GetRecentDeaths()
    {
      var testuser = Context.User.Id;
      string? sPlayerData = null;
      var sPlayerAlbionId = new PlayerDataLookUps().GetPlayerInfo(Context, null);
      string? sUserNickname = ((Context.User as SocketGuildUser).DisplayName != null) ? (Context.User as SocketGuildUser).DisplayName : (Context.User as SocketGuildUser).Nickname;

      await _logger.Log(new LogMessage(LogSeverity.Info, "Recent-Deaths ", $"User: {Context.User.Username} has used command, Command: Recent-Deaths", null));

      int iDeathDisplayCounter = 1;
      int iVisibleDeathsShown = Int32.Parse(System.Configuration.ConfigurationManager.AppSettings.Get("showDeathsQuantity")) - 1;  //can add up to 10 deaths. Adjust in config

      if (sPlayerAlbionId.Result != null)
      {
        using (HttpResponseMessage response = await AlbionOnlineDataParser.AlbionOnlineDataParser.ApiClient.GetAsync($"players/{sPlayerAlbionId.Result.Id}/deaths"))
        {
          if (response.IsSuccessStatusCode)
          {
            sPlayerData = await response.Content.ReadAsStringAsync();
            var parsedObjects = JArray.Parse(sPlayerData);
            var component = new ComponentBuilder();
            var searchDeaths = parsedObjects.Children<JObject>()
            .Select(jo => (int)jo["EventId"])
            .ToList();

            var embed = new EmbedBuilder()
            .WithTitle("Recent Deaths")
            .WithColor(new Color(238, 62, 75));


            var regearbutton = new ButtonBuilder()
            {
              Style = ButtonStyle.Secondary
            };


            for (int i = 0; i < searchDeaths.Count; i++)
            {
              if (i <= iVisibleDeathsShown)
              {
                embed.AddField($"Death {iDeathDisplayCounter} : KILL ID - {searchDeaths[i]}", $"https://albiononline.com/en/killboard/kill/{searchDeaths[i]}", false);

                regearbutton.Label = $"Regear Death {iDeathDisplayCounter}"; //QOL Update. Allows members to start the regear process straight from the recent deaths list
                regearbutton.CustomId = searchDeaths[i].ToString();
                component.WithButton(regearbutton);

                iDeathDisplayCounter++;
              }
            }
            //await RespondAsync(null, null, false, true, null, null, component.Build(), embed.Build()); //Enable once buttons are working
            await RespondAsync(null, null, false, true, null, null, null, embed.Build());
          }
          else
          {
            throw new Exception(response.ReasonPhrase);
          }
        }
      }
      else
      {
        await RespondAsync("Hey idiot. Does your discord nickname match your in-game name?");
      }
    }

    [SlashCommand("fetchprice", "Testing market item finder")]
    public async Task FetchMarketPrice(PricingOptions PriceOption, string ItemCode, ItemQuality ItemQuality, AlbionCitiesEnum? MarketLocation = null)
    {
      Task<List<EquipmentMarketData>> CurrentPriceMarketData = null;
      Task<List<AverageItemPrice>> DailyAverageMarketData = null;
      Task<List<EquipmentMarketDataMonthylyAverage>> MonthlyAverageMarketData = null;

      string combinedInfo = "";
      string SelectedMarkets = MarketLocation.ToString();

      await _logger.Log(new LogMessage(LogSeverity.Info, "Price Check", $"User: {Context.User.Username}, Command: Price check", null));


      if (MarketLocation == null)
      {
        SelectedMarkets = "Bridgewatch,BridgewatchPortal,Caerleon,FortSterling,FortSterlingPortal,Lymhurst,LymhurstPortal,Martlock,Martlockportal,Thetford,ThetfordPortal";
      }

      switch (PriceOption)
      {
        case PricingOptions.CurrentPrice:
          combinedInfo = $"{ItemCode}?qualities={(int)ItemQuality}&locations={SelectedMarkets}";
          CurrentPriceMarketData = new MarketDataFetching().GetMarketPriceCurrentAsync(combinedInfo);

          await RespondAsync($"The cheapest {PriceOption} of your {ItemQuality} item found in {CurrentPriceMarketData.Result.FirstOrDefault().city.ToString()} cost: " + CurrentPriceMarketData.Result.FirstOrDefault().sell_price_min.ToString("N0"), null, false, true);

          break;

        case PricingOptions.DayAverage:

          combinedInfo = $"{ItemCode}?qualities={(int)ItemQuality}&locations={SelectedMarkets}";
          DailyAverageMarketData = new MarketDataFetching().GetMarketPriceDailyAverage(combinedInfo);
          await RespondAsync($"The cheapest {PriceOption} of your {ItemQuality} item found in {DailyAverageMarketData.Result.FirstOrDefault().location.ToString()} cost: " + DailyAverageMarketData.Result.FirstOrDefault().data.FirstOrDefault().avg_price.ToString("N0"), null, false, true);

          break;

        case PricingOptions.MonthlyAverage:
          combinedInfo = $"{ItemCode}?qualities={(int)ItemQuality}&locations={SelectedMarkets}";
          MonthlyAverageMarketData = new MarketDataFetching().GetMarketPriceMonthlyAverage(combinedInfo);

          await RespondAsync($"The cheapest {PriceOption} of your {ItemQuality} item found in {MonthlyAverageMarketData.Result.FirstOrDefault().location.ToString()} cost: " + MonthlyAverageMarketData.Result.FirstOrDefault().data.FirstOrDefault().avg_price.ToString("N0"), null, false, true);
          break;

        default:
          await RespondAsync($"<@{Context.User.Id}> Option doesn't exist", null, false, true);
          break;
      }

    }

    [SlashCommand("blacklist", "Put a player on the shit list")]
    public async Task BlacklistPlayer(SocketGuildUser a_DiscordUsername, string IngameName = null, string Reason = null, string Fine = null, string AdditionalNotes = null)
    {
      await DeferAsync();

      string? sDiscordNickname = IngameName;
      string? AlbionInGameName = IngameName;
      string? sReason = Reason;
      string? sFine = Fine;
      string? sNotes = AdditionalNotes;

      Console.WriteLine("Dickhead " + a_DiscordUsername + " has been blacklisted");

      await GoogleSheetsDataWriter.WriteToFreeBeerRosterDatabase(a_DiscordUsername.ToString(), sDiscordNickname, sReason, sFine, sNotes);
      await FollowupAsync(a_DiscordUsername.ToString() + " has been blacklisted. <:kekw:1069714675081687200> ", null, false, false);
    }

    [SlashCommand("view-paychex", "Views your current paychex amount")]
    public async Task GetCurrentPaychexAmount()
    {
      await _logger.Log(new LogMessage(LogSeverity.Info, "View-Paychex", $"User: {Context.User.Username}, Command: view-paychex", null));
      string? sUserNickname = ((Context.User as SocketGuildUser).DisplayName != null) ? new PlayerDataLookUps().CleanUpShotCallerName((Context.User as SocketGuildUser).DisplayName) : (Context.User as SocketGuildUser).Nickname;
      var component = new ComponentBuilder();
      var paychexbutton = new ButtonBuilder();
      DataBaseService dataBaseService = new DataBaseService();
      
      if(await dataBaseService.CheckPlayerIsExist(sUserNickname))
      {
        await DeferAsync(true);
        List<string> paychexRunningTotal = GoogleSheetsDataWriter.GetRunningPaychexTotal(sUserNickname);
        Dictionary<string, string> paychexTotals = GoogleSheetsDataWriter.GetPaychexTotals(sUserNickname);

        string miniMarketCreditsTotal = GoogleSheetsDataWriter.GetMiniMarketCredits(sUserNickname) ?? "0";
        //string regearStatus = GoogleSheetsDataWriter.GetRegearStatus(sUserNickname); DISABLED
        string PaychexDate = "";
        //List<string> paychexSheets = GoogleSheetsDataWriter.GetPaychexSheets();
        var embed = new EmbedBuilder();
        int iGrandTotalPaychex = 0;

        embed.WithTitle($":moneybag: Your Free Beer Paychex Info :moneybag: ");

        //string biweeklyLastSundayDate = $"{GoogleSheetHelperMethods.StartOfWeek(DateTime.Today.AddDays(-7), DayOfWeek.Sunday).ToShortMonthName()}-{GoogleSheetHelperMethods.StartOfWeek(DateTime.Today.AddDays(-7), DayOfWeek.Sunday).Day}";

        //if (!paychexSheets.Any(s => s.Contains(biweeklyLastSundayDate)))
        //{
        //  embed.AddField("Last weeks estimated paychex:", $"${paychexRunningTotal[0]:n0}");
        //}

        embed.AddField("Current week running total:", $"${paychexRunningTotal[1]:n0}");
        embed.AddField("Mini-mart Credits balance:", $"{miniMarketCreditsTotal}");
        //embed.AddField("Regear Status:", $"{regearStatus}"); DISABLED

        if (paychexRunningTotal.Count > 0)
        {
          foreach (var entries in paychexTotals)
          {
            embed.AddField(entries.Key, $"{entries.Value:n0}");

            if (entries.Key.Length > 0)
            {
              int iter = entries.Key.IndexOf(" ") + 1;
              PaychexDate = entries.Key.Substring(0, iter);
            }

            if (entries.Key.Contains("NOT CLAIMED"))
            {
              paychexbutton.Style = ButtonStyle.Success;
              paychexbutton.IsDisabled = false;
              paychexbutton.Label = $"Transfer {PaychexDate.Split("(")[0]}";
              paychexbutton.CustomId = $"paychex:{PaychexDate.Split("(")[0].Trim()}:{sUserNickname}";

              iGrandTotalPaychex += Convert.ToInt32(entries.Value.Replace(",", ""));
            }
            else if (entries.Key.Contains("(CLAIMED)"))
            {
              paychexbutton.Style = ButtonStyle.Danger;
              paychexbutton.IsDisabled = true;
              paychexbutton.Label = $"Claimed {PaychexDate.Split("(")[0]}";
              paychexbutton.CustomId = $"Paychex{entries.Key}";
            }
            else
            {
              paychexbutton.Label = $"Pending {PaychexDate}";
              paychexbutton.CustomId = $"Paychex{entries.Key}";
              paychexbutton.Style = ButtonStyle.Secondary;
              paychexbutton.IsDisabled = true;
            }

            if (int.Parse(miniMarketCreditsTotal, NumberStyles.Currency) > iMiniMartAccountCap)
            {
              paychexbutton.Style = ButtonStyle.Danger;
              paychexbutton.IsDisabled = true;
              paychexbutton.Label = $"Credits Exceed Cap {PaychexDate.Split("(")[0]}";
              paychexbutton.CustomId = $"Paychex{entries.Key}";
            }
            component.WithButton(paychexbutton);
          }

          if(iGrandTotalPaychex > 0)
          {
            embed.AddField("Total Paychex total owed to you:", $"${iGrandTotalPaychex:n0}");
          }

          await FollowupAsync(null, null, false, true, null, null, component.Build(), embed.Build());

        }
        else
        {
          await RespondAsync("Sorry you don't seem to be registed. Ask for a @AO - REGEARS officer to get you squared away.", null, false, true);
        }
      }
      else
      {
        await RespondAsync("Looks like your not fully registered to the guild. Please contact a recruiter or officer to fix the issue.", null, false, true);
      }
    }

    [ComponentInteraction("paychex*")]
    public async Task TransferPaychexButton()
    {
      string? sUserNickname = ((Context.User as SocketGuildUser).DisplayName != null) ? new PlayerDataLookUps().CleanUpShotCallerName((Context.User as SocketGuildUser).DisplayName) : (Context.User as SocketGuildUser).Username;
      IComponentInteraction ButtonInteraction = Context.Interaction as IComponentInteraction;

      await DeferAsync(true);

      int iter = ButtonInteraction.Data.CustomId.IndexOf(":") + 1;
      string PaychexDate = ButtonInteraction.Data.CustomId.Split(":")[1];
      var previousButtons = ComponentBuilder.FromComponents(ButtonInteraction.Message.Components);

      if (ButtonInteraction.Data.CustomId.ToLower().Contains("paychex"))
      {
        var paychexbutton = new ButtonBuilder();
        var component = new ComponentBuilder();

        foreach (var button in previousButtons.ActionRows.FirstOrDefault().Components)
        {
          if (button.CustomId == ButtonInteraction.Data.CustomId)
          {
            paychexbutton.Style = ButtonStyle.Danger;
            paychexbutton.IsDisabled = true;
            paychexbutton.Label = $"Claimed {PaychexDate}";
            paychexbutton.CustomId = $"Claimed-{PaychexDate}-{sUserNickname}";

            component.WithButton(paychexbutton);
          }
          else
          {

            if (component.ActionRows != null)
            {
              component.ActionRows.FirstOrDefault().AddComponent(button);
            }
            else
            {
              var newActionRows = new List<ActionRowBuilder>();
              newActionRows.Add(new ActionRowBuilder());
              newActionRows.FirstOrDefault().AddComponent(button);
              component.ActionRows = newActionRows;
            }
          }
        }

        await Context.Interaction.ModifyOriginalResponseAsync((x) =>
        {
          x.Components = component.Build();
        });

        await GoogleSheetsDataWriter.TransferPaychexToMiniMartCredits(Context.User as SocketGuildUser, PaychexDate);

        await FollowupAsync($"Transfer Complete!", null, false, true);
      }
    }

    [SlashCommand("render-paychex", "If you don't know what this means at this point don't use it")]
    public async Task RenderPaychex()
    {
      await DeferAsync();
      await _logger.Log(new LogMessage(LogSeverity.Info, "Render Paychex", $"User: {Context.User.Username}, Command: render-paychex", null));
      await GoogleSheetsDataWriter.RenderPaychex(Context);

      var ListOfusers = Context.Guild.GetUsersAsync().FirstOrDefaultAsync().Result.ToList();
      int i = 0;

      foreach (var user in ListOfusers)
      {
        if (user.RoleIds.Any(x => x == NewRecruitRoleID || x == MemberRoleID || x == VeteranRoleID || x == OfficerRoleID))
        {
          PlayerLookupInfo? playerInfo = new PlayerLookupInfo();
          PlayerDataLookUps? albionData = new PlayerDataLookUps();

          string? sUserNickname = (user.DisplayName != null) ? new PlayerDataLookUps().CleanUpShotCallerName(user.DisplayName) : user.DisplayName;

          playerInfo = await albionData.GetPlayerInfo(Context, sUserNickname);

          if (playerInfo != null)
          {
            var detailedplayerinfo = await albionData.GetDetailedAlbionPlayerInfo(playerInfo.Id);

            dataBaseService = new DataBaseService();
            await dataBaseService.LogPlayerInfo(new LoggedPlayerInfo
            {
              PlayerId = detailedplayerinfo.Id,
              PlayerName = detailedplayerinfo.Name,
              GuildID = detailedplayerinfo.GuildId,
              DeathFame = detailedplayerinfo.DeathFame,
              KillFame = detailedplayerinfo.KillFame,
              FameRatio = (float)detailedplayerinfo.FameRatio,
              PVEFame = detailedplayerinfo.LifetimeStatistics.PvE.Total,
              GatheringFame = detailedplayerinfo.LifetimeStatistics.Gathering.All.Total,
              CraftingFame = detailedplayerinfo.LifetimeStatistics.Crafting.Total,
              RecordedDate = DateTime.Today
            });

            i++;
          }
        }
      }
      await FollowupAsync($"{i} Free Beer members have been logged");
    }

    [SlashCommand("view-player-paychex", "View someones paychex info")]
    public async Task ViewMembersPaychex(SocketGuildUser GuildUser)
    {
      await DeferAsync(true);

      Dictionary<string, string> paychexTotals = GoogleSheetsDataWriter.GetPaychexTotals(GuildUser.Nickname);
      int iGrandTotalPaychex = 0;

      if (paychexTotals.Count > 0)
      {
        foreach (var entries in paychexTotals)
        {
          if (entries.Key.Contains("NOT CLAIMED"))
          {
            iGrandTotalPaychex += Convert.ToInt32(entries.Value.Replace(",", ""));
          }
        }
      }

      await FollowupAsync($"{GuildUser.Nickname} total is {iGrandTotalPaychex}", ephemeral: true);
    }

    [SlashCommand("mm-transaction", "Submit transaction to Mini-mart")]
    public async Task MiniMartTransactions(SocketGuildUser GuildUser, int Amount, MiniMarketType TransactionType)
    {
      string? sManagerNickname = ((Context.User as SocketGuildUser).Nickname != null) ? new PlayerDataLookUps().CleanUpShotCallerName((Context.User as SocketGuildUser).Nickname) : (Context.User as SocketGuildUser).Username;
      string? sUserNickname = (GuildUser.Nickname != null) ? new PlayerDataLookUps().CleanUpShotCallerName(GuildUser.Nickname) : GuildUser.Username;
      var socketUser = (SocketGuildUser)Context.User;

      if (socketUser.Roles.Any(r => r.Id == OfficerRoleID) || socketUser.Roles.Any(r => r.Id == ManagementRoleID))
      {
        await DeferAsync();
        await _logger.Log(new LogMessage(LogSeverity.Info, "mm Transaction", $"User: {Context.User.Username}, Command: mm-transaction", null));
        await GoogleSheetsDataWriter.MiniMartTransaction(Context.User as SocketGuildUser, GuildUser, Amount, TransactionType);

        var miniMarketCreditsTotal = GoogleSheetsDataWriter.GetMiniMarketCredits(sUserNickname);

        if (TransactionType == MiniMarketType.Purchase)
        {
          int discount = Convert.ToInt32(Amount * .10);

          await ReplyAsync($"{TransactionType} of {Amount.ToString("N0")} is complete. {sUserNickname} current balance is {miniMarketCreditsTotal} ");
          await FollowupAsync("React :thumbsup: when you pickup your items.");
        }
        else
        {
          await FollowupAsync($"{sUserNickname} {TransactionType} of {Amount.ToString("N0")} is complete");
        }
      }
      else
      {
        await RespondAsync($"You need perms to do this ya bum.", null, false, true, null, null, null, null);
      }
    }

    [SlashCommand("regear", "Submit a regear")]
    public async Task RegearSubmission(int EventID, SocketGuildUser callerName, EventTypeEnum EventType, SocketGuildUser mentor = null)
    {
      PlayerDataLookUps eventData = new PlayerDataLookUps();
      RegearModule regearModule = new RegearModule();

      var guildUser = (SocketGuildUser)Context.User;
      IComponentInteraction interaction = Context.Interaction as IComponentInteraction;

      string? sUserNickname = ((Context.User as SocketGuildUser).DisplayName != null) ? (Context.User as SocketGuildUser).DisplayName : (Context.User as SocketGuildUser).Nickname;
      string? sCallerNickname = (callerName.DisplayName != null) ? callerName.DisplayName : callerName.Username;

      bool bRegearAllowed = true;
      await _logger.Log(new LogMessage(LogSeverity.Info, "Regear Submit", $"User: {Context.User.Username}, Command: regear", null));

      PlayerEventData = await eventData.GetAlbionEventInfo(EventID);
      //ulong regearPoster = regearModule.GetRegearPosterID(PlayerEventData.Victim.Name, Context);

      if (sUserNickname.Contains("!sl"))
      {
        sUserNickname = new PlayerDataLookUps().CleanUpShotCallerName(sUserNickname);
      }

      if (sCallerNickname.Contains("!sl"))
      {
        sCallerNickname = new PlayerDataLookUps().CleanUpShotCallerName(sCallerNickname);
      }

      if (DateTime.Parse(PlayerEventData.TimeStamp) <= DateTime.Now.AddHours(-72) && !guildUser.Roles.Any(r => r.Id == OfficerRoleID))
      {
        await RespondAsync($"Requirement failed. Your time to submit this regear is past 72 hours. Regear denied. ", null, false, true);
        bRegearAllowed = false;
      }
      else if ((!guildUser.Roles.Any(r => r.Id == OfficerRoleID)))
      {
        if(sUserNickname.ToLower() != PlayerEventData.Victim.Name.ToLower() || (Context.User as SocketGuildUser).Nickname == null)
        {
          IMessageChannel RecruitersModChannel = Context.Client.GetChannel(1024308022840918026) as IMessageChannel;//using bot workshop channel
          await RecruitersModChannel.SendMessageAsync($"{sUserNickname} may have not been registed. Please verify their registration ");
          await RespondAsync($"Regear submission failed. Please update your discord server nickname to EXACTLY match your in-game name. ", null, false, true);

          bRegearAllowed = false;
        }
      }
      else if (mentor != null && RegearModule.ISUserMentor(mentor) && guildUser.Roles.Any(r => r.Id == BronzeTierRegearID))
      {
        bRegearAllowed = true;
      }
      else if (guildUser.Roles.Any(r => r.Id == GoldTierRegearID || r.Id == SilverTierRegearID))
      {
        bRegearAllowed = true;
      }
      else if (mentor == null && guildUser.Roles.Any(r => r.Id == BronzeTierRegearID))
      {
        await RespondAsync($"You need to assign your regear a mentor", null, false, true);
        bRegearAllowed = false;
      }
      else if (mentor != null && RegearModule.ISUserMentor(mentor) == false && guildUser.Roles.Any(r => r.Id == BronzeTierRegearID))
      {
        await RespondAsync($"Cleary you need a mentor you idiot. Make sure you select an ACTUAL mentor", null, false, true);
        bRegearAllowed = false;
      }

      if (guildUser.Roles.Any(r => r.Id == OfficerRoleID || r.Id == ManagementRoleID) || bRegearAllowed)
      {
        dataBaseService = new DataBaseService();
        await dataBaseService.AddPlayerInfo(new DiscordBot.Models.Player
        {
          PlayerId = PlayerEventData.Victim.Id,
          PlayerName = PlayerEventData.Victim.Name
        });

        if (!await dataBaseService.PlayerReachRegearCap(sUserNickname, iRegearLimit) || guildUser.Roles.Any(r => r.Id == OfficerRoleID))
        {
          if (!await dataBaseService.CheckKillIdIsRegeared(EventID.ToString()))
          {
            if (PlayerEventData != null)
            {
              var moneyType = (MoneyTypes)Enum.Parse(typeof(MoneyTypes), "ReGear");

              if (PlayerEventData.Victim.Name.ToLower() == sUserNickname.ToLower() || guildUser.Roles.Any(r => r.Id == OfficerRoleID))
              {
                await DeferAsync(true);

                await regearModule.PostRegear(Context, PlayerEventData, sCallerNickname, EventType, moneyType, mentor);
                await Context.User.SendMessageAsync($"<@{Context.User.Id}> Your regear ID:{regearModule.RegearQueueID} Your Kill ID: {regearModule.KillID} has been submitted successfully.");

                await FollowupAsync("Regear Submission complete", ephemeral: true);
                //await DeleteOriginalResponseAsync();
              }
              else
              {
                await RespondAsync($"<@{Context.User.Id}>. You can't submit regears on the behalf of {PlayerEventData.Victim.Name}. Ask the Regear team if there's an issue.", null, false, true);
                await _logger.Log(new LogMessage(LogSeverity.Info, "Regear Submit", $"User: {Context.User.Username}, Tried submitting regear for {PlayerEventData.Victim.Name}", null));
              }
            }
            else
            {
              await RespondAsync("Event info not found. Please verify Kill ID or event has expired.", null, false, true);
            }
          }
          else
          {
            await RespondAsync($"You dumbass <@{Context.User.Id}>. Don't try to scam the guild and steal money. You can't submit another regear for same death. :middle_finger: ", null, false, true);
          }
        }
        else
        {
          await RespondAsync($"Woah woah waoh there <@{Context.User.Id}>.....I'm cutting you off. You already submitted 5 regears today. Time to use the eco money you don't have. You can't claim more than 5 regears in a day", null, false, false);
        }
      }
    }

    [ComponentInteraction("deny")]
    public async Task Denied()
    {
      var guildUser = (SocketGuildUser)Context.User;
      RegearModule regearModule = new RegearModule();
      var interaction = Context.Interaction as IComponentInteraction;
      string victimName = interaction.Message.Embeds.FirstOrDefault().Fields[1].Value.ToString();
      int killId = Convert.ToInt32(interaction.Message.Embeds.FirstOrDefault().Fields[0].Value);
      ulong regearPoster = regearModule.GetRegearPosterID(victimName, Context);
      string? sUserNickname = (guildUser.DisplayName != null) ? new PlayerDataLookUps().CleanUpShotCallerName(guildUser.DisplayName) : guildUser.Username;
      string? sSelectedMentor = (interaction.Message.Embeds.FirstOrDefault().Fields.Any(x => x.Name == "Mentor")) ? interaction.Message.Embeds.FirstOrDefault().Fields.Where(x => x.Name == "Mentor").FirstOrDefault().Value.ToString() : null;

      if (RegearModule.HasRegearOverride(guildUser) || (sSelectedMentor != null && RegearModule.ISUserMentor(guildUser) && sSelectedMentor.ToLower() == sUserNickname.ToLower()) || victimName.ToLower() == guildUser.DisplayName.ToLower())
      {
        dataBaseService = new DataBaseService();

        try
        {
          dataBaseService.DeletePlayerLootByKillId(killId.ToString());
        }
        catch (Exception ex)
        {
          Console.WriteLine(ex.ToString() + " ERROR DELETING RECORD FROM DATABASE");
        }

        if (victimName.ToLower() != guildUser.DisplayName.ToLower() && RegearModule.HasRegearOverride(guildUser))
        {
          try
          {
            await interaction.Message.DeleteAsync();
            await Context.Guild.GetUser(regearPoster).SendMessageAsync($"{guildUser.DisplayName} denied regear #{killId}. Please seek out them if you need a reason why.");
            //var mb = new ModalBuilder()
            //.WithTitle("Regear Denied")
            //.WithCustomId($"regear_deny_menu{killId}");
            //mb.AddTextInput("Why is this regear being denied?", "deny_reason", TextInputStyle.Paragraph, placeholder: "Enter something here why this person is robbing the guild", required: false, value: null, maxLength: 500);

            //string Reason = "Nothing";
            //bool confirmModal = false;

            //await Context.Interaction.RespondWithModalAsync(mb.Build());
            //Context.Client.ModalSubmitted += async modal =>
            //{
            //    if (!confirmModal)
            //    {
            //        await modal.DeferAsync();
            //        Reason = (modal.Data.Components.FirstOrDefault().Value != null || modal.Data.Components.FirstOrDefault().Value != "") ? modal.Data.Components.FirstOrDefault().Value : "None";
            //        await interaction.Message.DeleteAsync();
            //        await Context.Guild.GetUser(regearPoster).SendMessageAsync($"{guildUser.DisplayName} denied regear #{killId}. https://albiononline.com/en/killboard/kill/{killId} Reason: {Reason}");
            //        confirmModal = true;
            //    }
            //};
          }
          catch (Exception ex)
          {
            Console.WriteLine(ex);
          }
        }
        else
        {
          await interaction.Message.DeleteAsync();
          await RespondAsync($"Regear #{killId} cancelled", ephemeral: true);
        }

        await _logger.Log(new LogMessage(LogSeverity.Info, "Regear Denied", $"User: {Context.User.Username}, Denied regear {killId} for {victimName} ", null));
      }
      else
      {
        await RespondAsync($"<@{Context.User.Id}>Stop pressing random buttons idiot. That aint your job.", null, false, true);
      }
    }

    [ComponentInteraction("approve")]
    public async Task RegearApprove()
    {
      SocketGuildUser guildUser = (SocketGuildUser)Context.User;
      IComponentInteraction interaction = Context.Interaction as IComponentInteraction;
      int iKillId = Convert.ToInt32(interaction.Message.Embeds.FirstOrDefault().Fields[0].Value);
      string sVictimName = interaction.Message.Embeds.FirstOrDefault().Fields[1].Value.ToString();
      string sCallername = Regex.Replace(interaction.Message.Embeds.FirstOrDefault().Fields[3].Value.ToString(), @"\p{C}+", string.Empty);
      int iRefundAmount = Convert.ToInt32(interaction.Message.Embeds.FirstOrDefault().Fields[4].Value);
      ulong uRegearPosterID = Convert.ToUInt64(interaction.Message.Embeds.FirstOrDefault().Fields[6].Value);
      string sEventType = interaction.Message.Embeds.FirstOrDefault().Fields[7].Value.ToString();
      string? sUserNickname = (guildUser.DisplayName != null) ? new PlayerDataLookUps().CleanUpShotCallerName(guildUser.DisplayName) : guildUser.Nickname;
      string? sSelectedMentor = (interaction.Message.Embeds.FirstOrDefault().Fields.Any(x => x.Name == "Mentor")) ? interaction.Message.Embeds.FirstOrDefault().Fields.Where(x => x.Name == "Mentor").FirstOrDefault().Value.ToString() : null;

      if ((guildUser.Roles.Any(r => r.Id == ManagementRoleID || r.Id == OfficerRoleID)) || sSelectedMentor != null && RegearModule.ISUserMentor(guildUser) && sSelectedMentor.ToLower() == sUserNickname.ToLower())
      {
        PlayerDataLookUps eventData = new PlayerDataLookUps();
        PlayerEventData = await eventData.GetAlbionEventInfo(iKillId);
        await GoogleSheetsDataWriter.WriteToRegearSheet(Context, iRefundAmount, sCallername, sEventType, MoneyTypes.ReGear, null, PlayerEventData);
        await Context.Channel.DeleteMessageAsync(interaction.Message.Id);

        IReadOnlyCollection<Discord.Rest.RestGuildUser> regearPoster = await Context.Guild.SearchUsersAsync(sVictimName);

        if (regearPoster.Any(x => x.RoleIds.Any(x => x == 1052241667329118349)) || Context.Guild.GetUser(uRegearPosterID).Roles.Any(r => r.Name == "Free Regear - Eligible"))
        {
          await Context.Guild.GetUser(uRegearPosterID).RemoveRoleAsync(1052241667329118349);
          await Context.Guild.GetUser(uRegearPosterID).SendMessageAsync($"Your free regear https://albiononline.com/en/killboard/kill/{iKillId} has been approved! ${iRefundAmount.ToString("N0")} has been added to your paychex. " +
              $"Please seek a mentor to obtain regear roles to continue getting regears.");
        }
        else
        {
          await Context.Guild.GetUser(uRegearPosterID).SendMessageAsync($"Your regear https://albiononline.com/en/killboard/kill/{iKillId} has been approved! ${iRefundAmount.ToString("N0")} has been added to your paychex");
        }

        await _logger.Log(new LogMessage(LogSeverity.Info, "Regear Approved", $"User: {Context.User.Username}, Approved the regear {iKillId} for {sVictimName} ", null));
      }
      else
      {
        await RespondAsync($"Just because the button is green <@{Context.User.Id}> doesn't mean you can press it. Bug off.", null, false, true);
      }
    }

    [ComponentInteraction("audit")]
    public async Task AuditRegear()
    {
      SocketGuildUser socketGuildUser = (SocketGuildUser)Context.User;
      IComponentInteraction interaction = Context.Interaction as IComponentInteraction;
      int iKillId = Convert.ToInt32(interaction.Message.Embeds.FirstOrDefault().Fields[0].Value);

      PlayerEventData = await new PlayerDataLookUps().GetAlbionEventInfo(iKillId);

      string sBattleID = (PlayerEventData.EventId == PlayerEventData.BattleId) ? "No battle found" : PlayerEventData.BattleId.ToString();
      string sKillerGuildName = (PlayerEventData.Killer.GuildName == "" || PlayerEventData.Killer.GuildName == null) ? "No Guild" : PlayerEventData.Killer.GuildName;

      if (socketGuildUser.Roles.Any(r => r.Id == ManagementRoleID || r.Id == OfficerRoleID))
      {
        var embed = new EmbedBuilder()
        .WithTitle($"Regear audit on {PlayerEventData.Victim.Name}")
        .AddField("Event ID", PlayerEventData.EventId, true)
        .AddField("Victim", PlayerEventData.Victim.Name, true)
        .AddField("Average IP", PlayerEventData.Victim.AverageItemPower, true)
        .AddField("Killer Name", PlayerEventData.Killer.Name, true)
        .AddField("Killer Guild Name", sKillerGuildName, true)
        .AddField("Killer Avg IP", PlayerEventData.Killer.AverageItemPower, true)
        .AddField("Kill Area", PlayerEventData.KillArea, true)
        .AddField("Number of participants", PlayerEventData.numberOfParticipants, false)
        .AddField("Time Stamp", PlayerEventData.TimeStamp, true)
        .WithUrl($"https://albiononline.com/en/killboard/kill/{PlayerEventData.EventId}");

        if (PlayerEventData.EventId != PlayerEventData.BattleId)
        {
          embed.AddField("BattleID", sBattleID);
          embed.AddField("BattleBoard Name", $"https://albionbattles.com/battles/{sBattleID}", false);
          embed.AddField("Battle Killboard", $"https://albiononline.com/en/killboard/battles/{sBattleID}", false);
        }

        await RespondAsync($"Audit report for event {PlayerEventData.EventId}.", null, false, true, null, null, null, embed.Build());
      }
      else
      {
        await RespondAsync($"You cannot see this juicy info <@{Context.User.Id}> Not like you can read anyways.", null, false, true, null, null, null, null);
      }
    }

    [SlashCommand("regear-oc", "Submit a OC-break regear")]
    public async Task RegearOCSubmission(string items, SocketGuildUser callerName, EventTypeEnum enumEventType, int? a_iEstimatedGearPrice = null)
    {

      PlayerDataLookUps eventData = new PlayerDataLookUps();
      RegearModule regearModule = new RegearModule();
      var guildUser = (SocketGuildUser)Context.User;

      string? sUserNickname = ((Context.User as SocketGuildUser).DisplayName != null) ? new PlayerDataLookUps().CleanUpShotCallerName((Context.User as SocketGuildUser).DisplayName) : Context.User.Username;
      string? sCallerNickname = (callerName.DisplayName != null) ? new PlayerDataLookUps().CleanUpShotCallerName(callerName.DisplayName) : callerName.Username;

      var playerInfo = await eventData.GetPlayerInfo(Context, sUserNickname);
      //var PlayerEventData = playerInfo.players.Where(x => x.Name.ToLower() == sUserNickname.ToLower()).FirstOrDefault();


      await _logger.Log(new LogMessage(LogSeverity.Info, "OC break Submit", $"User: {Context.User.Username}, Command: oc-regear", null));

      if (playerInfo.Name.ToLower() == sUserNickname.ToLower() || guildUser.Roles.Any(r => r.Id == OfficerRoleID))
      {
        await DeferAsync();
        await regearModule.PostOCRegear(Context, items.Split(",").ToList(), sCallerNickname, MoneyTypes.OCBreak, enumEventType);
        await Context.User.SendMessageAsync($"Your OC Break ID:{regearModule.RegearQueueID} has been submitted successfully.");

        await FollowupAsync("Regear Submission Complete", null, false, false);
        await DeleteOriginalResponseAsync();
      }
      else
      {
        await RespondAsync($"There was an error processing your OC break. Try again or contact Regears Officer", null, false, true);
      }
    }

    [ComponentInteraction("oc-approve")]
    public async Task OCApproved()
    {
      await DeferAsync();
      var guildUser = (SocketGuildUser)Context.User;
      string? sUserNickname = ((Context.User as SocketGuildUser).DisplayName != null) ? new PlayerDataLookUps().CleanUpShotCallerName((Context.User as SocketGuildUser).DisplayName) : Context.User.Username;
      var interaction = Context.Interaction as IComponentInteraction;

      string victimName = interaction.Message.Embeds.FirstOrDefault().Fields[0].Value.ToString();
      string callername = Regex.Replace(interaction.Message.Embeds.FirstOrDefault().Fields[1].Value.ToString(), @"\p{C}+", string.Empty);
      int refundAmount = Convert.ToInt32(interaction.Message.Embeds.FirstOrDefault().Fields[2].Value);
      string eventType = interaction.Message.Embeds.FirstOrDefault().Fields[3].Value.ToString();
      ulong queueID = Convert.ToUInt64(interaction.Message.Embeds.FirstOrDefault().Fields[4].Value);
      ulong regearPosterID = Convert.ToUInt64(interaction.Message.Embeds.FirstOrDefault().Fields[5].Value);

      PlayerDataHandler.Rootobject tempPlayerEventData = new PlayerDataHandler.Rootobject();

      tempPlayerEventData.Victim = new()
      {
        Name = victimName
      };


      PlayerDataLookUps eventData = new PlayerDataLookUps();

      if (guildUser.Roles.Any(r => r.Id == ManagementRoleID || r.Id == OfficerRoleID))
      {
        await GoogleSheetsDataWriter.WriteToRegearSheet(Context, refundAmount, callername, eventType, MoneyTypes.OCBreak, null, tempPlayerEventData);
        await Context.Channel.DeleteMessageAsync(interaction.Message.Id);

        try
        {
          await Context.Guild.GetUser(regearPosterID).SendMessageAsync($"Your OC Break {queueID} has been approved by {sUserNickname}! ${refundAmount.ToString("N0")} has been added to your paychex");
        }
        catch
        {
          Console.WriteLine("DISCORD ERROR: 50007. User is blocking message from bot or has settings to preventing me to DM them");
        }

        await _logger.Log(new LogMessage(LogSeverity.Info, "OC Break Approved", $"User: {Context.User.Username}, Approved the regear {queueID} for {victimName} ", null));
      }
      else
      {
        await RespondAsync($"Just because the button is green <@{Context.User.Id}> doesn't mean you can press it. Bug off.", null, false, true);
      }

      await FollowupAsync("OC Approved. This thread can be deleted.");
    }

    [ComponentInteraction("oc-deny")]
    public async Task OCDenied()
    {
      var guildUser = (SocketGuildUser)Context.User;
      var interaction = Context.Interaction as IComponentInteraction;
      string victimName = interaction.Message.Embeds.FirstOrDefault().Fields[0].Value.ToString();
      var iQueueID = interaction.Message.Embeds.FirstOrDefault().Fields[4].Value;
      ulong regearPoster = Convert.ToUInt64(interaction.Message.Embeds.FirstOrDefault().Fields[5].Value);
      string Reason = "";

      await DeferAsync();

      if (guildUser.Roles.Any(r => r.Id == ManagementRoleID || r.Id == OfficerRoleID) || regearPoster == guildUser.Id)
      {
        dataBaseService = new DataBaseService();

        try
        {
          dataBaseService.DeletePlayerLootByQueueId(iQueueID.ToString());
          var guildUsertest = Context.Guild.GetUser(regearPoster);

          //var mb = new ModalBuilder()
          //.WithTitle("OC Break Denied")
          //.WithCustomId("deny_menu");
          //mb.AddTextInput("Why is this OC Break being denied?", "deny_reason", TextInputStyle.Paragraph, placeholder: "Enter something here why this person is robbing the guild", required: false, value: null, maxLength: 500);
          //await DeferAsync(true);
          //await Context.Interaction.RespondWithModalAsync(mb.Build());
          //Context.Client.ModalSubmitted += async modal =>
          //{

          //    Reason = (modal.Data.Components.FirstOrDefault().Value != null || modal.Data.Components.FirstOrDefault().Value != "") ? modal.Data.Components.FirstOrDefault().Value : "None";
          //};

          //await Context.Interaction.User.SendMessageAsync($"{guildUser.DisplayName} denied OC break {iQueueID}. Reason: {Reason}");
          await Context.Guild.GetUser(regearPoster).SendMessageAsync($"{guildUser.DisplayName} denied OC break {iQueueID}. If you need reason for deny seek out why.");
          await Context.Interaction.DeleteOriginalResponseAsync();

          //await Context.Channel.DeleteMessageAsync(interaction.Message.Id);
        }
        catch (Exception ex)
        {
          Console.WriteLine(ex.ToString() + " ERROR DELETING RECORD FROM DATABASE");
        }
        //cannot respond of defer twice to the same interaction

        await _logger.Log(new LogMessage(LogSeverity.Info, "OC Regear Denied", $"User: {Context.User.Username}, Denied regear {iQueueID} for {victimName} ", null));


      }
      else
      {
        await RespondAsync($"<@{Context.User.Id}>Stop pressing random buttons idiot. That aint your job.", null, false, true);
      }
    }

    [SlashCommand("play-song", "Play a song test")]
    public async Task PlaySong(string searchQuery)
    {
      //TODO: Switch to VICTORIA for music https://github.com/Yucked/Victoria/wiki/%F0%9F%A7%AC-Samples
      await DeferAsync();
      IVoiceChannel voiceChannel = ((IGuildUser)Context.User).VoiceChannel;

      LavalinkPlayer player = Program.lavalinkManager.GetPlayer(GuildID) ?? await Program.lavalinkManager.JoinAsync(voiceChannel);
      LoadTracksResponse response = null;

      var TodaysDate = DateTime.Today;

      //response = await Program.lavalinkManager.GetTracksAsync($"ytsearch:{searchQuery}");
      response = await Program.lavalinkManager.GetTracksAsync($"scsearch:{searchQuery}");
      //response = await Program.lavalinkManager.GetTracksAsync($"ytmsearch:{SongName}");

      // Gets the first track from the response
      LavalinkTrack track = response.Tracks.First();
      await player.PlayAsync(track);

      await FollowupAsync($"Playing song: {player.CurrentTrack.Url}");
    }

    [SlashCommand("stop-song", "stop a song ")]
    public async Task StopSong()
    {

      var player = Program.lavalinkManager.GetPlayer(Context.Guild.Id) ??
      await Program.lavalinkManager.JoinAsync((Context.User as IGuildUser)?.VoiceChannel);
      await player.StopAsync();
      await RespondAsync("Stopped playing. Your queue is still intact though. Use `clear` to Destroy Queue", ephemeral: true);
    }

    [SlashCommand("set-volume", "Volume between 1 - 100")]
    public async Task SetVolumne(uint volume)
    {
      if (volume > 111)
      {
        await RespondAsync($"Volume doesn't go higher than 111", ephemeral: true);
      }
      else
      {
        var player = Program.lavalinkManager.GetPlayer(Context.Guild.Id) ??
        await Program.lavalinkManager.JoinAsync((Context.User as IGuildUser)?.VoiceChannel);
        await player.SetVolumeAsync(volume);
        await FollowupAsync($"Volume set to {volume}.", ephemeral: true);
      }

    }
    [SlashCommand("clear-songs", "Clear the songs queue")]
    public async Task ClearQueue()
    {

      var player = Program.lavalinkManager.GetPlayer(Context.Guild.Id);
      await Program.lavalinkManager.StopAsync();
      await player.DisconnectAsync();
      await RespondAsync("Your queue has been cleared Queue", ephemeral: true);
    }


    [SlashCommand("split-loot", "Perform a loot split.")]
    public async Task SplitLoot(LootSplitType LootSplitType, SocketGuildUser CallerName, EventTypeEnum EventType, int? NonDamagedLootTotal = null, int? DamagedLootTotal = null, int? SilverBagsTotal = null)
    {
      await DeferAsync();
      string? sUserNickname = ((Context.User as SocketGuildUser).DisplayName != null) ? new PlayerDataLookUps().CleanUpShotCallerName((Context.User as SocketGuildUser).DisplayName) : Context.User.Username;
      string? sCallerNickname = (CallerName.DisplayName != null) ? CallerName.DisplayName : CallerName.Username;

      //Gets users active in thread
      var socketThreadChannel = (SocketThreadChannel)Context.Channel;
      var usersActiveInThread = await socketThreadChannel.GetUsersAsync();
      var UsersList = usersActiveInThread.ToList();
      List<string> cleanedUpNames = new List<string>();
      LootSplitModule lootSplitMod = new LootSplitModule();

      foreach (var user in UsersList)
      {
        if (!user.IsBot)
        {
          cleanedUpNames.Add(new PlayerDataLookUps().CleanUpShotCallerName(user.DisplayName));
        }
      }
      if (NonDamagedLootTotal != null || DamagedLootTotal != null || SilverBagsTotal != null)
      {
        await lootSplitMod.LootSplitInitialPrompt(Context, cleanedUpNames, sCallerNickname, LootSplitType, EventType, NonDamagedLootTotal, DamagedLootTotal, SilverBagsTotal);
        await FollowupAsync("Loot Split submitted");
      }
      else
      {
        await FollowupAsync("You must add some sort of loot amount", ephemeral: true);
      }

      await _logger.Log(new LogMessage(LogSeverity.Info, "Split-Loot Command", $"User: {Context.User.Username} initiated a split-loot", null));
    }

    [ComponentInteraction("add-member*")]
    async Task AddMembersToLootSplit()
    {
      ;
      LootSplitModule lootSplitMod = new LootSplitModule();
      await lootSplitMod.AddRemoveNamesFromList(Context, Options.Add);
      await _logger.Log(new LogMessage(LogSeverity.Info, "Add member", $"User: {Context.User.Username} added member from split", null));
      //await Context.Interaction.FollowupAsync("Member(s) added");
    }

    [ComponentInteraction("remove-member*")]
    async Task RemoveMembersFromSplit()
    {
      LootSplitModule lootSplitMod = new LootSplitModule();
      await lootSplitMod.AddRemoveNamesFromList(Context, Options.Remove);
      await _logger.Log(new LogMessage(LogSeverity.Info, "Remove member", $"User: {Context.User.Username} removed member from split", null));
      //await Context.Interaction.FollowupAsync("Member(s) removed");
    }

    [ComponentInteraction("approve-split")]
    async Task ApproveSplit()
    {
      LootSplitModule lootSplitMod = new LootSplitModule();
      RegearModule regearModule = new RegearModule();
      List<string> PlayersIncludedInSplit = new List<string>();

      var guildUser = (SocketGuildUser)Context.User;
      var interaction = Context.Interaction as IComponentInteraction;
      var partyLeader = interaction.Message.Embeds.FirstOrDefault().Fields[10].Value;
      var eventType = interaction.Message.Embeds.FirstOrDefault().Fields[7].Value;
      List<string> membersSplit = interaction.Message.Embeds.FirstOrDefault().Fields[9].Value.Replace(" ", "").Split(',').ToList();

      

      //check perms to push buttons
      if (guildUser.Roles.Any(r => r.Id == ManagementRoleID || r.Id == OfficerRoleID || r.Name == "admin"))
      {
        await RespondAsync("Processing. Please wait till you get the confirmation before closing thread...");

        var PayoutPerPlayer = interaction.Message.Embeds.FirstOrDefault().Fields[4].Value.Replace(",", "");

        try
        {
          //foreach (string playerName in membersSplit)
          //{
            //conditional to add members to .Player table if not in there already
            //if (dataBaseService.GetPlayerInfoByName(playerName) == null)
            //{
            //    using SqlConnection connection = new SqlConnection(constr);
            //    {
            //        using SqlCommand command = connection.CreateCommand();

            //        {
            //            //need to add player to Player table to work with foreign keys
            //            command.Parameters.AddWithValue("@playerName", playerName);
            //            command.Parameters.AddWithValue("@pid", lootSplitMod.scrapedDict[playerName].ToString());
            //            command.CommandText = "INSERT INTO [FreeBeerdbTest].[dbo].[Player] (PlayerName, PlayerId) VALUES (@playerName, @pid)";
            //            connection.Open();
            //            command.ExecuteNonQuery();
            //            connection.Close();
            //            command.Parameters.Clear();
            //        }
            //    }
            //}

            //int playerID = dataBaseService.GetPlayerInfoByName(playerName).Id;
            //await dataBaseService.AddPlayerReGear(new PlayerLoot
            //{
            //    TypeId = moneyType.Id,
            //    CreateDate = DateTime.Now,
            //    Loot = refundAmount,
            //    PlayerId = playerID,
            //    Message = tempStr,
            //    PartyLeader = partyLead,
            //    KillId = tempStr,
            //    Reason = reasonLootSplit,
            //    QueueId = tempStr
            //});
          //}

          //PlayerDataHandler.Rootobject playerInfo = new PlayerDataHandler.Rootobject();
          //PlayerDataHandler.Victim victimInfo = new PlayerDataHandler.Victim();
          //victimInfo.Name = playerName;
          //playerInfo.Victim = victimInfo;

          await GoogleSheetsDataWriter.WriteToRegearSheet(Context, Convert.ToInt32(PayoutPerPlayer), partyLeader, eventType, MoneyTypes.LootSplit, membersSplit);

          await FollowupAsync(($"This loot split has finished processing! {interaction.Message.Embeds.FirstOrDefault().Fields[4].Value} has been added to everyone's paychex. This thread can be deleted."));
          await Context.Channel.DeleteMessageAsync(interaction.Message.Id);
          await DeleteOriginalResponseAsync();

        }
        catch
        {
          await FollowupAsync("Oops I messed up. Send me the IT guy");
        }
        await _logger.Log(new LogMessage(LogSeverity.Info, "Loot Split Approved", $"User: {Context.User.Username}, Approved Loot split ", null));
      }
      else
      {
        await RespondAsync("You are stupid.", null, false, true);
      }
    }
    [ComponentInteraction("deny-split")]
    async Task DenySplit()
    {
      var socketThreadChannel = (SocketThreadChannel)Context.Channel;
      var usersActiveInThread = await socketThreadChannel.GetUsersAsync();
      var guildUser = (SocketGuildUser)Context.User;
      var interaction = Context.Interaction as IComponentInteraction;
      string sPartyLeader = interaction.Message.Embeds.FirstOrDefault().Fields[10].Value;

      //check perms for button pushing
      if (guildUser.Roles.Any(r => r.Id == ManagementRoleID || r.Id == OfficerRoleID || r.Name == "admin" || socketThreadChannel.Owner.DisplayName == Context.User.Username || Context.Interaction.User.Username == sPartyLeader))
      {
        await Context.Channel.DeleteMessageAsync(interaction.Message.Id);
        await RespondAsync("Loot split denied/cancelled");
        await DeleteOriginalResponseAsync();
      }
      else
      {
        await RespondAsync("Don't push buttons without perms you mongo.", null, false, true);
      }
      await _logger.Log(new LogMessage(LogSeverity.Info, "Split-Loot deny", $"User: {Context.User.Username} denied a split-loot", null));
    }

    [SlashCommand("add-game-to-portal", "Create Button roles for the portal")]
    public async Task AddGame(string Game_Name, SocketRole? Role = null)
    {
      SocketRole newRole = null;
      var embed = new EmbedBuilder();
      var comp = new ComponentBuilder();

      if (Role == null)
      {
        await Context.Guild.CreateRoleAsync(Game_Name, null);

        var roleIds = Context.Guild.Roles;

        foreach (var roles in roleIds)
        {
          if (roles.Name == Game_Name)
          {
            newRole = Context.Guild.GetRole(roles.Id);
          }
        }


        var membercount = Context.Guild.GetRole((Role == null) ? newRole.Id : Role.Id).Members.Count();

        embed.WithTitle($"{Game_Name}");
        embed.AddField("Role Name", (Role == null) ? newRole : Role.Name, true);
        embed.AddField("Current Members playing", membercount, true);
        embed.AddField("Game ID:", (Role == null) ? newRole.Id : Role.Id, true);
        var AddRemoveRoleButton = new ButtonBuilder()
        {
          Label = "Get/Remove Role",
          CustomId = $"getrole-{Game_Name}",
          Style = ButtonStyle.Success
        };
        comp.WithButton(AddRemoveRoleButton);

        await RespondAsync(null, null, false, false, null, null, comp.Build(), embed.Build());

      }
      else if (Role.Id == ApplicantRoleID)
      {
        //1156954659525771344 = application role. Code it to config

        embed.WithTitle($"Still interested in joining Free Beer?");
        //embed.WithDescription("Still interested in joining Free Beer? Press the button below to start the process.");
        embed.WithColor(Color.Green);
        embed.WithFooter("MAKE SURE YOUR DISCORD NICKNAME MATCHES YOUR IN-GAME NAME BEFORE APPLYING. CHECK YOUR PROFILE SETTINGS FOR THIS");


        var GetApplicationRole = new ButtonBuilder()
        {
          Label = "APPLY HERE",
          CustomId = $"getapplicantrole",
          Style = ButtonStyle.Success,
          Emote = Emoji.Parse(":point_right:")
        };
        comp.WithButton(GetApplicationRole);

        //await ReplyAsync(null, false, embed.Build(), null, null, null, comp.Build());
        await RespondAsync(null, null, false, false, null, null, comp.Build(), embed.Build());
      }
      else
      {
        await RespondAsync("Something went wrong.", ephemeral: true);
      }
    }

    [ComponentInteraction("getapplicantrole")]
    public async Task VerifyApplicant()
    {
      //ADD ANTI SPAM MEASURES HERE
      //MAKE THE EMBED APPLICATIONS A FOLLOWING

      var socketUser = (SocketGuildUser)Context.User;
      IComponentInteraction ButtonInteraction = Context.Interaction as IComponentInteraction;
      IMessageChannel RecruitersModChannel = Context.Client.GetChannel(RecruitersModChannelID) as IMessageChannel;

      if (HelperMethods.IsUserFreeBeerMember(socketUser))
      {
        await RespondAsync("You're already a member of Free Beer or your currently have an application open.", ephemeral: true);
      }
      else if (!HelperMethods.IsUserFreeBeerMember(socketUser) || socketUser.Roles.Any(r => r.Id == 1015016450265727057))
      {
        await DeferAsync(true);
        if (socketUser.Nickname != null)
        {
          PlayerLookupInfo? playerInfo = new PlayerLookupInfo();
          PlayerDataLookUps? albionData = new PlayerDataLookUps();//TODO: STATIC MEMBER

          try
          {
            playerInfo = await albionData.GetPlayerInfo(Context, socketUser.DisplayName);
            var detailedplayerinfo = await albionData.GetDetailedAlbionPlayerInfo(playerInfo.Id);

            if (detailedplayerinfo.Name.ToLower() == socketUser.Nickname.ToLower())
            {
              if(detailedplayerinfo.KillFame >= iRecruitmentKillFame)
              {
                await socketUser.AddRoleAsync(ApplicantRoleID);

                var componentInteraction = Context.Interaction as IComponentInteraction;
                var channel = Context.Guild.GetForumChannel(1157007187630100531);
                var threadChannel = await channel.CreatePostAsync($"{socketUser.Nickname} Application {DateTime.Now.ToString("M/d/yyyy")}", ThreadArchiveDuration.OneWeek, null, $"<@{socketUser.Id}> Please post any related info for your application from <#880611577236164628>");
                var user = Context.Guild.GetUser(socketUser.Id);

                if (user is not null)
                {
                  await threadChannel.AddUserAsync(user).ConfigureAwait(false);
                  await threadChannel.AddUserAsync(Context.User as IGuildUser).ConfigureAwait(false);

                  var embed = new EmbedBuilder();

                  embed.WithTitle($"{socketUser.Nickname}");
                  embed.AddField("PVE Total Fame:", $"{detailedplayerinfo.LifetimeStatistics.PvE.Total:n0}", true);
                  embed.AddField("Fame for Killing Players", $"{detailedplayerinfo.KillFame:n0}", true);
                  embed.AddField("Fame for Killing Mobs:", $"{detailedplayerinfo.DeathFame:n0}", true);
                  embed.AddField("Fame for Gathering:", $"{detailedplayerinfo.LifetimeStatistics.Gathering.All.Total:n0}", true);
                  embed.AddField("Fame for Crafting:", $"{detailedplayerinfo.LifetimeStatistics.Crafting.Total:n0}", true);
                  embed.AddField("Fishing Fame:", $"{detailedplayerinfo.LifetimeStatistics.FishingFame:n0}", true);
                  embed.AddField("Infamy: Corrupted Dungeons", $"{detailedplayerinfo.LifetimeStatistics.PvE.CorruptedDungeon:n0}", true);
                  embed.AddField("Infamy: Hellgates:", $"{detailedplayerinfo.LifetimeStatistics.PvE.Hellgate:n0}", true);
                  embed.AddField("Mists:", $"{detailedplayerinfo.LifetimeStatistics.PvE.Mists:n0}");
                  embed.AddField("Outlands:", $"{detailedplayerinfo.LifetimeStatistics.PvE.Outlands:n0}");
                  embed.AddField("Avalon:", $"{detailedplayerinfo.LifetimeStatistics.PvE.Avalon:n0}");

                  var approveButton = new ButtonBuilder()
                  {
                    Label = "Accept",
                    CustomId = "acceptapplicant",
                    Style = ButtonStyle.Success
                  };
                  var denyButton = new ButtonBuilder()
                  {
                    Label = "Deny",
                    CustomId = "denyApplicant",
                    Style = ButtonStyle.Danger
                  };

                  var component = new ComponentBuilder();
                  component.WithButton(approveButton);
                  component.WithButton(denyButton);

                  await threadChannel.SendMessageAsync($"Here's some in-game info I found for {socketUser.Nickname}", embed: embed.Build(), components: component.Build());
                  await FollowupAsync("Application created", ephemeral: true);
                }
                else
                {
                  Console.WriteLine("USER NOT FOUND DURING GET USER LOOKUP. Possibly bad ID");
                }
              }
              else
              {
                await RecruitersModChannel.SendMessageAsync($"{socketUser.DisplayName} application was auto denied. They don't meet the fame requirments of {iRecruitmentKillFame:n0}. ");

                await socketUser.SendMessageAsync($"Your application to Free Beer has been denied for now until you meet fame requirements of {iRecruitmentKillFame:n0}.");
              }
            }
            else
            {
              await RespondAsync("I couldn't find you. Update your server name to match your in-game name. If there's still an issues please reach out to a recruiter in #lobby", ephemeral: true);
            }
          }
          catch (Exception ex)
          {
            Console.WriteLine(ex.Message);
            await RespondAsync("Yeah applying ain't gonna work for ya. Please reach out to a recruiter in #lobby", ephemeral: true);
          }
        }
        else
        {
          await RespondAsync("Please change your Free Beer server nickname to match your in-game name. You can adjust that in your discord profile settings", ephemeral: true);
        }
      }
      else
      {
        await RespondAsync("Yeah applying ain't gonna work for ya. Please reach out to a recruiter in #lobby", ephemeral: true);
      }
    }

    
    [ComponentInteraction("denyApplicant")]
    public async Task DenyApplicant()
    {
      var socketUser = (SocketGuildUser)Context.User;

      if (socketUser.Roles.Any(r => r.Id == OfficerRoleID) || socketUser.Roles.Any(r => r.Id == RecruitersRoleID))
      {
        IMessageChannel RecruitersModChannel = Context.Client.GetChannel(RecruitersModChannelID) as IMessageChannel;
        await DeferAsync();

        var componentInteraction = Context.Interaction as IComponentInteraction;
        var channel = Context.Guild.GetForumChannel(1157007187630100531);
        RegearModule regearModule = new RegearModule();

        string ApplicantsName = componentInteraction.Message.Embeds.FirstOrDefault().Title.ToString();
        ulong DeniedApplicantID = regearModule.GetRegearPosterID(ApplicantsName, Context);
        var SocketApplicantUser = Context.Guild.GetUser(DeniedApplicantID);
        await SocketApplicantUser.RemoveRoleAsync(ApplicantRoleID);

        await SocketApplicantUser.SendMessageAsync($"{ApplicantsName} Your application to Free Beer has been denied for now.");

        await RecruitersModChannel.SendMessageAsync($"THIS IS A TEST: {ApplicantsName} application been denied by {socketUser.DisplayName}. Link to application: https://discord.com/channels/335894087397933056/{componentInteraction.ChannelId}");

        await FollowupAsync($"{ApplicantsName} application has been denied on {DateTime.Today.ToString("M/d/yyyy")}");
      }
      else
      {

      }
      

    }

    [ComponentInteraction("acceptapplicant")]
    async Task AcceptApplicant()
    {
      var socketUser = (SocketGuildUser)Context.User;

      if (socketUser.Roles.Any(r => r.Id == OfficerRoleID) || socketUser.Roles.Any(r => r.Id == RecruitersRoleID))
      {
        //await DeferAsync();
        IMessageChannel RecruitersModChannel = Context.Client.GetChannel(RecruitersModChannelID) as IMessageChannel;

        var componentInteraction = Context.Interaction as IComponentInteraction;

        string ApplicantsName = componentInteraction.Message.Embeds.FirstOrDefault().Title.ToString();

        RegearModule regearModule = new RegearModule();

        ulong AcceptedApplicantID = regearModule.GetRegearPosterID(ApplicantsName, Context);
        var SocketApplicantUser = Context.Guild.GetUser(AcceptedApplicantID);

        await socketUser.RemoveRoleAsync(ApplicantRoleID);

        await RecruitersModChannel.SendMessageAsync($"THIS IS A TEST: {ApplicantsName} has been accepted and auto registred by {socketUser.DisplayName}. Link to application: https://discord.com/channels/335894087397933056/{componentInteraction.ChannelId}");
        await RespondAsync($"{ApplicantsName} application has been accepted on {DateTime.Today.ToString("M/d/yyyy")}"); //Creates unknownwebhook same interaction twice
        await Register(SocketApplicantUser, ApplicantsName);
      }
    }

    [ComponentInteraction("getrole*")]
    public async Task GetRole()
    {
      var interaction = Context.Interaction as IComponentInteraction;
      IComponentInteraction ButtonInteraction = Context.Interaction as IComponentInteraction;

      string sGameName = ButtonInteraction.Message.Embeds.FirstOrDefault().Title.ToString();
      ulong roleID = Convert.ToUInt64(ButtonInteraction.Message.Embeds.FirstOrDefault().Fields[2].Value.ToString());




      var user = Context.Guild.GetUser(Context.User.Id);


      if (!user.Roles.Any(r => r.Name == sGameName))
      {
        await user.AddRoleAsync(roleID);
        await Context.User.SendMessageAsync($"You have been granted the {sGameName} role");
        await DeferAsync();
      }
      else
      {
        await user.RemoveRoleAsync(roleID);
        await Context.User.SendMessageAsync($"{sGameName} role has been removed");
        await DeferAsync();
      }


      var membercount = Context.Guild.GetRole(roleID).Members.Count();

      EmbedBuilder previousEmbed = new EmbedBuilder();
      var previousButtons = ComponentBuilder.FromComponents(ButtonInteraction.Message.Components);

      previousEmbed.Title = ButtonInteraction.Message.Embeds.FirstOrDefault().Title.ToString();
      previousEmbed.AddField("Role Name", ButtonInteraction.Message.Embeds.FirstOrDefault().Fields[0].Value.ToString(), true);
      previousEmbed.AddField("Current Members", membercount, true);
      previousEmbed.AddField("Game ID:", ButtonInteraction.Message.Embeds.FirstOrDefault().Fields[2].Value.ToString(), true);

      var fieldtest = interaction.Message.Embeds.FirstOrDefault().Fields[1].Value;

      await Context.Interaction.ModifyOriginalResponseAsync((x) =>
      {
        x.Embed = previousEmbed.Build();
        x.Components = previousButtons.Build();
      });

    }

    [SlashCommand("register-guild-to-alliance", "Register a guild to the alliance")]
    public async Task RegisterGuild(string Guild_Name)
    {
      //await DeferAsync();
      GuildDataHandler.GuildInfo guildData = await new GuildDataHandler().GetGuildSearchInfo(Context, Guild_Name);
      if (AllianceGuildGuidID == guildData.AllianceId)
      {
        dataBaseService = new DataBaseService();
        await dataBaseService.RegisterGuild(new RegisteredAllianceGuilds
        {
          GuildID = guildData.Id,
          GuildName = guildData.Name,
          DateRegistered = DateTime.Today,
          KillFame = (int)guildData.killFame
        });
        await RespondAsync($"Guild {guildData.Name} has been registered to the Free Beer alliance", ephemeral: false);

      }
      else
      {
        await RespondAsync("Registration failed. Can't find guild or guild is not currently in Free Beer alliance", ephemeral: true);
      }
    }


    [SlashCommand("register-to-allaince", "Register yourself to the alliance")]
    public async Task RegisterToAllaince()
    {
      string? sUserNickname = ((Context.Interaction.User as SocketGuildUser).DisplayName != null) ? (Context.Interaction.User as SocketGuildUser).DisplayName : Context.Interaction.User.Username;
      var socketUser = (SocketGuildUser)Context.User;

      PlayerLookupInfo playerInfo = new PlayerLookupInfo();
      PlayerDataLookUps albionData = new PlayerDataLookUps();

      var tempRoleIDReign = Context.Guild.GetRole(1128714322860843068);
      var tempRolIDFreeBeer = Context.Guild.GetRole(1128714260386689075);
      var tempRoleIDAeternums = Context.Guild.GetRole(1140453707058778182);
      var tempRoleIDAlpacasOnYourBack = Context.Guild.GetRole(1129090950971535500);

      var tempGuildIDReign = "gbwnj2Z2TFiImf3gAPTgRg";
      var tempGuildIDFreeBeer = "9ndyGFTPT0mYwPOPDXDmSQ";
      var tempGuildIDAeternums = "gbwnj2Z2TFiImf3gAPTgRg";
      string tempGuildIDAlpacasOnYourBack = "asUiraoQS02Rf7ipBjKo_g";

      var AllianceID = "VnJPzLDbROy3rdfbc28L_w";
      string GuildTag = "NA";
      dataBaseService = new DataBaseService();

      await _logger.Log(new LogMessage(LogSeverity.Info, "Alliance Register", $"User: {Context.User.Username}, Command: register-alliance", null));

      try
      {
        playerInfo = await albionData.GetPlayerInfo(Context, sUserNickname);

        await DeferAsync();
        if (await dataBaseService.CheckForRegisteredGuild(playerInfo.GuildId) && IsPlayerInAlliance(playerInfo.AllianceId))
        {
          if (playerInfo.Name == sUserNickname)
          {
            if (!await dataBaseService.CheckAlliancePlayerIsExist(playerInfo.Id))
            {
              if (playerInfo.GuildId == tempGuildIDFreeBeer)
              {
                await socketUser.AddRoleAsync(tempRolIDFreeBeer);
                GuildTag = $"[Free] {playerInfo.Name}";
              }
              else if (playerInfo.GuildId == tempGuildIDReign)
              {
                await socketUser.AddRoleAsync(tempRoleIDReign);
                GuildTag = $"[REIGN] {playerInfo.Name}";
              }
              else if (playerInfo.GuildId == tempGuildIDAeternums)
              {
                await socketUser.AddRoleAsync(tempRoleIDAeternums);
                GuildTag = $"[Aeter] {playerInfo.Name}";
              }
              else if (playerInfo.GuildId == tempGuildIDAlpacasOnYourBack)
              {
                await socketUser.AddRoleAsync(tempRoleIDAlpacasOnYourBack);
                GuildTag = $"[Alpacas] {playerInfo.Name}";

              }

              try
              {
                await socketUser.ModifyAsync(x => x.Nickname = GuildTag);
              }
              catch (Exception ex)
              {
                Console.WriteLine($"Modifying guild tag into server nickname failed. User: {playerInfo.Name}");
              }


              await dataBaseService.RegisterAlliancePlayerInfo(new RegisteredAllianceMembers
              {
                PlayerID = playerInfo.Id,
                PlayerName = playerInfo.Name,
                GuildID = playerInfo.GuildId,
                GuildName = playerInfo.GuildName,
                AllianceID = playerInfo.AllianceId,
                AllianceName = playerInfo.AllianceName,
                DateRegistered = DateTime.Today,
                KillFame = playerInfo.KillFame
              });

              await FollowupAsync($"<@{socketUser.Id}> in guild {playerInfo.GuildName} has been registed to the Alliance.");
            }
            else
            {
              await FollowupAsync("Registration failed. You're already registered to the Alliance");
            }
          }
          else
          {
            await FollowupAsync("Registration failed. Your discord name must match EXACTLY to your in-game name");
          }
        }
        else
        {
          await FollowupAsync("Registration failed. Your guild is not registered to the Alliance.");
        }
      }
      catch (Exception ex)
      {
        await RespondAsync("Registration failed. Can't find you or your discord name doesn't match your in-game name.", ephemeral: true);
      }


    }
    [SlashCommand("remove-guild-from-alliance", "Removes a guild from the alliance")]
    public async Task UnRegisterGuild(string Guild_Name)
    {


    }

    [SlashCommand("unregister-user-from-allaince", "Removes a user from the alliance")]
    public async Task UnRegisterAllianceMember(string Player_Name, SocketGuildUser? DiscordUser = null)
    {

      PlayerDataLookUps albionData = new PlayerDataLookUps();
      PlayerLookupInfo playerInfo = new PlayerLookupInfo();

      try
      {
        playerInfo = await albionData.GetPlayerInfo(Context, Player_Name);

        if (DiscordUser != null)
        {
          foreach (var roles in DiscordUser.Roles)
          {
            if (roles.Name != "@everyone")
            {
              await DiscordUser.RemoveRoleAsync(roles.Id);
            }
          }
        }

        dataBaseService = new DataBaseService();
        dataBaseService.DeleteRegisteredAlliancePlayer(playerInfo.Id);
        await RespondAsync($"Member {playerInfo.Name} has been de-registered from the allaince", ephemeral: true);
      }
      catch
      {
        await RespondAsync("De-register failed. Incorrect name or they don't exist in database", ephemeral: true);
      }

    }


    [SlashCommand("view-alliance-stats", "Check out alliance stats")]
    public async Task GetAllianceStats()
    {
      PlayerDataLookUps albionData = new PlayerDataLookUps();

      List<int> BattleIDs = new List<int>();
      //BattleIDs.Add(966563738); //adding BattleBoard ID's to list
      BattleIDs.Add(966557264);

      List<PlayerDataHandler.Rootobject> eventData = new List<PlayerDataHandler.Rootobject>();

      foreach (int Battle in BattleIDs)
      {
        eventData.Add(await albionData.GetAlbionEventInfo(Battle));
      }

      //Filter All Kills and deaths in events
      foreach (var Event in eventData) 
      {
        Console.WriteLine(Event.EventId);
      }

    }

    private bool IsPlayerGuildInAlliance(string guildID)
    {
      string tempGuildIDReign = "gbwnj2Z2TFiImf3gAPTgRg";
      string tempGuildIDFreeBeer = "9ndyGFTPT0mYwPOPDXDmSQ";
      //string tempGuildIDWarriorsCompany = "yD2A0-UjQgG6swbTkOrqiQ";
      string tempGuildIDAlpacasOnYourBack = "asUiraoQS02Rf7ipBjKo_g";

      if (guildID == tempGuildIDReign || guildID == tempGuildIDFreeBeer || guildID == tempGuildIDAlpacasOnYourBack)
      {
        return true;
      }
      return false;
    }

    private bool IsPlayerInAlliance(string a_sAllianceID)
    {
      var AllianceID = "VnJPzLDbROy3rdfbc28L_w";

      if (a_sAllianceID == AllianceID)
      {
        return true;
      }
      return false;
    }
  }
}
