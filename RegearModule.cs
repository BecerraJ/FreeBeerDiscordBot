using CoreHtmlToImage;
using Discord;
using Discord.Interactions;
using Discord.Rest;
using Discord.WebSocket;
using DiscordBot.Enums;
using DiscordBot.Models;
using DiscordBot.Services;
using MarketData;
using PlayerData;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;
using static PlayerData.PlayerDataHandler;

namespace DiscordBot.RegearModule
{
  public class RegearModule
  {
    private DataBaseService dataBaseService;

    private int goldTierRegearCap = int.Parse(System.Configuration.ConfigurationManager.AppSettings.Get("GoldTierRegearPriceCap"));
    private int silverTierRegearCap = int.Parse(System.Configuration.ConfigurationManager.AppSettings.Get("SilverTierRegearPriceCap"));
    private int bronzeTierRegearCap = int.Parse(System.Configuration.ConfigurationManager.AppSettings.Get("BronzeTierRegearPriceCap"));
    private int shitTierRegearCap = int.Parse(System.Configuration.ConfigurationManager.AppSettings.Get("DefaultTierSubmissionCap"));
    private int mountCap = int.Parse(System.Configuration.ConfigurationManager.AppSettings.Get("MountPriceCap"));

    private int iTankMinimumIP = int.Parse(System.Configuration.ConfigurationManager.AppSettings.Get("TankMinimumIP"));
    private int iDPSMinimumIP = int.Parse(System.Configuration.ConfigurationManager.AppSettings.Get("DPSMinimumIP"));
    private int iHealerMinmumIP = int.Parse(System.Configuration.ConfigurationManager.AppSettings.Get("HealerMinmumIP"));
    private int iSupportMinimumIP = int.Parse(System.Configuration.ConfigurationManager.AppSettings.Get("SupportMinimumIP"));

    private ulong roleIdNewRecruit = ulong.Parse(System.Configuration.ConfigurationManager.AppSettings.Get("newRecruit"));
    private ulong roleIdMember = ulong.Parse(System.Configuration.ConfigurationManager.AppSettings.Get("member"));
    private ulong roleIdOfficer = ulong.Parse(System.Configuration.ConfigurationManager.AppSettings.Get("officer"));
    private ulong roleIdVeteran = ulong.Parse(System.Configuration.ConfigurationManager.AppSettings.Get("veteran"));

    private static ulong TankMentorID = ulong.Parse(System.Configuration.ConfigurationManager.AppSettings.Get("TankMentorID"));
    private static ulong HealerMentorID = ulong.Parse(System.Configuration.ConfigurationManager.AppSettings.Get("HealerMentorID"));
    private static ulong DPSMentorID = ulong.Parse(System.Configuration.ConfigurationManager.AppSettings.Get("DPSMentorID"));
    private static ulong SupportMentorID = ulong.Parse(System.Configuration.ConfigurationManager.AppSettings.Get("SupportMentorID"));

    public double TotalRegearSilverAmount { get; set; }
    public ulong RegearQueueID { get; set; }
    public int KillID { get; set; }
    private string regearRoleIcon { get; set; }

