using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Random = System.Random;
using Oxide.Core;
using Oxide.Core.Plugins;
using Pool = Facepunch.Pool;
using System.Collections;
using Oxide.Plugins.BetterLootExtenstions;
using Facepunch.Extend;
using System.Data;
using System.Text.RegularExpressions;
using UnityEngine.Networking;
using static ConsoleSystem;

namespace Oxide.Plugins
{
    [Info("BetterLoot", "MagicServices.co // TGWA", "4.0.5")]
    [Description("A light loot container modification system with rarity support | Previously maintained and updated by Khan & Tryhard")]
    public class BetterLoot : RustPlugin
    {
        #region Fields
        [PluginReference] Plugin? CustomLootSpawns;

        // Static Instance
        private static BetterLoot? _instance;
        private static PluginConfig? _config;

        // System States
        private bool Changed = true;
        private bool initialized;

        private static Random? rng;
        private static Regex? uniqueTagRegex;
        private static Regex? lootyIdRegex;

        // Data Instances
        private Dictionary<string, List<string>[]> Items = new Dictionary<string, List<string>[]>(); // Cached Item Data for each container
        private Dictionary<string, List<string>[]> Blueprints = new Dictionary<string, List<string>[]>(); // Cached Blueprint Data for each container
        private Dictionary<string, int[]> itemWeights = new Dictionary<string, int[]>(); // Item weights for each container
        private Dictionary<string, int[]> blueprintWeights = new Dictionary<string, int[]>(); // Blueprint weights for each container
        private Dictionary<string, int> totalItemWeight = new Dictionary<string, int>(); // Total sum of item weights for each container
        private Dictionary<string, int> totalBlueprintWeight = new Dictionary<string, int>(); // Total sum of blueprint weights for each container

        /// <summary>
        /// Used for when a npc dies that we can check their prefab type and then use that to populate their loot drop.
        /// This has to be done to avoid issues with overhead of directly modifying loot slots (which would have to be restored on unload if anyone wanted to modify them
        /// without having to respawn the npc entity)
        /// 
        /// The userid is stored as the SteamID on the NPC corpse, so this can be used to do a reverse check to see what the entity was. When we know the prefab
        /// we can pull the loot table profile and generate our loot to replace the vanilla generated loot.
        /// </summary>
        private Dictionary<string, List<ulong>> npcLootMonitor = new Dictionary<string, List<ulong>>(); // NPC Prefab : Currently Spawned Ent IDs
        #endregion

        #region Instance Constants
        private const double BASE_ITEM_RARITY = 2;
        private const string ADMIN_PERM = "betterloot.admin";
        #endregion

