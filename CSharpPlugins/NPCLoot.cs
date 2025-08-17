using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using WebSocketSharp;

namespace Oxide.Plugins
{
    [Info("NPCLoot", "tofurahie", "1.2.7")]
    internal class NPCLoot : RustPlugin
    {
        #region Static

        private const string Layer = "UI_NPCLoot";
        private Configuration _config;
        private List<ItemDefinition> WeareableItems = new List<ItemDefinition>();
        private List<ItemDefinition> UsableItems = new List<ItemDefinition>();

        #region Classes

        private class ItemInfo
        {
            public string shortName = "rifle.ak";
            public int itemID = 1545779598;
            public ulong skin = 0;
            public int minAmount = 1;
            public int maxAmount = 1;
            public int minCondition = 100;
            public int maxCondition = 100;
            public int dropChance = 100;
            public bool isBlueprint = false;
        }

        private class BotContains
        {
            [JsonProperty("Weapon [Shortname]")]
            public string Weapon;
            public BotSkin[] BotSkins = new BotSkin[7];
            public List<LootTable> LootTables = new List<LootTable>();
        }
        private class BotSkin
        {
            public int itemID = 1266491000;
            public ulong skin = 0;
        }

        private class LootTable 
        {
            public int minItemAmount = 1;
            public int maxItemAmount = 2;
            public List<ItemInfo> items = new List<ItemInfo>();
        }

        private class Configuration
        {
            public Dictionary<string, BotContains> LootTables = new Dictionary<string, BotContains>(); 
        }

        #endregion

        #endregion
        