    public async Task PostRegear(SocketInteractionContext command, PlayerDataHandler.Rootobject a_EventData, string partyLeader, EventTypeEnum a_eEventType, MoneyTypes moneyTypes, SocketGuildUser? a_Mentor)
    {
      var chnl = command.Client.GetChannel(ulong.Parse(System.Configuration.ConfigurationManager.AppSettings.Get("regearTeamChannelId"))) as IMessageChannel;
      var marketDataAndGearImg = await GetMarketDataAndGearImg(command, a_EventData);

      var converter = new HtmlConverter();
      var html = marketDataAndGearImg[0];
      var bytes = converter.FromHtmlString(html);

      RegearQueueID = command.Interaction.Id;
      KillID = a_EventData.EventId;

      var approveButton = new ButtonBuilder()
      {
        Label = "Approve",
        CustomId = "approve",
        Style = ButtonStyle.Success
      };
      var denyButton = new ButtonBuilder()
      {
        Label = "Deny",
        CustomId = "deny",
        Style = ButtonStyle.Danger
      };
      var auditButton = new ButtonBuilder()
      {
        Label = "Audit",
        CustomId = "audit",
        Style = ButtonStyle.Secondary
      };

      var component = new ComponentBuilder();
      component.WithButton(approveButton);
      component.WithButton(denyButton);
      component.WithButton(auditButton);

      try
      {
        //dataBaseService = new DataBaseService();
        //var player = dataBaseService.GetPlayerInfoByName(a_EventData.Victim.Name);
        //var moneyType = dataBaseService.GetMoneyTypeByName(moneyTypes);
        //await dataBaseService.AddPlayerReGear(new PlayerLoot
        //{
        //  TypeId = moneyType.Id,
        //  CreateDate = DateTime.Now,
        //  Loot = Convert.ToDecimal(marketDataAndGearImg[1]),
        //  PlayerId = player.Id,
        //  Message = " Regear(s) have been processed.  Has been added to your account. Please emote :beers: to confirm",
        //  PartyLeader = partyLeader,
        //  KillId = a_EventData.EventId.ToString(),
        //  Reason = a_eEventType.ToString(),
        //  QueueId = "0"
        //});

        var commandUser = command.Guild.GetUser(command.User.Id);

        using (MemoryStream imgStream = new MemoryStream(bytes))
        {
          var embed = new EmbedBuilder()
                          .WithTitle($" {regearRoleIcon} Regear Submission from {a_EventData.Victim.Name} {regearRoleIcon}")
                          .AddField("KillID: ", a_EventData.EventId, true)
                          .AddField("Victim", a_EventData.Victim.Name, true)
                          .AddField("Killer", "[" + a_EventData.Killer.AllianceName + "] " + "[" + a_EventData.Killer.GuildName + "] " + a_EventData.Killer.Name, true)
                          .AddField("Caller Name: ", partyLeader, true)
                          .AddField("Refund Amount: ", TotalRegearSilverAmount, true)
                          .AddField("Death Average IP ", a_EventData.Victim.AverageItemPower, true)
                          .AddField("Discord User ID: ", command.User.Id, true)
                          //.AddField("Discord Username", command.User.Username, true)
                          .AddField("Event Type", a_eEventType, true)
                          .WithUrl($"https://albiononline.com/en/killboard/kill/{a_EventData.EventId}")
                          .WithCurrentTimestamp()
                          .WithImageUrl($"attachment://image.jpg");

          CheckRegearRequirments(a_EventData, out bool requirementsMet, out string? errorMessage);

          if (!requirementsMet)
          {
            embed.WithDescription($"<a:red_siren:1050052736206508132> WARNING: {errorMessage} <a:red_siren:1050052736206508132> ");
            embed.Color = Color.Red;
          }
          else if (!UserHaveRegearRole(command))
          {
            embed.WithDescription($"<a:red_siren:1050052736206508132>  THIS MEMBER DOESN'T HAVE REGEAR ROLES  <a:red_siren:1050052736206508132> ");
            embed.Color = Color.Red;
          }
          else
          {
            embed.WithDescription($":thumbsup:  Requreiments Met: This regear meets Free Beer Standards  :thumbsup: ");
            embed.Color = Color.Green;
          }

          if (a_EventData.Victim.Inventory.Any(x => x != null && x.Type == "UNIQUE_GVGTOKEN_GENERIC"))
          {
            embed.AddField("OC in Bag", $"Amount: {a_EventData.Victim.Inventory.Where(x => x != null && x.Type == "UNIQUE_GVGTOKEN_GENERIC").FirstOrDefault().Count}", true);
          }

          if (a_Mentor != null)
          {
            embed.AddField("Mentor", (a_Mentor.Nickname != null) ? new PlayerDataLookUps().CleanUpShotCallerName(a_Mentor.Nickname) : a_Mentor.Username, true);
            await a_Mentor.SendMessageAsync($"Your mentee {a_EventData.Victim.Name} has submitted a regear. https://discord.com/channels/335894087397933056/1047554594420564130/{command.Interaction.Id}");
          }

          //if (a_EventData.Victim.Inventory.Any(x => x != null && x.Type.Contains("TOOL") || x.Type.Contains("GATHERER")))
          //{
          //  embed.AddField("Has Gather equipment", "yes", true);
          //}

          await chnl.SendFileAsync(imgStream, "image.jpg", null, false, embed.Build(), null, false, null, null, components: component.Build());

        }
      }
      catch (Exception ex)
      {
        throw;
      }

    }
    public async Task PostOCRegear(SocketInteractionContext command, List<string> items, string a_sPartyLeader, MoneyTypes a_eMoneyTypes, EventTypeEnum a_eEventTypes)
    {
      ulong id = ulong.Parse(System.Configuration.ConfigurationManager.AppSettings.Get("regearTeamChannelId"));

      var chnl = command.Client.GetChannel(id) as IMessageChannel;

      string? sUserNickname = ((command.User as SocketGuildUser).Nickname != null) ? new PlayerDataLookUps().CleanUpShotCallerName((command.User as SocketGuildUser).Nickname) : command.User.Username;

      var marketData = await GetMarketDataForOCRegear(command, items);

      RegearQueueID = command.Interaction.Id;

      var converter = new HtmlConverter();
      var html = marketData[0];
      var bytes = converter.FromHtmlString(html);

      var approveButton = new ButtonBuilder()
      {
        Label = "Approve",
        CustomId = "oc-approve",
        Style = ButtonStyle.Success
      };
      var denyButton = new ButtonBuilder()
      {
        Label = "Deny",
        CustomId = "oc-deny",
        Style = ButtonStyle.Danger
      };

      var component = new ComponentBuilder();
      component.WithButton(approveButton);
      component.WithButton(denyButton);

      try
      {
        //dataBaseService = new DataBaseService();
        //var player = dataBaseService.GetPlayerInfoByName(sUserNickname);
        //var moneyType = dataBaseService.GetMoneyTypeByName(a_eMoneyTypes);
        //await dataBaseService.AddPlayerReGear(new PlayerLoot
        //{
        //    TypeId = moneyType.Id,
        //    CreateDate = DateTime.Now,
        //    Loot = Convert.ToDecimal(marketData[1]),
        //    PlayerId = player.Id,
        //    Message = " Regear(s) have been processed.  Has been added to your account. Please emote :beers: to confirm",
        //    PartyLeader = a_sPartyLeader,
        //    KillId = "NA",
        //    Reason = a_eEventTypes.ToString(),
        //    QueueId = RegearQueueID.ToString()
        //});


        using (MemoryStream imgStream = new MemoryStream(bytes))
        {
          var embed = new EmbedBuilder()
                              .WithTitle($" {regearRoleIcon} OC Submission from {sUserNickname}{regearRoleIcon}")
                              .AddField("Victim", sUserNickname, true)
                              .AddField("Caller Name: ", a_sPartyLeader, true)
                              .AddField("Refund Amount: ", TotalRegearSilverAmount, true)
                              .AddField("Event Type: ", a_eEventTypes, true)
                              .AddField("QueueID", command.Interaction.Id, true)
                              .AddField("Discord User ID: ", command.User.Id, true)
                              .WithImageUrl($"attachment://image.jpg");

          await command.Channel.SendFileAsync(imgStream, "image.jpg", $"Regear Submission from {command.User} ", false, embed.Build(), null, false, null, null, components: component.Build());
        }
      }
      catch (Exception ex)
      {
        throw;
      }
    }

