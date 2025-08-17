using System;
using System.Collections.Generic;
using System.Linq;
using ConVar;
using Facepunch;
using Facepunch.Math;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using UnityEngine;

// ToDo: better betterchat support

namespace Oxide.Plugins
{
    [Info("Chat Prefix", "Gonzi", "1.2.1")]
    [Description("Chat Prefix per Permission")]
    public class ChatPrefix : RustPlugin
    {
        #region Fields

        [PluginReference] Plugin ColouredChat;
        [PluginReference] Plugin Quests;
        [PluginReference] Plugin BetterChat;

        private Dictionary<ulong, PlayerPrefix> playerPrefixData = new Dictionary<ulong, PlayerPrefix>();

        private class PlayerPrefix
        {
            public string Prefix { get; set; }
            public string Color { get; set; }
            public bool active { get; set; }
        }

        private class PrefixConfig
        {
            public bool Disabled { get; set; }
            public int Priority { get; set; }
            public string Prefix { get; set; }
            public string Color { get; set; }
            public string Permission { get; set; }
            public string GroupName { get; set; }
        }

        private static ConfigData config;

        private class ConfigData
        {
            public bool debug = false;

            [JsonProperty("Use Groupname instead of Permission")]
            public bool useGroup = false;

            public Dictionary<string, PrefixConfig> Prefixes { get; set; }
        }

        #endregion Fields