        #region Lang
        private string BLLang(string key, string? id = null) => lang.GetMessage(key, this, id);
        private string BLLang(string key, string? id, params object[] args) => string.Format(BLLang(key, id), args);

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                { "initialized", "Plugin not enabled" },
                { "perm", "You are not authorized to use this command" },
                { "syntax", "Usage: /blacklist [additem|deleteitem] \"ITEMNAME\"" },
                { "none", "There are no blacklisted items" },
                { "blocked", "Blacklisted items: {0}" },
                { "notvalid", "Not a valid item: {0}" },
                { "blockedpass", "The item '{0}' is now blacklisted" },
                { "blockedtrue", "The item '{0}' is already blacklisted}" },
                { "unblacklisted", "The item '{0}' has been unblacklisted" },
                { "blockedfalse", "The item '{0}' is not blacklisted" },
                { "lootycmdformat", "Usage: /looty \"looty-id\"" },
                { "lootynotfound", "The requested table id was not found. Please ensure youve got the right code." },
                { "lootyinvalidid", "Invalid looty id format. Please ensure youve got the right code." }
            }, this); //en
        }
        #endregion

        #region Config
        private class PluginConfig : SerializableConfiguration
        {
            [JsonProperty("Chat Configuration")]
            public ChatConfiguration chatConfig = new ChatConfiguration();
            [JsonProperty("General Configuration")]
            public GenericConfiguration Generic = new GenericConfiguration();
            [JsonProperty("Loot Configuration")]
            public LootConfiguration Loot = new LootConfiguration();
            [JsonProperty("Loot Groups Configuration")]
            public LootGroupsConfiguration LootGroupsConfig = new LootGroupsConfiguration();
        }

        private class GenericConfiguration
        {
            [JsonProperty("Blueprint Probability")]
            public double blueprintProbability = 0.11;
            [JsonProperty("Log Updates On Load")]
            public bool listUpdatesOnLoaded = true;
            [JsonProperty("Remove Stacked Containers")]
            public bool removeStackedContainers = true;
            [JsonProperty("Watched Prefabs")]
            public HashSet<string> WatchedPrefabs = new HashSet<string>();
        }

        private class LootConfiguration
        {
            [JsonProperty("Enable Hammer Hit Loot Cycle")]
            public bool enableHammerLootCycle = false;
            [JsonProperty("Hammer Loot Cycle Time")]
            public double hammerLootCycleTime = 3.0;
            [JsonProperty("Loot Multiplier")]
            public int lootMultiplier = 1;
            [JsonProperty("Scrap Multipler")]
            public int scrapMultiplier = 1;
            [JsonProperty("Allow duplicate items")]
            public bool allowDuplicateItems = false;
        }

        private class ChatConfiguration
        {
            [JsonProperty("Chat Message Prefix")]
            public string prefix = $"[<color=#00ff00>{nameof(BetterLoot)}</color>]";
            [JsonProperty("Chat Message Icon SteamID (0 = None)")]
            public ulong messageIcon = 0;
        }

        private class LootGroupsConfiguration
        {
            [JsonProperty("Enable creation of example loot group on load?")]
            public bool enableExampleGroupCreation = true;
            [JsonProperty("Enable auto profile probability balancing?")]
            public bool enableProbabilityBalancing = true;
            [JsonProperty("Always allow duplicate items from loot groups (if true overrides 'Allow duplicate items option')")]
            public bool allowLootGroupDuplicateItems = true;
        }

        private void CheckWatchedPrefabs()
        {
            /* Watched Prefabs Auto-Population */
            if (_config?.Generic.WatchedPrefabs.Any() ?? false)
                return;

            Log("Updating watched prefabs from manifest...");

            // Name filtering
            List<string> negativePartialNames = Pool.Get<List<string>>();
            List<string> partialNames = Pool.Get<List<string>>();

            // If does not contain, skip
            negativePartialNames.AddRange(
                new List<string> {
                    "resource/loot",
                    "misc/supply drop/supply_drop",
                    "/npc/m2bradley/bradley_crate",
                    "/npc/patrol helicopter/heli_crate",
                    "/deployable/chinooklockedcrate/chinooklocked",
                    "/deployable/chinooklockedcrate/codelocked",
                    "prefabs/radtown",
                    "props/roadsigns",
                    "humannpc/scientist",
                    "humannpc/tunneldweller",
                    "humannpc/underwaterdweller"
                }
            );

            // If does contain, skip
            partialNames.AddRange(
                new List<string>
                {
                    "radtown/ore",
                    "static",
                    "/spawners",
                    "radtown/desk",
                    "radtown/loot_component_test",
                    "water_puddles_border_fix" // Weird container prefab from radtown update??
                }
            );

            foreach (GameManifest.PrefabProperties category in GameManifest.Current.prefabProperties)
            {
                string name = category.name;

                if (!negativePartialNames.ContainsPartial(name) || partialNames.ContainsPartial(name))
                    continue;

                if (!_config.Generic.WatchedPrefabs.Contains(name))
                    _config.Generic.WatchedPrefabs.Add(name);
            }

            SaveConfig();

            Log("Updated configuration with manifest values.");

            AttemptSendLootyLink();

            Pool.FreeUnmanaged(ref negativePartialNames);
            Pool.FreeUnmanaged(ref partialNames);
        }

        protected override void LoadDefaultConfig() => _config = new PluginConfig();

        protected override void LoadConfig()
        {
            base.LoadConfig();

            try
            {
                _config = Config.ReadObject<PluginConfig>();

                if (_config == null)
                {
                    Log($"Generating Config File for Better Loot");
                    throw new JsonException();
                }

                if (MaybeUpdateConfig(_config))
                {
                    Log("Configuration appears to be outdated; updating and saving Better Loot");
                    SaveConfig();
                }
            }
            catch (Exception ex)
            {
                Log("Failed to load Better Loot config file (is the config file corrupt?) (" + ex.Message + ")");
            }
        }

        protected override void SaveConfig()
        {
            Log($"Configuration changes saved to {nameof(BetterLoot)}.json");
            Config.WriteObject(_config, true);
        }

        #region Configuration Updater
        internal class SerializableConfiguration
        {
            public string ToJson() => JsonConvert.SerializeObject(this);

            public Dictionary<string, object> ToDictionary() => JsonHelper.Deserialize(ToJson()) as Dictionary<string, object>;
        }

        private static class JsonHelper
        {
            public static object Deserialize(string json) => ToObject(JToken.Parse(json));

            private static object ToObject(JToken token)
            {
                switch (token.Type)
                {
                    case JTokenType.Object:
                        return token.Children<JProperty>().ToDictionary(prop => prop.Name, prop => ToObject(prop.Value));
                    case JTokenType.Array:
                        return token.Select(ToObject).ToList();

                    default:
                        return ((JValue)token).Value;
                }
            }
        }

        private bool MaybeUpdateConfig(SerializableConfiguration config)
        {
            var currentWithDefaults = config.ToDictionary();
            var currentRaw = Config.ToDictionary(x => x.Key, x => x.Value);
            return MaybeUpdateConfigDict(currentWithDefaults, currentRaw);
        }

        private bool MaybeUpdateConfigDict(Dictionary<string, object> currentWithDefaults, Dictionary<string, object> currentRaw)
        {
            bool changed = false;

            foreach (var key in currentWithDefaults.Keys)
            {
                if (currentRaw.TryGetValue(key, out object? currentRawValue) && currentRawValue is not null)
                {
                    var defaultDictValue = currentWithDefaults[key] as Dictionary<string, object>;
                    var currentDictValue = currentRawValue as Dictionary<string, object>;

                    if (defaultDictValue != null)
                    {
                        if (currentDictValue == null)
                        {
                            currentRaw[key] = currentWithDefaults[key];
                            changed = true;
                        }
                        else if (MaybeUpdateConfigDict(defaultDictValue, currentDictValue))
                            changed = true;
                    }
                }
                else
                {
                    currentRaw[key] = currentWithDefaults[key];
                    changed = true;
                }
            }

            return changed;
        }
        #endregion
        #endregion

        #region Oxide Loaded / Unload / Server Load 
        private void Loaded()
        {
            _instance = this;
            rng = new Random();
            uniqueTagRegex = new Regex(@"\{\d+\}");
            lootyIdRegex = new Regex(@"^looty-[A-Za-z0-9]{3}$");

            DataSystem.LoadBlacklist();
            DataSystem.LoadLootTables();
            DataSystem.LoadLootGroups();

            CheckWatchedPrefabs();
        }

        private void OnServerInitialized()
        {
            ItemManager.Initialize();
            permission.RegisterPermission(ADMIN_PERM, this);
            LoadAllContainers();
            UpdateInternals(_config.Generic.listUpdatesOnLoaded);
        }

        private void Unload()
        {
            // Static variable instances
            uniqueTagRegex = null;
            lootyIdRegex = null;

            storedBlacklist = null;
            lootTables = null;
            lootGroups = null;
            rng = null;

            // Static BetterLoot instance
            _instance = null;

            // Change this to a local list to track HammerHitLootCycle or change to Coroutine
            var gameObjects = UnityEngine.Object.FindObjectsOfType<HammerHitLootCycle>().ToList();

            if (gameObjects.Any())
            {
                foreach (var objects in gameObjects)
                    if (objects is not null)
                        UnityEngine.Object.Destroy(objects);
            }
        }
        #endregion
        
        #region DataFile
        private static LootTableData? lootTables = null;
        private static StoredBlacklist? storedBlacklist = null;
        private static LootGroupsData? lootGroups = null;

        // LootTables.json structure
        private class LootTableData
        {
            public Dictionary<string, PrefabLoot> LootTables = new Dictionary<string, PrefabLoot>();

            public LootTableData() { }
        }

        // Blacklist.json structure
        private class StoredBlacklist
        {
            public HashSet<string> ItemList = new HashSet<string>();

            public StoredBlacklist() { }
        }

        private class LootGroupsData
        {
            [JsonProperty("Loot Groups", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public Dictionary<string, LootProfile> LootGroups = new Dictionary<string, LootProfile>
            {
                ["example_group"] = new LootProfile(new Dictionary<string, LootProfile.LootRNG> { ["lmg.m249"] = new LootProfile.LootRNG(10, new LootAmount(1, 2)) }, false)
            };

            public LootGroupsData() { }

            public static void ValidateGroups(LootGroupsData? Data)
            {
                ItemManager.Initialize();

                if (ItemManager.itemDictionaryByName is null)
                {
                    Log("Error: Failed to initialize ItemDictionary. Unloading");
                    _instance.Server.Command($"o.unload BetterLoot");
                    return;
                }

                if (Data is null || Data.LootGroups is null)
                {
                    Log($"Error: Invalid data was provided to the {nameof(LootGroupsData)} validator!");
                    return;
                }

                // Attempt to create an example group in the LootGroups file
                TryCreateExampleGroup();

                foreach((string profileName, LootProfile? profileData) in Data.LootGroups)
                {
                    Log($"Validating LootGroup: \"{profileName}\"");

                    // NRE Data Check
                    if (profileData.ItemList is null)
                    {
                        Log("- Error: Profile item list is null. Skipping...");
                        continue;
                    } 

                    // Ensure items are valid
                    List<string> invalidItemPrefabs = Pool.Get<List<string>>();
                    invalidItemPrefabs.AddRange(profileData.ItemList.Keys.Where(key => !ItemManager.itemDictionaryByName.ContainsKey(uniqueTagRegex.Replace(key.ToLower(), string.Empty))));
                    
                    if (profileData.ItemList.RemoveAll(invalidItemPrefabs.Contains) is int removeCount && removeCount > 0)
                        Log($"- Removed {removeCount} invalid item entries");

                    Pool.FreeUnmanaged(ref invalidItemPrefabs);

                    // Balance loot percentages
                    if (_config.LootGroupsConfig.enableProbabilityBalancing)
                    {
                        double GetSum() => profileData.ItemList.Sum(x => x.Value.Probability);
                        double Round(double x) => Math.Round(x, 2);

                        const double target = 100;
                        double sum = GetSum();

                        if (Math.Abs(target - sum) > 1e-3)
                        {
                            Log($"- Profile probability sum ({sum}) != 100. Balancing profile!");

                            double _ratio = target / sum;

                            string largestKey = string.Empty;
                            double largestValue = 0;
                            foreach (var item in profileData.ItemList)
                            {
                                double probability = item.Value.Probability;
                                if (probability > largestValue)
                                {
                                    largestValue = probability;
                                    largestKey = item.Key;
                                }

                                item.Value.Probability = Round(probability * _ratio);
                            }

                            var largestEntry = profileData.ItemList[largestKey];
                            largestEntry.Probability = Round(largestEntry.Probability - Round(target - GetSum()));
                        }
                    }

                    Log($"Profile \"{profileName}\" validation complete.");
                }
            }

            public static void TryCreateExampleGroup()
            {
                if (!_config.LootGroupsConfig.enableExampleGroupCreation)
                    return;

                // Create a default group in the first item of the loot table for reference if none exists
                var firstLootTable = lootTables.LootTables.FirstOrDefault();
                if (!firstLootTable.IsDefault() && !firstLootTable.Value.LootProfiles.Any())
                {
                    firstLootTable.Value.LootProfiles.Add(new PrefabLoot.LootProfileImport("example_group", 30, false));
                    Log($"Added LootGroup Import example to \"{firstLootTable.Key}\"");
                    DataSystem.SaveLootTables();
                }
            }
        }

        private static class DataSystem
        {
            #region Public Methods
            #region Blacklist
            private const string BL_FN = "Blacklist";

            public static void LoadBlacklist() 
                => LoadFile(BL_FN, (blacklistData) => CheckNull(ref blacklistData, ref storedBlacklist, blacklistData?.ItemList), ref storedBlacklist);

            public static void SaveBlacklist()
                => SaveFile(BL_FN, (blacklistData) => CheckNull(ref blacklistData, ref storedBlacklist, blacklistData?.ItemList), ref storedBlacklist);
            #endregion

            #region Loot Tables
            private const string LT_FN = "LootTables";

            public static void LoadLootTables()
                => LoadFile(LT_FN, (tableData) => CheckNull(ref tableData, ref lootTables, tableData?.LootTables), ref lootTables);

            public static void SaveLootTables()
                => SaveFile(LT_FN, (tableData) => CheckNull(ref tableData, ref lootTables, tableData?.LootTables), ref lootTables);
            #endregion

            #region Loot Groups
            private const string LG_FN = "LootGroups";

            public static void LoadLootGroups()
                => LoadFile(LG_FN, (groupsData) => CheckNull(ref groupsData, ref lootGroups), ref lootGroups, LootGroupsData.ValidateGroups);

            public static void SaveLootGroups()
                => SaveFile(LG_FN, (groupsData) => CheckNull(ref groupsData, ref lootGroups), ref lootGroups);
            #endregion
            #endregion

            #region DataFile Error Backup
            public static void BakDataFile(string filename, bool restoreMode = false, BasePlayer? msgPlayer = null)
            {
                bool sendPlayer = msgPlayer is not null;
                string notifyMessage = string.Format(restoreMode ? "Restoring backup of {0}" : "Created backup of datafile {0}", $"{filename}.json");
                
                if (sendPlayer)
                    _instance?.SendMessage(msgPlayer, notifyMessage);
                else
                    Log(notifyMessage);
                
                // Rename specified file to *.bak before regenerating a file in place of it
                string path = Path.Combine(Interface.Oxide.DataFileSystem.Directory, $"{nameof(BetterLoot)}/{filename}.json");
                string bakPath = $"{path}.bak";

                if (File.Exists(bakPath))
                {
                    if (!restoreMode)
                    {
                        File.Delete(bakPath);
                    }
                    else
                    {
                        const string msg = "No backup file to restore.";
                        if (sendPlayer)
                            _instance?.SendMessage(msgPlayer, msg);
                        else
                            Log(msg);
                        return;
                    }
                }

                File.Move(restoreMode ? bakPath : path, $"{path}{(!restoreMode ? ".bak" : string.Empty)}");
            }
            #endregion

            #region Save / Load Methods
            /// <summary>
            /// Load a data from a file within the plugin data directory.
            /// </summary>
            /// <typeparam name="T">The structure of the data being read from the file.</typeparam>
            /// <param name="fileName">The name of the file within the plugin data directory</param>
            /// <param name="validator">A custom data validator. Is nullable.</param>
            /// <param name="loadVar">The variable where the loaded data should be stored.</param>
            private static void LoadFile<T>(string fileName, Func<T, T>? validator, ref T loadVar, Action<T>? postLoadMethod = null) where T : class?
            {
                // If no validator was provided, set to check if instance is null, if it is create new instance.
                if (validator is null)
                    validator = (data) => data ?? Activator.CreateInstance<T>();

                try
                {
                    loadVar = validator(Interface.Oxide.DataFileSystem.ReadObject<T>($"{nameof(BetterLoot)}\\{fileName}"));
                    Log($"Loaded file \"{fileName}\" datafile successfully!");
                } catch (Exception e)
                {
                    Log($"ERROR: There was an issue loading your \"{fileName}.json\" datafile, a new one has been created.\n{e.Message}");

                    BakDataFile(fileName);
                    loadVar = Activator.CreateInstance<T>();
                }

                postLoadMethod?.Invoke(loadVar);

                SaveFile(fileName, validator, ref loadVar);
            }

            /// <summary>
            /// Save plugin data to the plugin data directory.
            /// </summary>
            /// <typeparam name="T">The type of the datafile structure</typeparam>
            /// <param name="fileName">The name of the datafile within the plugin data directory.</param>
            /// <param name="validator">A custom data validator. Is nullable.</param>
            /// <param name="saveVar">The variable where this data is currently stored.</param>
            private static void SaveFile<T>(string fileName, Func<T, T>? validator, ref T saveVar) where T : class?
            {
                if (validator is null)
                    validator = (data) => data ?? Activator.CreateInstance<T>();

                Interface.Oxide.DataFileSystem.WriteObject($"{nameof(BetterLoot)}\\{fileName}", validator(saveVar));

                Log($"Saved {fileName}.json");
            }
            #endregion

            #region Data Validator
            /// <summary>
            /// Checks if the provided datafile's data is null, if so create a new instance and optionally write it to file.
            /// </summary>
            /// <typeparam name="T">The type of the data structure that is being checked</typeparam>
            /// <param name="obj">The local instance of the data to check</param>
            /// <param name="target">The global instance of where the data is held</param>
            /// <param name="additional">Additional objects to check if null aside from arguement 'obj'</param>
            /// <returns>>Non null type of provided object type</returns>
            private static T CheckNull<T> (ref T? obj, ref T? target, params object?[] additional) where T : class? 
            {
                if (obj is null || additional.Any(x => x is null))
                {
                    target = Activator.CreateInstance<T>();
                    obj = target;
                }

                return obj;
            }
            #endregion
        }

        private static void AttemptSendLootyLink()
        {
            Log("--------------------------------------------------------------------------");
            Log("Use the Looty Editor to easily edit and create loot tables for BetterLoot!");
            Log("Find it here -> https://editor.looty.cc/betterloot-v2");
            Log("--------------------------------------------------------------------------");
        }
        #endregion

        #region Loot Classes
        /// <summary>
        /// Prefab Loot system will be contained in a list. This is the new loot class for loot containers that will
        /// allow the import of custom loot groups allowing for RNG on groups as well as individual items
        /// </summary>
        private class PrefabLoot
        {
            [JsonProperty("Is Prefab Enabled?")]
            public bool Enabled;

            [JsonProperty("Loot Profiles", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<LootProfileImport> LootProfiles;

            [JsonProperty("Ungrouped Items")]
            public Dictionary<string, LootAmount> UngroupedItems;

            [JsonProperty("Item Settings")]
            public ItemSettings itemSettings;

            [JsonIgnore]
            public int ItemCount => GetTotalItemCount();

            public PrefabLoot()
            {
                LootProfiles = new List<LootProfileImport>();
                UngroupedItems = new Dictionary<string, LootAmount>();
                itemSettings = new ItemSettings();
            }

            internal class LootProfileImport
            {
                [JsonProperty("Group Enabled?")]
                public bool Enabled = true;
                [JsonProperty("Loot Profile Name")]
                public string LootProfileName = string.Empty;
                [JsonProperty("Loot Profile Probability (1% - 100%)")]
                public double LootProfileProbability;

                internal LootProfileImport() { }

                internal LootProfileImport(string LootProfileName, double LootProfileProbability, bool Enabled = true)
                {
                    this.LootProfileName = LootProfileName;
                    this.LootProfileProbability = LootProfileProbability;
                    this.Enabled = Enabled;
                }
            }

            internal class ItemSettings
            {
                [JsonProperty("Minimum Amount of Items")]
                public int ItemsMin;

                [JsonProperty("Maximum Amount of Items")]
                public int ItemsMax;

                [JsonProperty("Scrap Amount")]
                public int Scrap;

                [JsonProperty("Max Blueprints")]
                public int MaxBPs;

                internal ItemSettings() { }
            }

            private int GetTotalItemCount()
            {
                int count = 0;
                count += UngroupedItems.Count();

                if (lootGroups is not null)
                    foreach (LootProfileImport import in LootProfiles)
                        if (lootGroups.LootGroups.TryGetValue(import.LootProfileName, out LootProfile? profile) && profile is not null)
                            count += profile.ItemList.Count;

                return count;
            }
        }

        /// <summary>
        /// LootProfile for containing all items that will be part of a certain profile
        /// Will be referenced by the specified profile name that the user creates within the LootGroups.json
        /// </summary>
        public class LootProfile
        {
            [JsonProperty("Enabled?")]
            public bool Enabled = true;

            [JsonProperty("Item List")]
            public Dictionary<string, LootRNG> ItemList;

            [JsonIgnore]
            private List<double> _culminativeProbabilities = new List<double>();

            [JsonIgnore]
            public bool probabilitiesExist
                => _culminativeProbabilities.Any();

            public LootProfile()
                => ItemList = new Dictionary<string, LootRNG>();

            public LootProfile(Dictionary<string, LootRNG> ItemList, bool Enabled = true)
            {
                this.ItemList = ItemList;
                this.Enabled = Enabled;
            }

            public class LootRNG
            {
                [JsonProperty("Item Probability (1-100)")]
                public double Probability;

                [JsonProperty("Item Amount")]
                public LootAmount Amount;

                public LootRNG(double Probability, LootAmount Amount)
                {
                    this.Probability = Probability;
                    this.Amount = Amount;
                }
            }

            #region Probalistic Selector Methods
            public void UpdateProbabilities()
            {
                double _culminative = 0;
                foreach(var item in ItemList.Values)
                {
                    _culminative += item.Probability;
                    _culminativeProbabilities.Add(_culminative);
                }
            }
               
            /// <summary>
            /// Get a random item from this loot group based off of items probabilities
            /// </summary>
            public Item? GetItem()
            {
                double randomSelect = rng.NextDouble() * 1e2;
                int itemIndex = _culminativeProbabilities.BinarySearch(randomSelect);

                if (itemIndex < 0)
                    itemIndex = ~itemIndex;

                // No item found
                if (itemIndex >= ItemList.Count)
                    return null;

                var entry = ItemList.ElementAt(itemIndex);

                // Select Amount
                int _min = entry.Value.Amount.Min, _max = entry.Value.Amount.Max;
                int amount = UnityEngine.Random.Range(Mathf.Min(_min, _max), Mathf.Max(_min, _max));

                // Get Custom Properties
                ulong skinId = entry.Value.Amount.skinId;
                string? customName = entry.Value.Amount.displayName;

                // Create Item
                Item item = ItemManager.CreateByPartialName(uniqueTagRegex.Replace(entry.Key, string.Empty), amount);
                item.name = customName ?? string.Empty;
                item.skin = skinId;

                item.MarkDirty();

                if (item is null)
                    Log($"ERROR: item \"{entry.Key}\" could not be created! System returned null entry!");

                return item;
            }
            #endregion
        }

        public class LootAmount
        {
            [JsonProperty("Skin ID (0 = default)")]
            public ulong skinId = 0;

            [JsonProperty("Display Name (empty = none)")]
            public string? displayName = string.Empty;

            [JsonProperty("Item Minimum")]
            public int Min;

            [JsonProperty("Item Maximum")]
            public int Max;

            public LootAmount(int Min, int Max)
            {
                this.Min = Min;
                this.Max = Max;
            }
        }
        #endregion

        #region Util
        private static void Log(string msg, params object[] args) => _instance?.Puts(msg, args);
        private void SendMessage(BasePlayer player, string message, params object[] args) => Player.Reply(player, message, _config.chatConfig.prefix, _config.chatConfig.messageIcon, args);
        #endregion

        #region Oxide Hooks
        private object OnLootSpawn(LootContainer container)
        {
            if ((!initialized || container == null) || (CustomLootSpawns != null && CustomLootSpawns.Call<bool>("IsLootBox", container)))
                return null;

            if (PopulateContainer(container))
                return true;

            return null;
        }

        private void OnEntitySpawned(NPCPlayerCorpse corpse)
            => NextTick(() => PopulateContainer(corpse));

        private void OnEntitySpawned(NPCPlayer npc)
        {
            string name = npc.name;
            if (_config.Generic.WatchedPrefabs.Contains(name))
            {
                ulong id = npc.userID;

                if (npcLootMonitor.ContainsKey(name))
                    npcLootMonitor[name].Add(id);
                else
                    npcLootMonitor.Add(name, new List<ulong> { id });
            }
        }
        #endregion

        #region Loot Methods
        private int ItemWeight(double baseRarity, int index) => (int)(Math.Pow(baseRarity, 4 - index) * 1000);

        // OPTIMIZE
        private LootAmount GetAmounts(ItemAmount amount)
        {
            LootAmount options = new LootAmount(
                (int)amount.amount, 
                ((ItemAmountRanged)amount).maxAmount > 0 && ((ItemAmountRanged)amount).maxAmount > amount.amount
                     ? (int)((ItemAmountRanged)amount).maxAmount 
                     : (int)amount.amount
            );

            return options;
        }

        private void GetLootSpawn(LootSpawn lootSpawn, ref Dictionary<string, LootAmount> items)
        {
            if (lootSpawn.subSpawn != null && lootSpawn.subSpawn.Any())
            {
                foreach (var entry in lootSpawn.subSpawn)
                    GetLootSpawn(entry.category, ref items);
            }
            else if (lootSpawn.items != null && lootSpawn.items.Any())
            {
                foreach (var amount in lootSpawn.items)
                {
                    LootAmount options = GetAmounts(amount);
                    string itemName = amount.itemDef.shortname;
                    if (amount.itemDef.spawnAsBlueprint)
                        itemName += ".blueprint";
                    if (!items.ContainsKey(itemName))
                        items.Add(itemName, options);
                }
            }
        }

        private void LoadAllContainers()
        {
            var nullTablePrefabs = Pool.Get<List<string>>();

            bool wasAdded = false;

            // OPTIMIZE
            foreach (var lootPrefab in _config.Generic.WatchedPrefabs)
            {
                if (!lootTables.LootTables.ContainsKey(lootPrefab))
                {
                    var basePrefab = GameManager.server.FindPrefab(lootPrefab);
                    if (basePrefab is null)
                    {
                        nullTablePrefabs.Add(lootPrefab);
                        continue;
                    }

                    var npc = basePrefab.GetComponent<global::HumanNPC>();
                    if (npc is not null)
                    { // is npc
                        var container = new PrefabLoot();

                        container.Enabled = !lootPrefab.Contains("bradley_crate") && !lootPrefab.Contains("heli_crate");
                        container.itemSettings.Scrap = 0;

                        var slotItemCount = 0;
                        var itemList = new Dictionary<string, LootAmount>();

                        foreach (var slot in npc.LootSpawnSlots)
                        {
                            GetLootSpawn(slot.definition, ref itemList);
                            slotItemCount += slot.numberToSpawn;
                        }

                        container.itemSettings.ItemsMin = container.itemSettings.ItemsMax = slotItemCount;
                        container.UngroupedItems = itemList;

                        lootTables.LootTables.Add(lootPrefab, container);
                        wasAdded = true;
                    }
                    else
                    { // is not npc
                        var loot = basePrefab.GetComponent<LootContainer>();

                        if (loot is null)
                        {
                            nullTablePrefabs.Add(lootPrefab);
                            continue;
                        }

                        var container = new PrefabLoot();

                        container.Enabled = !lootPrefab.Contains("bradley_crate") && !lootPrefab.Contains("heli_crate");
                        container.itemSettings.Scrap = loot.scrapAmount;

                        int slots = 0;
                        if (loot.LootSpawnSlots.Length > 0)
                        {
                            LootContainer.LootSpawnSlot[] lootSpawnSlots = loot.LootSpawnSlots;
                            for (int i = 0; i < lootSpawnSlots.Length; i++)
                                slots += lootSpawnSlots[i].numberToSpawn;
                        }
                        else
                            slots = loot.maxDefinitionsToSpawn;

                        container.itemSettings.ItemsMin = container.itemSettings.ItemsMax = slots;
                        container.itemSettings.MaxBPs = 1;

                        var itemList = new Dictionary<string, LootAmount>();
                        if (loot.lootDefinition is not null)
                            GetLootSpawn(loot.lootDefinition, ref itemList);
                        else if (loot.LootSpawnSlots.Any())
                        {
                            LootContainer.LootSpawnSlot[] lootSpawnSlots = loot.LootSpawnSlots;
                            foreach (var lootSpawnSlot in lootSpawnSlots)
                                GetLootSpawn(lootSpawnSlot.definition, ref itemList);
                        }

                        // Default items
                        container.UngroupedItems = itemList;

                        lootTables.LootTables.Add(lootPrefab, container);
                        wasAdded = true;
                    } 
                }
            }

            // Some prefabs are loaded but not used (unloaded or invalid prefab)
            if (nullTablePrefabs.Any() && _config.Generic.WatchedPrefabs.RemoveWhere(prefab => nullTablePrefabs.Contains(prefab)) is int missing && missing > 0)
            {
                Puts($"Removed {missing} invalid / unloaded prefabs from watch list:\n{string.Join(", \n", nullTablePrefabs)}");
                SaveConfig();
            }

            Pool.FreeUnmanaged(ref nullTablePrefabs);

            // Write Changes
            if (wasAdded)
            {
                // Try to create an example loot group within the LootTables.json file for user reference :)
                LootGroupsData.TryCreateExampleGroup();
                DataSystem.SaveLootTables();
            }
                
            wasAdded = false;

            // Correct any invalid entries
            bool wasRemoved = false;
            int activeTypes = 0;

            foreach (var lootTable in lootTables.LootTables.ToList())
            {
                var basePrefab = GameManager.server.FindPrefab(lootTable.Key);
                
                var npc = basePrefab?.GetComponent<global::HumanNPC>();
                var loot = basePrefab?.GetComponent<LootContainer>();
                var container = lootTable.Value;

                if (npc is null && loot is null)
                {
                    lootTables.LootTables.Remove(lootTable.Key);
                    Log($"Removed Invalid Loot Table {lootTable.Key}");
                    wasRemoved = true;

                    continue;
                }

                // Groups items by rarity (weight). Reference: ItemDefinition.Rarity enum

                Items.Add(lootTable.Key, new List<string>[5]);
                Blueprints.Add(lootTable.Key, new List<string>[5]);

                for (var i = 0; i < 5; ++i)
                {
                    Items[lootTable.Key][i] = new List<string>();
                    Blueprints[lootTable.Key][i] = new List<string>();
                }

                foreach (var itemEntry in container.UngroupedItems)
                {
                    bool isBP = itemEntry.Key.EndsWith(".blueprint");
                    var def = ItemManager.FindItemDefinition(itemEntry.Key.Replace(".blueprint", ""));

                    if (def is not null)
                    {
                        if (isBP && def.Blueprint is not null && def.Blueprint.isResearchable)
                        {
                            int index = (int)def.rarity;
                            if (!Blueprints[lootTable.Key][index].Contains(def.shortname))
                                Blueprints[lootTable.Key][index].Add(def.shortname);
                        }
                        else
                        {
                            int index = (int)def.rarity;
                            if (!Items[lootTable.Key][index].Contains(def.shortname))
                                Items[lootTable.Key][index].Add(def.shortname);
                        }
                    }
                }

                totalItemWeight.Add(lootTable.Key, 0);
                totalBlueprintWeight.Add(lootTable.Key, 0);
                itemWeights.Add(lootTable.Key, new int[5]);
                blueprintWeights.Add(lootTable.Key, new int[5]);

                for (var i = 0; i < 5; ++i)
                {
                    totalItemWeight[lootTable.Key] += (itemWeights[lootTable.Key][i] = ItemWeight(BASE_ITEM_RARITY, i) * Items[lootTable.Key][i].Count);
                    totalBlueprintWeight[lootTable.Key] += (blueprintWeights[lootTable.Key][i] = ItemWeight(BASE_ITEM_RARITY, i) * Blueprints[lootTable.Key][i].Count);
                }
            }

            if (wasAdded || wasRemoved)
                DataSystem.SaveLootTables();

            activeTypes = lootTables.LootTables.Count(table => table.Value.Enabled);

            Log($"Using '{activeTypes}' active of '{lootTables.LootTables.Count}' supported container types");
        }
        #endregion

        #region Core
        // NPC Implementation
        private bool PopulateContainer(NPCPlayerCorpse npc)
        {
            if (npc is null)
                return false;

            // API Call
            if (Interface.CallHook("ShouldBLPopulate_NPC", npc.playerSteamID) != null)
                return false;

            // Reverse search entity for prefab
            string prefab = string.Empty;

            foreach (var entry in npcLootMonitor)
            {
                if (entry.Value.Contains(npc.playerSteamID))
                {
                    prefab = entry.Key;
                    break;
                }
            }

            if (string.IsNullOrEmpty(prefab) || (!npc.containers.Any()) || npc.containers[0] is not ItemContainer inventory)
                return false;
                
            return PopulateContainer(inventory, prefab);
        }

        private bool PopulateContainer(LootContainer container)
        {
            if (container is null) 
                return false;

            if (Interface.CallHook("ShouldBLPopulate_Container", container.net.ID.Value) != null)
                return false;

            if (container.inventory is null)
            {
                container.CreateInventory(true);
                container.OnInventoryFirstCreated(container.inventory);
            }

            return PopulateContainer(container.inventory, container.PrefabName);
        }

        private bool PopulateContainer(ItemContainer container, string prefab)
        {
            if (container is null || lootTables is null || !lootTables.LootTables.TryGetValue(prefab, out PrefabLoot? con) || con is null || !con.Enabled)
                return false;

            int lootItemCount = con.ItemCount;

            int min = con.itemSettings.ItemsMin, max = con.itemSettings.ItemsMax;
            int itemCount = UnityEngine.Random.Range(Mathf.Min(min, max), Mathf.Max(min, max));
            if (lootItemCount > 0 && itemCount > lootItemCount && lootItemCount < 36)
                itemCount = lootItemCount;

            container.Clear();
            container.capacity = 36;
            
            List<string> itemNames = Pool.Get<List<string>>();
            List<Item> items = Pool.Get<List<Item>>();
            List<int> itemBlueprints = Pool.Get<List<int>>();

            var maxRetry = 10;
            for (int i = 0; i < itemCount; ++i)
            {
                if (maxRetry is 0)
                    break;

                Item? item = null;
                bool isLootGroupItem = false;

                foreach(var import in con.LootProfiles)
                {
                    if (!import.Enabled)
                        continue;

                    if (!lootGroups.LootGroups.TryGetValue(import.LootProfileName, out LootProfile? profile) || profile is null)
                    {
                        Log($"WARNING: prefab \"{prefab}\" requested a loot group import with name \"{import.LootProfileName}\". Group does not exist!");
                        continue;
                    }
                    else if (!profile.Enabled)
                        continue;

                    // RNG => Use Profile
                    double rng_pr = rng.NextDouble() * 1e2;

                    // RNG Check Failed
                    if (rng_pr > import.LootProfileProbability)
                        continue;

                    // Ensure probabilities are generated, if not populate them
                    if (!profile.probabilitiesExist)
                        profile.UpdateProbabilities();

                    // Get item
                    if (profile.GetItem() is Item _item)
                        item = _item;

                    if (item != null)
                        isLootGroupItem = true;
                }

                // Loot import not used, generate from ungrouped items with default rng system
                if (item == null)
                    item = MightyRNG(prefab, itemCount, itemBlueprints.Count >= con.itemSettings.MaxBPs);

                // No item was generated from either system, attempt to regenerate.
                if (item == null)
                {
                    --maxRetry;
                    --i;
                    continue;
                }

                // Duplicates
                if (((isLootGroupItem && !_config.LootGroupsConfig.allowLootGroupDuplicateItems) || !_config.Loot.allowDuplicateItems) && (itemNames.Contains(item.info.shortname) || (item.IsBlueprint() && itemBlueprints.Contains(item.blueprintTarget))))
                {
                    item.Remove();
                    --maxRetry;
                    --i;
                    continue;
                }
                else if (item.IsBlueprint())
                    itemBlueprints.Add(item.blueprintTarget);
                else
                    itemNames.Add(item.info.shortname);

                items.Add(item);

                if (storedBlacklist.ItemList.Contains(item.info.shortname))
                {
                    items.Remove(item);
                    item.Remove(); // broken item fix
                }
            }

            foreach (var item in items.Where(x => x != null && x.IsValid()))
                if (!item.MoveToContainer(container, -1, true)) // broken item fix / fixes full container 
                    item.DoRemove();

            if (con.itemSettings.Scrap > 0)
            {
                int scrapCount = con.itemSettings.Scrap;
                Item item = ItemManager.CreateByItemID(-932201673, scrapCount * _config.Loot.scrapMultiplier); // Scrap item ID
                if (!item.MoveToContainer(container, -1, false))
                    item.DoRemove();
            }

            container.capacity = container.itemList.Count;
            container.MarkDirty();

            Pool.FreeUnmanaged(ref items);
            Pool.FreeUnmanaged(ref itemNames);
            Pool.FreeUnmanaged(ref itemBlueprints);

            return true;
        }

        private void UpdateInternals(bool doLog)
        {
            if (Changed)
            {
                SaveConfig();
                Changed = false;
            }

            if (doLog)
                Log("Updating internals ...");

            int populatedContainers = 0;
            int populatedNPCContainers = 0;
            int trackedNPCs = 0;

            NextTick(() =>
            {
                if (_config.Generic.removeStackedContainers)
                    FixLoot();

                foreach (var container in BaseNetworkable.serverEntities.Where(p => p is (not null) and LootContainer or NPCPlayerCorpse))
                {
                    // API Check
                    if (container is LootContainer lootContainer)
                    {
                        if (CustomLootSpawns is not null && CustomLootSpawns.Call<bool>("IsLootBox", container))
                            continue;
                        else if (PopulateContainer(lootContainer))
                            populatedContainers++;
                    } else if (container is NPCPlayerCorpse corpse)
                    {
                        if (PopulateContainer(corpse))
                            populatedNPCContainers++;
                    }
                }

                // NPC Implementation
                foreach (var npcContainer in BaseNetworkable.serverEntities.Where(n => n is (not null) and global::HumanNPC).Cast<global::HumanNPC>())
                {
                    if (npcLootMonitor.ContainsKey(npcContainer.name))
                        npcLootMonitor[npcContainer.name].Add(npcContainer.userID);
                    else
                        npcLootMonitor.Add(npcContainer.name, new List<ulong> { npcContainer.userID });

                    trackedNPCs++;
                }

                if (doLog)
                {
                    Log($"Populated ({populatedContainers}) supported loot containers.");
                    Log($"Populated ({populatedNPCContainers}) supported npc corpses.");
                    Log($"Tracking ({trackedNPCs}) spawned NPCs.");
                }
                    
                initialized = true;
                populatedContainers = 0;
                trackedNPCs = 0;
            });
        }
        
        private void FixLoot()
        {
            var spawns = Resources.FindObjectsOfTypeAll<LootContainer>()
                .Where(c => c.isActiveAndEnabled)
                .OrderBy(c => c.transform.position.x).ThenBy(c => c.transform.position.z)
                .ToList();
            
            var count = spawns.Count();
            var racelimit = count ^ 2;

            var antirace = 0;
            var deleted = 0;

            for (var i = 0; i < count; i++)
            {
                var box = spawns[i];
                var pos = new Vector2(box.transform.position.x, box.transform.position.z);

                if (++antirace > racelimit)
                    return;

                var next = i + 1;
                while (next < count)
                {
                    var box2 = spawns[next];
                    var pos2 = new Vector2(box2.transform.position.x, box2.transform.position.z);
                    var distance = Vector2.Distance(pos, pos2);

                    if (++antirace > racelimit)
                        return;

                    if (distance < 0.25f)
                    {
                        spawns.RemoveAt(next);
                        count--;

                        if (box2 is BaseEntity _box2 && !_box2.IsDestroyed)
                        {
                            _box2.KillMessage();
                            deleted++;
                        }
                    }
                    else
                        break;
                }
            }

            if (deleted > 0)
                Log($"Removed {deleted} stacked LootContainer");
            else
                Log($"No stacked LootContainer found.");
        }
        
        private Item? MightyRNG(string type, int itemCount, bool blockBPs = false)
        {
            bool asBP = rng.NextDouble() < _config.Generic.blueprintProbability && !blockBPs;
            List<string>? selectFrom = Pool.Get<List<string>>();
            Item? item;
            int maxRetry = 10 * itemCount;
            int limit = 0;
            string itemName;

            do
            {
                if (selectFrom.Any())
                {
                    Pool.FreeUnmanaged(ref selectFrom);
                    selectFrom = Pool.Get<List<string>>();
                }

                item = null;

                var _totalWeight = 0;
                List<int> _weightList = Pool.Get<List<int>>();
                var _prefabList = Pool.Get<List<List<string>>>();

                _totalWeight = asBP ? totalBlueprintWeight[type] : totalItemWeight[type];
                _weightList.AddRange(asBP ? blueprintWeights[type] : itemWeights[type]);
                _prefabList.AddRange(asBP ? Blueprints[type] : Items[type]);

                var r = rng.Next(_totalWeight);
                for (int i = 0; i < 5; ++i)
                {
                    limit += _weightList[i];
                    if (r < limit)
                    {
                        selectFrom.AddRange(_prefabList[i]);
                        break;
                    }
                }

                Pool.FreeUnmanaged(ref _weightList);
                Pool.FreeUnmanaged(ref _prefabList);

                if (!selectFrom.Any())
                {
                    if (--maxRetry <= 0)
                        break;

                    continue;
                }

                itemName = uniqueTagRegex.Replace(selectFrom[rng.Next(0, selectFrom.Count)], string.Empty);

                ItemDefinition itemDef = ItemManager.FindItemDefinition(itemName);
                if (asBP && itemDef.Blueprint != null && itemDef.Blueprint.isResearchable)
                {
                    var blueprintBaseDef = ItemManager.FindItemDefinition("blueprintbase");
                    item = ItemManager.Create(blueprintBaseDef, 1);
                    item.blueprintTarget = itemDef.itemid;
                }
                else
                    item = ItemManager.CreateByName(itemName, 1);

                if (item is null || item.info is null)
                    continue;
                break;
            } while (true);

            if (selectFrom is not null && selectFrom.Any())
                Pool.FreeUnmanaged(ref selectFrom);

            if (item is null)
                return null;
            
            if (lootTables.LootTables.TryGetValue(type, out PrefabLoot? entry) && entry.UngroupedItems.TryGetValue(item.info.shortname, out LootAmount amounts))
                item.amount = UnityEngine.Random.Range(Math.Min(amounts.Min, amounts.Max), Math.Max(amounts.Min, amounts.Max)) * _config.Loot.lootMultiplier;

            item.OnVirginSpawn();
            return item;
        }
        
        private bool ItemExists(string name) => 
            ItemManager.itemList.Any(x => x.shortname == name);
        
        // API
        private bool isSupplyDropActive()
        {
            if (!lootTables.LootTables.TryGetValue("assets/prefabs/misc/supply drop/supply_drop.prefab", out PrefabLoot? con) || con is null)
                return false;

            if (con.Enabled)
                return true;

            return false;
        }
        #endregion

        #region Looty API Commands
        // Add console command for this as well
        [ChatCommand("looty")]
        private void LootyConfigDownload(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, ADMIN_PERM))
            {
                SendMessage(player, BLLang("perm"));
                return;
            }

            if (args.Length != 1)
            {
                SendMessage(player, BLLang("lootycmdformat", player.UserIDString));
                return;
            }

            // Parse args
            string lootyId = args[0];

            if (!lootyIdRegex?.IsMatch(lootyId) ?? false)
            {
                SendMessage(player, BLLang("lootyinvalidid"));
                return;
            }

            GetLootyAPI(lootyId, player);
        }

        [ConsoleCommand("looty")]
        private void LootyConfigDownload_Console(Arg arg)
        {
            if (!arg.IsRcon)
            {
                arg.ReplyWith("Error: Should not execute command outside of RCON.");
                return;
            }

            if (arg.Args is not string[] args || args.Length != 1)
            {
                Puts(BLLang("lootycmdformat"));
                Puts("Please visit https://editor.looty.cc/betterloot to create your custom loot configuration!");
                return;
            }            

            // Parse Args
            string betterLootId = args[0];

            if (!lootyIdRegex?.IsMatch(betterLootId) ?? false)
            {
                Puts(BLLang("lootyinvalidid"));
                return;
            }

            // Send Request
            GetLootyAPI(betterLootId);
        }

        #region Processing Routine
        private void GetLootyAPI(string lootyId, BasePlayer? player = null)
        {
            // Compatibility between console and chat
            void Respond(string key)
            {
                string lang = BLLang(key);
                if (player is not null)
                    SendMessage(player, BLLang(key));
                else
                    Puts(BLLang(key));
            }

            IEnumerator SendRequest()
            {
                using (UnityWebRequest www = UnityWebRequest.Get($"https://editor.looty.cc/api/fetch-loot-table?id={lootyId}"))
                {
                    Respond($"Attempting to download configuration: {lootyId}");
                    yield return www.SendWebRequest();

                    if (www.result is UnityWebRequest.Result.ConnectionError or UnityWebRequest.Result.ProtocolError)
                    {
                        Puts($"Error: Could not download request: {www.result} ({www.responseCode})");

                        long code = www.responseCode;
                        if (www.result is UnityWebRequest.Result.ProtocolError && (code == 404 || code == 410))
                            Respond("lootynotfound");
                    }
                    else
                    {
                        bool restoreFailsafe = false;

                        try
                        {
                            LootTableData tableData = JsonConvert.DeserializeObject<LootTableData>(www.downloadHandler.text);

                            if (tableData.IsUnityNull())
                            {
                                Respond("Error: Failed to load data. Aborting...");
                                yield break;
                            }

                            lootTables = tableData;

                            DataSystem.BakDataFile("LootTables");
                            restoreFailsafe = true; // Failsafe flag. Restore on error / fail

                            DataSystem.SaveLootTables();

                            Respond("Loaded new file successfully");
                        }
                        catch (Exception error)
                        {
                            Respond("Error loading requested LootTable.");

                            if (restoreFailsafe)
                            {
                                Respond("Restoring backup file.");
                                DataSystem.BakDataFile("LootTables", true);
                            }

                            Puts($"Please forward this message to the developer. {error}");
                        }
                    }
                }
            }

            ServerMgr.Instance.StartCoroutine(SendRequest());
        }
        #endregion
        #endregion

        #region Commands
        #region Backup / Restore
        [ChatCommand("bl-backup")]
        private void ManualBackupCommand(BasePlayer player)
            => ManualBackupRestore(player, false);

        [ChatCommand("bl-restore")]
        private void RestoreBackupCommand(BasePlayer player)
            => ManualBackupRestore(player, true);

        private void ManualBackupRestore(BasePlayer player, bool restore)
        {
            if (!permission.UserHasPermission(player.UserIDString, ADMIN_PERM))
            {
                SendMessage(player, BLLang("perm"));
                return;
            }

            DataSystem.BakDataFile("LootTables", restore, player);
        }

        [ConsoleCommand("bl-backup")]
        private void ManualBackupCommand_Console(Arg arg)
            => ManualBackupRestore_Console(arg, false);

        [ConsoleCommand("bl-restore")]
        private void RestoreBackupCommand_Console(Arg arg)
            => ManualBackupRestore_Console(arg, true);

        private void ManualBackupRestore_Console(Arg arg, bool restore)
        {
            if (!arg.IsRcon)
                return;

            DataSystem.BakDataFile("LootTables", restore);
        }
        #endregion

        [ChatCommand("blacklist")]
        private void CmdChatBlacklistNew(BasePlayer player, string command, string[] args)
        {
            if (!initialized)
            {
                SendMessage(player, BLLang("initialized")); 
                return;
            }

            if (!permission.UserHasPermission(player.UserIDString, ADMIN_PERM))
            {
                SendMessage(player, BLLang("perm")); 
                return;
            }

            if (args.Length is 0)
            {
                if (storedBlacklist.ItemList.Count is 0)
                    SendMessage(player, BLLang("none"));
                else
                {
                    string _BLItems = string.Join(", ", storedBlacklist.ItemList);
                    SendMessage(player, BLLang("blocked", player.UserIDString, _BLItems));
                }

                return;
            }

            switch (args[0].ToLower())
            {
                case "additem":
                    if (!ItemExists(args[1]))
                    {
                        SendMessage(player, BLLang("notvalid", player.UserIDString, args[1]));
                        return;
                    }

                    if (!storedBlacklist.ItemList.Contains(args[1]))
                    {
                        storedBlacklist.ItemList.Add(args[1]);
                        UpdateInternals(false);
                        SendMessage(player, BLLang("blockedpass", player.UserIDString, args[1]));
                        DataSystem.SaveBlacklist();
                        return;
                    }

                    SendMessage(player, BLLang("blockedtrue", player.UserIDString, args[1]));
                    break;
                case "deleteitem":
                    if (!ItemExists(args[1]))
                    {
                        SendMessage(player, BLLang("notvalid", player.UserIDString, args[1]));
                        return;
                    }

                    if (storedBlacklist.ItemList.Contains(args[1]))
                    {
                        storedBlacklist.ItemList.Remove(args[1]);
                        UpdateInternals(false);
                        SendMessage(player, BLLang("unblacklisted", player.UserIDString, args[1]));
                        DataSystem.SaveBlacklist();
                        return;
                    }

                    SendMessage(player, BLLang("blockedfalse", player.UserIDString, args[1]));
                    break;
                default:
                    SendMessage(player, BLLang("syntax"));
                    break;
            }
        }
        #endregion

        #region Hammer loot cycle
        private void OnMeleeAttack(BasePlayer player, HitInfo c)
        {
            if (!_config.Loot.enableHammerLootCycle || player is null || c is null || player.GetActiveItem() is not Item item || item.hasCondition || !player.IsAdmin || !item.ToString().Contains("hammer")) 
                return;

            BaseEntity entity = c.HitEntity;
            ItemContainer container = null;

            string panelName = string.Empty;
            bool isNpc = false;

            if (entity is NPCPlayerCorpse npc)
            {
                container = npc.containers[0];
                panelName = npc.lootPanelName;
                isNpc = true;
            }
            else if (entity.GetComponent<LootContainer>() is StorageContainer _inv)
            {
                container = _inv.inventory;
                panelName = _inv.panelName;
            }   
            else
                return;
                
            HammerHitLootCycle lootCycle = entity.gameObject.AddComponent<HammerHitLootCycle>();

            lootCycle.isNpc = isNpc;
            player.inventory.loot.StartLootingEntity(entity, false);
            player.inventory.loot.AddContainer(container);
            player.inventory.loot.SendImmediate();
            player.ClientRPC(RpcTarget.Player("RPC_OpenLootPanel", player), panelName);
        }

        private class HammerHitLootCycle : FacepunchBehaviour
        {
            public bool isNpc = false;

            private void Awake()
                => InvokeRepeating(Repeater, 0, (float)_config.Loot.hammerLootCycleTime);

            private void Repeater()
            {
                if (!enabled) 
                    return;

                if (isNpc)
                {
                    NPCPlayerCorpse corpse = GetComponent<NPCPlayerCorpse>();
                    _instance.PopulateContainer(corpse);
                }
                else
                {
                    LootContainer loot = GetComponent<LootContainer>();
                    _instance.PopulateContainer(loot);
                }
            }

            private void PlayerStoppedLooting(BasePlayer _)
            {
                CancelInvoke(Repeater);
                Destroy(this);
            }
        }
        #endregion
    }
}

namespace Oxide.Plugins.BetterLootExtenstions
{
    public static class BetterLootExtenstions
    {
        public static bool ContainsPartial(this List<string> list, string partialString)
            => list.Any(partialString.Contains);
        public static bool IsDefault<T>(this T obj)
            => EqualityComparer<T>.Default.Equals(obj, default);
        public static int RemoveAll<TKey, TValue>(this IDictionary<TKey, TValue> dict, Func<TKey, bool> predicate)
        {
            int removeCount = 0;
            var keys = dict.Keys.Where(k => predicate(k)).ToList();
            foreach (var key in keys)
                if (dict.Remove(key))
                    removeCount++;
            return removeCount;
        }
    }
}