    public bool CheckIfPlayerHaveReGearIcon(SocketInteractionContext socketInteractionUser)
    {
      ulong GoldTierID = Convert.ToUInt64(System.Configuration.ConfigurationManager.AppSettings.Get("GoldTierRegear"));
      ulong SilverTierID = Convert.ToUInt64(System.Configuration.ConfigurationManager.AppSettings.Get("SilverTierRegear"));
      ulong BronzeTierID = Convert.ToUInt64(System.Configuration.ConfigurationManager.AppSettings.Get("BronzeTierRegear"));

      if (socketInteractionUser.User is SocketGuildUser guildUser)
      {
        if (guildUser.Roles.Any(r => r.Name == "Silver Tier Regear - Elligible" || r.Name == "Gold Tier Regear - Elligible" || r.Name == "Bronze Tier Regear - Elligible") || guildUser.Roles.Any(r => r.Id == GoldTierID || r.Id == SilverTierID))
        {
          return true;
        }
      }
      return true;

    }

    public async Task<List<string>> GetMarketDataAndGearImg(SocketInteractionContext command, PlayerDataHandler.Rootobject a_Playerdata)
    {
      double returnValue = 0;
      string sMarketLocation = System.Configuration.ConfigurationManager.AppSettings.Get("chosenCityMarket"); //If field is null, all selling locations market data will be pulled
      var guildUser = (SocketGuildUser)command.User;
      var regearIconType = "";
      PlayerDataHandler.Equipment1 victimEquipment = a_Playerdata.Victim.Equipment;

      List<string> equipmentList = new List<string>();
      List<Equipment> submittedRegearItems = new List<Equipment>();
      List<string> notAvailableInMarketList = new List<string>();

      if (victimEquipment.Head != null)
      {
        equipmentList.Add($"{victimEquipment.Head.Type + $"?Locations={sMarketLocation}&qualities=" + victimEquipment.Head.Quality}");
        submittedRegearItems.Add(new Equipment
        {
          Image = $"https://render.albiononline.com/v1/item/{victimEquipment.Head.Type + "?quality=" + victimEquipment.Head.Quality}",
          Type = "HEAD"
        });
      }

      if (victimEquipment.MainHand != null)
      {
        equipmentList.Add($"{victimEquipment.MainHand.Type + $"?Locations={sMarketLocation}&qualities=" + victimEquipment.MainHand.Quality}");
        submittedRegearItems.Add(new Equipment
        {
          Image = $"https://render.albiononline.com/v1/item/{victimEquipment.MainHand.Type + "?quality=" + victimEquipment.MainHand.Quality}",
          Type = "MAIN"
        });
      }
      if (victimEquipment.OffHand != null)
      {
        equipmentList.Add($"{victimEquipment.OffHand.Type + $"?Locations={sMarketLocation}&qualities=" + victimEquipment.OffHand.Quality}");
        submittedRegearItems.Add(new Equipment
        {
          Image = $"https://render.albiononline.com/v1/item/{victimEquipment.OffHand.Type + "?quality=" + victimEquipment.OffHand.Quality}",
          Type = "OFF"
        });
      }

      if (victimEquipment.Armor != null)
      {
        equipmentList.Add($"{victimEquipment.Armor.Type + $"?Locations={sMarketLocation}&qualities=" + victimEquipment.Armor.Quality}");
        submittedRegearItems.Add(new Equipment
        {
          Image = $"https://render.albiononline.com/v1/item/{victimEquipment.Armor.Type + "?quality=" + victimEquipment.Armor.Quality}",
          Type = "ARMOR"
        });
      }

      if (victimEquipment.Shoes != null)
      {
        equipmentList.Add($"{victimEquipment.Shoes.Type + $"?Locations={sMarketLocation}&qualities=" + victimEquipment.Shoes.Quality}");
        submittedRegearItems.Add(new Equipment
        {
          Image = $"https://render.albiononline.com/v1/item/{victimEquipment.Shoes.Type + "?quality=" + victimEquipment.Shoes.Quality}",
          Type = "SHOES"
        });
      }

      if (victimEquipment.Cape != null)
      {
        equipmentList.Add($"{victimEquipment.Cape.Type + $"?Locations={sMarketLocation}&qualities=" + victimEquipment.Cape.Quality}");
        submittedRegearItems.Add(new Equipment
        {
          Image = $"https://render.albiononline.com/v1/item/{victimEquipment.Cape.Type + "?quality=" + victimEquipment.Cape.Quality}",
          Type = "CAPE"
        });
      }
      if (victimEquipment.Mount != null)
      {
        equipmentList.Add($"{victimEquipment.Mount.Type + $"?Locations={sMarketLocation}&qualities=" + victimEquipment.Mount.Quality}");
        submittedRegearItems.Add(new Equipment
        {
          Image = $"https://render.albiononline.com/v1/item/{victimEquipment.Mount.Type + "?quality=" + victimEquipment.Mount.Quality}",
          Type = "MOUNT"
        });
      }

      foreach (var item in equipmentList)
      {
        string itemType = (item.Split('_')[1] == "2H") ? "MAIN" : item.Split('_')[1];
        double equipmentFetchPrice = 0;

        Equipment underRegearItem = submittedRegearItems.Where(x => itemType.Contains(x.Type)).FirstOrDefault();
        MarketDataFetching marketDataFetching = new MarketDataFetching();

        //Check for Current Price
        List<EquipmentMarketData> marketDataCurrent = await marketDataFetching.GetMarketPriceCurrentAsync(item);

        if (marketDataCurrent == null || marketDataCurrent.Where(x => x.sell_price_min == 0).Count() == 0)
        {
          //Check for daily average price
          List<AverageItemPrice> marketDataDaily = await marketDataFetching.GetMarketPriceDailyAverage(item);

          if (marketDataDaily == null || marketDataDaily.Where(x => x.data != null).Count() == 0)
          {
            //Check for monthly average price
            List<EquipmentMarketDataMonthylyAverage> marketDataMonthly = await marketDataFetching.GetMarketPriceMonthlyAverage(item);
            if (marketDataMonthly == null || marketDataMonthly.Where(x => x.data != null).Count() == 0)
            {
              notAvailableInMarketList.Add(marketDataCurrent.FirstOrDefault().item_id.Replace('_', ' ').Replace('@', '.'));
              underRegearItem.ItemPrice = "$0 (Not Found)";
            }
            else
            {
              //get monthly prices
              equipmentFetchPrice = FetchItemPrice(marketDataMonthly, out string? errorMessage);
              underRegearItem.ItemPrice = (errorMessage == null) ? "$" + equipmentFetchPrice.ToString("N0") : errorMessage;
            }
          }
          else
          {
            //get daily prices
            equipmentFetchPrice = FetchItemPrice(marketDataDaily, out string? errorMessage);
            underRegearItem.ItemPrice = (errorMessage == null) ? "$" + equipmentFetchPrice.ToString("N0") : errorMessage;
          }
        }
        else
        {
          //get current prices
          equipmentFetchPrice = FetchItemPrice(marketDataCurrent, out string? errorMessage);
          underRegearItem.ItemPrice = (errorMessage == null) ? "$" + equipmentFetchPrice.ToString("N0") : errorMessage;
        }

        //if mount is T5 or higher and exceed cap amount
        if (underRegearItem.Type != null && underRegearItem.Type == "MOUNT" && equipmentFetchPrice > mountCap)
        {
          equipmentFetchPrice = MountRegearCap(equipmentFetchPrice, victimEquipment);
          underRegearItem.ItemPrice = "$" + equipmentFetchPrice.ToString("N0") + (" (CAP REACHED)");
        }

        returnValue += equipmentFetchPrice;
      }


      if (guildUser.Roles.Any(r => r.Name == "Gold Tier Regear - Eligible")) // Role ID 1049889855619989515
      {
        returnValue = Math.Min(goldTierRegearCap, returnValue);
        regearIconType = "Gold Tier Regear - Eligible";
        regearRoleIcon = "<:FreeBeerGoldCreditCard:1071162762056708206>";
      }
      else if (guildUser.Roles.Any(r => r.Name == "Silver Tier Regear - Eligible")) //ROLE ID 970083338591289364
      {
        returnValue = Math.Min(silverTierRegearCap, returnValue);
        regearIconType = "Silver Tier Regear - Eligible";
        regearRoleIcon = "<:FreeBeerSilverCreditCard:1071163029493919905>";
      }
      else if (guildUser.Roles.Any(r => r.Name == "Bronze Tier Regear - Eligible")) //Role ID 970083088241672245
      {
        returnValue = Math.Min(bronzeTierRegearCap, returnValue);
        regearIconType = "Bronze Tier Regear - Eligible";
        regearRoleIcon = "<:FreeBeerBronzeCreditCard:1072023947899576412> ";
      }
      else if (guildUser.Roles.Any(r => r.Name == "Free Regear - Eligible")) // Role ID 1052241667329118349
      {

        returnValue = Math.Min(shitTierRegearCap, returnValue);
        regearIconType = "Free Regear - Eligible";
        regearRoleIcon = "<:FreeRegearToken:1052241548856791040> ";
      }
      else
      {
        returnValue = Math.Min(shitTierRegearCap, returnValue);
        regearIconType = "NOT ELIGIBLE FOR REGEAR";
        regearRoleIcon = ":poop:";
      }

      TotalRegearSilverAmount = returnValue;

      var gearImage = $"<div style='background-color: #c7a98f;'> <div> <center><h3>Regear Submitted By {a_Playerdata.Victim.Name} ({regearIconType})</h3>";
      foreach (var item in submittedRegearItems)
      {
        gearImage += $"<div style='display: inline-block;line-height: .1;'>" +
            $"<img style='width:150px;height:150px' src='{item.Image}'/>" +
            $"<p >{item.ItemPrice}</p></div>";
      }

      gearImage += $"<div style='font-weight : bold;'>Refund amount. : {returnValue.ToString("N0")}</div></center></div>";

      gearImage += $"<center><br/>";
      if (notAvailableInMarketList.Count() != 0)
      {
        gearImage += $"<center><br/><h3> Items not found or price is too high </h3>";
      }

      foreach (var item in notAvailableInMarketList)
      {
        gearImage += $"{item}<br/>";
      }

      gearImage += $"</center></div>";

      return new List<string> { gearImage, returnValue.ToString("N0") };
    }
    public async Task<List<string>> GetMarketDataForOCRegear(SocketInteractionContext command, List<string> submittedOCItems)
    {
      var regearIconType = "";
      var guildUser = (SocketGuildUser)command.User;
      string? sUserNickname = ((command.User as SocketGuildUser).Nickname != null) ? (command.User as SocketGuildUser).Nickname : command.User.Username;
      string sMarketLocation = System.Configuration.ConfigurationManager.AppSettings.Get("chosenCityMarket");
      List<Item> items = new List<Item>();
      double returnValue = 0;

      //using (StreamReader r = new StreamReader("Files/items.json"))
      //{
      //    string json = r.ReadToEnd();
      //     items = JsonConvert.DeserializeObject<List<Item>>(json);
      //}

      List<Equipment> underRegearItem = new List<Equipment>();



      foreach (var item in submittedOCItems)
      {
        var itemDes = item.Replace(" ", "");

        MarketDataFetching marketDataFetching = new MarketDataFetching();

        //Check for Current Price
        List<EquipmentMarketData> marketDataCurrent = await marketDataFetching.GetMarketPriceCurrentAsync(itemDes + $"?quality=3&{sMarketLocation}");

        if (marketDataCurrent == null || marketDataCurrent.Where(x => x.sell_price_min != 0).Count() == 0)
        {
          //Check for Daily Average
          List<AverageItemPrice> marketDataDaily = await marketDataFetching.GetMarketPriceDailyAverage(itemDes + $"?quality=3&{sMarketLocation}");

          if (marketDataDaily == null || marketDataDaily.Where(x => x.data != null).Count() == 0)
          {

            //Check for 24 Day Average
            List<EquipmentMarketDataMonthylyAverage> marketDataMonthly = await marketDataFetching.GetMarketPriceMonthlyAverage(itemDes + $"?quality=3&{sMarketLocation}");
            if (marketDataMonthly == null || marketDataMonthly.Where(x => x.data != null).Count() == 0)
            {
              underRegearItem.Add(new Equipment
              {
                Image = $"https://render.albiononline.com/v1/item/{itemDes}?quality=3",
                ItemPrice = "0"
                //ItemPrice = "$0 (Not Found)",
              });
            }
            else
            {
              //get monthly prices
              var equipmentFetchPrice = FetchItemPrice(marketDataMonthly, out string? errorMessage);
              returnValue += equipmentFetchPrice;
              underRegearItem.Add(new Equipment
              {
                Image = $"https://render.albiononline.com/v1/item/{itemDes}?quality=3",
                ItemPrice = equipmentFetchPrice.ToString("N0")
                //ItemPrice = (errorMessage == null) ? "$" + equipmentFetchPrice.ToString("N0") : errorMessage,
              });
            }
          }
          else
          {
            //get daily prices
            var equipmentFetchPrice = FetchItemPrice(marketDataDaily, out string? errorMessage);
            returnValue += equipmentFetchPrice;
            underRegearItem.Add(new Equipment
            {
              Image = $"https://render.albiononline.com/v1/item/{itemDes}?quality=3",
              ItemPrice = equipmentFetchPrice.ToString("N0")
              //ItemPrice = (errorMessage == null) ? "$" + equipmentFetchPrice.ToString("N0") : errorMessage,
            });

          }
        }
        else
        {
          //get current prices
          var equipmentFetchPrice = FetchItemPrice(marketDataCurrent, out string? errorMessage);

          returnValue += equipmentFetchPrice;
          underRegearItem.Add(new Equipment
          {
            Image = $"https://render.albiononline.com/v1/item/{itemDes}?quality=3",
            ItemPrice = equipmentFetchPrice.ToString("N0")
            //ItemPrice = (errorMessage == null) ? "$" + equipmentFetchPrice.ToString("N0") : errorMessage,
          });
        }
      }

      if (guildUser.Roles.Any(r => r.Name == "Gold Tier Regear - Eligible")) // Role ID 1049889855619989515
      {
        returnValue = Math.Min(goldTierRegearCap, returnValue);
        regearIconType = "Gold Tier Regear - Eligible";
        regearRoleIcon = "<:FreeBeerGoldCreditCard:1071162762056708206>";
      }
      else if (guildUser.Roles.Any(r => r.Name == "Silver Tier Regear - Eligible")) //ROLE ID 970083338591289364
      {
        returnValue = Math.Min(silverTierRegearCap, returnValue);
        regearIconType = "Silver Tier Regear - Eligible";
        regearRoleIcon = "<:FreeBeerSilverCreditCard:1071163029493919905> ";
      }
      else if (guildUser.Roles.Any(r => r.Name == "Bronze Tier Regear - Eligible")) //Role ID 970083088241672245
      {
        returnValue = Math.Min(bronzeTierRegearCap, returnValue);
        regearIconType = "Bronze Tier Regear - Eligible";
        regearRoleIcon = "<:FreeBeerBronzeCreditCard:1072023947899576412> ";
      }
      else if (guildUser.Roles.Any(r => r.Name == "Free Regear - Eligible")) // Role ID 1052241667329118349
      {
        returnValue = Math.Min(bronzeTierRegearCap, returnValue);
        regearIconType = "Free Regear - Eligible";
        regearRoleIcon = "<:FreeRegearToken:1052241548856791040> ";
      }
      else
      {
        returnValue = Math.Min(shitTierRegearCap, returnValue);
        regearIconType = "Shit Tier Regear - Eligible";
        regearRoleIcon = ":poop:";
      }

      TotalRegearSilverAmount = returnValue;

      var gearImage = $"<div style='background-color: #c7a98f;'> <div> <center><h3>OC Regear Submitted By {sUserNickname} ({regearIconType})</h3>";
      foreach (var item in underRegearItem)
      {
        gearImage += $"<div style='display: inline-block;line-height: .1;'>" +
            $"<img style='width:150px;height:150px' src='{item.Image}'/>" +
            $"<p >{item.ItemPrice}</p></div>";
      }

      gearImage += $"<div style='font-weight : bold;'>Refund amount. : {returnValue.ToString("N0")}</div></center></div>";

      gearImage += $"<center><br/>";

      gearImage += $"</center></div>";

      return new List<string> { gearImage, returnValue.ToString("N0") };
    }

