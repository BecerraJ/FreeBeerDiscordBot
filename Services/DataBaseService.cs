using DiscordBot.Enums;
using DiscordBot.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DiscordBot.Services
{
  public class DataBaseService
  {
    private FreeBeerdbTestContext freeBeerdbContext = new FreeBeerdbTestContext();

    public async Task AddPlayerInfo(Player player)
    {
      if (!await CheckPlayerIsExist(player.PlayerName))
      {
        await freeBeerdbContext.Player.AddAsync(player);
        await freeBeerdbContext.SaveChangesAsync();
      }
    }

    public async Task LogPlayerInfo(LoggedPlayerInfo a_PlayerInfo)
    {
      
      if (await CheckPlayerIsExist(a_PlayerInfo.PlayerName) || a_PlayerInfo.GuildID == System.Configuration.ConfigurationManager.AppSettings.Get("FreeBeerGuildAPIID"))
      {
        await freeBeerdbContext.LoggedPlayerInfo.AddAsync(a_PlayerInfo);
        await freeBeerdbContext.SaveChangesAsync();
      }
    }

    public async Task RegisterAlliancePlayerInfo(RegisteredAllianceMembers a_Player)
    {
      if (!await CheckAlliancePlayerIsExist(a_Player.PlayerID))
      {
        await freeBeerdbContext.RegisteredAllianceMembers.AddAsync(a_Player);
        await freeBeerdbContext.SaveChangesAsync();
      }
    }
    public async Task RegisterGuild(RegisteredAllianceGuilds a_guildID)
    {
      await freeBeerdbContext.RegisteredAllianceGuilds.AddAsync(a_guildID);
      await freeBeerdbContext.SaveChangesAsync();
    }

    public List<Player> GetRegisteredPlayerRoster()
    {
      //var somethinglist = freeBeerdbContext.Player.ToList();
      return freeBeerdbContext.Player.ToList();
    }
    public async Task<Boolean> CheckPlayerIsExist(string playerName)
    {
      return await freeBeerdbContext.Player.AnyAsync(x => x.PlayerName.ToLower() == playerName.ToLower());
    }
    public async Task<Boolean> CheckAlliancePlayerIsExist(string playerID)
    {
      return await freeBeerdbContext.RegisteredAllianceMembers.AnyAsync(x => x.PlayerID == playerID);
    }
    public async Task<Boolean> CheckForRegisteredGuild(string guildID)
    {
      return await freeBeerdbContext.RegisteredAllianceGuilds.AnyAsync(x => x.GuildID == guildID);
    }

    public Task<Boolean> PlayerReachRegearCap(string playerName, int a_iRegearLimitCap)
    {
      List<PlayerLoot> playerLoots = new List<PlayerLoot>();
      var playerLoot = freeBeerdbContext.PlayerLoot.AsQueryable().Where(x => x.Player.PlayerName == playerName).ToList();
      foreach (var item in playerLoot)
      {
        if (item.CreateDate.Value.ToString("yyyy-MM-dd").Equals(DateTime.UtcNow.ToString("yyyy-MM-dd")))
        {
          playerLoots.Add(item);
        }
      }

      if (playerLoots == null)
      {
        return Task.FromResult(false);
      }
      else if (playerLoots.Count > a_iRegearLimitCap)
      {
        return Task.FromResult(true);
      }
      else
      {
        return Task.FromResult(false);
      }
    }
    public async Task<Boolean> CheckKillIdIsRegeared(string killId)
    {
      return await freeBeerdbContext.PlayerLoot.AnyAsync(x => x.KillId == killId);
    }
    public Player GetPlayerInfoByName(string playerName)
    {
      return freeBeerdbContext.Player.AsQueryable().Where(x => x.PlayerName == playerName).FirstOrDefault();

    }
    public void DeletePlayer(string a_sPlayerName)
    {
      Player PlayerName = freeBeerdbContext.Player.AsQueryable().Where(x => x.PlayerName == a_sPlayerName).FirstOrDefault();
      if (PlayerName != null)
      {
        freeBeerdbContext.Player.Remove(PlayerName);
        freeBeerdbContext.SaveChanges();
      }

    }
    public void DeletePlayerLootByKillId(string killID)
    {
      var playerLoot = freeBeerdbContext.PlayerLoot.AsQueryable().Where(x => x.KillId == killID).FirstOrDefault();
      if (playerLoot != null)
      {
        freeBeerdbContext.PlayerLoot.Remove(playerLoot);
        freeBeerdbContext.SaveChanges();
      }

    }

    public void DeletePlayerLootByQueueId(string queueID)
    {
      var playerLoot = freeBeerdbContext.PlayerLoot.AsQueryable().Where(x => x.QueueId == queueID).FirstOrDefault();
      if (playerLoot != null)
      {
        freeBeerdbContext.PlayerLoot.Remove(playerLoot);
        freeBeerdbContext.SaveChanges();
      }
      else
      {
        Console.WriteLine($"Issue writing OC Break to database. Null Reference. QeueueID={queueID}");
      }

    }
    public void DeleteRegisteredAlliancePlayer(string a_sPlayerID)
    {
      var player = freeBeerdbContext.RegisteredAllianceMembers.AsQueryable().Where(x => x.PlayerID == a_sPlayerID).FirstOrDefault();
      if (player != null)
      {
        freeBeerdbContext.RegisteredAllianceMembers.Remove(player);
        freeBeerdbContext.SaveChanges();
      }

    }

    public MoneyType GetMoneyTypeByName(MoneyTypes moneyType)
    {
      return freeBeerdbContext.MoneyType.AsQueryable().Where(x => x.Type == (int)moneyType).FirstOrDefault();
    }
    public async Task AddPlayerReGear(PlayerLoot playerLoot)
    {
      await freeBeerdbContext.PlayerLoot.AddAsync(playerLoot);
      await freeBeerdbContext.SaveChangesAsync();
    }
    public async Task AddSeedingData()
    {
      if (!await freeBeerdbContext.MoneyType.AnyAsync(x => x.Type == (int)MoneyTypes.FocusSale))
      {
        await freeBeerdbContext.MoneyType.AddAsync(new MoneyType
        {
          Type = (int)MoneyTypes.FocusSale
        });
      }
      if (!await freeBeerdbContext.MoneyType.AnyAsync(x => x.Type == (int)MoneyTypes.Hellgates))
      {
        await freeBeerdbContext.MoneyType.AddAsync(new MoneyType
        {
          Type = (int)MoneyTypes.Hellgates
        });
      }
      if (!await freeBeerdbContext.MoneyType.AnyAsync(x => x.Type == (int)MoneyTypes.LootSplit))
      {
        await freeBeerdbContext.MoneyType.AddAsync(new MoneyType
        {
          Type = (int)MoneyTypes.LootSplit
        });
      }
      if (!await freeBeerdbContext.MoneyType.AnyAsync(x => x.Type == (int)Enums.MoneyTypes.OCBreak))
      {
        await freeBeerdbContext.MoneyType.AddAsync(new MoneyType
        {
          Type = (int)MoneyTypes.OCBreak
        });
      }
      if (!await freeBeerdbContext.MoneyType.AnyAsync(x => x.Type == (int)Enums.MoneyTypes.Other))
      {
        await freeBeerdbContext.MoneyType.AddAsync(new MoneyType
        {
          Type = (int)MoneyTypes.Other
        });
      }
      if (!await freeBeerdbContext.MoneyType.AnyAsync(x => x.Type == (int)Enums.MoneyTypes.ReGear))
      {
        await freeBeerdbContext.MoneyType.AddAsync(new MoneyType
        {
          Type = (int)MoneyTypes.ReGear
        });
      }
      await freeBeerdbContext.SaveChangesAsync();
    }
  }
}
