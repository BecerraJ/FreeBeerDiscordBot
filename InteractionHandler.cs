﻿using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using System.Reflection;
using System;
using System.Threading.Tasks;
using GoogleSheetsData;
using PlayerData;

namespace InteractionHandlerService
{
    public class InteractionHandler
    {
        private readonly DiscordSocketClient _client;
        private readonly InteractionService _commands;
        private readonly IServiceProvider _services;

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
            _client.InteractionCreated += HandleInteraction;
            _client.ThreadCreated += ThreadCreationExecuted;

            // Process the command execution results 
            _commands.SlashCommandExecuted += SlashCommandExecuted;
            _commands.ContextCommandExecuted += ContextCommandExecuted;
            _commands.ComponentCommandExecuted += ComponentCommandExecuted;
            _commands.ModalCommandExecuted += ModalCommandExecuted;

        }

        private Task ComponentCommandExecuted(ComponentCommandInfo arg1, Discord.IInteractionContext arg2, IResult arg3)
        {
            return Task.CompletedTask;
        }

        private Task ContextCommandExecuted(ContextCommandInfo arg1, Discord.IInteractionContext arg2, IResult arg3)
        {
            return Task.CompletedTask;
        }

        private Task SlashCommandExecuted(SlashCommandInfo arg1, Discord.IInteractionContext arg2, IResult arg3)
        {
            return Task.CompletedTask;
        }
        private Task ModalCommandExecuted(ModalCommandInfo arg1, Discord.IInteractionContext arg2, IResult arg3)
        {
            return Task.CompletedTask;
        }

        private ulong ChannelThreadId { get; set; }
        private async Task ThreadCreationExecuted (SocketThreadChannel arg)
        {
            
            string? sUserNickname = (arg.Owner.Nickname != null) ? arg.Owner.Nickname : arg.Owner.Username;
            if (sUserNickname.Contains("!sl"))
            {
                sUserNickname = new PlayerDataLookUps().CleanUpShotCallerName(sUserNickname);
            }

            //return Task.CompletedTask;
            if (arg.ParentChannel.Id == 1083448728632954950 && ChannelThreadId != arg.Owner.Thread.Id)
            {
                ChannelThreadId = arg.Owner.Thread.Id;
                string miniMarketCreditsTotal = GoogleSheetsDataWriter.GetMiniMarketCredits(sUserNickname);
                await arg.SendMessageAsync($"{sUserNickname} Mini market credits balance: {miniMarketCreditsTotal}");
            }
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
    }
}