    private ClassType GetRegearClassType(string a_sGearItem)
    {
      ClassType returnvalue = ClassType.Unknown;

      if (IsRegearTankClass(a_sGearItem))
      {
        returnvalue = ClassType.Tank;
      }
      else if (IsRegearDPSClass(a_sGearItem))
      {
        returnvalue = ClassType.DPS;
      }
      else if (IsRegearHealerClass(a_sGearItem))
      {
        returnvalue = ClassType.Healer;
      }
      else if (IsRegearSupportClass(a_sGearItem))
      {
        returnvalue = ClassType.Support;
      }

      return returnvalue;
    }

    public void CheckRegearRequirments(PlayerDataHandler.Rootobject a_EventData, out bool requirementsMet, out string? ErrorMessage)
    {

      requirementsMet = true;
      ErrorMessage = null;
      ClassType eClassTypeEnum = ClassType.Unknown;


      if (a_EventData.Victim.Equipment.MainHand != null)
      {
        eClassTypeEnum = GetRegearClassType(a_EventData.Victim.Equipment.MainHand.Type.ToString());
      }


      switch (eClassTypeEnum)
      {
        case ClassType.Tank:

          if (a_EventData.Victim.AverageItemPower < iTankMinimumIP)
          {
            ErrorMessage = $"Tank Regear doesn't meet the minimum IP requirments of at least {iTankMinimumIP}";
            requirementsMet = false;
          }

          break;

        case ClassType.DPS:
          if (a_EventData.Victim.AverageItemPower < iDPSMinimumIP)
          {
            ErrorMessage = $"DPS Regear doesn't meet the minimum IP requirments of at least {iDPSMinimumIP}";
            requirementsMet = false;
          }
          break;

        case ClassType.Healer:
          if (a_EventData.Victim.AverageItemPower < iHealerMinmumIP)
          {
            ErrorMessage = $"Healing Regear doesn't meet the minimum IP requirments of at least {iHealerMinmumIP}";
            requirementsMet = false;
          }
          break;

        case ClassType.Support:
          if (a_EventData.Victim.AverageItemPower < iSupportMinimumIP)
          {
            ErrorMessage = $"Support Regear doesn't meet the minimum IP requirments of at least {iSupportMinimumIP}";
            requirementsMet = false;
          }
          break;
        default:
          ErrorMessage = "REGEAR CLASS NOT FOUND.";
          requirementsMet = false;
          break;
      }

      if (a_EventData.Victim.Equipment.Cape != null && a_EventData.Victim.Equipment.Cape.ToString().Contains("@3"))
      {
        ErrorMessage = $"Cape doesn't meet the minimum requriments of being at least 4.3";
        requirementsMet = false;
      }

      string mountTier = a_EventData.Victim.Equipment.Mount.Type.ToString().Split('_')[0];
      if (a_EventData.Victim.Equipment.Mount.Type.ToString().Contains("UNIQUE_MOUNT") || mountTier == "T5" || mountTier == "T6" || mountTier == "T7" || mountTier == "T8")
      {

      }
      else
      {
        ErrorMessage = $"Mount is not T5 equivalent or higher";
        requirementsMet = false;
      }
    }

