﻿using Discord.WebSocket;
using IRSE.Managers.Plugins;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Game.Server;
using IRSE.Managers;
using Game.ClientServer.Packets;
using Game.Framework.Networking;
using System.Linq.Expressions;
using System.Configuration;
using IRSE.Managers.Events;

namespace IRSEDiscordChatBot
{
    [Plugin(API = "1.0.0", Author = "generalwrex", Description = "Links Chat to/from discord via a bot", Name = "IRSEDiscordChatBot", Version = "1.0.0")]
    public class PluginMain : PluginBase
    {
        private static bool debugMode;
        private static ulong channelID;
        private static DiscordClient discordClient;


        public PluginMain()
        {
           
              
        }
 
        public override void Init(string modDirectory)
        {
            try
            {

                MyConfig.FileName = Path.Combine(modDirectory, "Config.xml");

                var config = new MyConfig();
                debugMode = config.Settings.DebugMode;
                channelID = config.Settings.MainChannelID;

                try
                {
                    discordClient = new DiscordClient(config);

                    DiscordClient.SocketClient.MessageReceived += SocketClient_MessageReceived;

                    DiscordClient.Instance.Start();
                }
                catch (Exception ex1)
                {
                    GetLogger.Warn(ex1, "HESDiscordChatBot Discord Initialization failed.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                GetLogger.Warn(ex, "HESDiscordChatBot Initialization failed.");
            }
          
        }


        [IRSEEvent(EventType = typeof(ClientRespawn))]
        public async void OnPacketRespawn(GenericEvent evt) { 

            if (MyConfig.Instance.Settings.PlayerRespawningMessage.StartsWith("null")) return;

            if (debugMode)
                Console.WriteLine($"<- Sending Respawn Message To Discord");

            Player player = evt.Data.DeserializedObject as Player;

            if (player == null)
                return;

            string outMsg = MyConfig.Instance.Settings.PlayerRespawningMessage.Replace("(%PlayerName%)", player.Name).Replace("(%CurrentDateTime%)", new DateTime().ToString())
                ?? $"Player '{player.Name}' is respawning on the game server.";

            await(DiscordClient.SocketClient.GetChannel(MyConfig.Instance.Settings.MainChannelID) as Discord.IMessageChannel).SendMessageAsync(outMsg);
        }

        [IRSEEvent(EventType = typeof(ClientConnected))]
        public async void Players_OnAddPlayer(GenericEvent evt)
        {
            if (MyConfig.Instance.Settings.NewPlayerSpawningMessage.StartsWith("null")) return;

            if (debugMode)
                Console.WriteLine($"<- Sending Player Spawn Message To Discord");

            Player player = evt.Data.DeserializedObject as Player;

            if (player == null)
                return;

            string outMsg = MyConfig.Instance.Settings.NewPlayerSpawningMessage.Replace("(%PlayerName%)", player.Name).Replace("(%CurrentDateTime%)", new DateTime().ToString())
                ?? $"A player '{player.Name}' has connected to the game server.";

            await(DiscordClient.SocketClient.GetChannel(MyConfig.Instance.Settings.MainChannelID) as Discord.IMessageChannel).SendMessageAsync(outMsg);
        }

        [IRSEEvent(EventType = typeof(ClientDisconnected))]
        public async void Players_OnRemovePlayer(GenericEvent evt)
        {
            if (MyConfig.Instance.Settings.PlayerLeavingMessage.StartsWith("null")) return;

            if (debugMode)
                Console.WriteLine($"<- Sending Disconnect Message To Discord");

            Player player = evt.Data.DeserializedObject as Player;

            if (player == null)
                return;

            string outMsg = MyConfig.Instance.Settings.PlayerLeavingMessage.Replace("(%PlayerName%)", player.Name).Replace("(%CurrentDateTime%)", new DateTime().ToString())
                ?? $"{player.Name} disconnected from the game server.";

            await (DiscordClient.SocketClient.GetChannel(MyConfig.Instance.Settings.MainChannelID) as Discord.IMessageChannel).SendMessageAsync(outMsg);
        }

        [IRSEEvent(EventType = typeof(ClientChatMessage))]
        public async void OnPacketChatMessage(GenericEvent data)
        {
            if (MyConfig.Instance.Settings.MessageSentToDiscord.StartsWith("null")) return;

            try
            {
                Player player = ServerInstance.Instance.Handlers
                    .PlayerHandler.Players.GetPlayerByConnectionId((long)data.Data.OriginalMessage.SenderConnectionId);


                string name = player.Name;
                string message = ((ClientChatMessage)data.Data.DeserializedObject).message;

                
                if (debugMode)
                    Console.WriteLine($"<- Sending Message To Discord");


                string outMsg = MyConfig.Instance.Settings.MessageSentToDiscord.Replace("(%PlayerName%)", name).Replace("(%ChatMessage%)", message).Replace("(%CurrentDateTime%)", new DateTime().ToString())
                    ?? $"Game Server - {name}: {message} ";


                await(DiscordClient.SocketClient.GetChannel(MyConfig.Instance.Settings.MainChannelID) as Discord.IMessageChannel).SendMessageAsync(outMsg);

                if (MyConfig.Instance.Settings.PrintDiscordChatToConsole)
                    Console.WriteLine(outMsg);

            }
            catch (Exception ex)
            {
                GetLogger.Error(ex.ToString());
            }
        }

        private Task SocketClient_MessageReceived(SocketMessage messageParam)
        {
            var message = messageParam as SocketUserMessage;
            string outMsg = String.Empty;

            if (message != null)
            {
                // if the channel is the main channel
                if (message.Channel.Id == MyConfig.Instance.Settings.MainChannelID)
                {
                    // if the sender isn't this bot
                    if (message.Author.Id != MyConfig.Instance.Settings.BotClientID)
                    {

                        outMsg = MyConfig.Instance.Settings.MessageSentToGameServer
                            .Replace("(%DiscordChannelName%)", message.Channel.Name)
                            .Replace("(%DiscordUserName%)", message.Author.Username)
                            .Replace("(%ChatMessage%)", message.Content)
                            .Replace("(%CurrentDateTime%)", new DateTime().ToString())
                            ?? $"Discord - {message.Author.Username}: {message.Content}";

                        GetControllers.Chat.SendToAll(Game.Configuration.Config.Singleton.AllChatColor, outMsg);

                        
                        if (debugMode)
                            Console.WriteLine($"-> Got Message From Discord: {outMsg}");
                    }
                }
            }

            return Task.Run(() => Console.WriteLine(!MyConfig.Instance.Settings.PrintDiscordChatToConsole ? String.Empty : outMsg));
        }


    }
}