        #region Config

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                _config = Config.ReadObject<Configuration>();
                if (_config == null) throw new Exception();
                SaveConfig();
            }
            catch
            {
                PrintError("Your configuration file contains an error. Using default configuration values.");
                LoadDefaultConfig();
            }
        }

        protected override void SaveConfig() => Config.WriteObject(_config);

        protected override void LoadDefaultConfig() => _config = new Configuration();

        #endregion

        #region OxideHooks

        private void OnServerInitialized()
        {
            LoadConfig();
            foreach (var check in ItemManager.itemList)
            {
                if (check.ItemModWearable != null) WeareableItems.Add(check);
                else UsableItems.Add(check);
            }
        }

        private void Unload()
        {
            foreach (var check in BasePlayer.activePlayerList) 
                CuiHelper.DestroyUi(check, Layer + ".bg");
        }

        private void OnCorpsePopulate(HumanNPC player, NPCPlayerCorpse corpse)
        {
            if (corpse == null || player == null) 
                return;

            if (Interface.CallHook("CanModifySpawnedBot", player) != null)
                return;

            if (corpse == null || string.IsNullOrEmpty(corpse.parentEnt?.PrefabName) || !_config.LootTables.TryGetValue(corpse.parentEnt.PrefabName, out BotContains contains)) 
                return;

            NextTick(() =>
            {   
                if (contains?.LootTables == null || contains.LootTables.Count == 0 || corpse == null || corpse.containers == null || corpse.containers.Length == 0) 
                    return;
                
                var tables = contains.LootTables;
                foreach (var check in corpse.containers) 
                    check?.itemList?.Clear();


                var lootTable = tables.GetRandom();
                if (lootTable.items.Count == 0)
                    return;

                var count = 0;
                while (count < lootTable.minItemAmount)
                {
                    foreach (var spawnableItem in lootTable.items)
                    {
                        if (spawnableItem.dropChance <= UnityEngine.Random.Range(0, 101) || lootTable.maxItemAmount <= count)
                            continue;
                    
                        var item = spawnableItem.isBlueprint ? ItemManager.CreateByName("blueprintbase", UnityEngine.Random.Range(spawnableItem.minAmount, spawnableItem.maxAmount)) : ItemManager.CreateByItemID(spawnableItem.itemID, UnityEngine.Random.Range(spawnableItem.minAmount, spawnableItem.maxAmount + 1), spawnableItem.skin);

                        if (item == null)
                            return;
                    
                        if (spawnableItem.isBlueprint) 
                            item.blueprintTarget = spawnableItem.itemID;
                    
                        else item.condition = item.maxCondition * (UnityEngine.Random.Range(spawnableItem.minCondition, spawnableItem.maxCondition + 1) / 100f);
                        item.MarkDirty();
                        item.MoveToContainer(corpse.containers[0]);
                        count++; 
                    }
                }
            });
        }

        private void OnEntitySpawned(HumanNPC bot)
        {
            if (bot == null || !_config.LootTables.ContainsKey(bot.PrefabName) || bot.skinID == 14922524 || bot.skinID == 11162132011012)
                return;

            if (Interface.CallHook("CanModifySpawnedBot", bot) != null)
                return;

            if (_config.LootTables[bot.PrefabName].BotSkins.Length == 0) 
                return;
            
            var data = _config.LootTables[bot.PrefabName].BotSkins;
            var botInventory = bot.inventory.containerWear;
            botInventory.Clear();

            if (!_config.LootTables[bot.PrefabName].Weapon.IsNullOrEmpty())
            {
                bot.inventory.containerBelt.Clear();
                bot.GiveItem(ItemManager.CreateByName(_config.LootTables[bot.PrefabName].Weapon, 1, 0));
            }
            
            foreach (var check in data)
            {
                if (check == null) continue;
                var item = ItemManager.CreateByItemID(check.itemID, 1, check.skin);
                item?.MoveToContainer(botInventory);
            }
        }

        #endregion

        #region Commands
        
        [ChatCommand("nlsettings")]
        private void cmdChatnlsettings(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin)
            {
                SendReply(player, "You do not have permission to use this command");
                return;
            }

            ShowUIMain(player);
        }

        [ConsoleCommand("UI_NL")]
        private void cmdConsole(ConsoleSystem.Arg arg)
        {
            if (arg?.Args == null && arg.Args.Length < 1) return;
            var player = arg.Player();
            List<string> argsList;
            switch (arg.GetString(0))
            {
                case "OPENPREFAB":
                    ShowUIPrefab(player, string.Join(" ", arg.Args.Skip(1)));
                    break;
                case "CHANGEPREFABNAME":
                    argsList = arg.Args.ToList();
                    var indexOfPrefabName = argsList.IndexOf("newname");
                    if (indexOfPrefabName + 1 == argsList.Count) break;
                    var currentPrefab = string.Join(" ", arg.Args.Skip(1).Take(indexOfPrefabName - 1));
                    var newPrefab = string.Join(" ", arg.Args.Skip(indexOfPrefabName + 1));
                    if (!_config.LootTables.TryAdd(newPrefab, _config.LootTables[currentPrefab])) return;
                    _config.LootTables.Remove(currentPrefab);
                    ShowUIPrefab(player, newPrefab);
                    break;
                case "ADDLOOTTABLE":
                    var prefab = string.Join(" ", arg.Args.Skip(1));
                    _config.LootTables[prefab].LootTables.Add(new LootTable());
                    ShowUIPrefab(player, prefab);
                    break;
                case "OPENPREFABS":
                    if (arg.GetBool(2)) _config.LootTables.TryAdd("ENTER NPC PREFAB HERE", new BotContains());
                    ShowUIPrefabs(player, arg.GetInt(1));
                    break;
                case "REMOVEPREFAB":
                    _config.LootTables.Remove(string.Join(" ", arg.Args.Skip(2)));
                    ShowUIPrefabs(player, arg.GetInt(1));
                    break;
                case "OPENTABLESETTINGS":
                    ShowUIItems(player, string.Join(" ", arg.Args.Skip(2)), arg.GetInt(1));
                    break; 
                case "OPENSKINS":
                    if (_config.LootTables[string.Join(" ", arg.Args.Skip(2))].BotSkins[arg.GetInt(1)] == null) _config.LootTables[string.Join(" ", arg.Args.Skip(2))].BotSkins[arg.GetInt(1)] = new BotSkin();
                    ShowUISkinPanel(player, arg.GetInt(1), string.Join(" ", arg.Args.Skip(2)));
                    break;
                case "SETMINCOUNTVALUE":
                    argsList = arg.Args.ToList();
                    var indexOfAmountMin = argsList.IndexOf("amount");
                    if (indexOfAmountMin == arg.Args.Length - 1) return; 
                    var prefabForMin = string.Join(" ", arg.Args.Skip(2).Take(indexOfAmountMin - 2));
                    var tableNumberForMin = arg.GetInt(1);
                    _config.LootTables[prefabForMin].LootTables[tableNumberForMin].minItemAmount = arg.GetInt(arg.Args.Length - 1);
                    ShowUIItems(player, prefabForMin, tableNumberForMin);
                    break;
                case "SETMAXCOUNTVALUE":
                    argsList = arg.Args.ToList();
                    var indexOfAmountMax = argsList.IndexOf("amount");
                    if (indexOfAmountMax == arg.Args.Length - 1) return;
                    var prefabForMax = string.Join(" ", arg.Args.Skip(2).Take(indexOfAmountMax - 2));
                    var tableNumberForMax = arg.GetInt(1);
                    _config.LootTables[prefabForMax].LootTables[tableNumberForMax].maxItemAmount = arg.GetInt(arg.Args.Length - 1);
                    ShowUIItems(player, prefabForMax, tableNumberForMax);
                    break;
                case "ADDNEWITEM":
                    var prefabNewItem = string.Join(" ", arg.Args.Skip(2));
                    var tableNumberForNewItem = arg.GetInt(1);
                    _config.LootTables[prefabNewItem].LootTables[tableNumberForNewItem].items.Add(new ItemInfo());
                    ShowUIItems(player, prefabNewItem, tableNumberForNewItem);
                    break;
                case "SETNEWITEM":
                    ShowUIInfoItemPanel(player, arg.GetInt(1), string.Join(" ", arg.Args.Skip(3)), arg.GetInt(2));
                    break;
                case "ADDSKIN":
                    _config.LootTables[string.Join(" ", arg.Args.Skip(5))].BotSkins[arg.GetInt(3)].itemID = arg.GetInt(2);
                    ShowUISkinInfo(player, string.Join(" ", arg.Args.Skip(5)), arg.GetInt(3));
                    break;
                case "ADDITEM":
                    var prefabNameAddItem = string.Join(" ", arg.Args.Skip(5));
                    var tableNumberAddItem = arg.GetInt(4);
                    var indexAddItem = arg.GetInt(3);
                    _config.LootTables[prefabNameAddItem].LootTables[tableNumberAddItem].items[indexAddItem].shortName = arg.GetString(1);
                    _config.LootTables[prefabNameAddItem].LootTables[tableNumberAddItem].items[indexAddItem].itemID = arg.GetInt(2);
                    ShowUIInfoItemPanel(player, indexAddItem, prefabNameAddItem, tableNumberAddItem);
                    SaveConfig();
                    break;
                case "SEARCHING":
                    var endPos = arg.Args.ToList().IndexOf("amount") + 1;
                    if (endPos != arg.Args.Length) ShowUIItem(player, string.Join(" ", arg.Args.Skip(4).Take(endPos - 1 - 4)), arg.GetInt(3), arg.GetInt(2), string.Join(" ", arg.Args.Skip(endPos)).ToLower(), arg.GetBool(1));
                    break;
                case "CHGSKIN":
                    var skinPos = arg.Args.ToList().IndexOf("amount");
                    if (skinPos + 1 == arg.Args.Length) return;
                    var prefabNameSkin = string.Join(" ", arg.Args.Skip(3).Take(skinPos - 3));
                    var tableNumberSkin = arg.GetInt(2);
                    var indexSkin = arg.GetInt(1);
                    var skin = arg.GetULong(arg.Args.Length - 1);
                    _config.LootTables[prefabNameSkin].LootTables[tableNumberSkin].items[indexSkin].skin = skin;
                    ShowUIItemInfo(player, prefabNameSkin, tableNumberSkin, indexSkin);
                    SaveConfig();
                    break;
                case "CHGSKINBOT":
                    var minAmouintPosSkinBot = arg.Args.ToList().IndexOf("amount");
                    _config.LootTables[string.Join(" ", arg.Args.Skip(2).Take(minAmouintPosSkinBot - 2))].BotSkins[arg.GetInt(1)].skin = arg.GetULong(4);
                    ShowUISkinInfo(player, string.Join(" ", arg.Args.Skip(2).Take(minAmouintPosSkinBot - 2)), arg.GetInt(1));
                    SaveConfig();
                    break;
                case "CHGMINAMOUNT":
                    var minAmouintPos = arg.Args.ToList().IndexOf("amount");
                    if (minAmouintPos + 1 == arg.Args.Length) return;
                    var prefabMinAmount = string.Join(" ", arg.Args.Skip(3).Take(minAmouintPos - 3));
                    var tableMinAmount = arg.GetInt(2);
                    var indexMinAmount = arg.GetInt(1);
                    var minAmount = arg.GetInt(arg.Args.Length - 1);
                    _config.LootTables[prefabMinAmount].LootTables[tableMinAmount].items[indexMinAmount].minAmount = minAmount;
                    ShowUIItemInfo(player, prefabMinAmount, tableMinAmount, indexMinAmount);
                    SaveConfig();
                    break;
                case "CHGMAXAMOUNT":
                    var maxAmouintPos = arg.Args.ToList().IndexOf("amount");
                    if (maxAmouintPos + 1 == arg.Args.Length) return;
                    var prefabMaxAmount = string.Join(" ", arg.Args.Skip(3).Take(maxAmouintPos - 3));
                    var tableMaxAmount = arg.GetInt(2);
                    var indexMaxAmount = arg.GetInt(1);
                    var maxAmount = arg.GetInt(arg.Args.Length - 1);
                    _config.LootTables[prefabMaxAmount].LootTables[tableMaxAmount].items[indexMaxAmount].maxAmount = maxAmount;
                    ShowUIItemInfo(player, prefabMaxAmount, tableMaxAmount, indexMaxAmount);
                    SaveConfig();
                    break;
                case "CHGMINCONDITION":
                    var minConditionPos = arg.Args.ToList().IndexOf("amount");
                    if (minConditionPos + 1 == arg.Args.Length) return;
                    var prefabMinCondition = string.Join(" ", arg.Args.Skip(3).Take(minConditionPos - 3));
                    var tableMinCondition = arg.GetInt(2);
                    var indexMinCondition = arg.GetInt(1);
                    var minCondition = arg.GetInt(arg.Args.Length - 1);
                    minCondition = minCondition > 100 ? 100 : minCondition < 1 ? 1 : minCondition;
                    _config.LootTables[prefabMinCondition].LootTables[tableMinCondition].items[indexMinCondition].minCondition = minCondition;
                    ShowUIItemInfo(player, prefabMinCondition, tableMinCondition, indexMinCondition);
                    SaveConfig();
                    break;
                case "CHGMAXCONDITION":
                    var maxConditionPos = arg.Args.ToList().IndexOf("amount");
                    if (maxConditionPos + 1 == arg.Args.Length) return;
                    var prefabMaxCondition = string.Join(" ", arg.Args.Skip(3).Take(maxConditionPos - 3));
                    var tableMaxCondition = arg.GetInt(2);
                    var indexMaxCondition = arg.GetInt(1);
                    var maxCondition = arg.GetInt(arg.Args.Length - 1);
                    maxCondition = maxCondition > 100 ? 100 : maxCondition < 1 ? 1 : maxCondition;
                    _config.LootTables[prefabMaxCondition].LootTables[tableMaxCondition].items[indexMaxCondition].maxCondition = maxCondition;
                    ShowUIItemInfo(player, prefabMaxCondition, tableMaxCondition, indexMaxCondition);
                    SaveConfig();
                    break;
                case "CHGDROPCHANCE":
                    var dropChance = arg.Args.ToList().IndexOf("amount");
                    if (dropChance + 1 == arg.Args.Length) return;
                    var prefabChanceDrop = string.Join(" ", arg.Args.Skip(3).Take(dropChance - 3));
                    var tableChanceDrop = arg.GetInt(2);
                    var indexChanceDrop = arg.GetInt(1);
                    var chanceDrop = arg.GetInt(arg.Args.Length - 1);
                    chanceDrop = chanceDrop > 100 ? 100 : chanceDrop < 1 ? 1 : chanceDrop;
                    _config.LootTables[prefabChanceDrop].LootTables[tableChanceDrop].items[indexChanceDrop].dropChance = chanceDrop;
                    ShowUIItemInfo(player, prefabChanceDrop, tableChanceDrop, indexChanceDrop);
                    SaveConfig();
                    break;
                case "CHGISBLUEPRINT":
                    var prefabBlueprint = string.Join(" ", arg.Args.Skip(3));
                    var tableBlueprint = arg.GetInt(2);
                    var indexBlueprint = arg.GetInt(1);
                    _config.LootTables[prefabBlueprint].LootTables[tableBlueprint].items[indexBlueprint].isBlueprint =  !_config.LootTables[prefabBlueprint].LootTables[tableBlueprint].items[indexBlueprint].isBlueprint;
                    ShowUIItemInfo(player, prefabBlueprint, tableBlueprint, indexBlueprint);
                    SaveConfig();
                    break;
                case "BACKTOITEMS":
                    CuiHelper.DestroyUi(player, Layer);
                    ShowUIItems(player, string.Join(" ", arg.Args.Skip(2)), arg.GetInt(1));
                    break;
                case "BACKTOPREFAB":
                    CuiHelper.DestroyUi(player, Layer);
                    ShowUIPrefab(player, string.Join(" ", arg.Args.Skip(1)));
                    break;
                case "BACKTOPREFABS":
                    CuiHelper.DestroyUi(player, Layer);
                    ShowUIPrefabs(player);
                    break;
            }
            
            SaveConfig(); 
        }

        #endregion

        #region Functions

        private Dictionary<T, P> GetDictionary<T, P>(Dictionary<T, P> dictionary, int skipAmount, int takeAmount)
        {
            var result = new Dictionary<T, P>();
            var takeCounter = 0;
            foreach (var check in dictionary)
            {
                if (skipAmount > 0)
                {
                    skipAmount--;
                    continue;
                }

                if (takeCounter >= takeAmount) break;
                result.Add(check.Key, check.Value);
                takeCounter++;
            }

            return result;
        }

        private List<T> GetList<T>(List<T> list, int skipAmount, int takeAmount)
        {
            var result = new List<T>();
            var takeCounter = 0;
            foreach (var check in list)
            {
                if (skipAmount > 0)
                {
                    skipAmount--;
                    continue;
                }

                if (takeCounter >= takeAmount) break;
                result.Add(check);
                takeCounter++;
            }

            return result;
        }

        #endregion

        #region UI

        private void ShowUIMain(BasePlayer player)
        {
            var container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                KeyboardEnabled = true,
                RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
                Image = {Color = "0 0 0 0.95", Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat"}
            }, "Overlay", Layer + ".bg");

            container.Add(new CuiPanel
            {
                RectTransform = {AnchorMin = "0.125 0.15", AnchorMax = "0.875 0.85"},
                Image = {Color = "0 0 0 0.8"}
            }, Layer + ".bg", Layer + ".mainPanel");
            Outline(container, Layer + ".mainPanel");

            container.Add(new CuiPanel
            {
                RectTransform = {AnchorMin = "0 0.92", AnchorMax = "1 0.92", OffsetMin = "0 0", OffsetMax = "0 2"},
                Image = {Color = "1 1 1 1"}
            }, Layer + ".mainPanel");

            container.Add(new CuiButton
            {
                RectTransform = {AnchorMin = "1 1", AnchorMax = "1 1", OffsetMin = "-40 -40", OffsetMax = "0 0"},
                Button = {Color = "0 0 0 0", Close = Layer + ".bg"},
                Text =
                {
                    Text = "×", Font = "robotocondensed-regular.ttf", FontSize = 30, Align = TextAnchor.MiddleCenter,
                    Color = "0.56 0.58 0.64 1.00"
                }
            }, Layer + ".mainPanel", Layer + ".buttonClose");
            Outline(container, Layer + ".buttonClose");

            CuiHelper.DestroyUi(player, Layer + ".bg");
            CuiHelper.AddUi(player, container);

            ShowUIPrefabs(player);
        }

        private void ShowUIPrefabs(BasePlayer player, int page = 0)
        {
            var container = new CuiElementContainer();
            var posY = -40;
            var listCount = (int) Math.Floor(_config.LootTables.Count / 12f);

            container.Add(new CuiPanel
            {
                RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "0 0", OffsetMax = "0 -40"},
                Image = {Color = "0 0 0 0"}
            }, Layer + ".mainPanel", Layer);

            container.Add(new CuiLabel
            {
                RectTransform = {AnchorMin = "0 1", AnchorMax = "1 1", OffsetMin = "0 0", OffsetMax = "-40 40"},
                Text =
                {
                    Text = "NPC PREFABS", Font = "robotocondensed-bold.ttf", FontSize = 25,
                    Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 1"
                }
            }, Layer);
            
            foreach (var check in GetDictionary(_config.LootTables, 12 * page, 12))
            {
                container.Add(new CuiButton
                {
                    RectTransform = {AnchorMin = "0.025 1", AnchorMax = "0.975 1", OffsetMin = $"0 {posY}", OffsetMax = $"0 {posY + 30}"},
                    Button = {Color = "0 0 0 0", Command = $"UI_NL OPENPREFAB {check.Key}"},
                    Text =
                    {
                        Text = check.Key, Font = "robotocondensed-bold.ttf", FontSize = 18, Align = TextAnchor.MiddleCenter,
                        Color = "1 1 1 1"
                    }
                }, Layer, Layer + ".line" + posY);
                Outline(container, Layer + ".line" + posY);
                container.Add(new CuiButton
                {
                    RectTransform = {AnchorMin = "1 1", AnchorMax = "1 1", OffsetMin = "-30 -30", OffsetMax = "0 0"},
                    Button = {Color = "0 0 0 0", Command = $"UI_NL REMOVEPREFAB {page} {check.Key}"},
                    Text =
                    {
                        Text = "×", Font = "robotocondensed-bold.ttf", FontSize = 24, Align = TextAnchor.MiddleCenter,
                        Color = "1 0 0 1"
                    }
                }, Layer + ".line" + posY);
                posY -= 35;
            }

            if (listCount == page)
            {
                container.Add(new CuiButton
                {
                    RectTransform = {AnchorMin = "0.025 1", AnchorMax = "0.975 1", OffsetMin = $"0 {posY}", OffsetMax = $"0 {posY + 30}"},
                    Button = {Color = "0 0 0 0", Command = $"UI_NL OPENPREFABS {page} true"},
                    Text =
                    {
                        Text = "+", Font = "robotocondensed-bold.ttf", FontSize = 18, Align = TextAnchor.MiddleCenter,
                        Color = "1 1 1 1"
                    }
                }, Layer, Layer + ".line");
                Outline(container, Layer + ".line");
            }

            container.Add(new CuiLabel
            {
                RectTransform = {AnchorMin = "0.5 0", AnchorMax = "0.5 0", OffsetMin = "-10 10", OffsetMax = "10 40"},
                Text =
                {
                    Text = $"{page + 1}", Font = "robotocondensed-regular.ttf", FontSize = 24, Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 1"
                }
            }, Layer);

            if (page > 0)
                container.Add(new CuiButton
                {
                    RectTransform = {AnchorMin = "0.5 0", AnchorMax = "0.5 0", OffsetMin = "-40 10", OffsetMax = "-10 40"},
                    Button = {Color = "0 0 0 0", Command = $"UI_NL OPENPREFABS {page - 1} false"},
                    Text =
                    {
                        Text = "<", Font = "robotocondensed-regular.ttf", FontSize = 24, Align = TextAnchor.MiddleCenter,
                        Color = "1 1 1 1"
                    }
                }, Layer);

            if (listCount > page)
                container.Add(new CuiButton
                {
                    RectTransform = {AnchorMin = "0.5 0", AnchorMax = "0.5 0", OffsetMin = "10 10", OffsetMax = "40 40"},
                    Button = {Color = "0 0 0 0", Command = $"UI_NL OPENPREFABS {page + 1} false"},
                    Text =
                    {
                        Text = ">", Font = "robotocondensed-regular.ttf", FontSize = 24, Align = TextAnchor.MiddleCenter,
                        Color = "1 1 1 1"
                    }
                }, Layer);

            CuiHelper.DestroyUi(player, Layer);
            CuiHelper.AddUi(player, container);
        }

        private void ShowUIPrefab(BasePlayer player, string prefabName = "")
        {
            var container = new CuiElementContainer();
            var posY = -40;
            var posX = 10;
            var itemSize = 46;
            var itemSpace = 10;

            container.Add(new CuiPanel
            {
                RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "0 0", OffsetMax = "0 -40"},
                Image = {Color = "0 0 0 0"}
            }, Layer + ".mainPanel", Layer);

            container.Add(new CuiLabel
            {
                RectTransform = {AnchorMin = "0 1", AnchorMax = "1 1", OffsetMin = "0 0", OffsetMax = "-40 40"},
                Text =
                {
                    Text = "NPC LOOT SETTINGS", Font = "robotocondensed-bold.ttf", FontSize = 25,
                    Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 1"
                }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = {AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "0 0", OffsetMax = "40 40"},
                Button = {Color = "0 0 0 0", Command = $"UI_NL BACKTOPREFABS"},
                Text =
                {
                    Text = "<-", Font = "robotocondensed-regular.ttf", FontSize = 30, Align = TextAnchor.MiddleCenter,
                    Color = "0.56 0.58 0.64 1.00"
                }
            }, Layer, Layer + ".buttonBack");
            Outline(container, Layer + ".buttonBack");

            container.Add(new CuiLabel
            {
                RectTransform = {AnchorMin = "0.025 1", AnchorMax = "0.975 1", OffsetMin = $"0 {posY}", OffsetMax = $"0 {posY + 30}"},
                Text =
                {
                    Text = "PrefabName: ", Font = "robotocondensed-regular.ttf", FontSize = 18, Align = TextAnchor.MiddleLeft,
                    Color = "1 1 1 1"
                }
            }, Layer);

            container.Add(new CuiPanel
            {
                RectTransform = {AnchorMin = "0.125 1", AnchorMax = "0.975 1", OffsetMin = $"0 {posY}", OffsetMax = $"0 {posY + 30}"},
                Image = {Color = "0.3 0.3 0.3 0.8"}
            }, Layer, Layer + ".input");

            container.Add(new CuiLabel
            {
                RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
                Text =
                {
                    Text = prefabName, Font = "robotocondensed-regular.ttf", FontSize = 18, Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 0.3"
                }
            }, Layer + ".input");

            container.Add(new CuiElement
            {
                Parent = Layer + ".input",
                Components =
                {
                    new CuiInputFieldComponent
                    {
                        Align = TextAnchor.MiddleCenter, CharsLimit = 150, FontSize = 18,
                        Command = $"UI_NL CHANGEPREFABNAME {prefabName} newname"
                    },
                    new CuiRectTransformComponent {AnchorMin = "0 0", AnchorMax = "1 1"}
                }
            });

            posY -= 60;

            container.Add(new CuiLabel
            {
                RectTransform = {AnchorMin = "0.025 1", AnchorMax = "1 1", OffsetMin = $"0 {posY}", OffsetMax = $"0 {posY + 35}"},
                Text =
                {
                    Text = "Bot clothes:", Font = "robotocondensed-regular.ttf", FontSize = 18, Align = TextAnchor.MiddleLeft,
                    Color = "1 1 1 1"
                }
            }, Layer);

            var dataBotSkins = _config.LootTables[prefabName].BotSkins;
            for (var i = 0; i < dataBotSkins.Length; i++)
            {
                var isExist = dataBotSkins[i] != null && dataBotSkins[i].itemID != 0;
                if (isExist)
                {
                    container.Add(new CuiElement
                    {
                        Parent = Layer,
                        Components =
                        {
                            new CuiImageComponent {ItemId = dataBotSkins[i].itemID, SkinId = dataBotSkins[i].skin},
                            new CuiRectTransformComponent {AnchorMin = "0.115 1", AnchorMax = "0.115 1", OffsetMin = $"{posX} {posY}", OffsetMax = $"{posX + itemSize} {posY + itemSize}"}
                        }
                    });
                }
                container.Add(new CuiButton
                {
                    RectTransform = {AnchorMin = "0.115 1", AnchorMax = "0.115 1", OffsetMin = $"{posX} {posY}", OffsetMax = $"{posX + itemSize} {posY + itemSize}"},
                    Button = {Color = "0 0 0 0", Command = $"UI_NL OPENSKINS {i} {prefabName}"},
                    Text =
                    {
                        Text = isExist ? "" : "+", Font = "robotocondensed-bold.ttf", FontSize = 18, Align = TextAnchor.MiddleCenter,
                        Color = "1 1 1 1"
                    }
                }, Layer, Layer + ".item" + posX);
                Outline(container, Layer + ".item" + posX);
                posX += itemSize + itemSpace;
            }
            posY -= 35;

            posX = 10;
            
            container.Add(new CuiLabel
            {
                RectTransform = {AnchorMin = "0 1", AnchorMax = "1 1", OffsetMin = $"0 {posY}", OffsetMax = $"0 {posY + 35}"},
                Text =
                {
                    Text = "LOOT TABLES", Font = "robotocondensed-regular.ttf", FontSize = 28, Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 1"
                }
            }, Layer);

            posY -= 45;
            for (var i = 0; i < _config.LootTables[prefabName].LootTables.Count; i++)
            {
                container.Add(new CuiButton
                {
                    RectTransform = {AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = $"{posX} {posY}", OffsetMax = $"{posX + itemSize} {posY + itemSize}"},
                    Button = {Color = "0 0 0 0", Command = $"UI_NL OPENTABLESETTINGS {i} {prefabName}"},
                    Text =
                    {
                        Text = i.ToString(), Font = "robotocondensed-bold.ttf", FontSize = 18, Align = TextAnchor.MiddleCenter,
                        Color = "1 1 1 1"
                    }
                }, Layer, Layer + ".item" + posX);
                Outline(container, Layer + ".item" + posX);
                posX += itemSize + itemSpace;
                if (posX < 950) continue;
                posY -= itemSize + itemSpace;
                posX = 10;
            }

            container.Add(new CuiButton
            {
                RectTransform = {AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = $"{posX} {posY}", OffsetMax = $"{posX + itemSize} {posY + itemSize}"},
                Button = {Color = "0 0 0 0", Command = $"UI_NL ADDLOOTTABLE {prefabName}"},
                Text =
                {
                    Text = "+", Font = "robotocondensed-bold.ttf", FontSize = 18, Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 1"
                }
            }, Layer, Layer + ".item" + posX);
            Outline(container, Layer + ".item" + posX);

            CuiHelper.DestroyUi(player, Layer);
            CuiHelper.AddUi(player, container);
        }

        private void ShowUIItems(BasePlayer player, string prefabName, int lootTable)
        {
            var settings = _config.LootTables[prefabName].LootTables[lootTable];
            var container = new CuiElementContainer();
            var posY = -40;
            var posX = 10;
            var itemSize = 46;
            var itemSpace = 10;

            container.Add(new CuiPanel
            {
                RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = " ", OffsetMax = "0 -40"},
                Image = {Color = "0 0 0 0"}
            }, Layer + ".mainPanel", Layer);

            container.Add(new CuiLabel
            {
                RectTransform = {AnchorMin = "0 1", AnchorMax = "1 1", OffsetMin = "0 0", OffsetMax = "-40 40"},
                Text =
                {
                    Text = "NEW ITEMS FOR LOOT TABLE", Font = "robotocondensed-bold.ttf", FontSize = 25,
                    Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 1"
                }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = {AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "0 0", OffsetMax = "40 40"},
                Button = {Color = "0 0 0 0", Command = $"UI_NL BACKTOPREFAB {prefabName}"},
                Text =
                {
                    Text = "<-", Font = "robotocondensed-regular.ttf", FontSize = 30, Align = TextAnchor.MiddleCenter,
                    Color = "0.56 0.58 0.64 1.00"
                }
            }, Layer, Layer + ".buttonBack");
            Outline(container, Layer + ".buttonBack");

            container.Add(new CuiLabel
            {
                RectTransform = {AnchorMin = "0.025 1", AnchorMax = "0.475 1", OffsetMin = $"0 {posY}", OffsetMax = $"0 {posY + 30}"},
                Text =
                {
                    Text = "Min item amount:", Font = "robotocondensed-regular.ttf", FontSize = 18, Align = TextAnchor.MiddleLeft,
                    Color = "1 1 1 1"
                }
            }, Layer);
            container.Add(new CuiPanel
            {
                RectTransform = {AnchorMin = "0.175 1", AnchorMax = "0.475 1", OffsetMin = $"0 {posY}", OffsetMax = $"0 {posY + 30}"},
                Image = {Color = "0.3 0.3 0.3 0.8"}
            }, Layer, Layer + ".input");
            container.Add(new CuiLabel
            {
                RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
                Text =
                {
                    Text = settings.minItemAmount.ToString(), Font = "robotocondensed-regular.ttf", FontSize = 18, Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 0.3"
                }
            }, Layer + ".input");
            container.Add(new CuiElement
            {
                Parent = Layer + ".input",
                Components =
                {
                    new CuiInputFieldComponent
                    {
                        Align = TextAnchor.MiddleCenter, CharsLimit = 10, FontSize = 18,
                        Command = $"UI_NL SETMINCOUNTVALUE {lootTable} {prefabName} amount"
                    },
                    new CuiRectTransformComponent {AnchorMin = "0 0", AnchorMax = "1 1"}
                }
            });

            container.Add(new CuiLabel
            {
                RectTransform = {AnchorMin = "0.525 1", AnchorMax = "0.975 1", OffsetMin = $"0 {posY}", OffsetMax = $"0 {posY + 30}"},
                Text =
                {
                    Text = "Max item amount:", Font = "robotocondensed-regular.ttf", FontSize = 18, Align = TextAnchor.MiddleLeft,
                    Color = "1 1 1 1"
                }
            }, Layer);
            container.Add(new CuiPanel
            {
                RectTransform = {AnchorMin = "0.675 1", AnchorMax = "0.975 1", OffsetMin = $"0 {posY}", OffsetMax = $"0 {posY + 30}"},
                Image = {Color = "0.3 0.3 0.3 0.8"}
            }, Layer, Layer + ".input");
            container.Add(new CuiLabel
            {
                RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
                Text =
                {
                    Text = settings.maxItemAmount.ToString(), Font = "robotocondensed-regular.ttf", FontSize = 18, Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 0.3"
                }
            }, Layer + ".input");
            container.Add(new CuiElement
            {
                Parent = Layer + ".input",
                Components =
                {
                    new CuiInputFieldComponent
                    {
                        Align = TextAnchor.MiddleCenter, CharsLimit = 10, FontSize = 18,
                        Command = $"UI_NL SETMAXCOUNTVALUE {lootTable} {prefabName} amount"
                    },
                    new CuiRectTransformComponent {AnchorMin = "0 0", AnchorMax = "1 1"}
                }
            });
            posY -= 35;

            container.Add(new CuiLabel
            {
                RectTransform = {AnchorMin = "0 1", AnchorMax = "1 1", OffsetMin = $"0 {posY}", OffsetMax = $"0 {posY + 35}"},
                Text =
                {
                    Text = "ALL ITEMS", Font = "robotocondensed-regular.ttf", FontSize = 28, Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 1"
                }
            }, Layer);

            posY -= 45;

            for (var index = 0; index < _config.LootTables[prefabName].LootTables[lootTable].items.Count; index++)
            {
                var check = _config.LootTables[prefabName].LootTables[lootTable].items[index];
                container.Add(new CuiElement
                {
                    Parent = Layer,
                    Components =
                    {
                        new CuiImageComponent {ItemId = check.itemID},
                        new CuiRectTransformComponent {AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = $"{posX} {posY}", OffsetMax = $"{posX + itemSize} {posY + itemSize}"}
                    }
                });
                container.Add(new CuiButton
                {
                    RectTransform = {AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = $"{posX} {posY}", OffsetMax = $"{posX + itemSize} {posY + itemSize}"},
                    Button = {Color = "0 0 0 0", Command = $"UI_NL SETNEWITEM {index} {lootTable} {prefabName}"},
                    Text =
                    {
                        Text = "", Font = "robotocondensed-bold.ttf", FontSize = 18, Align = TextAnchor.MiddleCenter,
                        Color = "1 1 1 1"
                    }
                }, Layer, Layer + ".item" + posX);
                Outline(container, Layer + ".item" + posX);
                posX += itemSize + itemSpace;
                if (posX < 950) continue;
                posY -= itemSize + itemSpace;
                posX = 10;
            }

            container.Add(new CuiButton
            {
                RectTransform = {AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = $"{posX} {posY}", OffsetMax = $"{posX + itemSize} {posY + itemSize}"},
                Button = {Color = "0 0 0 0", Command = $"UI_NL ADDNEWITEM {lootTable} {prefabName}"},
                Text =
                {
                    Text = "+", Font = "robotocondensed-bold.ttf", FontSize = 18, Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 1"
                }
            }, Layer, Layer + ".item" + posX);
            Outline(container, Layer + ".item" + posX);

            CuiHelper.DestroyUi(player, Layer);
            CuiHelper.AddUi(player, container);
        }
        
        private void ShowUISkinPanel(BasePlayer player, int index, string prefabName)
        {
            var container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 -40"},
                Image = {Color = "0 0 0 0"}
            }, Layer + ".mainPanel", Layer);

            container.Add(new CuiLabel
            {
                RectTransform = {AnchorMin = "0 1", AnchorMax = "1 1", OffsetMax = "-40 40"},
                Text =
                {
                    Text = "SKIN", Font = "robotocondensed-regular.ttf", FontSize = 25, Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 1"
                }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = {AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "0 0", OffsetMax = "40 40"},
                Button = {Color = "0 0 0 0", Command = $"UI_NL BACKTOPREFAB {prefabName}"},
                Text =
                {
                    Text = "<-", Font = "robotocondensed-regular.ttf", FontSize = 30, Align = TextAnchor.MiddleCenter,
                    Color = "0.56 0.58 0.64 1.00"
                }
            }, Layer, Layer + ".buttonBack");
            Outline(container, Layer + ".buttonBack");

            container.Add(new CuiPanel
            {
                RectTransform = {AnchorMin = "0.5 0", AnchorMax = "0.5 1", OffsetMax = "2 0"},
                Image = {Color = "1 1 1 0.7"}
            }, Layer);


            CuiHelper.DestroyUi(player, Layer);
            CuiHelper.AddUi(player, container);

            ShowUIItem(player, prefabName, 0, index, isSkin:true);
            ShowUISkinInfo(player, prefabName, index);
        }

        private void ShowUISkinInfo(BasePlayer player, string prefabName, int index)
        {
            var container = new CuiElementContainer();
            var posY = -40;
            var list = _config.LootTables[prefabName].BotSkins[index];

            container.Add(new CuiPanel
            {
                RectTransform = {AnchorMin = "0.5 0", AnchorMax = "1 1"},
                Image = {Color = "0 0 0 0"}
            }, Layer, Layer + ".itemInfo");

            #region Row 1
            container.Add(new CuiLabel
            {
                RectTransform = {AnchorMin = "0.025 1", AnchorMax = "1 1", OffsetMin = $"0 {posY}", OffsetMax = $"0 {posY + 30}"},
                Text =
                {
                    Text = "ItemID: ", Font = "robotocondensed-regular.ttf", FontSize = 18, Align = TextAnchor.MiddleLeft,
                    Color = "1 1 1 1"
                }
            }, Layer + ".itemInfo");

            container.Add(new CuiPanel
            {
                RectTransform = {AnchorMin = "0.325 1", AnchorMax = "0.975 1", OffsetMin = $"0 {posY}", OffsetMax = $"0 {posY + 30}"},
                Image = {Color = "0 0 0 0.8"}
            }, Layer + ".itemInfo", Layer + ".input");
            
            container.Add(new CuiLabel
            {
                RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
                Text =
                {
                    Text = list.itemID.ToString(), Font = "robotocondensed-regular.ttf", FontSize = 18, Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 0.3"
                }
            }, Layer + ".input");
            posY -= 35;

            #endregion

            #region Row 2

            container.Add(new CuiLabel
            {
                RectTransform = {AnchorMin = "0.025 1", AnchorMax = "1 1", OffsetMin = $"0 {posY}", OffsetMax = $"0 {posY + 30}"},
                Text =
                {
                    Text = "Skin: ", Font = "robotocondensed-regular.ttf", FontSize = 18, Align = TextAnchor.MiddleLeft,
                    Color = "1 1 1 1"
                }
            }, Layer + ".itemInfo");

            container.Add(new CuiPanel
            {
                RectTransform = {AnchorMin = "0.325 1", AnchorMax = "0.975 1", OffsetMin = $"0 {posY}", OffsetMax = $"0 {posY + 30}"},
                Image = {Color = "0 0 0 0.8"}
            }, Layer + ".itemInfo", Layer + ".input");
            container.Add(new CuiLabel
            {
                RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
                Text =
                {
                    Text = list.skin.ToString(), Font = "robotocondensed-regular.ttf", FontSize = 18, Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 0.3"
                }
            }, Layer + ".input");
            container.Add(new CuiElement
            {
                Parent = Layer + ".input",
                Components =
                {
                    new CuiInputFieldComponent
                    {
                        Align = TextAnchor.MiddleCenter, CharsLimit = 20, FontSize = 18,
                        Command = $"UI_NL CHGSKINBOT {index} {prefabName} amount"
                    },
                    new CuiRectTransformComponent {AnchorMin = "0 0", AnchorMax = "1 1"}
                }
            });

            #endregion

            CuiHelper.DestroyUi(player, Layer + ".itemInfo");
            CuiHelper.AddUi(player, container);
        }

        private void ShowUIInfoItemPanel(BasePlayer player, int index, string prefabName, int lootable)
        {
            var container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 -40"},
                Image = {Color = "0 0 0 0"}
            }, Layer + ".mainPanel", Layer);

            container.Add(new CuiLabel
            {
                RectTransform = {AnchorMin = "0 1", AnchorMax = "1 1", OffsetMax = "-40 40"},
                Text =
                {
                    Text = "ITEM INFO", Font = "robotocondensed-regular.ttf", FontSize = 25, Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 1"
                }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = {AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "0 0", OffsetMax = "40 40"},
                Button = {Color = "0 0 0 0", Command = $"UI_NL BACKTOITEMS {lootable} {prefabName}"},
                Text =
                {
                    Text = "<-", Font = "robotocondensed-regular.ttf", FontSize = 30, Align = TextAnchor.MiddleCenter,
                    Color = "0.56 0.58 0.64 1.00"
                }
            }, Layer, Layer + ".buttonBack");
            Outline(container, Layer + ".buttonBack");

            container.Add(new CuiPanel
            {
                RectTransform = {AnchorMin = "0.5 0", AnchorMax = "0.5 1", OffsetMax = "2 0"},
                Image = {Color = "1 1 1 0.7"}
            }, Layer);


            CuiHelper.DestroyUi(player, Layer);
            CuiHelper.AddUi(player, container);

            ShowUIItem(player, prefabName, lootable, index);
            ShowUIItemInfo(player, prefabName, lootable, index);
        }

        private void ShowUIItem(BasePlayer player, string prefabName, int lootTable, int index, string name = "", bool isSkin = false)
        {
            var container = new CuiElementContainer();
            var posY = -40;
            var posX = 22;
            var itemSize = 46;
            var itemSpace = 10;

            var allItems = isSkin ? WeareableItems : UsableItems;
            var itemList = new List<ItemDefinition>();
            var count = 0;
            foreach (var check in allItems)
            {
                if (count > 55) break;
                if (!check.displayName.english.ToLower().Contains(name)) continue;
                itemList.Add(check);
                count++;
            }

            container.Add(new CuiPanel
            {
                RectTransform = {AnchorMin = "0 0", AnchorMax = "0.5 1"},
                Image = {Color = "0 0 0 0"}
            }, Layer, Layer + ".searchPanel");

            container.Add(new CuiPanel
            {
                RectTransform = {AnchorMin = "0.025 1", AnchorMax = "0.975 1", OffsetMin = $"0 {posY}", OffsetMax = $"0 {posY + 30}"},
                Image = {Color = "0 0 0 0.8"}
            }, Layer + ".searchPanel", Layer + ".input");
            container.Add(new CuiLabel
            {
                RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
                Text =
                {
                    Text = "SERACH", Font = "robotocondensed-regular.ttf", FontSize = 18, Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 0.3"
                }
            }, Layer + ".input");
            container.Add(new CuiElement
            {
                Parent = Layer + ".input",
                Components =
                {
                    new CuiInputFieldComponent
                    {
                        Align = TextAnchor.MiddleCenter, CharsLimit = 18, FontSize = 18,
                        Command = $"UI_NL SEARCHING {isSkin} {index} {lootTable} {prefabName} amount"
                    },
                    new CuiRectTransformComponent {AnchorMin = "0 0", AnchorMax = "1 1"}
                }
            });
            posY -= 55;
            foreach (var check in itemList)
            {
                container.Add(new CuiPanel
                {
                    RectTransform = {AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = $"{posX} {posY}", OffsetMax = $"{posX + itemSize} {posY + itemSize}"},
                    Image = {Color = "0 0 0 0.8"}
                }, Layer + ".searchPanel", Layer + ".item" + posX);

                container.Add(new CuiElement
                {
                    Parent = Layer + ".item" + posX,
                    Components =
                    {
                        new CuiImageComponent {ItemId = check.itemid},
                        new CuiRectTransformComponent {AnchorMin = "0 0", AnchorMax = "1 1"}
                    }
                });
                
                container.Add(new CuiButton
                {
                    RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
                    Button = {Color = "0 0 0 0", Command = $"UI_NL {(isSkin ? "ADDSKIN" : "ADDITEM")} {check.shortname} {check.itemid} {index} {lootTable} {prefabName}"},
                    Text =
                    {
                        Text = "", Font = "robotocondensed-bold.ttf", FontSize = 18, Align = TextAnchor.MiddleCenter,
                        Color = "1 1 1 1"
                    }
                }, Layer + ".item" + posX);
                Outline(container, Layer + ".item" + posX);
                posX += itemSize + itemSpace;
                if (posX < 450) continue;
                posY -= itemSize + itemSpace;
                posX = 22;
            }

            CuiHelper.DestroyUi(player, Layer + ".searchPanel");
            CuiHelper.AddUi(player, container);
        }

        private void ShowUIItemInfo(BasePlayer player, string prefabName, int lootTable, int index)
        {
            var container = new CuiElementContainer();
            var posY = -40;
            var list = _config.LootTables[prefabName].LootTables[lootTable].items[index];

            container.Add(new CuiPanel
            {
                RectTransform = {AnchorMin = "0.5 0", AnchorMax = "1 1"},
                Image = {Color = "0 0 0 0"}
            }, Layer, Layer + ".itemInfo");

            #region Row 1

            container.Add(new CuiLabel
            {
                RectTransform = {AnchorMin = "0.025 1", AnchorMax = "1 1", OffsetMin = $"0 {posY}", OffsetMax = $"0 {posY + 30}"},
                Text =
                {
                    Text = "Shortname: ", Font = "robotocondensed-regular.ttf", FontSize = 18, Align = TextAnchor.MiddleLeft,
                    Color = "1 1 1 1"
                }
            }, Layer + ".itemInfo");

            container.Add(new CuiPanel
            {
                RectTransform = {AnchorMin = "0.325 1", AnchorMax = "0.975 1", OffsetMin = $"0 {posY}", OffsetMax = $"0 {posY + 30}"},
                Image = {Color = "0 0 0 0.8"}
            }, Layer + ".itemInfo", Layer + ".input");
            container.Add(new CuiLabel
            {
                RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
                Text =
                {
                    Text = list.shortName, Font = "robotocondensed-regular.ttf", FontSize = 18, Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 0.3"
                }
            }, Layer + ".input");
            posY -= 35;

            #endregion

            #region Row 2

            container.Add(new CuiLabel
            {
                RectTransform = {AnchorMin = "0.025 1", AnchorMax = "1 1", OffsetMin = $"0 {posY}", OffsetMax = $"0 {posY + 30}"},
                Text =
                {
                    Text = "ItemID: ", Font = "robotocondensed-regular.ttf", FontSize = 18, Align = TextAnchor.MiddleLeft,
                    Color = "1 1 1 1"
                }
            }, Layer + ".itemInfo");

            container.Add(new CuiPanel
            {
                RectTransform = {AnchorMin = "0.325 1", AnchorMax = "0.975 1", OffsetMin = $"0 {posY}", OffsetMax = $"0 {posY + 30}"},
                Image = {Color = "0 0 0 0.8"}
            }, Layer + ".itemInfo", Layer + ".input");
            container.Add(new CuiLabel
            {
                RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
                Text =
                {
                    Text = list.itemID.ToString(), Font = "robotocondensed-regular.ttf", FontSize = 18, Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 0.3"
                }
            }, Layer + ".input");
            posY -= 35;

            #endregion

            #region Row 3

            container.Add(new CuiLabel
            {
                RectTransform = {AnchorMin = "0.025 1", AnchorMax = "1 1", OffsetMin = $"0 {posY}", OffsetMax = $"0 {posY + 30}"},
                Text =
                {
                    Text = "Skin: ", Font = "robotocondensed-regular.ttf", FontSize = 18, Align = TextAnchor.MiddleLeft,
                    Color = "1 1 1 1"
                }
            }, Layer + ".itemInfo");

            container.Add(new CuiPanel
            {
                RectTransform = {AnchorMin = "0.325 1", AnchorMax = "0.975 1", OffsetMin = $"0 {posY}", OffsetMax = $"0 {posY + 30}"},
                Image = {Color = "0 0 0 0.8"}
            }, Layer + ".itemInfo", Layer + ".input");
            container.Add(new CuiLabel
            {
                RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
                Text =
                {
                    Text = list.skin.ToString(), Font = "robotocondensed-regular.ttf", FontSize = 18, Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 0.3"
                }
            }, Layer + ".input");
            container.Add(new CuiElement
            {
                Parent = Layer + ".input",
                Components =
                {
                    new CuiInputFieldComponent
                    {
                        Align = TextAnchor.MiddleCenter, CharsLimit = 20, FontSize = 18,
                        Command = $"UI_NL CHGSKIN {index} {lootTable} {prefabName} amount"
                    },
                    new CuiRectTransformComponent {AnchorMin = "0 0", AnchorMax = "1 1"}
                }
            });
            posY -= 35;

            #endregion

            #region Row 4

            container.Add(new CuiLabel
            {
                RectTransform = {AnchorMin = "0.025 1", AnchorMax = "1 1", OffsetMin = $"0 {posY}", OffsetMax = $"0 {posY + 30}"},
                Text =
                {
                    Text = "Min amount: ", Font = "robotocondensed-regular.ttf", FontSize = 18, Align = TextAnchor.MiddleLeft,
                    Color = "1 1 1 1"
                }
            }, Layer + ".itemInfo");

            container.Add(new CuiPanel
            {
                RectTransform = {AnchorMin = "0.325 1", AnchorMax = "0.975 1", OffsetMin = $"0 {posY}", OffsetMax = $"0 {posY + 30}"},
                Image = {Color = "0 0 0 0.8"}
            }, Layer + ".itemInfo", Layer + ".input");
            container.Add(new CuiLabel
            {
                RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
                Text =
                {
                    Text = list.minAmount.ToString(), Font = "robotocondensed-regular.ttf", FontSize = 18, Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 0.3"
                }
            }, Layer + ".input");
            container.Add(new CuiElement
            {
                Parent = Layer + ".input",
                Components =
                {
                    new CuiInputFieldComponent
                    {
                        Align = TextAnchor.MiddleCenter, CharsLimit = 20, FontSize = 18,
                        Command = $"UI_NL CHGMINAMOUNT {index} {lootTable} {prefabName} amount"
                    },
                    new CuiRectTransformComponent {AnchorMin = "0 0", AnchorMax = "1 1"}
                }
            });
            posY -= 35;

            #endregion

            #region Row 5

            container.Add(new CuiLabel
            {
                RectTransform = {AnchorMin = "0.025 1", AnchorMax = "1 1", OffsetMin = $"0 {posY}", OffsetMax = $"0 {posY + 30}"},
                Text =
                {
                    Text = "Max amount: ", Font = "robotocondensed-regular.ttf", FontSize = 18, Align = TextAnchor.MiddleLeft,
                    Color = "1 1 1 1"
                }
            }, Layer + ".itemInfo");

            container.Add(new CuiPanel
            {
                RectTransform = {AnchorMin = "0.325 1", AnchorMax = "0.975 1", OffsetMin = $"0 {posY}", OffsetMax = $"0 {posY + 30}"},
                Image = {Color = "0 0 0 0.8"}
            }, Layer + ".itemInfo", Layer + ".input");
            container.Add(new CuiLabel
            {
                RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
                Text =
                {
                    Text = list.maxAmount.ToString(), Font = "robotocondensed-regular.ttf", FontSize = 18, Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 0.3"
                }
            }, Layer + ".input");
            container.Add(new CuiElement
            {
                Parent = Layer + ".input",
                Components =
                {
                    new CuiInputFieldComponent
                    {
                        Align = TextAnchor.MiddleCenter, CharsLimit = 20, FontSize = 18,
                        Command = $"UI_NL CHGMAXAMOUNT {index} {lootTable} {prefabName} amount"
                    },
                    new CuiRectTransformComponent {AnchorMin = "0 0", AnchorMax = "1 1"}
                }
            });
            posY -= 35;

            #endregion

            #region Row 6

            container.Add(new CuiLabel
            {
                RectTransform = {AnchorMin = "0.025 1", AnchorMax = "1 1", OffsetMin = $"0 {posY}", OffsetMax = $"0 {posY + 30}"},
                Text =
                {
                    Text = "Min condition: ", Font = "robotocondensed-regular.ttf", FontSize = 18, Align = TextAnchor.MiddleLeft,
                    Color = "1 1 1 1"
                }
            }, Layer + ".itemInfo");

            container.Add(new CuiPanel
            {
                RectTransform = {AnchorMin = "0.325 1", AnchorMax = "0.975 1", OffsetMin = $"0 {posY}", OffsetMax = $"0 {posY + 30}"},
                Image = {Color = "0 0 0 0.8"}
            }, Layer + ".itemInfo", Layer + ".input");
            container.Add(new CuiLabel
            {
                RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
                Text =
                {
                    Text = list.minCondition.ToString(), Font = "robotocondensed-regular.ttf", FontSize = 18, Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 0.3"
                }
            }, Layer + ".input");
            container.Add(new CuiElement
            {
                Parent = Layer + ".input",
                Components =
                {
                    new CuiInputFieldComponent
                    {
                        Align = TextAnchor.MiddleCenter, CharsLimit = 20, FontSize = 18,
                        Command = $"UI_NL CHGMINCONDITION {index} {lootTable} {prefabName} amount"
                    },
                    new CuiRectTransformComponent {AnchorMin = "0 0", AnchorMax = "1 1"}
                }
            });
            posY -= 35;

            #endregion

            #region Row 7

            container.Add(new CuiLabel
            {
                RectTransform = {AnchorMin = "0.025 1", AnchorMax = "1 1", OffsetMin = $"0 {posY}", OffsetMax = $"0 {posY + 30}"},
                Text =
                {
                    Text = "Max condition: ", Font = "robotocondensed-regular.ttf", FontSize = 18, Align = TextAnchor.MiddleLeft,
                    Color = "1 1 1 1"
                }
            }, Layer + ".itemInfo");

            container.Add(new CuiPanel
            {
                RectTransform = {AnchorMin = "0.325 1", AnchorMax = "0.975 1", OffsetMin = $"0 {posY}", OffsetMax = $"0 {posY + 30}"},
                Image = {Color = "0 0 0 0.8"}
            }, Layer + ".itemInfo", Layer + ".input");
            container.Add(new CuiLabel
            {
                RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
                Text =
                {
                    Text = list.maxCondition.ToString(), Font = "robotocondensed-regular.ttf", FontSize = 18, Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 0.3"
                }
            }, Layer + ".input");
            container.Add(new CuiElement
            {
                Parent = Layer + ".input",
                Components =
                {
                    new CuiInputFieldComponent
                    {
                        Align = TextAnchor.MiddleCenter, CharsLimit = 20, FontSize = 18,
                        Command = $"UI_NL CHGMAXCONDITION {index} {lootTable} {prefabName} amount"
                    },
                    new CuiRectTransformComponent {AnchorMin = "0 0", AnchorMax = "1 1"}
                }
            });
            posY -= 35;

            #endregion

            #region Row 8

            container.Add(new CuiLabel
            {
                RectTransform = {AnchorMin = "0.025 1", AnchorMax = "1 1", OffsetMin = $"0 {posY}", OffsetMax = $"0 {posY + 30}"},
                Text =
                {
                    Text = "Drop chance: ", Font = "robotocondensed-regular.ttf", FontSize = 18, Align = TextAnchor.MiddleLeft,
                    Color = "1 1 1 1"
                }
            }, Layer + ".itemInfo");

            container.Add(new CuiPanel
            {
                RectTransform = {AnchorMin = "0.325 1", AnchorMax = "0.975 1", OffsetMin = $"0 {posY}", OffsetMax = $"0 {posY + 30}"},
                Image = {Color = "0 0 0 0.8"}
            }, Layer + ".itemInfo", Layer + ".input");
            container.Add(new CuiLabel
            {
                RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
                Text =
                {
                    Text = list.dropChance.ToString(), Font = "robotocondensed-regular.ttf", FontSize = 18, Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 0.3"
                }
            }, Layer + ".input");
            container.Add(new CuiElement
            {
                Parent = Layer + ".input",
                Components =
                {
                    new CuiInputFieldComponent
                    {
                        Align = TextAnchor.MiddleCenter, CharsLimit = 20, FontSize = 18,
                        Command = $"UI_NL CHGDROPCHANCE {index} {lootTable} {prefabName} amount"
                    },
                    new CuiRectTransformComponent {AnchorMin = "0 0", AnchorMax = "1 1"}
                }
            });
            posY -= 35;

            #endregion

            #region Row 9

            container.Add(new CuiLabel
            {
                RectTransform = {AnchorMin = "0.025 1", AnchorMax = "0.975 1", OffsetMin = $"0 {posY}", OffsetMax = $"0 {posY + 30}"},
                Text =
                {
                    Text = list.isBlueprint ? "<color=green>ON</color>" : "<color=red>OFF</color>", Font = "robotocondensed-bold.ttf", FontSize = 18, Align = TextAnchor.MiddleRight,
                    Color = "1 1 1 1"
                }
            }, Layer + ".itemInfo");

            container.Add(new CuiButton
            {
                RectTransform = {AnchorMin = "0.025 1", AnchorMax = "1 1", OffsetMin = $"0 {posY}", OffsetMax = $"0 {posY + 30}"},
                Button = {Color = "0 0 0 0", Command = $"UI_NL CHGISBLUEPRINT {index} {lootTable} {prefabName}"},
                Text =
                {
                    Text = "Is blueprint:", Font = "robotocondensed-regular.ttf", FontSize = 18, Align = TextAnchor.MiddleLeft,
                    Color = "1 1 1 1"
                }
            }, Layer + ".itemInfo");

            #endregion

            CuiHelper.DestroyUi(player, Layer + ".itemInfo");
            CuiHelper.AddUi(player, container);
        }

        private void Outline(CuiElementContainer container, string parent, string color = "1 1 1 1", string size = "2")
        {
            container.Add(new CuiPanel
            {
                RectTransform = {AnchorMin = "0 0", AnchorMax = "1 0", OffsetMin = $"0 0", OffsetMax = $"0 {size}"},
                Image = {Color = color}
            }, parent);
            container.Add(new CuiPanel
            {
                RectTransform = {AnchorMin = "0 1", AnchorMax = "1 1", OffsetMin = $"0 -{size}", OffsetMax = $"0 0"},
                Image = {Color = color}
            }, parent);
            container.Add(new CuiPanel
            {
                RectTransform =
                    {AnchorMin = "0 0", AnchorMax = "0 1", OffsetMin = $"0 {size}", OffsetMax = $"{size} -{size}"},
                Image = {Color = color}
            }, parent);
            container.Add(new CuiPanel
            {
                RectTransform =
                    {AnchorMin = "1 0", AnchorMax = "1 1", OffsetMin = $"-{size} {size}", OffsetMax = $"0 -{size}"},
                Image = {Color = color}
            }, parent);
        }

        #endregion
    }
}    