    private double MountRegearCap(double a_iEquipmentPrice, PlayerDataHandler.Equipment1 a_PlayerEquipment)
    {
      double returnValue = a_iEquipmentPrice;

      string mountTier = a_PlayerEquipment.Mount.Type.ToString().Split('_')[0];

      returnValue = Math.Min(mountCap, a_iEquipmentPrice);

      return returnValue;
    }


    private bool IsRegearTankClass(string a_sGearItem)
    {
      if (a_sGearItem.Contains("HAMMER") || a_sGearItem.Contains("MACE") || a_sGearItem.Contains("KEEPER") || a_sGearItem.Contains("FLAIL"))
      {
        return true;
      }
      return false;
    }

    private bool IsRegearDPSClass(string a_sGearItem)
    {
      if (!IsRegearTankClass(a_sGearItem) && !IsRegearHealerClass(a_sGearItem) && !IsRegearSupportClass(a_sGearItem))
      {
        return true;
      }
      return false;
    }

    private bool IsRegearHealerClass(string a_sGearItem)
    {
      if (a_sGearItem.Contains("HOLY") || a_sGearItem.Contains("NATURE") || a_sGearItem.Contains("WILD") || a_sGearItem.Contains("DIVINE"))
      {
        return true;
      }
      return false;
    }

