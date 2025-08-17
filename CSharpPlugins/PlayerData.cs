using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using Oxide.Core.Libraries.Covalence;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core;
using System;

namespace Oxide.Plugins
{
    [Info("PlayerData", "UndercoverNL", "1.0.0")]
    [Description("Plugin for the Pterodactyl Rust Player Manager addon")]
    class PlayerData : CovalencePlugin
    {
        private Dictionary<ulong, int> playerKills;
        private Dictionary<ulong, int> playerDeaths;
        private Dictionary<ulong, double> playerPlaytime;
        private Dictionary<ulong, DateTime> loginTimes;

        void OnServerInitialized()
        {
            playerKills = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, int>>("PlayerData_kills") ?? new Dictionary<ulong, int>();
            playerDeaths = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, int>>("PlayerData_deaths") ?? new Dictionary<ulong, int>();
            playerPlaytime = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, double>>("PlayerData_playtime") ?? new Dictionary<ulong, double>();
            loginTimes = new Dictionary<ulong, DateTime>();
        }

        void OnServerSave()
        {
            SaveData();
        }

        void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject("PlayerData_kills", playerKills);
            Interface.Oxide.DataFileSystem.WriteObject("PlayerData_deaths", playerDeaths);
            Interface.Oxide.DataFileSystem.WriteObject("PlayerData_playtime", playerPlaytime);
        }

        void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            var victim = entity as BasePlayer;
            if (victim == null || info == null) return;

            ulong victimId = victim.userID;
            if (playerDeaths.ContainsKey(victimId))
                playerDeaths[victimId]++;
            else
                playerDeaths[victimId] = 1;

            var killer = info.Initiator as BasePlayer;
            if (killer != null && !killer.Equals(victim))
            {
                ulong killerId = killer.userID;
                if (playerKills.ContainsKey(killerId))
                    playerKills[killerId]++;
                else
                    playerKills[killerId] = 1;
            }