        #region Configuration

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<ConfigData>();
                if (config == null)
                {
                    LoadDefaultConfig();
                    SaveConfig();
                }
            }
            catch (Exception e)
            {
                Puts("{0} Exception caught.", e);
                PrintError("The configuration file is corrupted! Using Default Config!");
                LoadDefaultConfig();
            }
            RegPerm("reload");
        }

        protected override void LoadDefaultConfig() => config = DefaultConfig();
        protected override void SaveConfig() => Config.WriteObject(config);
        private object GetConfig(string menu, string datavalue, object defaultValue)
        {
            var data = Config[menu] as Dictionary<string, object>;
            if (data == null)
            {
                data = new Dictionary<string, object>();
                Config[menu] = data;
            }

            object value;
            if (!data.TryGetValue(datavalue, out value))
            {
                value = defaultValue;
                data[datavalue] = value;
            }
            return value;
        }

        ConfigData DefaultConfig()
        {
            var DefaultConfig = new ConfigData
            {
                Prefixes = new Dictionary<string, PrefixConfig>
                {
                    {
                        "admin", new PrefixConfig
                        {
                            Disabled = false,
                            Priority = 1,
                            Prefix = "[ADMIN]",
                            Color = "#FF0000",
                            Permission = "admin",
                            GroupName = "admin"
                        }
                    },
                    {
                        "mod", new PrefixConfig
                        {
                            Disabled = false,
                            Priority = 2,
                            Prefix = "[MOD]",
                            Color = "#0000FF",
                            Permission = "mod",
                            GroupName = "mod"
                        }
                    },
                    {
                        "vip", new PrefixConfig
                        {
                            Disabled = false,
                            Priority = 3,
                            Prefix = "[VIP]",
                            Color = "#ffb400",
                            Permission = "vip",
                            GroupName = "vip"
                        }
                    }
                }
            };
            return DefaultConfig;
        }

        #endregion Configuration

        #region Hooks

        // ColouredChat integration
        private string ColChat_GetColName(IPlayer player) => Interface.Oxide.CallHook("API_GetColouredName", player) as string;

        private string ColChat_GetColMessage(IPlayer player, string message) => Interface.Oxide.CallHook("API_GetColouredMessage", player, message) as string;
        // end ColouredChat

        private void OnPlayerConnected(BasePlayer player) => ReloadPrefix(player);

        private void OnUserPermissionGranted(string id, string permName)
        {
            if (config.debug) Puts($"Player '{id}' granted permission: {permName}");
            playerPrefixData.Clear();
            foreach (BasePlayer bplayer in BasePlayer.activePlayerList)
            {
                ReloadPrefix(bplayer);
            }
            return;
        }

        private void OnUserPermissionRevoked(string id, string permName)
        {
            if (config.debug) Puts($"Player '{id}' revoked permission: {permName}");
            playerPrefixData.Clear();
            foreach (BasePlayer bplayer in BasePlayer.activePlayerList)
            {
                ReloadPrefix(bplayer);
            }
        }

        private void OnGroupPermissionGranted(string name, string perm)
        {
            if (config.debug) Puts($"Group '{name}' granted permission: {perm}");
            playerPrefixData.Clear();
            foreach (BasePlayer bplayer in BasePlayer.activePlayerList)
            {
                ReloadPrefix(bplayer);
            }
        }

        private void OnGroupPermissionRevoked(string name, string perm)
        {
            if (config.debug) Puts($"Group '{name}' revoked permission: {perm}");
            playerPrefixData.Clear();
            foreach (BasePlayer bplayer in BasePlayer.activePlayerList)
            {
                ReloadPrefix(bplayer);
            }
        }

        private void OnUserGroupAdded(string id, string groupName)
        {
            if (config.debug) Puts($"Player '{id}' added to group: {groupName}");
            var p = BasePlayer.activePlayerList.Where(pl => pl.UserIDString == id && pl.IsValid() == true).FirstOrDefault();
            if (!p) return;
            ReloadPrefix(p);
        }

        private void OnUserGroupRemoved(string id, string groupName)
        {
            if (config.debug) Puts($"Player '{id}' removed from group: {groupName}");
            var p = BasePlayer.activePlayerList.Where(pl => pl.UserIDString == id && pl.IsValid() == true).FirstOrDefault();
            if (!p) return;
            ReloadPrefix(p);
        }

        private object OnPlayerChat(BasePlayer player, string message, Chat.ChatChannel channel)
        {
            if (config.debug) Puts("OnPlayerChat");

            // Plugin Quests - prevents to send input to globalchat while quest creation is active
            if (QuestsActv() && Quests.Call<bool>("API_GetNotChatOutput", player))
            {
                Puts("Quests Plugin API requests return of msg! - no chat output!");
                return true;
            }

            PlayerPrefix pP;
            playerPrefixData.TryGetValue(player.userID, out pP);
            if (pP == null) return null;

            if (ColouredChatActv())
            {
                var name = Interface.Oxide.CallHook("API_GetColouredName", player.IPlayer) as string;
                var cmsg = ColChat_GetColMessage(player.IPlayer, message);

                Puts(Interface.Oxide.CallHook("API_GetColouredMessage", player.IPlayer, message) as string);

                if (pP.active) SendChatMessage(player, "<color=" + pP.Color + ">", pP.Prefix, name + "</color>" + ":", cmsg, channel);
                else SendChatMessage(player, null, null, name + ":", cmsg, channel);
                return true;
            }
            else
            {
                if (pP.active) SendChatMessage(player, "<color=" + pP.Color + ">", pP.Prefix, player.displayName + "</color>" + ":", message, channel);
                else return null;
            }
            return true;
        }

        // cancel message to prevent duplicate messages - // ToDo: Add better support (Username etc.)
        private object OnBetterChat(Dictionary<string, object> dict)
        {
            var player = (dict["Player"] as IPlayer).Object as BasePlayer;
            if (player == null) return null;
            dict["CancelOption"] = 2;
            return dict;
        }

        // always return to prevent double messages
        private object OnColouredChat(Dictionary<string, object> dict)
        {
            return false;
        }

        #endregion Hooks

        #region Util

        private object SendChatMessage(BasePlayer player, string pColor, string prefix, string displayname, string message, Chat.ChatChannel channel)
        {
            // if (Chat.serverlog && player.IsValid())
            // {
            //     object[] logMsgArr = new object[] { ConsoleColor.DarkYellow, null, null, null };
            //     logMsgArr[1] = string.Concat(new object[] { "[", channel, "] ", player.displayName.EscapeRichText(), ": " });
            //     logMsgArr[2] = ConsoleColor.DarkGreen;
            //     logMsgArr[3] = new System.Text.RegularExpressions.Regex("<[^>]*>").Replace(string.Join(" ", message), "");
            //     ServerConsole.PrintColoured(logMsgArr);
            // }

            RCon.Broadcast(RCon.LogType.Chat, new Chat.ChatEntry
            {
                Channel = channel,
                Message = new System.Text.RegularExpressions.Regex("<[^>]*>").Replace(string.Join(" ", message), ""),
                UserId = player.IPlayer.Id,
                Username = player.displayName,
                Color = pColor,
                Time = Epoch.Current
            });

            switch ((int)channel)
            {
                // global chat
                case 0:
                    if (config.debug) Puts("default / global chat");

                    var gMsg = ArrayPool.Get(3);
                    gMsg[0] = (int)channel;
                    gMsg[1] = player.UserIDString;

                    foreach (BasePlayer p in BasePlayer.activePlayerList.Where(p => p.IsValid() == true))
                    {
                        gMsg[2] = $"{pColor}{Lang(prefix, p.UserIDString)} {displayname} {message}";
                        p.SendConsoleCommand("chat.add", gMsg);
                        if (config.debug) Puts("sended GLOBAL message (" + message + ") to " + p.displayName);
                    }
                    ArrayPool.Free(gMsg);
                    break;

                // team channel
                case 1:
                    if (config.debug) Puts("team chat");

                    var tMsg = ArrayPool.Get(3);
                    tMsg[0] = (int)channel;
                    tMsg[1] = player.UserIDString;

                    foreach (BasePlayer p in BasePlayer.activePlayerList.Where(p => p.Team != null && player.Team != null && p.Team.teamID == player.Team.teamID && p.IsValid() == true))
                    {
                        tMsg[2] = $"{pColor}{Lang(prefix, p.UserIDString)} {displayname} {message}";
                        p.SendConsoleCommand("chat.add", tMsg);
                        if (config.debug) Puts("sended GLOBAL message (" + message + ") to " + p.displayName);
                    }
                    ArrayPool.Free(tMsg);
                    break;

                default:
                    break;
            }
            return true;
        }

        private void RegPerm(string name)
        {
            if (permission.PermissionExists("chatprefix." + name, this)) return;
            permission.RegisterPermission("chatprefix." + name, this);
            if (config.debug) Puts("Registered permission: chatprefix." + name);
            return;
        }

        private object ReloadPrefix(BasePlayer player)
        {
            if (config.debug) Puts("ReloadPrefix for " + player.displayName);
            if (!player.IsValid()) return false;

            playerPrefixData.Remove(player.userID);

            bool foundPrefixForPly = false;
            foreach (var p in config.Prefixes.OrderBy(x => x.Value.Priority))
            {
                if (foundPrefixForPly) return true;
                if (permission.UserHasPermission(player.UserIDString, p.Value.Permission) && !config.useGroup && !p.Value.Disabled || permission.UserHasGroup(player.UserIDString, p.Value.GroupName) && config.useGroup && !p.Value.Disabled)
                {
                    foundPrefixForPly = true;
                    if (config.debug) Puts("Found Prefix for " + player.displayName + " Prefix: " + p.Value.Prefix + " Prefixcolor" + p.Value.Color + " Permission:" + p.Value.Permission + " GroupName:" + p.Value.GroupName);
                    playerPrefixData.Add(player.userID, new PlayerPrefix { Prefix = p.Value.Prefix, Color = p.Value.Color, active = true });
                }
            }

            if (!foundPrefixForPly) playerPrefixData.Add(player.userID, new PlayerPrefix { Prefix = null, Color = null, active = false });
            return true;
        }

        private bool QuestsActv() => (Quests != null && Quests.IsLoaded);
        private bool ColouredChatActv() => (ColouredChat != null && ColouredChat.IsLoaded);
        private bool BetterChatActv() => (BetterChat != null && BetterChat.IsLoaded);
        #endregion

        #region Localization

        protected override void LoadDefaultMessages()
        {
            var messages = new Dictionary<string, string>();
            foreach (var pCfg in config.Prefixes.OrderBy(x => x.Value.Priority))
            {
                messages.Add(pCfg.Value.Prefix, pCfg.Value.Prefix);
                RegPerm(pCfg.Value.Permission);
            }

            messages.Add("xPrefixesReloaded", "Prefix for {0} players was reloaded!");

            lang.RegisterMessages(messages, this, "en");

            playerPrefixData.Clear();
            foreach (BasePlayer bplayer in BasePlayer.activePlayerList)
            {
                ReloadPrefix(bplayer);
            }
        }

        private string Lang(string key, string id = null, params object[] args)
        {
            if (key == null) return null;
            return string.Format(lang.GetMessage(key, this, id), args);
        }

        #endregion

        #region API

        private string API_GetPrefixedMessageForPlayer(BasePlayer player, string messageText)
        {
            PlayerPrefix pP;
            playerPrefixData.TryGetValue(player.userID, out pP);
            if (pP == null || !pP.active) return null;

            if (ColouredChat)
            {
                if (pP.active) return "<color=" + pP.Color + ">" + pP.Prefix + " " + ColChat_GetColName(player.IPlayer) + "</color>:" + ColChat_GetColMessage(player.IPlayer, messageText);
                else return null;
            }
            else
            {
                if (pP.active) return "<color=" + pP.Color + ">" + pP.Prefix + " " + player.displayName + "</color>:" + messageText;
                else return null;
            }
        }

        private bool API_ReloadPrefixForPlayer(BasePlayer player)
        {
            if (player != null)
            {
                ReloadPrefix(player);
                return true;
            }
            else return false;
        }

        private bool API_ReloadPrefixForAllPlayers()
        {
            playerPrefixData.Clear();
            foreach (BasePlayer bplayer in BasePlayer.activePlayerList)
            {
                ReloadPrefix(bplayer);
            }
            return true;
        }

        #endregion
    }
}