    private bool IsRegearSupportClass(string a_sGearItem)
    {

      if (a_sGearItem.Contains("ARCANE") || a_sGearItem.Contains("ENIGMATIC") || a_sGearItem.Contains("KEEPER") || a_sGearItem.Contains("ENIGMATICORB") || a_sGearItem.Contains("CURSEDSTAFF"))
      {
        return true;
      }
      return false;
    }

    public double FetchItemPrice<T>(T ItemData, out string? ErrorMessage)
    {
      double returnValue = 0;
      ErrorMessage = null;

      if (ItemData.GetType() == typeof(List<EquipmentMarketData>))
      {
        var marketData = (List<EquipmentMarketData>)(object)ItemData;
        var itemsData = marketData.Where(x => x.sell_price_min != 0);

        if (itemsData != null)
        {
          if (itemsData.Count() != 0 && itemsData.Where(x => x.sell_price_min != 0).FirstOrDefault().sell_price_min != 0)
          {
            var value = itemsData.Min(x => x.sell_price_min);

            if (value <= 15000000)// Very simple check to verify if a single item is too high. (a single item shouldn't cost over 5 mil. more checks need to be in place)
            {
              returnValue = value;
            }
            else
            {
              ErrorMessage = $"$0 (Price to high - {value}";
            }
          }
        }
        else
        {
          ErrorMessage = "$0 (Price not found)";
        }
        return returnValue;
      }
      if (ItemData.GetType() == typeof(List<AverageItemPrice>))
      {
        var marketData = (List<AverageItemPrice>)(object)ItemData;
        var itemsData = marketData.Where(x => x.data.Any(x => x.avg_price != 0));

        if (itemsData != null)
        {
          if (marketData.Count() != 0 && marketData.Where(x => x.data.Any(x => x.avg_price != 0)).FirstOrDefault().data.FirstOrDefault().avg_price != 0)
          {
            var value = marketData.Min(x => x.data.Min(x => x.avg_price));

            if (value <= 15000000)// Very simple check to verify if a single item is too high. (a single item shouldn't cost over 5 mil. more checks need to be in place)
            {
              returnValue = value;
            }
            else
            {
              ErrorMessage = $"$0 (Price to high - {value}";
            }
          }
        }
        else
        {
          ErrorMessage = "$0 (Price not found)";
        }
        return returnValue;

      }
      if (ItemData.GetType() == typeof(List<EquipmentMarketDataMonthylyAverage>))
      {
        var marketData = (List<EquipmentMarketDataMonthylyAverage>)(object)ItemData;
        var itemsData = marketData.Where(x => x.data.Any(x => x.avg_price != 0));

        if (itemsData != null)
        {
          if (marketData.Count() != 0 && marketData.Where(x => x.data.Any(x => x.avg_price != 0)).FirstOrDefault().data.FirstOrDefault().avg_price != 0)
          {
            var value = marketData.Min(x => x.data.FirstOrDefault().avg_price);

            if (value <= 15000000)// Very simple check to verify if a single item is too high. (a single item shouldn't generally cost over 5 mil. more checks need to be in place)
            {
              returnValue = value;
            }
            else
            {
              ErrorMessage = $"$0 (Price to high - {value}";
            }
          }
        }
        else
        {
          ErrorMessage = "$0 (Price not found)";
        }
      }
      return returnValue;

    }