            SaveData();
        }

        void OnUserConnected(IPlayer player)
        {
            var basePlayer = player.Object as BasePlayer;
            if (basePlayer == null) return;

            loginTimes[basePlayer.userID] = DateTime.UtcNow;
        }

        void OnUserDisconnected(IPlayer player)
        {
            var basePlayer = player.Object as BasePlayer;
            if (basePlayer == null) return;

            ulong id = basePlayer.userID;
            if (loginTimes.TryGetValue(id, out DateTime loginTime))
            {
                double sessionMinutes = (DateTime.UtcNow - loginTime).TotalMinutes;

                if (playerPlaytime.ContainsKey(id))
                    playerPlaytime[id] += sessionMinutes;
                else
                    playerPlaytime[id] = sessionMinutes;

                loginTimes.Remove(id);
                SaveData();
            }
        }

        [Command("playerdata.data")]
        void GetPlayerStatsCommand(IPlayer caller, string command, string[] args)
        {
            if (!caller.IsAdmin)
            {
                caller.Reply("You do not have permission to use this command.");
                return;
            }

            if (args.Length != 1)
            {
                caller.Reply("Usage: /playerdata.vitals <username>");
                return;
            }

            var target = players.FindPlayer(args[0]);
            if (target == null || !target.IsConnected)
            {
                caller.Reply($"Player '{args[0]}' not found or not online.");
                return;
            }

            var basePlayer = target.Object as BasePlayer;
            if (basePlayer == null)
            {
                caller.Reply("Failed to get player object.");
                return;
            }

            var id = basePlayer.userID;
            float health = basePlayer.health;
            float calories = basePlayer.metabolism.calories.value;
            float hydration = basePlayer.metabolism.hydration.value;
            var position = basePlayer.transform.position;
            int kills = playerKills.ContainsKey(id) ? playerKills[id] : 0;
            int deaths = playerDeaths.ContainsKey(id) ? playerDeaths[id] : 0;
            double playtime = playerPlaytime.ContainsKey(id) ? playerPlaytime[id] : 0;
            string playtimeFormatted = FormatPlaytime(playtime);

            caller.Reply($"Id: {id}\n" +
                         $"Health: {health:F0}\n" +
                         $"Food: {calories:F0}\n" +
                         $"Water: {hydration:F0}\n" +
                         $"Position: {position}\n" +
                         $"Kills: {kills}\n" +
                         $"Deaths: {deaths}\n" +
                         $"Playtime: {playtimeFormatted}");
        }

        [Command("playerdata.health")]
        private void HealthCommand(IPlayer caller, string command, string[] args)
        {
            if (!caller.IsAdmin)
            {
                caller.Reply("You do not have permission to use this command.");
                return;
            }

            if (args.Length != 2)
            {
                caller.Reply("Usage: /playerdata.health <username> <amount>");
                return;
            }

            var target = players.FindPlayer(args[0]);
            if (target == null || !target.IsConnected)
            {
                caller.Reply($"Player '{args[0]}' not found or not online.");
                return;
            }

            if (!float.TryParse(args[1], out float amount))
            {
                caller.Reply("Invalid amount. Must be a number (positive or negative).");
                return;
            }

            var basePlayer = target.Object as BasePlayer;
            if (basePlayer == null)
            {
                caller.Reply("Failed to get player object.");
                return;
            }

            if (amount > 0f)
            {
                basePlayer.Heal(amount);
                basePlayer.metabolism.bleeding.value = 0f;
                basePlayer.metabolism.poison.value = 0f;
                basePlayer.CancelInvoke("BleedingOut");
                caller.Reply($"Player '{target.Name}' healed for {amount} HP.");
            }
            else if (amount < 0f)
            {
                basePlayer.Hurt(-amount);
                caller.Reply($"Player '{target.Name}' took {-amount} damage.");
            }
            else
            {
                caller.Reply("Amount is zero — no effect applied.");
            }
        }

        [Command("playerdata.food")]
        private void FoodCommand(IPlayer caller, string command, string[] args)
        {
            if (!caller.IsAdmin)
            {
                caller.Reply("You do not have permission to use this command.");
                return;
            }

            if (args.Length != 2)
            {
                caller.Reply("Usage: /playerdata.food <username> <amount>");
                return;
            }

            var target = players.FindPlayer(args[0]);
            if (target == null || !target.IsConnected)
            {
                caller.Reply($"Player '{args[0]}' not found or not online.");
                return;
            }

            if (!float.TryParse(args[1], out float amount))
            {
                caller.Reply("Invalid amount. Must be a number.");
                return;
            }

            var basePlayer = target.Object as BasePlayer;
            if (basePlayer == null)
            {
                caller.Reply("Failed to get player object.");
                return;
            }

            var calories = basePlayer.metabolism.calories;
            calories.value = Clamp(calories.value + amount, calories.min, calories.max);

            caller.Reply($"Player '{target.Name}' food adjusted by {amount}. New value: {calories.value:F0}");
        }

        [Command("playerdata.water")]
        private void WaterCommand(IPlayer caller, string command, string[] args)
        {
            if (!caller.IsAdmin)
            {
                caller.Reply("You do not have permission to use this command.");
                return;
            }

            if (args.Length != 2)
            {
                caller.Reply("Usage: /playerdata.water <username> <amount>");
                return;
            }

            var target = players.FindPlayer(args[0]);
            if (target == null || !target.IsConnected)
            {
                caller.Reply($"Player '{args[0]}' not found or not online.");
                return;
            }

            if (!float.TryParse(args[1], out float amount))
            {
                caller.Reply("Invalid amount. Must be a number.");
                return;
            }

            var basePlayer = target.Object as BasePlayer;
            if (basePlayer == null)
            {
                caller.Reply("Failed to get player object.");
                return;
            }

            var hydration = basePlayer.metabolism.hydration;
            hydration.value = Clamp(hydration.value + amount, hydration.min, hydration.max);

            caller.Reply($"Player '{target.Name}' water adjusted by {amount}. New value: {hydration.value:F0}");
        }

        [Command("playerdata.inventory")]
        private void InventoryCommand(IPlayer caller, string command, string[] args)
        {
            if (!caller.IsAdmin)
            {
                caller.Reply("You do not have permission to use this command.");
                return;
            }

            if (args.Length != 1)
            {
                caller.Reply("Usage: /playerdata.inventory <username>");
                return;
            }

            var target = players.FindPlayer(args[0]);
            if (target == null || !target.IsConnected)
            {
                caller.Reply($"Player '{args[0]}' not found or not online.");
                return;
            }

            var basePlayer = target.Object as BasePlayer;
            if (basePlayer == null)
            {
                caller.Reply("Failed to get player object.");
                return;
            }

            var inventoryData = new List<Dictionary<string, object>>();

            var containers = new Dictionary<string, ItemContainer>
            {
                { "main", basePlayer.inventory.containerMain },
                { "belt", basePlayer.inventory.containerBelt },
                { "wear", basePlayer.inventory.containerWear }
            };

            foreach (var kvp in containers)
            {
                var containerName = kvp.Key;
                var container = kvp.Value;

                if (container == null) continue;

                foreach (var item in container.itemList)
                {
                    inventoryData.Add(new Dictionary<string, object>
                    {
                        { "name", item.info.displayName.english },
                        { "shortname", item.info.shortname },
                        { "amount", item.amount },
                        { "skin", item.skin },
                        { "condition", item.condition },
                        { "maxCondition", item.maxCondition },
                        { "position", item.position },
                        { "container", containerName }
                    });
                }
            }

            if (inventoryData.Count == 0)
            {
                caller.Reply($"Speler '{target.Name}' heeft een lege inventory.");
                return;
            }

            string json = JsonConvert.SerializeObject(inventoryData, Formatting.None);
            caller.Reply(json);
        }

        [Command("playerdata.backpack")]
        private void BackpackCommand(IPlayer caller, string command, string[] args)
        {
            if (!caller.IsAdmin)
            {
                caller.Reply(JsonConvert.SerializeObject(new
                {
                    error = "You do not have permission to use this command."
                }));
                return;
            }

            if (args.Length != 1)
            {
                caller.Reply(JsonConvert.SerializeObject(new
                {
                    error = "Usage: /playerdata.backpack <username>"
                }));
                return;
            }

            var target = players.FindPlayer(args[0]);
            if (target == null || !target.IsConnected)
            {
                caller.Reply(JsonConvert.SerializeObject(new
                {
                    error = $"Player '{args[0]}' not found or not online."
                }));
                return;
            }

            var basePlayer = target.Object as BasePlayer;
            if (basePlayer == null)
            {
                caller.Reply(JsonConvert.SerializeObject(new
                {
                    error = "Failed to get player object."
                }));
                return;
            }

            var backpackItem = basePlayer.inventory.containerWear?.itemList
                ?.FirstOrDefault(item => item.info.shortname == "largebackpack" || item.info.shortname == "smallbackpack");

            if (backpackItem == null)
            {
                caller.Reply("null");
                return;
            }

            string backpackType = backpackItem.info.shortname == "largebackpack" ? "Large Backpack" : "Small Backpack";

            var backpackData = new List<Dictionary<string, object>>();
            var backpackContainer = backpackItem.contents;

            if (backpackContainer != null)
            {
                foreach (var item in backpackContainer.itemList)
                {
                    backpackData.Add(new Dictionary<string, object>
                    {
                        { "name", item.info.displayName.english },
                        { "shortname", item.info.shortname },
                        { "amount", item.amount },
                        { "skin", item.skin },
                        { "condition", item.condition },
                        { "maxCondition", item.maxCondition },
                        { "position", item.position }
                    });
                }
            }

            var result = new Dictionary<string, object>
            {
                { "type", backpackType },
                { "items", backpackData }
            };

            caller.Reply(JsonConvert.SerializeObject(result, Formatting.None));
        }

        [Command("playerdata.removeitem")]
        private void RemoveItemBySlotCommand(IPlayer caller, string command, string[] args)
        {
            if (!caller.IsAdmin)
            {
                caller.Reply("You do not have permission to use this command.");
                return;
            }

            if (args.Length != 2)
            {
                caller.Reply("Usage: /playerdata.removeitem <username> <container.slot> (e.g. main.7 or backpack.3)");
                return;
            }

            var target = players.FindPlayer(args[0]);
            if (target == null || !target.IsConnected)
            {
                caller.Reply($"Player '{args[0]}' not found or not online.");
                return;
            }

            var basePlayer = target.Object as BasePlayer;
            if (basePlayer == null)
            {
                caller.Reply("Failed to get player object.");
                return;
            }

            var parts = args[1].Split('.');
            if (parts.Length != 2 || !int.TryParse(parts[1], out int slot))
            {
                caller.Reply("Invalid format. Use <container.slot>, e.g. main.7 or backpack.3");
                return;
            }

            string containerName = parts[0].ToLower();
            ItemContainer container = containerName switch
            {
                "main" => basePlayer.inventory.containerMain,
                "belt" => basePlayer.inventory.containerBelt,
                "wear" => basePlayer.inventory.containerWear,
                "backpack" => GetBackpackContainer(basePlayer),
                _ => null
            };

            if (container == null)
            {
                caller.Reply($"Invalid container '{containerName}' or player has no backpack.");
                return;
            }

            var item = container.itemList.FirstOrDefault(i => i.position == slot);
            if (item == null)
            {
                caller.Reply($"No item found in {containerName}.{slot} for player '{target.Name}'.");
                return;
            }

            string itemName = item.info.displayName.english;
            item.Remove();

            caller.Reply($"Removed item '{itemName}' from {containerName}.{slot} of player '{target.Name}'.");
        }

        [Command("playerdata.tp")]
        private void TeleportPlayerCommand(IPlayer caller, string command, string[] args)
        {
            if (!caller.IsAdmin)
            {
                caller.Reply("You do not have permission to use this command.");
                return;
            }

            if (args.Length != 4)
            {
                caller.Reply("Usage: /playerdata.tp <username> <x> <y> <z>");
                return;
            }

            var target = players.FindPlayer(args[0]);
            if (target == null || !target.IsConnected)
            {
                caller.Reply($"Player '{args[0]}' not found or not online.");
                return;
            }

            if (!float.TryParse(args[1], out float x) ||
                !float.TryParse(args[2], out float y) ||
                !float.TryParse(args[3], out float z))
            {
                caller.Reply("Invalid coordinates. All values must be numbers.");
                return;
            }

            var basePlayer = target.Object as BasePlayer;
            if (basePlayer == null)
            {
                caller.Reply("Failed to get player object.");
                return;
            }

            basePlayer.Teleport(new UnityEngine.Vector3(x, y, z));
            caller.Reply($"Teleported '{target.Name}' to position ({x}, {y}, {z}).");
        }

        [Command("playerdata.tpto")]
        private void TeleportPlayerToPlayerCommand(IPlayer caller, string command, string[] args)
        {
            if (!caller.IsAdmin)
            {
                caller.Reply("You do not have permission to use this command.");
                return;
            }

            if (args.Length != 2)
            {
                caller.Reply("Usage: /playerdata.tpto <player_to_teleport> <target_player>");
                return;
            }

            var playerToTeleport = players.FindPlayer(args[0]);
            var targetPlayer = players.FindPlayer(args[1]);

            if (playerToTeleport == null || !playerToTeleport.IsConnected)
            {
                caller.Reply($"Player '{args[0]}' not found or not online.");
                return;
            }

            if (targetPlayer == null || !targetPlayer.IsConnected)
            {
                caller.Reply($"Player '{args[1]}' not found or not online.");
                return;
            }

            var basePlayerToTeleport = playerToTeleport.Object as BasePlayer;
            var baseTargetPlayer = targetPlayer.Object as BasePlayer;

            if (basePlayerToTeleport == null || baseTargetPlayer == null)
            {
                caller.Reply("Failed to get player objects.");
                return;
            }

            basePlayerToTeleport.Teleport(baseTargetPlayer.transform.position);
            caller.Reply($"Teleported '{playerToTeleport.Name}' to '{targetPlayer.Name}'.");
        }

        [Command("playerdata.whisper")]
        private void WhisperCommand(IPlayer caller, string command, string[] args)
        {
            if (args.Length < 2)
            {
                caller.Reply("Usage: /playerdata.whisper <username> <message>");
                return;
            }

            var target = players.FindPlayer(args[0]);
            if (target == null || !target.IsConnected)
            {
                caller.Reply($"Player '{args[0]}' not found or not online.");
                return;
            }

            string message = string.Join(" ", args.Skip(1));

            string callerName = caller?.Name ?? "Server";

            target.Message($"Whisper from {callerName}: {message}");

            if (caller?.Id != "server_console")
            {
                caller.Reply($"You whispered to {target.Name}: {message}");
            }
        }

        private ItemContainer GetBackpackContainer(BasePlayer player)
        {
            var backpackItem = player.inventory.containerWear?.itemList
                ?.FirstOrDefault(item => item.info.shortname == "largebackpack" || item.info.shortname == "smallbackpack");

            return backpackItem?.contents;
        }

        // ======== Helper: FormatPlaytime ========
        private string FormatPlaytime(double totalMinutes)
        {
            int hours = (int)(totalMinutes / 60);
            int minutes = (int)(totalMinutes % 60);
            return $"{hours}h {minutes}m";
        }

        // ======== Helper: Clamp ========
        private float Clamp(float value, float min, float max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}
