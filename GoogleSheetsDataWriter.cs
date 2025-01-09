using Aspose.Imaging.Xmp.Types.Complex.Version;
using Aspose.Words;
using Aspose.Words.Lists;
using Discord;
using Discord.Interactions;
using Discord.Rest;
using Discord.WebSocket;
using DiscordBot.Enums;
using DiscordBot.RegearModule;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Util.Store;
using Newtonsoft.Json;
using PlayerData;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace GoogleSheetsData
{
  public class GoogleSheetsDataWriter
  {
    public static string ReadRange { get; set; }
    public static string WriteRange { get; set; }

    private const string GoogleCredentialsFileName = "credentials.json";

    static string[] Scopes = { SheetsService.Scope.Spreadsheets };
    public static string GuildSpreadsheetId = System.Configuration.ConfigurationManager.AppSettings.Get("guildDataBaseSheetID");
    public static string RegearSheetID = System.Configuration.ConfigurationManager.AppSettings.Get("regearSpreadSheetID");

    static string ApplicationName = "Google Sheets API .NET Quickstart";
    public bool enableGoogleApi = bool.Parse(System.Configuration.ConfigurationManager.AppSettings.Get("enableGoogleAPI"));
    static SemaphoreSlim sheetWriterSemaphore = new SemaphoreSlim(1,1);


    public void ConnectToGoogleAPI()
    {
      try
      {
        UserCredential credential;

        using (var stream =
               new FileStream("credentials.json", FileMode.Open, FileAccess.Read))
        {
          string credPath = "token.json";

          credential = GoogleWebAuthorizationBroker.AuthorizeAsync(GoogleClientSecrets.FromStream(stream).Secrets, Scopes, "user", CancellationToken.None, new FileDataStore(credPath, true)).Result;
          Console.WriteLine("Credential file saved to: " + credPath);
        }

        // Create Google Sheets API service.
        var service = new SheetsService(new BaseClientService.Initializer
        {
          HttpClientInitializer = credential,
          ApplicationName = ApplicationName
        });

        // Define request parameters.

        string range = "Free Beer blackList!A2:G";
        SpreadsheetsResource.ValuesResource.GetRequest request =
            service.Spreadsheets.Values.Get(GuildSpreadsheetId, range);

        // Prints the names and majors of students in a sample spreadsheet:
        // https://docs.google.com/spreadsheets/d/1BxiMVs0XRA5nFMdKvBdBZjgmUUqptlbs74OgvE2upms/edit
        ValueRange response = request.Execute();
        IList<IList<object>> values = response.Values;
        if (values == null || values.Count == 0)
        {
          Console.WriteLine("No data found.");
          return;
        }

        Console.WriteLine("DiscordName, InGameName, Blacklisted, Reason, Date Recruited, DateLeftKicked, Notes");

        foreach (var row in values)
        {
          // Print columns A and D, which correspond to indices 0 and 5.
          Console.WriteLine("{0}, {1}, {2}, {3}, {4}, {5}, {6}", row[0], row[1], row[2], row[3], row[4], row[5], row[6]);
        }

      }
      catch (FileNotFoundException e)
      {
        Console.WriteLine(e.Message);
      }
    }
    public static SheetsService GetSheetsService()
    {
      using (var stream = new FileStream(GoogleCredentialsFileName, FileMode.Open, FileAccess.Read))
      {
        string credPath = "token.json";
        var serviceInitializer = new BaseClientService.Initializer
        {
          HttpClientInitializer = GoogleWebAuthorizationBroker.AuthorizeAsync(GoogleClientSecrets.FromStream(stream).Secrets, Scopes, "user", CancellationToken.None, new FileDataStore(credPath, true)).Result  //GoogleCredential.FromStream(stream).CreateScoped(Scopes)
        };
        return new SheetsService(serviceInitializer);
      }
    }

    public async Task ReadAsync(SpreadsheetsResource.ValuesResource valuesResource, string sReadrange)
    {
      var response = await valuesResource.Get(GuildSpreadsheetId, sReadrange).ExecuteAsync();
      var values = response.Values;
      if (values == null || !values.Any())
      {
        Console.WriteLine("No data found.");
        return;
      }
      var header = string.Join(" ", values.First().Select(r => r.ToString()));
      Console.WriteLine($"Header: {header}");

      foreach (var row in values.Skip(1))
      {

        var res = string.Join(" ", row.Select(r => r.ToString()));
        Console.WriteLine(res);
      }
    }


    public static async Task WriteToFreeBeerRosterDatabase(string a_SocketGuildUser, string? a_sIngameName, string? a_sReason, string? a_sFine, string? a_sNotes)
    {
      //THIS ONLY WRITES TO THE FREE BEER BLACKLIST SPREADSHEET. ADJUST THIS METHOD SO THAT IT CAN WRITE TO ANYSPREADSHEET
      var serviceValues = GoogleSheetsDataWriter.GetSheetsService().Spreadsheets.Values;
      var col1 = 2;
      var col2 = 2;
      ReadRange = $"Free Beer BlackList!A{col1}";
      WriteRange = $"Free Beer BlackList!A{col1}:G{col2}";

      ValueRange GetResponse = null;
      IList<IList<object>> values = null;

      while (true)
      {
        GetResponse = await serviceValues.Get(GuildSpreadsheetId, ReadRange).ExecuteAsync();
        values = GetResponse.Values;

        if (values == null || !values.Any())
        {
          break;
        }

        col1++;
        col2++;

        ReadRange = $"Free Beer BlackList!A{col1}";
        WriteRange = $"Free Beer BlackList!A{col1}:G{col2}";
      }

      if (values == null || !values.Any())
      {
        var rowValues = new ValueRange { Values = new List<IList<object>> { new List<object> { a_SocketGuildUser, a_sIngameName, "TRUE", a_sReason, a_sFine, DateTime.Now.ToString("M/d/yyyy"), a_sNotes } } };
        var update = serviceValues.Update(rowValues, GuildSpreadsheetId, WriteRange);
        update.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;
        await update.ExecuteAsync();
      }

    }
    public static async Task WriteToRegearSheet(SocketInteractionContext a_command, int a_iTotalSilverRefund, string a_sCallerName, string a_sEventType, MoneyTypes a_eMoneyType, List<string> a_MemberList = null, PlayerDataHandler.Rootobject? a_playerData = null)
    {
      await sheetWriterSemaphore.WaitAsync();

      try 
      {
        var serviceValues = GetSheetsService().Spreadsheets.Values;
        int numberOfActiveRows = serviceValues.Get(RegearSheetID, "Current Season Dumps!B2:B").Execute().Values.Count; // This finds the nearest last row int he spreadsheet. This saves on hitting the rate limit when hitting google API.

        int iStartRowLocation = numberOfActiveRows + 2; //+1 for the headers and +1 for the new row
        int iEndColumnLocation = 0;

        var messages = await a_command.Channel.GetMessagesAsync(1).FlattenAsync();
        var msgRef = new MessageReference(messages.First().Id);
        string transactionType = "No Selection";
        var valueRange = new ValueRange();

        valueRange.Values = [];

        switch (a_eMoneyType)
        {
          case MoneyTypes.ReGear:
            transactionType = "Re-Gear";
            break;
          case MoneyTypes.LootSplit:
            transactionType = "Loot Split";
            break;
          case MoneyTypes.OCBreak:
            transactionType = "OC Break";
            break;
        }

        if(a_MemberList != null)
        {
          foreach (string playerName in a_MemberList)
          {
            valueRange.Values.Add(new List<object> { "@" + playerName, a_iTotalSilverRefund, DateTime.UtcNow.Date.ToString("M/d/yyyy"), transactionType, a_sEventType, a_sCallerName, msgRef.MessageId.ToString(), "N/A", a_command.User.ToString() });
          }
        }
        else if (a_playerData != null)
        {
          valueRange.Values.Add(new List<object> { "@" + a_playerData.Victim.Name, a_iTotalSilverRefund, DateTime.UtcNow.Date.ToString("M/d/yyyy"), transactionType, a_sEventType, a_sCallerName, msgRef.MessageId.ToString(), a_playerData.EventId, a_command.User.ToString() });
        }
        else
        {
          throw new Exception("NO USER DATA FOUND TO WRITE TO GOOGLE SHEETS");
        }
        
        iEndColumnLocation = valueRange.Values.Count + numberOfActiveRows + 1;
        WriteRange = $"Current Season Dumps!R{iStartRowLocation}C2:R{iEndColumnLocation}C10";

        //append data to spreadsheet
        var appendRequest = serviceValues.Update(valueRange, RegearSheetID, WriteRange);
        appendRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
        var appendResponse = appendRequest.Execute();
      }
      finally 
      {
        sheetWriterSemaphore.Release();
      }

    }

    public static async Task RegisterUserToDataRoster(string a_SocketGuildUser, string? a_sIngameName, string? a_sReason, string? a_sFine, string? a_sNotes)
    {
      var serviceValues = GetSheetsService().Spreadsheets.Values;

      var numberOfRow = serviceValues.Get(GuildSpreadsheetId, "Guild Roster!B2:B").Execute().Values.Count; // This finds the nearest last row int he spreadsheet. This saves on hitting the rate limit when hitting google API.
      var col1 = numberOfRow + 1;
      var col2 = numberOfRow + 1;

      ReadRange = $"Guild Roster!A{col1 + 1}";
      WriteRange = $"Guild Roster!A{col1 + 1}:E{col2 + 1}";


      var rowValues = new ValueRange { Values = new List<IList<object>> { new List<object> { DateTime.Now.ToString("M/d/yyyy"), a_SocketGuildUser, "N/A", "N/A", "No notes" } } };
      var update = serviceValues.Update(rowValues, GuildSpreadsheetId, WriteRange);
      update.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;
      await update.ExecuteAsync();
    }

    public static async Task RegisterUserToRegearSheets(SocketGuildUser a_SocketGuildUser, string? a_sIngameName, string? a_sReason, string? a_sFine, string? a_sNotes)
    {
      var serviceValues = GetSheetsService().Spreadsheets.Values;
      string? sUserNickname = (a_SocketGuildUser.DisplayName != null) ? new PlayerDataLookUps().CleanUpShotCallerName(a_SocketGuildUser.DisplayName) : a_SocketGuildUser.Username;

      var payOutsNumberOfRow = serviceValues.Get(RegearSheetID, "Payouts!B2:B").Execute().Values.Count;
      var dataValidationNumberOfRow = serviceValues.Get(RegearSheetID, "Data Validation!B2:B").Execute().Values.Count;
      var MiniMarketCredits = serviceValues.Get(RegearSheetID, "Mini-Market Credits!B2:B").Execute().Values.Count;

      var col1 = payOutsNumberOfRow + 2;
      var col2 = dataValidationNumberOfRow + 2;

      try
      {
        var payoutsRowValues = new ValueRange { Values = new List<IList<object>> { new List<object> { sUserNickname } } };
        var payoutsUpdate = serviceValues.Update(payoutsRowValues, RegearSheetID, $"B{col1}");
        payoutsUpdate.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
        await payoutsUpdate.ExecuteAsync();

        var dataValidationRowValues = new ValueRange { Values = new List<IList<object>> { new List<object> { sUserNickname, "@" + sUserNickname } } };
        var dataValidationUpdate = serviceValues.Update(dataValidationRowValues, RegearSheetID, $"Data Validation!B{col2}:C2{col2}");
        dataValidationUpdate.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
        await dataValidationUpdate.ExecuteAsync();

        var MiniMarketRowValues = new ValueRange { Values = new List<IList<object>> { new List<object> { sUserNickname } } };
        var MiniMaarketUpdate = serviceValues.Update(MiniMarketRowValues, RegearSheetID, $"B{col1}");
        MiniMaarketUpdate.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
        await MiniMaarketUpdate.ExecuteAsync();
      }
      catch (Exception ex)
      {
        Console.WriteLine(ex.ToString());
      }
    }

    public static bool GetRegisteredUser(string a_sUserName)
    {
      var serviceValues = GetSheetsService().Spreadsheets.Values;

      IList<IList<object>> rowValues = serviceValues.Get(RegearSheetID, $"Payouts!B4:B").Execute().Values;

      int i = 0;
      foreach (var users in rowValues)
      {
        if (users[0].ToString().ToLower() == a_sUserName.ToLower())
        {
          return true;
        }

        i++;
      }

      return false;
    }


    public static List<string> GetRunningPaychexTotal(string a_sUserName)
    {
      SpreadsheetsResource serviceValues = GetSheetsService().Spreadsheets;

      DateTime lastSunday = GoogleSheetHelperMethods.StartOfWeek(DateTime.Today, DayOfWeek.Sunday);
      DateTime lastbiweeklySunday = GoogleSheetHelperMethods.StartOfWeek(lastSunday.AddDays(-7), DayOfWeek.Sunday);

      string biweeklyLastSundayDate = $"{lastbiweeklySunday.ToShortMonthName()}-{lastbiweeklySunday.Day}";
      string currentWeekPaychexDate = $"{lastSunday.ToShortMonthName()}-{lastSunday.Day}";

      List<string> paychexData = new List<string>();
      List<object> DaterowValues = serviceValues.Values.Get(RegearSheetID, "Payouts!3:3").Execute().Values.FirstOrDefault().ToList();

      IList<IList<object>> paychexClaimedColumn = null;
      IList<IList<object>> PayoutsRowValues = serviceValues.Values.Get(RegearSheetID, $"Payouts!R3C2:R350C{DaterowValues.Count}").Execute().Values;

      int i = 0;

      foreach (var users in PayoutsRowValues)
      {

        if (users[0].ToString().ToLower() == a_sUserName.ToLower())
        {
          foreach (var dates in PayoutsRowValues[0])
          {
            if (dates.ToString() == biweeklyLastSundayDate)
            {
              paychexData.Add(users[i].ToString() + $" {biweeklyLastSundayDate}");
            }

            if (dates.ToString() == currentWeekPaychexDate)
            {
              paychexData.Add(users[i].ToString() + $" {currentWeekPaychexDate}");
            }
            i++;
          }
          break;
        }
      }

      //List<string> paychexSheets = GoogleSheetsDataWriter.GetPaychexSheets();

      //         foreach(var sheet in paychexSheets)
      //         {
      //	paychexClaimedColumn = serviceValues.Values.Get(RegearSheetID, $"{sheet}!R2C1:R350C4").Execute().Values;

      //	i = 0;
      //	foreach (var users in paychexClaimedColumn)
      //	{
      //		if (users.Count > 0 && users[0].ToString().ToLower() == a_sUserName.ToLower())
      //		{
      //			if (users[3].ToString() == "TRUE")
      //			{
      //				paychexData[0] += " (CLAIMED)";
      //			}
      //			else if (users[3].ToString() == "FALSE")
      //			{
      //				paychexData[0] += " (NOT CLAIMED)";
      //			}
      //			break;
      //		}
      //		else if (users.Count == 0)
      //		{
      //			paychexData[0] += " (NO PAYCHEX)";
      //			break;
      //		}
      //		i++;
      //	}
      //}


      //if (CheckIfSheetExists($"{biweeklyLastSundayDate} Paychex"))
      //{
      //    paychexClaimedColumn = serviceValues.Values.Get(RegearSheetID, $"{biweeklyLastSundayDate} Paychex!R2C1:R350C4").Execute().Values;

      //    i = 0;
      //    foreach (var users in paychexClaimedColumn)
      //    {
      //        if (users.Count > 0 && users[0].ToString().ToLower() == a_sUserName.ToLower())
      //        {
      //            if (users[3].ToString() == "TRUE")
      //            {
      //                paychexData[0] += " (CLAIMED)";
      //            }
      //            else if (users[3].ToString() == "FALSE")
      //            {
      //                paychexData[0] += " (NOT CLAIMED)";
      //            }
      //            break;
      //        }
      //        else if (users.Count == 0)
      //        {
      //            paychexData[0] += " (NO PAYCHEX)";
      //            break;
      //        }
      //        i++;
      //    }
      //}
      //else if (paychexData.Count > 0)
      //{
      //    paychexData[0] += " (NOT RENDERED)";
      //}

      return paychexData;
    }

    public static Dictionary<string, string> GetPaychexTotals(string a_sUserName)
    {
      SpreadsheetsResource serviceValues = GetSheetsService().Spreadsheets;

      DateTime lastSunday = GoogleSheetHelperMethods.StartOfWeek(DateTime.Today, DayOfWeek.Sunday);
      DateTime lastbiweeklySunday = GoogleSheetHelperMethods.StartOfWeek(lastSunday.AddDays(-7), DayOfWeek.Sunday);

      string biweeklyLastSundayDate = $"{lastbiweeklySunday.ToShortMonthName()}-{lastbiweeklySunday.Day}";
      string currentWeekPaychexDate = $"{lastSunday.ToShortMonthName()}-{lastSunday.Day}";

      Dictionary<string, string> paychexData = new Dictionary<string, string>();
      IList<IList<object>> paychexClaimedColumn = null;

      int i = 0;


      List<string> paychexSheets = GoogleSheetsDataWriter.GetPaychexSheets();

      foreach (var sheet in paychexSheets)
      {
        paychexClaimedColumn = serviceValues.Values.Get(RegearSheetID, $"{sheet}!R2C1:R350C4").Execute().Values;

        i = 0;
        foreach (var users in paychexClaimedColumn)
        {
          if (users.Count > 0 && users[0].ToString().ToLower() == a_sUserName.ToLower())
          {
            if (users[3].ToString() == "TRUE")
            {
              paychexData.Add($"{sheet} (CLAIMED)", users[1].ToString());
            }
            else if (users[3].ToString() == "FALSE")
            {
              paychexData.Add($"{sheet} (NOT CLAIMED)", users[1].ToString());
            }
            break;
          }
          //else if (users.Count == 0)
          //{
          //	paychexData[0] += " (NO PAYCHEX)";
          //	break;
          //}
          i++;
        }
      }
      return paychexData;
    }


    public static string GetMiniMarketCredits(string a_sUserName)
    {
      var serviceValues = GetSheetsService().Spreadsheets.Values;

      ReadRange = $"Mini-Market Credits!A2:A305";

      var rowValues = serviceValues.Get(RegearSheetID, $"Mini-Market Credits!R2C2:R305C3").Execute().Values;

      int i = 0;
      foreach (var users in rowValues)
      {
        if (users[0].ToString().ToLower() == a_sUserName.ToLower() && users[0].ToString() != null)
        {
          return users.Last().ToString();
        }

        i++;
      }

      return "$0 (NOT ENROLLED)";
    }

    public static async Task RenderPaychex(SocketInteractionContext a_socketInteraction)
    {
      ulong paychexTemplateSheetID = ulong.Parse(System.Configuration.ConfigurationManager.AppSettings.Get("paychexTemplateSheetID"));
      var serviceValues = GetSheetsService().Spreadsheets;
      SocketGuildUser socketGuildUser = (SocketGuildUser)a_socketInteraction.User;

      string lastSundayMonth = GoogleSheetHelperMethods.StartOfWeek(DateTime.Today, DayOfWeek.Sunday).ToShortMonthName();
      int lastSunday = GoogleSheetHelperMethods.StartOfWeek(DateTime.Today.AddDays(-7), DayOfWeek.Sunday).Day;
      //string paychexRenderedName = $"{lastSundayMonth}-{lastSunday} Paychex";
      string paychexRenderedName = $"Rendered {lastSundayMonth}-{lastSunday} Paychex ";


      if (!CheckIfSheetExists(paychexRenderedName))
      {
        BatchUpdateSpreadsheetRequest batchUpdateSpreadsheetRequest = new BatchUpdateSpreadsheetRequest();
        batchUpdateSpreadsheetRequest.Requests =
        [
          new Request()
          {
            DuplicateSheet = new DuplicateSheetRequest()
            {
              NewSheetName = paychexRenderedName,
              SourceSheetId = (int?)paychexTemplateSheetID

            },
          },
        ];

        var req = serviceValues.BatchUpdate(batchUpdateSpreadsheetRequest, RegearSheetID);  //public SheetsService Service; property of parent class
        BatchUpdateSpreadsheetResponse response = req.Execute();

        List<object> DaterowValues = serviceValues.Values.Get(RegearSheetID, "Payouts!3:3").Execute().Values.FirstOrDefault().ToList();
        //IList<IList<object>> rowValues = serviceValues.Values.Get(RegearSheetID, $"Payouts!R3C2:R350C{DaterowValues.Count}").Execute().Values;

        //List<string> GuildMembersNames = null;
        //List<int> GuildMembersAmounts = null;

        var numberOfGuildMembers = serviceValues.Values.Get(RegearSheetID, "Payouts!B4:B").Execute().Values;
        var dateRowValues = serviceValues.Values.Get(RegearSheetID, "Payouts!3:3").Execute().Values.FirstOrDefault().ToList();
        //var testPaymentAmounts = serviceValues.Values.Get(RegearSheetID, "Payouts!B4:B").Execute().Values;


        //DateTime lastSundayPaychexDate = GoogleSheetHelperMethods.StartOfWeek(DateTime.Today, DayOfWeek.Sunday);
        //string shortmonth = DateTime.Now.ToShortMonthName();

        string combinedDate = $"{lastSundayMonth}-{lastSunday}";//This gets the running total
        //List<string> paychexData = new List<string>();
        //List<string> row2 = new List<string>();



        //get all user info
        RegearModule regearModule = new RegearModule();
        regearModule.CreateMemberDict(a_socketInteraction);
        //get all paychex info

        int dateIndex = 1;

        foreach (var dates in dateRowValues)
        {
          if (dates.ToString() == combinedDate)
          {
            break;

          }
          dateIndex++;
        }

        var PayoutRowValues = serviceValues.Values.Get(RegearSheetID, $"Payouts!R4C2:R{numberOfGuildMembers.Count}C{dateIndex}").Execute().Values;

        ValueRange valueRange = new ValueRange();
        valueRange.Values = new List<IList<object>>();
        WriteRange = $"A2:B{numberOfGuildMembers.Count}";

        foreach (var payoutUsers in PayoutRowValues)
        {
          IList<object> objectListthis = new List<object>() { payoutUsers[0], payoutUsers.LastOrDefault() };

          if (payoutUsers.LastOrDefault().ToString() != "0")
          {
            valueRange.Values.Add(objectListthis);
          }
        }

        var appendRequest = GetSheetsService().Spreadsheets.Values.Update(valueRange, RegearSheetID, WriteRange);
        appendRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
        var appendResponse = appendRequest.Execute();

        await a_socketInteraction.Interaction.FollowupAsync($"Paychex rendered. Sheet is called {paychexRenderedName}", null, false, true);
      }
      else
      {
        await a_socketInteraction.Interaction.FollowupAsync("Paychex has already been rendered for the week. Aborting rendering", null, false, true);
      }
    }

    public static async Task TransferPaychexToMiniMartCredits(SocketGuildUser a_SocketGuildUser, string a_sDateToTransfer)
    {
      string? sUserNickname = (a_SocketGuildUser.DisplayName != null) ? new PlayerDataLookUps().CleanUpShotCallerName(a_SocketGuildUser.DisplayName) : a_SocketGuildUser.Username;

      var serviceValues = GetSheetsService().Spreadsheets;
      DateTime lastSunday = GoogleSheetHelperMethods.StartOfWeek(DateTime.Today, DayOfWeek.Sunday);
      var shortmonth = DateTime.Now.ToShortMonthName();
      string cleanupedTotal = "NA";

      IList<IList<object>> paychexClaimedColumn = null;
      string combinedDate = $"{shortmonth}-{lastSunday.Day}";

      Dictionary<string, string> paychexRunningTotal = GoogleSheetsDataWriter.GetPaychexTotals(sUserNickname);

      foreach (var paychex in paychexRunningTotal)
      {
        if (paychex.Key.Contains(a_sDateToTransfer))
        {
          cleanupedTotal = paychex.Value;

          if (paychex.Key.Contains("(NOT CLAIMED"))
          {
            var numberOfRow = serviceValues.Values.Get(RegearSheetID, "MiniMart Ledger!B2:B").Execute().Values.Count;
            var col1 = numberOfRow + 2;

            try
            {
              var rowValues = new ValueRange { Values = new List<IList<object>> { new List<object> { "@" + sUserNickname, cleanupedTotal, DateTime.Now.ToString("M/d/yyyy"), "Free Beer Bot", "Credits Transfer" } } };
              var rowUpdates = serviceValues.Values.Update(rowValues, RegearSheetID, $"MiniMart Ledger!B{col1}:F{col1}");
              rowUpdates.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
              await rowUpdates.ExecuteAsync();


              //string splitPaychexString = paychexRunningTotal[0].Split(" ");
              paychexClaimedColumn = serviceValues.Values.Get(RegearSheetID, $"{a_sDateToTransfer} Paychex!R2C1:R350C4").Execute().Values;

              int i = 0;
              foreach (var users in paychexClaimedColumn)
              {
                if (users[0].ToString().ToLower() == sUserNickname.ToLower())
                {
                  var dataValidationRowValues = new ValueRange { Values = new List<IList<object>> { new List<object> { "Free Beer Bot", "TRUE" } } };
                  var dataValidationUpdate = serviceValues.Values.Update(dataValidationRowValues, RegearSheetID, $"{a_sDateToTransfer} Paychex!C{i + 2}:D{i + 2}");
                  dataValidationUpdate.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
                  await dataValidationUpdate.ExecuteAsync();
                  break;
                }
                i++;
              }
            }
            catch (Exception ex)
            {
              Console.WriteLine(ex.ToString());
            }
            await a_SocketGuildUser.SendMessageAsync($"Your Paychex have successfully been transfered to mini-mart credits. Total: {cleanupedTotal}");
          }
          else if (paychex.Key.Contains("(CLAIMED)"))
          {
            await a_SocketGuildUser.SendMessageAsync("You already claimed or transferred your paychex");
          }
          else if (paychex.Key.Contains("(NOT RENDERED)"))
          {
            await a_SocketGuildUser.SendMessageAsync("The new paychex have not been rendered yet. Please be patient.");
          }
          else
          {
            await a_SocketGuildUser.SendMessageAsync("The Paychex are not rendered yet or there was an issue transferring your paychex to mini mart credits. Seek out an Officer to investigate.");
          }
        }
      }

      //string cleanupedTotal = paychexRunningTotal[0].Substring(0, paychexRunningTotal[0].IndexOf(" "));
      //int a = Int32.Parse(cleanupedTotal.Replace(",", ""));



    }

    public static async Task MiniMartTransaction(SocketGuildUser a_Manager, SocketGuildUser a_User, int a_iAmount, MiniMarketType a_eTransactionType)
    {
      string? sManagerNickname = (a_Manager.DisplayName != null) ? new PlayerDataLookUps().CleanUpShotCallerName(a_Manager.DisplayName) : a_Manager.Username;
      string? sUserNickname = (a_User.DisplayName != null) ? new PlayerDataLookUps().CleanUpShotCallerName(a_User.DisplayName) : a_User.Username;

      double dMiniMartDiscount = .10;

      var serviceValues = GetSheetsService().Spreadsheets.Values;

      var numberOfRow = serviceValues.Get(RegearSheetID, "MiniMart Ledger!B2:B").Execute().Values.Count; // This finds the nearest last row int he spreadsheet. This saves on hitting the rate limit when hitting google API.

      var col1 = numberOfRow + 1;
      var col2 = numberOfRow + 1;

      //var messages = await a_command.Channel.GetMessagesAsync(1).FlattenAsync();
      //var msgRef = new MessageReference(messages.First().Id);

      ReadRange = $"MiniMart Ledger!B{col1 + 1}";
      WriteRange = $"MiniMart Ledger!B{col1 + 1}:f{col2 + 1}";

      ValueRange rowValues = null;

      int discountedAmount = Convert.ToInt32(Math.Floor(a_iAmount - (a_iAmount * dMiniMartDiscount)));

      switch (a_eTransactionType)
      {
        case MiniMarketType.AccountSetup:
          rowValues = new ValueRange { Values = new List<IList<object>> { new List<object> { "@" + sUserNickname, a_iAmount, DateTime.UtcNow.Date.ToString("M/d/yyyy"), sManagerNickname, "Account Setup" } } };
          break;
        case MiniMarketType.Deposit:
          rowValues = new ValueRange { Values = new List<IList<object>> { new List<object> { "@" + sUserNickname, a_iAmount, DateTime.UtcNow.Date.ToString("M/d/yyyy"), sManagerNickname, "Deposit" } } };
          break;
        case MiniMarketType.Purchase:
          rowValues = new ValueRange { Values = new List<IList<object>> { new List<object> { "@" + sUserNickname, -a_iAmount, DateTime.UtcNow.Date.ToString("M/d/yyyy"), sManagerNickname, "Purchase" } } };
          break;
        case MiniMarketType.CreditsTransfer:
          rowValues = new ValueRange { Values = new List<IList<object>> { new List<object> { "@" + sUserNickname, a_iAmount, DateTime.UtcNow.Date.ToString("M/d/yyyy"), sManagerNickname, "Credits Transfer" } } };
          break;
        case MiniMarketType.Withdrawal:
          rowValues = new ValueRange { Values = new List<IList<object>> { new List<object> { "@" + sUserNickname, -a_iAmount, DateTime.UtcNow.Date.ToString("M/d/yyyy"), sManagerNickname, "Withdrawal" } } };
          break;
        case MiniMarketType.Other:
          rowValues = new ValueRange { Values = new List<IList<object>> { new List<object> { "@" + sUserNickname, a_iAmount, DateTime.UtcNow.Date.ToString("M/d/yyyy"), sManagerNickname, "Other" } } };
          break;
      }

      var update = serviceValues.Update(rowValues, RegearSheetID, WriteRange);

      update.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
      await update.ExecuteAsync();


    }

    public static bool CheckIfSheetExists(string a_sSheetName)
    {

      var serviceValues = GetSheetsService().Spreadsheets;
      var ssRequest = serviceValues.Get(RegearSheetID);
      Spreadsheet ss = ssRequest.Execute();
      List<string> sheetList = new List<string>();

      foreach (Sheet sheet in ss.Sheets)
      {
        sheetList.Add(sheet.Properties.Title);
      }

      if (sheetList.Contains(a_sSheetName))
      {
        return true;
      }

      return false;
    }
    public static List<string> GetPaychexSheets()
    {
      var serviceValues = GetSheetsService().Spreadsheets;
      var ssRequest = serviceValues.Get(RegearSheetID);
      Spreadsheet ss = ssRequest.Execute();
      List<string> sheetList = new List<string>();

      foreach (Sheet sheet in ss.Sheets)
      {
        if (sheet.Properties.Title.Contains("Paychex") && sheet.Properties.Hidden == null)
        {
          sheetList.Add(sheet.Properties.Title);
        }
      }
      return sheetList;
    }
    public static async Task UnResgisterUserFromDataSources(string a_MemberName, SocketGuildUser? a_SocketUser)
    {


      string? UserName = null;
      if (a_SocketUser != null)
      {
        UserName = (a_SocketUser != null && a_SocketUser.DisplayName != null) ? new PlayerDataLookUps().CleanUpShotCallerName(a_SocketUser.DisplayName) : a_SocketUser.Username;
      }
      else
      {
        UserName = a_MemberName;
      }

      await ClearUserFromSheet(GuildSpreadsheetId, $"Guild Roster!B2:B", "Guild Roster", UserName, 1537937834);
      await ClearUserFromSheet(RegearSheetID, $"Payouts!B2:B", "Payouts", UserName, 649059484);
      await ClearUserFromSheet(RegearSheetID, $"Mini-Market Credits!B2:B", "Mini-Market Credits", UserName, 233056152);
    }
    private static async Task ClearUserFromSheet(string a_sSpreadSheetID, string a_sRange, string a_sSpreadSheetName, string a_sUsername, int a_SheetID)
    {
      var serviceValues = GetSheetsService().Spreadsheets;
      int i = 1;

      ClearValuesRequest requestBody = new ClearValuesRequest();
      //SpreadsheetsResource.ValuesResource.ClearRequest request = null;
      var SheetUserList = serviceValues.Values.Get(a_sSpreadSheetID, a_sRange).Execute().Values;
      SortRangeRequest sortRageRequest = new SortRangeRequest();

      try
      {
        foreach (var socketuser in SheetUserList)
        {
          if (socketuser.Count > 0 && socketuser[0].ToString().ToLower() == a_sUsername.ToLower())
          {
            var col1 = i;
            var col2 = i;

            var request = new Request
            {
              DeleteDimension = new DeleteDimensionRequest
              {
                Range = new DimensionRange
                {
                  SheetId = a_SheetID,
                  Dimension = "ROWS",
                  StartIndex = col1,
                  EndIndex = col1 + 1
                }
              }
            };


            if (a_sSpreadSheetName == "Guild Roster")
            {
              //request = serviceValues.Clear(requestBody, a_sSpreadSheetID, $"{a_sSpreadSheetName}!R{i}C1:R{i}C12");
              //await request.ExecuteAsync();

              //Deletes row from sheet
              var deleteRequest = new BatchUpdateSpreadsheetRequest { Requests = new List<Request> { request } };
              var responseSecondWay = await serviceValues.BatchUpdate(deleteRequest, a_sSpreadSheetID).ExecuteAsync();
            }
            else
            {
              //request = serviceValues.Clear(requestBody, a_sSpreadSheetID, $"{a_sSpreadSheetName}!R{i}C1:R{i}C2");
              //await request.ExecuteAsync();

              var deleteRequest = new BatchUpdateSpreadsheetRequest { Requests = new List<Request> { request } };
              var responseSecondWay = await serviceValues.BatchUpdate(deleteRequest, a_sSpreadSheetID).ExecuteAsync();
            }
            break;
          }
          i++;

        }
      }
      catch (Exception ex)
      {
        Console.WriteLine(ex.ToString());
      }
    }
    public static async Task UpdateRegearRole(List<SocketGuildUser> a_SocketGuildUser, DateTime a_dExpirationDate, RegearTiers a_eRegearTier)
    {
      var serviceValues = GetSheetsService().Spreadsheets.Values;
      string? sSocketUserNameCleaned = "";

      var SheetUserList = serviceValues.Get(GuildSpreadsheetId, $"Guild Roster!B2:D").Execute().Values;

      int i = 2;
      int x = 2;
      try
      {
        foreach (var sheetUser in SheetUserList)
        {
          foreach (var socketuser in a_SocketGuildUser)
          {
            sSocketUserNameCleaned = (socketuser.DisplayName != null) ? new PlayerDataLookUps().CleanUpShotCallerName(socketuser.DisplayName) : socketuser.Username;
            if (sheetUser[0].ToString().ToLower() == sSocketUserNameCleaned.ToLower())
            {
              var col1 = i;
              var col2 = i;
              WriteRange = $"Guild Roster!B{col1}:D{col2}";
              ValueRange rowValues = new ValueRange { Values = new List<IList<object>> { new List<object> { sSocketUserNameCleaned, a_eRegearTier.ToString(), a_dExpirationDate.ToString("M/d/yyyy") } } };
              var update = serviceValues.Update(rowValues, GuildSpreadsheetId, WriteRange);
              update.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
              await update.ExecuteAsync();
              break;
            }
            x++;
          }
          i++;
        }
      }
      catch (Exception ex)
      {
        Console.WriteLine(ex.ToString());
      }
    }
    public static string GetRegearStatus(string a_MemberName)
    {
      var serviceValues = GetSheetsService().Spreadsheets.Values;
      var rowValues = serviceValues.Get(GuildSpreadsheetId, $"Guild Roster!B2:D310").Execute().Values;
      string returnValue = "Not enrolled or regears expired.";

      foreach (var users in rowValues)
      {
        if (users[0].ToString().ToLower() == a_MemberName.ToLower() && users.Count != 1)
        {
          switch (users[1].ToString().ToLower())
          {
            case "bronze":
              returnValue = users[1].ToString() + " expires on " + users[2].ToString();
              break;
            case "silver":
              returnValue = users[1].ToString() + ": No expiration";
              break;
            case "gold":
              returnValue = users[1].ToString() + ": No expiration";
              break;

            default:
              return "Not enrolled or regears expired.";
          }
        }
      }
      return returnValue;
    }
  }
  public static class GoogleSheetHelperMethods
  {
    public static DateTime StartOfWeek(this DateTime dt, DayOfWeek startOfWeek)
    {
      int diff = (7 + (dt.DayOfWeek - startOfWeek)) % 7;
      return dt.AddDays(-1 * diff).Date;
    }

    public static string ToMonthName(this DateTime dateTime)
    {
      return CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(dateTime.Month);
    }

    public static string ToShortMonthName(this DateTime dateTime)
    {
      return CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(dateTime.Month);
    }

  }
}