    public bool UserHaveRegearRole(SocketInteractionContext a_SocketContext)
    {

      var guildUser = (SocketGuildUser)a_SocketContext.User;

      if (guildUser.Roles.Any(r => r.Name == "Gold Tier Regear - Eligible"))
      {
        return true;
      }
      else if (guildUser.Roles.Any(r => r.Name == "Silver Tier Regear - Eligible"))
      {
        return true;
      }
      else if (guildUser.Roles.Any(r => r.Name == "Bronze Tier Regear - Eligible"))
      {
        return true;
      }
      else if (guildUser.Roles.Any(r => r.Name == "Free Regear - Eligible"))
      {
        return true;
      }
      else
      {
        return false;
      }
    }

    public static bool ISUserMentor(SocketGuildUser a_SocketGuildUser)
    {

      if (a_SocketGuildUser.Roles.Any(r => r.Id == TankMentorID))
      {
        return true;
      }
      else if (a_SocketGuildUser.Roles.Any(r => r.Id == HealerMentorID))
      {
        return true;
      }
      else if (a_SocketGuildUser.Roles.Any(r => r.Id == DPSMentorID))
      {
        return true;
      }
      else if (a_SocketGuildUser.Roles.Any(r => r.Id == SupportMentorID))
      {
        return true;
      }
      else
      {
        return false;
      }
    }

    public static bool HasRegearOverride(SocketGuildUser a_SocketGuildUser)
    {
      ulong ManagementRoleID = ulong.Parse(System.Configuration.ConfigurationManager.AppSettings.Get("ManagementRoleID"));
      ulong OfficerRoleID = ulong.Parse(System.Configuration.ConfigurationManager.AppSettings.Get("OfficerRoleID"));

      if (a_SocketGuildUser.Roles.Any(r => r.Id == ManagementRoleID || r.Id == OfficerRoleID || r.Name == "Admin"))
      {
        return true;
      }
      return false;
    }

    public ulong GetRegearPosterID(string a_sVictimName, SocketInteractionContext a_SocketInteractionContext)
    {
      var UsersList = CreateMemberDict(a_SocketInteractionContext);

      foreach (var user in UsersList)
      {
        if (user.Key.ToLower() == a_sVictimName.ToLower())
        {
          return user.Value;
        }
      }


      return 0;
    }

    public Dictionary<string, ulong> CreateMemberDict(SocketInteractionContext context)
    {
      Dictionary<string, ulong> dict = new Dictionary<string, ulong>();

      var iterable = context.Guild.GetUsersAsync().ToListAsync().Result.ToList();
      foreach (var member in iterable.FirstOrDefault())
      {
        if (member.RoleIds.Contains(roleIdNewRecruit) || member.RoleIds.Contains(ulong.Parse("602244098908225537"))
          || member.RoleIds.Contains(roleIdOfficer) || member.RoleIds.Contains(roleIdVeteran) || member.RoleIds.Contains(ulong.Parse("1156954659525771344")))
        {
          if (member.DisplayName != null)
          {
            if (member.DisplayName.StartsWith("!!"))
            {
              string temp = member.DisplayName.Remove(0, 5);
              dict.Add(temp, member.Id);
            }
            else
            {
              dict.Add(member.DisplayName, member.Id);
            }
          }
          else if (member.DisplayName == null)
          {
            dict.Add(member.Username, member.Id);
          }
          else
          {
            continue;
          }
        }
      }
      return dict;
    }
    public class Equipment
    {
      private string image;
      private string itemPrice;
      private string type;
      public string Image { get => image; set => image = value; }
      public string ItemPrice { get => itemPrice; set => itemPrice = value; }
      public string Type { get => type; set => type = value; }
    }
  }
}
