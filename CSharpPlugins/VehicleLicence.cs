// #define DEBUG

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Facepunch;
using Network;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust;
using Rust;
using Rust.Modular;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

// TODO: Fix mincopters spawning above user.

namespace Oxide.Plugins
{
    [Info("Vehicle Licence", "Sorrow/TheDoc/Arainrr", "1.8.8")]
    [Description("Allows players to buy vehicles and then spawn or store it")]
    public class VehicleLicence : RustPlugin
    {
        #region Fields

        [PluginReference]
        private readonly Plugin Economics, ServerRewards, Friends, Clans, NoEscape, LandOnCargoShip, RustTranslationAPI, ZoneManager;

        private readonly string PERMISSION_USE = "vehiclelicence.use";
        private readonly string PERMISSION_ALL = "vehiclelicence.all";
        private readonly string PERMISSION_ADMIN = "vehiclelicence.admin";

        private readonly string PERMISSION_BYPASS_COST = "vehiclelicence.bypasscost";
        private readonly string PERMISSION_NO_DAMAGE = "vehiclelicence.nodamage";
        private readonly string PERMISSION_NO_COLLISION_DAMAGE = "vehiclelicence.nocollisiondamage";

        private const int ITEMID_FUEL = -946369541;
        private const int ITEMID_HOTAIRBALLOON_ARMOR = -1989600732;
        private const string PREFAB_ITEM_DROP = "assets/prefabs/misc/item drop/item_drop.prefab";

        //BIKES
        private const string PREFAB_PEDALBIKE = "assets/content/vehicles/bikes/pedalbike.prefab";
        private const string PREFAB_PEDALTRIKE = "assets/content/vehicles/bikes/pedaltrike.prefab";
        private const string PREFAB_MOTORBIKE = "assets/content/vehicles/bikes/motorbike.prefab";
        private const string PREFAB_MOTORBIKE_SIDECAR = "assets/content/vehicles/bikes/motorbike_sidecar.prefab";

        //SPECIAL
        private const string PREFAB_ATV = "assets/custom/atv.prefab";
        private const string PREFAB_SOFA = "assets/custom/racesofa.prefab";
        private const string PREFAB_WATERBIRD = "assets/custom/waterheli.prefab";
        private const string PREFAB_WARBIRD = "assets/custom/warbird.prefab";
        private const string PREFAB_LITTLEBIRD = "assets/custom/littlebird.prefab";
        private const string PREFAB_FIGHTER = "assets/custom/fighter.prefab";
        private const string PREFAB_OLDFIGHTER = "assets/custom/oldfighter.prefab";
        private const string PREFAB_FIGHTERBUS = "assets/custom/fighterbus.prefab";
        private const string PREFAB_WARBUS = "assets/custom/warbus.prefab";
        private const string PREFAB_AIRBUS = "assets/custom/airbus.prefab";
        private const string PREFAB_PATROLHELI = "assets/custom/patrolheli.prefab";
        private const string PREFAB_RUSTWING = "assets/custom/rustwing.prefab";
        private const string PREFAB_RUSTWINGDETAILED = "assets/custom/rustwing_detailed.prefab";
        private const string PREFAB_RUSTWINGDETAILEDOLD = "assets/custom/rustwing_detailed_old.prefab";
        private const string PREFAB_TINFIGHTER = "assets/custom/tinfighter.prefab";
        private const string PREFAB_TINFIGHTERDETAILED = "assets/custom/tinfighter_detailed.prefab";
        private const string PREFAB_TINFIGHTERDETAILEDOLD = "assets/custom/tinfighter_detailed_old.prefab";
        private const string PREFAB_MARSFIGHTER = "assets/custom/marsfighter.prefab";
        private const string PREFAB_MARSFIGHTERDETAILED = "assets/custom/marsfighter_detailed.prefab";
        private const string PREFAB_SKYPLANE = "assets/custom/skyplane.prefab";
        private const string PREFAB_SKYBOAT = "assets/custom/skyboat.prefab";
        private const string PREFAB_TWISTEDTRUCK = "assets/custom/twistedtruck.prefab";
        private const string PREFAB_TRIANWRECK = "assets/custom/trainwreck.prefab";
        private const string PREFAB_TRIANWRECKER = "assets/custom/trainwrecker.prefab";
        private const string PREFAB_SANTA = "assets/custom/santa.prefab";
        private const string PREFAB_WARSANTA = "assets/custom/warsanta.prefab";
        private const string PREFAB_WITCH = "assets/custom/witch.prefab";
        private const string PREFAB_MAGICCARPET = "assets/custom/magiccarpet.prefab";
        private const string PREFAB_AH69T = "assets/custom/ah69t.prefab";
        private const string PREFAB_AH69R = "assets/custom/ah69r.prefab";
        private const string PREFAB_AH69A = "assets/custom/ah69a.prefab";
        private const string PREFAB_MAVIK = "assets/custom/mavik.prefab";
        private const string PREFAB_HEAVYFIGHTER = "assets/custom/heavyfighter.prefab";
        private const string PREFAB_PORCELAINCOMMANDER = "assets/custom/porcelaincommander.prefab";
        private const string PREFAB_DUNEBUGGIE = "assets/custom/dunebuggie.prefab";
        private const string PREFAB_DUNETRUCKARMED = "assets/custom/dunetruckarmed.prefab";
        private const string PREFAB_DUNETRUCKUNARMED = "assets/custom/dunetruckunarmed.prefab";
        private const string PREFAB_DOOMSDAYDISCOVAN = "assets/custom/doomsdaydiscovan.prefab";
        private const string PREFAB_FORKLIFT = "assets/custom/forklift.prefab";
        private const string PREFAB_LAWNMOWER = "assets/custom/lawnmower.prefab";
        private const string PREFAB_CHARIOT = "assets/custom/chariot.prefab";
        private const string PREFAB_SOULHARVESTER = "assets/custom/soulharvester.prefab";

        //OTHER
        private const string PREFAB_KAYAK = "assets/content/vehicles/boats/kayak/kayak.prefab";
        private const string PREFAB_TUGBOAT = "assets/content/vehicles/boats/tugboat/tugboat.prefab";
        private const string PREFAB_ROWBOAT = "assets/content/vehicles/boats/rowboat/rowboat.prefab";
        private const string PREFAB_RHIB = "assets/content/vehicles/boats/rhib/rhib.prefab";
        private const string PREFAB_SEDAN = "assets/content/vehicles/sedan_a/sedantest.entity.prefab";
        private const string PREFAB_HOTAIRBALLOON = "assets/prefabs/deployable/hot air balloon/hotairballoon.prefab";
        private const string PREFAB_MINICOPTER = "assets/content/vehicles/minicopter/minicopter.entity.prefab";
        private const string PREFAB_ATTACKHELICOPTER = "assets/content/vehicles/attackhelicopter/attackhelicopter.entity.prefab";
        private const string PREFAB_TRANSPORTCOPTER = "assets/content/vehicles/scrap heli carrier/scraptransporthelicopter.prefab";
        private const string PREFAB_CHINOOK = "assets/prefabs/npc/ch47/ch47.entity.prefab";
        private const string PREFAB_RIDABLEHORSE = "assets/content/vehicles/horse/ridablehorse.prefab";
        private const string PREFAB_WORKCART = "assets/content/vehicles/trains/workcart/workcart.entity.prefab";
        private const string PREFAB_SEDANRAIL = "assets/content/vehicles/sedan_a/sedanrail.entity.prefab";
        private const string PREFAB_MAGNET_CRANE = "assets/content/vehicles/crane_magnet/magnetcrane.entity.prefab";
        private const string PREFAB_SUBMARINE_DUO = "assets/content/vehicles/submarine/submarineduo.entity.prefab";
        private const string PREFAB_SUBMARINE_SOLO = "assets/content/vehicles/submarine/submarinesolo.entity.prefab";

        private const string PREFAB_CHASSIS_SMALL = "assets/content/vehicles/modularcar/car_chassis_2module.entity.prefab";
        private const string PREFAB_CHASSIS_MEDIUM = "assets/content/vehicles/modularcar/car_chassis_3module.entity.prefab";
        private const string PREFAB_CHASSIS_LARGE = "assets/content/vehicles/modularcar/car_chassis_4module.entity.prefab";

        private const string PREFAB_SNOWMOBILE = "assets/content/vehicles/snowmobiles/snowmobile.prefab";
        private const string PREFAB_SNOWMOBILE_TOMAHA = "assets/content/vehicles/snowmobiles/tomahasnowmobile.prefab";

        // Train Engine
        private const string PREFAB_TRAINENGINE = "assets/content/vehicles/trains/workcart/workcart_aboveground.entity.prefab";
        private const string PREFAB_TRAINENGINE_COVERED = "assets/content/vehicles/trains/workcart/workcart_aboveground2.entity.prefab";
        private const string PREFAB_TRAINENGINE_LOCOMOTIVE = "assets/content/vehicles/trains/locomotive/locomotive.entity.prefab";

        // Train Car
        private const string PREFAB_TRAINWAGON_A = "assets/content/vehicles/trains/wagons/trainwagona.entity.prefab";
        private const string PREFAB_TRAINWAGON_B = "assets/content/vehicles/trains/wagons/trainwagonb.entity.prefab";
        private const string PREFAB_TRAINWAGON_C = "assets/content/vehicles/trains/wagons/trainwagonc.entity.prefab";
        private const string PREFAB_TRAINWAGON_UNLOADABLE = "assets/content/vehicles/trains/wagons/trainwagonunloadable.entity.prefab";
        private const string PREFAB_TRAINWAGON_UNLOADABLE_FUEL = "assets/content/vehicles/trains/wagons/trainwagonunloadablefuel.entity.prefab";
        private const string PREFAB_TRAINWAGON_UNLOADABLE_LOOT = "assets/content/vehicles/trains/wagons/trainwagonunloadableloot.entity.prefab";
        private const string PREFAB_CABOOSE = "assets/content/vehicles/trains/caboose/traincaboose.entity.prefab";

        // Defaults for Vehicle Modifications
        private readonly float TUGBOAT_ENGINETHRUST = 200000f;
        private readonly float HELICOPTER_LIFT = 0.25f;
        private readonly Vector3 SCRAP_HELICOPTER_TORQUE = new Vector3(8000.0f, 8000.0f, 4000.0f);
        private readonly Vector3 MINICOPTER_TORQUE = new Vector3(400.0f, 400.0f, 200.0f);
        private readonly Vector3 ATTACK_HELICOPTER_TORQUE = new Vector3(8000.0f, 8000.0f, 5200.0f);

        private const int LAYER_GROUND = Layers.Solid | Layers.Mask.Water;

        private readonly object _false = false;
        private bool finishedLoading = false;

        public static VehicleLicence Instance { get; private set; }

        public readonly Dictionary<BaseEntity, Vehicle> vehiclesCache = new Dictionary<BaseEntity, Vehicle>();
        public readonly Dictionary<string, BaseVehicleSettings> allVehicleSettings = new Dictionary<string, BaseVehicleSettings>();
        public readonly Dictionary<string, string> commandToVehicleType = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        
        public enum NormalVehicleType
        {
            Tugboat,
            Rowboat,
            RHIB,
            Sedan,
            HotAirBalloon,
            ArmoredHotAirBalloon,
            MiniCopter,
            AttackHelicopter,
            TransportHelicopter,
            Chinook,
            RidableHorse,
            WorkCart,
            SedanRail,
            MagnetCrane,
            SubmarineSolo,
            SubmarineDuo,
            Snowmobile,
            TomahaSnowmobile,
            Kayak,
            PedalBike,
            PedalTrike,
            MotorBike,
            MotorBike_SideCar,
        }

        public enum CustomVehicleType
        {
            ATV,
            RaceSofa,
            WaterBird,
            WarBird,
            LittleBird,
            Fighter,
            OldFighter,
            FighterBus,
            WarBus,
            AirBus,
            PatrolHeli,
            RustWing,
            RustWingDetailed,
            RustWingDetailedOld,
            TinFighter,
            TinFighterDetailed,
            TinFighterDetailedOld,
            MarsFighter,
            MarsFighterDetailed,
            SkyPlane,
            SkyBoat,
            TwistedTruck,
            TrainWreck,
            TrainWrecker,
            Santa,
            WarSanta,
            Witch,
            MagicCarpet,
            Ah69t,
            Ah69r,
            Ah69a,
            Mavik,
            HeavyFighter,
            PorcelainCommander,
            DuneBuggie,
            DuneTruckArmed,
            DuneTruckUnArmed,
            DoomsDayDiscoVan,
            ForkLift,
            LawnMower,
            Chariot,
            SoulHarvester
        }

        [JsonConverter(typeof(StringEnumConverter))]
        public enum ChassisType
        {
            Small,
            Medium,
            Large
        }

        [JsonConverter(typeof(StringEnumConverter))]
        public enum TrainComponentType
        {
            Engine,
            CoveredEngine,
            Locomotive,
            WagonA,
            WagonB,
            WagonC,
            Unloadable,
            UnloadableLoot,
            UnloadableFuel,
            Caboose,
        }

        #endregion Fields

        #region Oxide Hooks

        private void Init()
        {
            LoadData();
            Instance = this;
            permission.RegisterPermission(PERMISSION_USE, this);
            permission.RegisterPermission(PERMISSION_ALL, this);
            permission.RegisterPermission(PERMISSION_ADMIN, this);
            permission.RegisterPermission(PERMISSION_BYPASS_COST, this);
            permission.RegisterPermission(PERMISSION_NO_DAMAGE, this);
            permission.RegisterPermission(PERMISSION_NO_COLLISION_DAMAGE, this);

            bool useCustomVehicles = configData.global.useCustomVehicles;
            if(useCustomVehicles && configData.customVehicles == null)
            {
                configData.customVehicles = new CustomVehicleSettings();
                SaveConfig();
            }
            
            if(useCustomVehicles)
            {
                foreach (NormalVehicleType value in Enum.GetValues(typeof(NormalVehicleType)))
                {
                    allVehicleSettings.Add(value.ToString(), GetBaseVehicleSettings(value));
                }
                
                foreach (CustomVehicleType value in Enum.GetValues(typeof(CustomVehicleType)))
                {
                    allVehicleSettings.Add(value.ToString(), GetCustomVehicleSettings(value));
                }
            }
            else
            {
                foreach (NormalVehicleType value in Enum.GetValues(typeof(NormalVehicleType)))
                {
                    allVehicleSettings.Add(value.ToString(), GetBaseVehicleSettings(value));
                }
            }
            
            foreach (var entry in configData.modularVehicles)
            {
                allVehicleSettings.Add(entry.Key, entry.Value);
            }
            foreach (var entry in configData.trainVehicles)
            {
                allVehicleSettings.Add(entry.Key, entry.Value);
            }
            foreach (var entry in allVehicleSettings)
            {
                BaseVehicleSettings settings = entry.Value;
                
                if (settings.UsePermission && !string.IsNullOrEmpty(settings.Permission))
                {
                    if (!permission.PermissionExists(settings.Permission, this))
                    {
                        permission.RegisterPermission(settings.Permission, this);
                    }
                }

                if (settings.UsePermission && !string.IsNullOrEmpty(settings.BypassCostPermission))
                {
                    if (!permission.PermissionExists(settings.BypassCostPermission, this))
                    {
                        permission.RegisterPermission(settings.BypassCostPermission, this);
                    }
                }

                foreach (var perm in settings.CooldownPermissions.Keys)
                {
                    if (!permission.PermissionExists(perm, this))
                    {
                        permission.RegisterPermission(perm, this);
                    }
                }

                foreach (var command in settings.Commands)
                {
                    if (string.IsNullOrEmpty(command))
                    {
                        continue;
                    }
                    if (!commandToVehicleType.ContainsKey(command))
                    {
                        commandToVehicleType.Add(command, entry.Key);
                    }
                    else
                    {
                        PrintError($"You have the same two commands({command}).");
                    }
                    if (configData.chat.useUniversalCommand)
                    {
                        cmd.AddChatCommand(command, this, nameof(CmdUniversal));
                    }
                    if (!string.IsNullOrEmpty(configData.chat.customKillCommandPrefix))
                    {
                        cmd.AddChatCommand(configData.chat.customKillCommandPrefix + command, this, nameof(CmdCustomKill));
                    }
                }
            }

            cmd.AddChatCommand(configData.chat.helpCommand, this, nameof(CmdLicenseHelp));
            cmd.AddChatCommand(configData.chat.buyCommand, this, nameof(CmdBuyVehicle));
            cmd.AddChatCommand(configData.chat.spawnCommand, this, nameof(CmdSpawnVehicle));
            cmd.AddChatCommand(configData.chat.recallCommand, this, nameof(CmdRecallVehicle));
            cmd.AddChatCommand(configData.chat.killCommand, this, nameof(CmdKillVehicle));

            Unsubscribe(nameof(CanMountEntity));
            Unsubscribe(nameof(OnEntityTakeDamage));
            Unsubscribe(nameof(OnEntityDismounted));
            Unsubscribe(nameof(OnEntityEnter));
            Unsubscribe(nameof(CanLootEntity));
            Unsubscribe(nameof(OnEntitySpawned));
            Unsubscribe(nameof(OnRidableAnimalClaimed));
            Unsubscribe(nameof(OnEngineStarted));
            Unsubscribe(nameof(OnVehiclePush));
        }

        private void OnServerInitialized()
        {
            ServerMgr.Instance.StartCoroutine(UpdatePlayerData(TimeEx.currentTimestamp));
            if (configData.global.preventMounting)
            {
                Subscribe(nameof(CanMountEntity));
            }
            if (configData.global.noDecay)
            {
                Subscribe(nameof(OnEntityTakeDamage));
            }
            if (configData.global.preventDamagePlayer || configData.global.safeTrainDismount || configData.global.preventDamageNPCs)
            {
                Subscribe(nameof(OnEntityEnter));
            }
            if (configData.global.preventLooting)
            {
                Subscribe(nameof(CanLootEntity));
            }
            if (configData.global.autoClaimFromVendor)
            {
                Subscribe(nameof(OnEntitySpawned));
                Subscribe(nameof(OnRidableAnimalClaimed));
            }
            if (configData.global.checkVehiclesInterval > 0 && allVehicleSettings.Any(x => x.Value.WipeTime > 0))
            {
                Subscribe(nameof(OnEntityDismounted));
                timer.Every(configData.global.checkVehiclesInterval, CheckVehicles);
            }
            else if (configData.normalVehicles.miniCopter.flyHackPause > 0 || configData.normalVehicles.transportHelicopter.flyHackPause > 0 || configData.normalVehicles.attackHelicopter.flyHackPause > 0)
            {
                Subscribe(nameof(OnEntityDismounted));
            }
            if (configData.normalVehicles.miniCopter.instantTakeoff || configData.normalVehicles.attackHelicopter.instantTakeoff
                 || configData.normalVehicles.transportHelicopter.instantTakeoff)
            {
                Subscribe(nameof(OnEngineStarted));
            }
            if (configData.global.preventPushing)
            {
                Subscribe(nameof(OnVehiclePush));
            }
        }

        private void Unload()
        {
            if (!configData.global.storeVehicle)
            {
                foreach (var entry in vehiclesCache)
                {
                    if (entry.Key != null && !entry.Key.IsDestroyed)
                    {
                        RefundVehicleItems(entry.Value, isUnload: true);
                        entry.Key.Kill(BaseNetworkable.DestroyMode.Gib);
                    }
                    entry.Value.EntityId = 0;
                }
            }
            SaveData();
            Instance = null;
        }

        private void OnServerSave()
        {
            timer.Once(Random.Range(0f, 60f), SaveData);
        }

        private void OnEntityDismounted(BaseMountable entity, BasePlayer player)
        {
            if (entity == null)
            {
                return;
            }
            if (player != null)
            {
                BaseEntity vehicleEntity = entity.GetParentEntity();
                if (configData.normalVehicles.miniCopter.flyHackPause > 0 && vehicleEntity is Minicopter)
                {
                    player.PauseFlyHackDetection(configData.normalVehicles.miniCopter.flyHackPause);
                }
                else if (configData.normalVehicles.transportHelicopter.flyHackPause > 0 && vehicleEntity is ScrapTransportHelicopter)
                {
                    player.PauseFlyHackDetection(configData.normalVehicles.transportHelicopter.flyHackPause);
                }
                else if (configData.normalVehicles.attackHelicopter.flyHackPause > 0 && vehicleEntity is AttackHelicopter)
                {
                    player.PauseFlyHackDetection(configData.normalVehicles.attackHelicopter.flyHackPause);
                }
            }
            var vehicleParent = entity.VehicleParent();
            if (vehicleParent == null || vehicleParent.IsDestroyed)
            {
                return;
            }
            Vehicle vehicle;
            if (!vehiclesCache.TryGetValue(vehicleParent, out vehicle))
            {
                return;
            }
            vehicle.OnDismount();
        }

        // TODO: Fix/finish
        private void OnEngineStarted(BaseMountable entity, BasePlayer player)
        {
            if (player == null || entity == null) return;

            if (!permission.UserHasPermission(player.UserIDString, PERMISSION_USE)) return;
            BaseVehicle mounted = player.GetMountedVehicle();
            // Only allows vehicles spawned with the plugin to use instant take off.
            if (mounted == null || !vehiclesCache.ContainsKey(mounted)) return;

            PlayerHelicopter heli = mounted as PlayerHelicopter;

            NextTick(() =>
            {
                if (heli == null) return;

                if (configData.normalVehicles.miniCopter.instantTakeoff && heli is Minicopter)
                {
                    heli.engineController.FinishStartingEngine();
                    return;
                }

                if (configData.normalVehicles.attackHelicopter.instantTakeoff && heli is AttackHelicopter)
                {
                    heli.engineController.FinishStartingEngine();
                }

                if (configData.normalVehicles.transportHelicopter.instantTakeoff && heli is ScrapTransportHelicopter)
                {
                    heli.engineController.FinishStartingEngine();
                }
            });
        }

        private object OnVehiclePush(BaseVehicle vehicle, BasePlayer player)
        {
            if (vehicle == null || player == null) return null;
            if (!vehiclesCache.TryGetValue(vehicle, out Vehicle foundVehicle)) return null;
            // ulong userID = player.userID.Get();
            //
            // if (foundVehicle.PlayerId == userID || AreFriends(foundVehicle.PlayerId, player.userID)) return null;
            // if (HasAdminPermission(player)) return null;
            
            // Respond here
            SendCantPushMessage(player, foundVehicle);
            return true;
        }

        #region Mount

        private object CanMountEntity(BasePlayer friend, BaseMountable entity)
        {
            if (friend == null || entity == null)
            {
                return null;
            }
            var vehicleParent = entity.VehicleParent();
            if (vehicleParent == null || vehicleParent.IsDestroyed)
            {
                return null;
            }
            Vehicle vehicle;
            if (!vehiclesCache.TryGetValue(vehicleParent, out vehicle))
            {
                return null;
            }
            if (AreFriends(vehicle.PlayerId, friend.userID))
            {
                return null;
            }
            if (configData.global.preventDriverSeat && vehicleParent.HasMountPoints())
            {
                foreach (var mountPointInfo in vehicleParent.allMountPoints)
                {
                    if (mountPointInfo == null || mountPointInfo.mountable != entity) continue;
                    if (!mountPointInfo.isDriver)
                    {
                        return null;
                    }
                    break;
                }
            }
            if (HasAdminPermission(friend))
            {
                return null;
            }
            SendCantUseMessage(friend, vehicle);
            return _false;
        }

        #endregion Mount

        #region Loot

        private object CanLootEntity(BasePlayer friend, RidableHorse horse)
        {
            if (friend == null || horse == null)
            {
                return null;
            }
            return CanLootEntityInternal(friend, horse);
        }

        private object CanLootEntity(BasePlayer friend, StorageContainer container)
        {
            if (friend == null || container == null) return null;

            var parentEntity = container.GetParentEntity();

            if (parentEntity == null) return null;

            return CanLootEntityInternal(friend, parentEntity);
        }

        private object CanLootEntityInternal(BasePlayer friend, BaseEntity parentEntity)
        {
            Vehicle vehicle;
            if (!TryGetVehicle(parentEntity, out vehicle))
            {
                return null;
            }

            if (AreFriends(vehicle.PlayerId, friend.userID)) return null;

            if (HasAdminPermission(friend)) return null;

            SendCantUseMessage(friend, vehicle);
            return _false;
        }

        #endregion Loot

        #region Decay

        private void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo hitInfo)
        {
            if (entity == null || hitInfo?.damageTypes == null)
            {
                return;
            }
            Vehicle vehicle;
            if (!TryGetVehicle(entity, out vehicle))
            {
                return;
            }
            if (permission.UserHasPermission(vehicle.PlayerId.ToString(), PERMISSION_NO_DAMAGE) && GetBaseVehicleDamage(vehicle.VehicleType))
            {
                hitInfo.damageTypes.ScaleAll(0);
                return;
            }
            if (hitInfo.damageTypes.Has(DamageType.Collision) && permission.UserHasPermission(vehicle.PlayerId.ToString(), PERMISSION_NO_COLLISION_DAMAGE) && GetBaseVehicleCollisionDamage(vehicle.VehicleType))
            {
                hitInfo.damageTypes.Scale(DamageType.Collision, 0);
                return;
            }
            if (!hitInfo.damageTypes.Has(DamageType.Decay)) return;
            hitInfo.damageTypes.Scale(DamageType.Decay, 0);
        }

        #endregion Decay

        #region Claim

        private void OnEntitySpawned(Tugboat tugboat)
        {
            TryClaimVehicle(tugboat);
        }

        private void OnEntitySpawned(BaseSubmarine baseSubmarine)
        {
            TryClaimVehicle(baseSubmarine);
        }

        private void OnEntitySpawned(MotorRowboat motorRowboat)
        {
            TryClaimVehicle(motorRowboat);
        }

        private void OnEntitySpawned(Minicopter miniCopter)
        {
            TryClaimVehicle(miniCopter);
        }

        private void OnEntitySpawned(AttackHelicopter attackHelicopter)
        {
            TryClaimVehicle(attackHelicopter);
        }

        private void OnRidableAnimalClaimed(RidableHorse ridableAnimal, BasePlayer player)
        {
            TryClaimVehicle(ridableAnimal, player);
        }

        #endregion Claim

        #region Damage

        // ScrapTransportHelicopter / ModularCar / TrainEngine / MagnetCrane
        private object OnEntityEnter(TriggerHurtNotChild triggerHurtNotChild, BasePlayer player)
        {
            if (triggerHurtNotChild == null || player == null || triggerHurtNotChild.SourceEntity == null)
            {
                return null;
            }
            var sourceEntity = triggerHurtNotChild.SourceEntity;

            if (!vehiclesCache.ContainsKey(sourceEntity) || (!configData.global.preventDamageNPCs && !player.userID.IsSteamId())) return null;

            var baseVehicle = sourceEntity as BaseVehicle;

            if ((baseVehicle == null || player.userID.IsSteamId()) && configData.global.preventDamagePlayer) return _false;

            if (configData.global.preventDamageNPCs && !player.userID.IsSteamId()) return _false;

            if (baseVehicle is TrainEngine)
            {
                if (!configData.global.safeTrainDismount && configData.global.preventDamagePlayer && player.userID.IsSteamId()) return _false;

                if (!configData.global.safeTrainDismount) return null;

                var transform = triggerHurtNotChild.transform;
                MoveToPosition(player, transform.position + (Random.value >= 0.5f ? -transform.right : transform.right) * 2.5f);

                return configData.global.preventDamagePlayer ? _false : null;
            }

            if (!configData.global.preventDamagePlayer) return null;

            Vector3 pos;
            if (GetDismountPosition(baseVehicle, player, out pos))
            {
                MoveToPosition(player, pos);
            }
            //triggerHurtNotChild.enabled = false;
            return _false;
        }

        // HotAirBalloon
        private object OnEntityEnter(TriggerHurt triggerHurt, BasePlayer player)
        {
            if (triggerHurt == null || player == null)
            {
                return null;
            }
            var sourceEntity = triggerHurt.gameObject.ToBaseEntity();
            if (sourceEntity == null || !vehiclesCache.ContainsKey(sourceEntity)) return null;

            if (configData.global.preventDamagePlayer && player.userID.IsSteamId()
                || (configData.global.preventDamageNPCs && !player.userID.IsSteamId()))
            {
                MoveToPosition(player, sourceEntity.CenterPoint() + Vector3.down);
                //triggerHurt.enabled = false;
                return _false;
            }
            return null;
        }

        #endregion Damage

        #region Destroy

        private void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            OnEntityDeathOrKill(entity, true);
        }

        private void OnEntityKill(BaseCombatEntity entity)
        {
            OnEntityDeathOrKill(entity);
        }

        #endregion Destroy

        #region Reskin

        private object OnEntityReskin(BaseEntity entity, ItemSkinDirectory.Skin skin, BasePlayer player)
        {
            if (entity == null || player == null)
            {
                return null;
            }
            Vehicle vehicle;
            if (TryGetVehicle(entity, out vehicle))
            {
                return _false;
            }
            return null;
        }

        #endregion Reskin

        #endregion Oxide Hooks

        #region Methods

        #region Message

        private void SendCantUseMessage(BasePlayer friend, Vehicle vehicle)
        {
            var settings = GetBaseVehicleSettings(vehicle.VehicleType);
            if (settings != null)
            {
                var player = RustCore.FindPlayerById(vehicle.PlayerId);
                var playerName = player?.displayName ?? ServerMgr.Instance.persistance.GetPlayerName(vehicle.PlayerId) ?? "Unknown";
                Print(friend, Lang("CantUse", friend.UserIDString, settings.DisplayName, $"<color=#{(player != null && player.IsConnected ? "69D214" : "FF6347")}>{playerName}</color>"));
            }
        }

        private void SendCantPushMessage(BasePlayer friend, Vehicle vehicle)
        {
            var settings = GetBaseVehicleSettings(vehicle.VehicleType);
            if (settings == null) return;
            
            var player = RustCore.FindPlayerById(vehicle.PlayerId);
            var playerName = player?.displayName ?? ServerMgr.Instance.persistance.GetPlayerName(vehicle.PlayerId) ?? "Unknown";
            Print(friend, Lang("CantPush", friend.UserIDString, settings.DisplayName, $"<color=#{(player != null && player.IsConnected ? "69D214" : "FF6347")}>{playerName}</color>"));
        }

        #endregion Message

        #region CheckEntity

        private void OnEntityDeathOrKill(BaseCombatEntity entity, bool isCrash = false)
        {
            if (entity == null)
            {
                return;
            }
            Vehicle vehicle;
            if (!vehiclesCache.TryGetValue(entity, out vehicle))
            {
                return;
            }

            RefundVehicleItems(vehicle, isCrash);

            var settings = GetBaseVehicleSettings(vehicle.VehicleType);
            if (isCrash && settings.RemoveLicenseOnceCrash)
            {
                RemoveVehicleLicense(vehicle.PlayerId, vehicle.VehicleType);
            }

            vehicle.OnDeath();
            vehiclesCache.Remove(entity);
        }

        #endregion CheckEntity

        #region CheckVehicles

        private void CheckVehicles()
        {
            var currentTimestamp = TimeEx.currentTimestamp;
            foreach (var entry in vehiclesCache.ToArray())
            {
                if (entry.Key == null || entry.Key.IsDestroyed)
                {
                    continue;
                }
                if (VehicleIsActive(entry.Key, entry.Value, currentTimestamp))
                {
                    continue;
                }

                if (VehicleAnyMounted(entry.Key))
                {
                    continue;
                }
                entry.Key.Kill(BaseNetworkable.DestroyMode.Gib);
            }
        }

        private bool VehicleIsActive(BaseEntity entity, Vehicle vehicle, double currentTimestamp)
        {
            var settings = GetBaseVehicleSettings(vehicle.VehicleType);
            if (settings.WipeTime <= 0)
            {
                return true;
            }
            if (settings.ExcludeCupboard && entity.GetBuildingPrivilege() != null)
            {
                return true;
            }
            return currentTimestamp - vehicle.LastDismount < settings.WipeTime;
        }

        #endregion CheckVehicles

        #region Refund

        private void RefundVehicleItems(Vehicle vehicle, bool isCrash = false, bool isUnload = false)
        {
            var entity = vehicle.Entity;
            if (entity == null || entity.IsDestroyed)
            {
                return;
            }

            var settings = GetBaseVehicleSettings(vehicle.VehicleType);
            settings.RefundVehicleItems(vehicle, isCrash, isUnload);
        }

        private static void DropItemContainer(BaseEntity entity, ulong playerId, List<Item> collect)
        {
            var droppedItemContainer = GameManager.server.CreateEntity(PREFAB_ITEM_DROP, entity.GetDropPosition(), entity.transform.rotation) as DroppedItemContainer;
            if (droppedItemContainer != null)
            {
                droppedItemContainer.inventory = new ItemContainer();
                droppedItemContainer.inventory.ServerInitialize(null, Mathf.Min(collect.Count, droppedItemContainer.maxItemCount));
                droppedItemContainer.inventory.GiveUID();
                droppedItemContainer.inventory.entityOwner = droppedItemContainer;
                droppedItemContainer.inventory.SetFlag(ItemContainer.Flag.NoItemInput, true);
                for (var i = collect.Count - 1; i >= 0; i--)
                {
                    var item = collect[i];
                    if (!item.MoveToContainer(droppedItemContainer.inventory))
                    {
                        item.DropAndTossUpwards(droppedItemContainer.transform.position);
                    }
                }

                droppedItemContainer.OwnerID = playerId;
                droppedItemContainer.Spawn();
            }
        }

        #endregion Refund

        #region TryPay

        private bool TryPay(BasePlayer player, BaseVehicleSettings settings, Dictionary<string, PriceInfo> prices, out string resources)
        {
            if (permission.UserHasPermission(player.UserIDString, PERMISSION_BYPASS_COST) || permission.UserHasPermission(player.UserIDString, settings.BypassCostPermission))
            {
                resources = null;
                return true;
            }

            if (!CanPay(player, prices, out resources))
            {
                return false;
            }

            var collect = Pool.Get<List<Item>>();
            foreach (var entry in prices)
            {
                if (entry.Value.amount <= 0)
                {
                    continue;
                }
                var itemDefinition = ItemManager.FindItemDefinition(entry.Key);
                if (itemDefinition != null)
                {
                    player.inventory.Take(collect, itemDefinition.itemid, entry.Value.amount);
                    player.Command("note.inv", itemDefinition.itemid, -entry.Value.amount);
                    continue;
                }
                switch (entry.Key.ToLower())
                {
                    case "economics":
                        Economics?.Call("Withdraw", player.userID.Get(), (double)entry.Value.amount);
                        continue;

                    case "serverrewards":
                        ServerRewards?.Call("TakePoints", player.userID.Get(), entry.Value.amount);
                        continue;
                }
            }

            foreach (var item in collect)
            {
                item.Remove();
            }
            Pool.FreeUnmanaged(ref collect);
            resources = null;
            return true;
        }

        private bool CanPay(BasePlayer player, Dictionary<string, PriceInfo> prices, out string resources)
        {
            var entries = new Hash<string, int>();
            var language = RustTranslationAPI != null ? lang.GetLanguage(player.UserIDString) : null;
            foreach (var entry in prices)
            {
                if (entry.Value.amount <= 0)
                {
                    continue;
                }
                int missingAmount;
                var itemDefinition = ItemManager.FindItemDefinition(entry.Key);
                if (itemDefinition != null)
                {
                    missingAmount = entry.Value.amount - player.inventory.GetAmount(itemDefinition.itemid);
                }
                else
                {
                    missingAmount = CheckBalance(entry.Key, entry.Value.amount, player.userID.Get());
                }

                if (missingAmount <= 0)
                {
                    continue;
                }
                var displayName = GetItemDisplayName(language, entry.Key, entry.Value.displayName);
                entries[displayName] += missingAmount;
            }
            if (entries.Count > 0)
            {
                var stringBuilder = new StringBuilder();
                foreach (var entry in entries)
                {
                    stringBuilder.AppendLine($"* {Lang("PriceFormat", player.UserIDString, entry.Key, entry.Value)}");
                }
                resources = stringBuilder.ToString();
                return false;
            }
            resources = null;
            return true;
        }

        private int CheckBalance(string key, int price, ulong playerId)
        {
            switch (key.ToLower())
            {
                case "economics":
                    var balance = Economics?.Call("Balance", playerId);
                    if (balance is double)
                    {
                        var n = price - (double)balance;
                        return n <= 0 ? 0 : (int)Math.Ceiling(n);
                    }
                    return price;

                case "serverrewards":
                    var points = ServerRewards?.Call("CheckPoints", playerId);
                    if (points is int)
                    {
                        var n = price - (int)points;
                        return n <= 0 ? 0 : n;
                    }
                    return price;

                default:
                    PrintError($"Unknown Currency Type '{key}'");
                    return price;
            }
        }

        #endregion TryPay

        #region AreFriends

        private bool AreFriends(ulong playerId, ulong friendId)
        {
            if (playerId == friendId)
            {
                return true;
            }
            if (configData.global.useTeams && SameTeam(playerId, friendId))
            {
                return true;
            }

            if (configData.global.useFriends && HasFriend(playerId, friendId))
            {
                return true;
            }
            if (configData.global.useClans && SameClan(playerId, friendId))
            {
                return true;
            }
            return false;
        }

        private static bool SameTeam(ulong playerId, ulong friendId)
        {
            if (!RelationshipManager.TeamsEnabled())
            {
                return false;
            }
            var playerTeam = RelationshipManager.ServerInstance.FindPlayersTeam(playerId);
            if (playerTeam == null)
            {
                return false;
            }
            var friendTeam = RelationshipManager.ServerInstance.FindPlayersTeam(friendId);
            if (friendTeam == null)
            {
                return false;
            }
            return playerTeam == friendTeam;
        }

        private bool HasFriend(ulong playerId, ulong friendId)
        {
            if (Friends == null)
            {
                return false;
            }
            return (bool)Friends.Call("HasFriend", playerId, friendId);
        }

        private bool SameClan(ulong playerId, ulong friendId)
        {
            if (Clans == null)
            {
                return false;
            }
            //Clans
            var isMember = Clans.Call("IsClanMember", playerId.ToString(), friendId.ToString());
            if (isMember != null)
            {
                return (bool)isMember;
            }
            //Rust:IO Clans
            var playerClan = Clans.Call("GetClanOf", playerId);
            if (playerClan == null)
            {
                return false;
            }
            var friendClan = Clans.Call("GetClanOf", friendId);
            if (friendClan == null)
            {
                return false;
            }
            return playerClan == friendClan;
        }

        #endregion AreFriends

        #region IsPlayerBlocked

        private bool IsPlayerBlocked(BasePlayer player)
        {
            if (NoEscape == null)
            {
                return false;
            }
            if (configData.global.useRaidBlocker && IsRaidBlocked(player.UserIDString))
            {
                Print(player, Lang("RaidBlocked", player.UserIDString));
                return true;
            }
            if (configData.global.useCombatBlocker && IsCombatBlocked(player.UserIDString))
            {
                Print(player, Lang("CombatBlocked", player.UserIDString));
                return true;
            }
            return false;
        }

        private bool IsRaidBlocked(string playerId)
        {
            return (bool)NoEscape.Call("IsRaidBlocked", playerId);
        }

        private bool IsCombatBlocked(string playerId)
        {
            return (bool)NoEscape.Call("IsCombatBlocked", playerId);
        }

        private bool InZone(BasePlayer player)
        {
            if (ZoneManager == null || !ZoneManager.IsLoaded) return false;
            return configData.AntiSpawnZones.Any(x => (bool)ZoneManager?.Call("PlayerHasFlag", player, x.ToLower()));
        }

        #endregion IsPlayerBlocked

        #region GetSettings

        private BaseVehicleSettings GetBaseVehicleSettings(string vehicleType)
        {
            BaseVehicleSettings settings;
            return allVehicleSettings.TryGetValue(vehicleType, out settings) ? settings : null;
        }

        private BaseVehicleSettings GetBaseVehicleSettings(NormalVehicleType normalVehicleType)
        {
            switch (normalVehicleType)
            {
                case NormalVehicleType.Tugboat:
                    return configData.normalVehicles.tugboat;
                case NormalVehicleType.Rowboat:
                    return configData.normalVehicles.rowboat;
                case NormalVehicleType.RHIB:
                    return configData.normalVehicles.rhib;
                case NormalVehicleType.Sedan:
                    return configData.normalVehicles.sedan;
                case NormalVehicleType.HotAirBalloon:
                    return configData.normalVehicles.hotAirBalloon;
                case NormalVehicleType.ArmoredHotAirBalloon:
                    return configData.normalVehicles.armoredHotAirBalloon;
                case NormalVehicleType.MiniCopter:
                    return configData.normalVehicles.miniCopter;
                case NormalVehicleType.AttackHelicopter:
                    return configData.normalVehicles.attackHelicopter;
                case NormalVehicleType.TransportHelicopter:
                    return configData.normalVehicles.transportHelicopter;
                case NormalVehicleType.Chinook:
                    return configData.normalVehicles.chinook;
                case NormalVehicleType.RidableHorse:
                    return configData.normalVehicles.ridableHorse;
                case NormalVehicleType.WorkCart:
                    return configData.normalVehicles.workCart;
                case NormalVehicleType.SedanRail:
                    return configData.normalVehicles.sedanRail;
                case NormalVehicleType.MagnetCrane:
                    return configData.normalVehicles.magnetCrane;
                case NormalVehicleType.SubmarineSolo:
                    return configData.normalVehicles.submarineSolo;
                case NormalVehicleType.SubmarineDuo:
                    return configData.normalVehicles.submarineDuo;
                case NormalVehicleType.Snowmobile:
                    return configData.normalVehicles.snowmobile;
                case NormalVehicleType.TomahaSnowmobile:
                    return configData.normalVehicles.tomahaSnowmobile;
                case NormalVehicleType.PedalBike:
                    return configData.normalVehicles.pedalBike;
                case NormalVehicleType.PedalTrike:
                    return configData.normalVehicles.pedalTrike;
                case NormalVehicleType.MotorBike:
                    return configData.normalVehicles.motorBike;
                case NormalVehicleType.MotorBike_SideCar:
                    return configData.normalVehicles.motorBikeSidecar;
                case NormalVehicleType.Kayak:
                    return configData.normalVehicles.Kayak;
                default:
                    return null;
            }
        }

        private BaseVehicleSettings GetCustomVehicleSettings(CustomVehicleType normalVehicleType)
        {
            switch (normalVehicleType)
            {
                case CustomVehicleType.ATV:
                    return configData.customVehicles.atv;
                case CustomVehicleType.RaceSofa:
                    return configData.customVehicles.raceSofa;
                case CustomVehicleType.WaterBird:
                    return configData.customVehicles.waterBird;
                case CustomVehicleType.WarBird:
                    return configData.customVehicles.warBird;
                case CustomVehicleType.LittleBird:
                    return configData.customVehicles.littleBird;
                case CustomVehicleType.Fighter:
                    return configData.customVehicles.fighter;
                case CustomVehicleType.OldFighter:
                    return configData.customVehicles.oldFighter;
                case CustomVehicleType.FighterBus:
                    return configData.customVehicles.fighterBus;
                case CustomVehicleType.WarBus:
                    return configData.customVehicles.warBus;
                case CustomVehicleType.AirBus:
                    return configData.customVehicles.airBus;
                case CustomVehicleType.PatrolHeli:
                    return configData.customVehicles.patrolHeli;
                case CustomVehicleType.RustWing:
                    return configData.customVehicles.rustWing;
                case CustomVehicleType.RustWingDetailed:
                    return configData.customVehicles.rustWingDetailed;
                case CustomVehicleType.RustWingDetailedOld:
                    return configData.customVehicles.rustWingDetailedOld;
                case CustomVehicleType.TinFighter:
                    return configData.customVehicles.tinFighter;
                case CustomVehicleType.TinFighterDetailed:
                    return configData.customVehicles.tinFighterDetailed;
                case CustomVehicleType.TinFighterDetailedOld:
                    return configData.customVehicles.tinFighterDetailedOld;
                case CustomVehicleType.MarsFighter:
                    return configData.customVehicles.marsFighter;
                case CustomVehicleType.MarsFighterDetailed:
                    return configData.customVehicles.marsFighterDetailed;
                case CustomVehicleType.SkyPlane:
                    return configData.customVehicles.skyPlane;
                case CustomVehicleType.SkyBoat:
                    return configData.customVehicles.skyBoat;
                case CustomVehicleType.TwistedTruck:
                    return configData.customVehicles.twistedTruck;
                case CustomVehicleType.TrainWreck:
                    return configData.customVehicles.trainWreck;
                case CustomVehicleType.TrainWrecker:
                    return configData.customVehicles.trainWrecker;
                case CustomVehicleType.Santa:
                    return configData.customVehicles.santa;
                case CustomVehicleType.WarSanta:
                    return configData.customVehicles.warSanta;
                case CustomVehicleType.Witch:
                    return configData.customVehicles.witch;
                case CustomVehicleType.MagicCarpet:
                    return configData.customVehicles.magicCarpet;
                case CustomVehicleType.Ah69t:
                    return configData.customVehicles.ah69t;
                case CustomVehicleType.Ah69r:
                    return configData.customVehicles.ah69r;
                case CustomVehicleType.Ah69a:
                    return configData.customVehicles.ah69a;
                case CustomVehicleType.Mavik:
                    return configData.customVehicles.mavik;
                case CustomVehicleType.HeavyFighter:
                    return configData.customVehicles.heavyFighter;
                case CustomVehicleType.PorcelainCommander:
                    return configData.customVehicles.porcelainCommander;
                case CustomVehicleType.DuneBuggie:
                    return configData.customVehicles.duneBuggie;
                case CustomVehicleType.DuneTruckArmed:
                    return configData.customVehicles.duneTruckArmed;
                case CustomVehicleType.DuneTruckUnArmed:
                    return configData.customVehicles.duneTruckUnArmed;
                case CustomVehicleType.DoomsDayDiscoVan:
                    return configData.customVehicles.doomsDayDiscoVan;
                case CustomVehicleType.ForkLift:
                    return configData.customVehicles.forkLift;
                case CustomVehicleType.LawnMower:
                    return configData.customVehicles.lawnMower;
                case CustomVehicleType.Chariot:
                    return configData.customVehicles.chariot;
                case CustomVehicleType.SoulHarvester:
                    return configData.customVehicles.soulHarvester;
                default:
                    return null;
            }
        }

        private bool GetBaseVehicleCollisionDamage(string vehicleType)
        {
            BaseVehicleSettings settings;
            return allVehicleSettings.TryGetValue(vehicleType, out settings) && settings.NoCollisionDamage;
        }

        // private bool GetBaseVehicleCollisionDamage(string vehicleType)
        // {
        //     BaseVehicleSettings settings;
        //     return allVehicleSettings.TryGetValue(vehicleType, out settings) && settings.NoCollisionDamage;
        // }

        private bool GetBaseVehicleCollisionDamage(NormalVehicleType normalVehicleType)
        {
            switch (normalVehicleType)
            {
                case NormalVehicleType.Tugboat:
                    return configData.normalVehicles.tugboat.NoCollisionDamage;
                case NormalVehicleType.Rowboat:
                    return configData.normalVehicles.rowboat.NoCollisionDamage;
                case NormalVehicleType.RHIB:
                    return configData.normalVehicles.rhib.NoCollisionDamage;
                case NormalVehicleType.Sedan:
                    return configData.normalVehicles.sedan.NoCollisionDamage;
                case NormalVehicleType.HotAirBalloon:
                    return configData.normalVehicles.hotAirBalloon.NoCollisionDamage;
                case NormalVehicleType.ArmoredHotAirBalloon:
                    return configData.normalVehicles.armoredHotAirBalloon.NoCollisionDamage;
                case NormalVehicleType.MiniCopter:
                    return configData.normalVehicles.miniCopter.NoCollisionDamage;
                case NormalVehicleType.AttackHelicopter:
                    return configData.normalVehicles.attackHelicopter.NoCollisionDamage;
                case NormalVehicleType.TransportHelicopter:
                    return configData.normalVehicles.transportHelicopter.NoCollisionDamage;
                case NormalVehicleType.Chinook:
                    return configData.normalVehicles.chinook.NoCollisionDamage;
                case NormalVehicleType.RidableHorse:
                    return configData.normalVehicles.ridableHorse.NoCollisionDamage;
                case NormalVehicleType.WorkCart:
                    return configData.normalVehicles.workCart.NoCollisionDamage;
                case NormalVehicleType.SedanRail:
                    return configData.normalVehicles.sedanRail.NoCollisionDamage;
                case NormalVehicleType.MagnetCrane:
                    return configData.normalVehicles.magnetCrane.NoCollisionDamage;
                case NormalVehicleType.SubmarineSolo:
                    return configData.normalVehicles.submarineSolo.NoCollisionDamage;
                case NormalVehicleType.SubmarineDuo:
                    return configData.normalVehicles.submarineDuo.NoCollisionDamage;
                case NormalVehicleType.Snowmobile:
                    return configData.normalVehicles.snowmobile.NoCollisionDamage;
                case NormalVehicleType.TomahaSnowmobile:
                    return configData.normalVehicles.tomahaSnowmobile.NoCollisionDamage;
                case NormalVehicleType.PedalBike:
                    return configData.normalVehicles.pedalBike.NoCollisionDamage;
                case NormalVehicleType.PedalTrike:
                    return configData.normalVehicles.pedalTrike.NoCollisionDamage;
                case NormalVehicleType.MotorBike:
                    return configData.normalVehicles.motorBike.NoCollisionDamage;
                case NormalVehicleType.MotorBike_SideCar:
                    return configData.normalVehicles.motorBikeSidecar.NoCollisionDamage;
                case NormalVehicleType.Kayak:
                    return configData.normalVehicles.Kayak.NoCollisionDamage;
                default:
                    return false;
            }
        }

        private bool GetBaseVehicleCollisionDamage(CustomVehicleType normalVehicleType)
        {
            switch (normalVehicleType)
            {
                case CustomVehicleType.ATV:
                    return configData.customVehicles.atv.NoCollisionDamage;
                case CustomVehicleType.RaceSofa:
                    return configData.customVehicles.raceSofa.NoCollisionDamage;
                case CustomVehicleType.WaterBird:
                    return configData.customVehicles.waterBird.NoCollisionDamage;
                case CustomVehicleType.WarBird:
                    return configData.customVehicles.warBird.NoCollisionDamage;
                case CustomVehicleType.LittleBird:
                    return configData.customVehicles.littleBird.NoCollisionDamage;
                case CustomVehicleType.Fighter:
                    return configData.customVehicles.fighter.NoCollisionDamage;
                case CustomVehicleType.OldFighter:
                    return configData.customVehicles.oldFighter.NoCollisionDamage;
                case CustomVehicleType.FighterBus:
                    return configData.customVehicles.fighterBus.NoCollisionDamage;
                case CustomVehicleType.WarBus:
                    return configData.customVehicles.warBus.NoCollisionDamage;
                case CustomVehicleType.AirBus:
                    return configData.customVehicles.airBus.NoCollisionDamage;
                case CustomVehicleType.PatrolHeli:
                    return configData.customVehicles.patrolHeli.NoCollisionDamage;
                case CustomVehicleType.RustWing:
                    return configData.customVehicles.rustWing.NoCollisionDamage;
                case CustomVehicleType.RustWingDetailed:
                    return configData.customVehicles.rustWingDetailed.NoCollisionDamage;
                case CustomVehicleType.RustWingDetailedOld:
                    return configData.customVehicles.rustWingDetailedOld.NoCollisionDamage;
                case CustomVehicleType.TinFighter:
                    return configData.customVehicles.tinFighter.NoCollisionDamage;
                case CustomVehicleType.TinFighterDetailed:
                    return configData.customVehicles.tinFighterDetailed.NoCollisionDamage;
                case CustomVehicleType.TinFighterDetailedOld:
                    return configData.customVehicles.tinFighterDetailedOld.NoCollisionDamage;
                case CustomVehicleType.MarsFighter:
                    return configData.customVehicles.marsFighter.NoCollisionDamage;
                case CustomVehicleType.MarsFighterDetailed:
                    return configData.customVehicles.marsFighterDetailed.NoCollisionDamage;
                case CustomVehicleType.SkyPlane:
                    return configData.customVehicles.skyPlane.NoCollisionDamage;
                case CustomVehicleType.SkyBoat:
                    return configData.customVehicles.skyBoat.NoCollisionDamage;
                case CustomVehicleType.TwistedTruck:
                    return configData.customVehicles.twistedTruck.NoCollisionDamage;
                case CustomVehicleType.TrainWreck:
                    return configData.customVehicles.trainWreck.NoCollisionDamage;
                case CustomVehicleType.TrainWrecker:
                    return configData.customVehicles.trainWrecker.NoCollisionDamage;
                case CustomVehicleType.Santa:
                    return configData.customVehicles.santa.NoCollisionDamage;
                case CustomVehicleType.WarSanta:
                    return configData.customVehicles.warSanta.NoCollisionDamage;
                case CustomVehicleType.Witch:
                    return configData.customVehicles.witch.NoCollisionDamage;
                case CustomVehicleType.MagicCarpet:
                    return configData.customVehicles.magicCarpet.NoCollisionDamage;
                case CustomVehicleType.Ah69t:
                    return configData.customVehicles.ah69t.NoCollisionDamage;
                case CustomVehicleType.Ah69r:
                    return configData.customVehicles.ah69r.NoCollisionDamage;
                case CustomVehicleType.Ah69a:
                    return configData.customVehicles.ah69a.NoCollisionDamage;
                case CustomVehicleType.Mavik:
                    return configData.customVehicles.mavik.NoCollisionDamage;
                case CustomVehicleType.HeavyFighter:
                    return configData.customVehicles.heavyFighter.NoCollisionDamage;
                case CustomVehicleType.PorcelainCommander:
                    return configData.customVehicles.porcelainCommander.NoCollisionDamage;
                case CustomVehicleType.DuneBuggie:
                    return configData.customVehicles.duneBuggie.NoCollisionDamage;
                case CustomVehicleType.DuneTruckArmed:
                    return configData.customVehicles.duneTruckArmed.NoCollisionDamage;
                case CustomVehicleType.DuneTruckUnArmed:
                    return configData.customVehicles.duneTruckUnArmed.NoCollisionDamage;
                case CustomVehicleType.DoomsDayDiscoVan:
                    return configData.customVehicles.doomsDayDiscoVan.NoCollisionDamage;
                case CustomVehicleType.ForkLift:
                    return configData.customVehicles.forkLift.NoCollisionDamage;
                case CustomVehicleType.LawnMower:
                    return configData.customVehicles.lawnMower.NoCollisionDamage;
                case CustomVehicleType.Chariot:
                    return configData.customVehicles.chariot.NoCollisionDamage;
                case CustomVehicleType.SoulHarvester:
                    return configData.customVehicles.soulHarvester.NoCollisionDamage;
                default:
                    return false;
            }
        }

        private bool GetBaseVehicleDamage(string vehicleType)
        {
            BaseVehicleSettings settings;
            return allVehicleSettings.TryGetValue(vehicleType, out settings) && settings.NoDamage;
        }

        private bool GetBaseVehicleDamage(NormalVehicleType normalVehicleType)
        {
            switch (normalVehicleType)
            {
                case NormalVehicleType.Tugboat:
                    return configData.normalVehicles.tugboat.NoDamage;
                case NormalVehicleType.Rowboat:
                    return configData.normalVehicles.rowboat.NoDamage;
                case NormalVehicleType.RHIB:
                    return configData.normalVehicles.rhib.NoDamage;
                case NormalVehicleType.Sedan:
                    return configData.normalVehicles.sedan.NoDamage;
                case NormalVehicleType.HotAirBalloon:
                    return configData.normalVehicles.hotAirBalloon.NoDamage;
                case NormalVehicleType.ArmoredHotAirBalloon:
                    return configData.normalVehicles.armoredHotAirBalloon.NoDamage;
                case NormalVehicleType.MiniCopter:
                    return configData.normalVehicles.miniCopter.NoDamage;
                case NormalVehicleType.AttackHelicopter:
                    return configData.normalVehicles.attackHelicopter.NoDamage;
                case NormalVehicleType.TransportHelicopter:
                    return configData.normalVehicles.transportHelicopter.NoDamage;
                case NormalVehicleType.Chinook:
                    return configData.normalVehicles.chinook.NoDamage;
                case NormalVehicleType.RidableHorse:
                    return configData.normalVehicles.ridableHorse.NoDamage;
                case NormalVehicleType.WorkCart:
                    return configData.normalVehicles.workCart.NoDamage;
                case NormalVehicleType.SedanRail:
                    return configData.normalVehicles.sedanRail.NoDamage;
                case NormalVehicleType.MagnetCrane:
                    return configData.normalVehicles.magnetCrane.NoDamage;
                case NormalVehicleType.SubmarineSolo:
                    return configData.normalVehicles.submarineSolo.NoDamage;
                case NormalVehicleType.SubmarineDuo:
                    return configData.normalVehicles.submarineDuo.NoDamage;
                case NormalVehicleType.Snowmobile:
                    return configData.normalVehicles.snowmobile.NoDamage;
                case NormalVehicleType.TomahaSnowmobile:
                    return configData.normalVehicles.tomahaSnowmobile.NoDamage;
                case NormalVehicleType.PedalBike:
                    return configData.normalVehicles.pedalBike.NoDamage;
                case NormalVehicleType.PedalTrike:
                    return configData.normalVehicles.pedalTrike.NoDamage;
                case NormalVehicleType.MotorBike:
                    return configData.normalVehicles.motorBike.NoDamage;
                case NormalVehicleType.MotorBike_SideCar:
                    return configData.normalVehicles.motorBikeSidecar.NoDamage;
                case NormalVehicleType.Kayak:
                    return configData.normalVehicles.Kayak.NoDamage;
                default:
                    return false;
            }
        }

        private bool GetBaseVehicleDamage(CustomVehicleType normalVehicleType)
        {
            switch (normalVehicleType)
            {
                case CustomVehicleType.ATV:
                    return configData.customVehicles.atv.NoDamage;
                case CustomVehicleType.RaceSofa:
                    return configData.customVehicles.raceSofa.NoDamage;
                case CustomVehicleType.WaterBird:
                    return configData.customVehicles.waterBird.NoDamage;
                case CustomVehicleType.WarBird:
                    return configData.customVehicles.warBird.NoDamage;
                case CustomVehicleType.LittleBird:
                    return configData.customVehicles.littleBird.NoDamage;
                case CustomVehicleType.Fighter:
                    return configData.customVehicles.fighter.NoDamage;
                case CustomVehicleType.OldFighter:
                    return configData.customVehicles.oldFighter.NoDamage;
                case CustomVehicleType.FighterBus:
                    return configData.customVehicles.fighterBus.NoDamage;
                case CustomVehicleType.WarBus:
                    return configData.customVehicles.warBus.NoDamage;
                case CustomVehicleType.AirBus:
                    return configData.customVehicles.airBus.NoDamage;
                case CustomVehicleType.PatrolHeli:
                    return configData.customVehicles.patrolHeli.NoDamage;
                case CustomVehicleType.RustWing:
                    return configData.customVehicles.rustWing.NoDamage;
                case CustomVehicleType.RustWingDetailed:
                    return configData.customVehicles.rustWingDetailed.NoDamage;
                case CustomVehicleType.RustWingDetailedOld:
                    return configData.customVehicles.rustWingDetailedOld.NoDamage;
                case CustomVehicleType.TinFighter:
                    return configData.customVehicles.tinFighter.NoDamage;
                case CustomVehicleType.TinFighterDetailed:
                    return configData.customVehicles.tinFighterDetailed.NoDamage;
                case CustomVehicleType.TinFighterDetailedOld:
                    return configData.customVehicles.tinFighterDetailedOld.NoDamage;
                case CustomVehicleType.MarsFighter:
                    return configData.customVehicles.marsFighter.NoDamage;
                case CustomVehicleType.MarsFighterDetailed:
                    return configData.customVehicles.marsFighterDetailed.NoDamage;
                case CustomVehicleType.SkyPlane:
                    return configData.customVehicles.skyPlane.NoDamage;
                case CustomVehicleType.SkyBoat:
                    return configData.customVehicles.skyBoat.NoDamage;
                case CustomVehicleType.TwistedTruck:
                    return configData.customVehicles.twistedTruck.NoDamage;
                case CustomVehicleType.TrainWreck:
                    return configData.customVehicles.trainWrecker.NoDamage;
                case CustomVehicleType.Santa:
                    return configData.customVehicles.santa.NoDamage;
                case CustomVehicleType.WarSanta:
                    return configData.customVehicles.warSanta.NoDamage;
                case CustomVehicleType.Witch:
                    return configData.customVehicles.witch.NoDamage;
                case CustomVehicleType.MagicCarpet:
                    return configData.customVehicles.magicCarpet.NoDamage;
                case CustomVehicleType.Ah69t:
                    return configData.customVehicles.ah69t.NoDamage;
                case CustomVehicleType.Ah69r:
                    return configData.customVehicles.ah69r.NoDamage;
                case CustomVehicleType.Ah69a:
                    return configData.customVehicles.ah69a.NoDamage;
                case CustomVehicleType.Mavik:
                    return configData.customVehicles.mavik.NoDamage;
                case CustomVehicleType.HeavyFighter:
                    return configData.customVehicles.heavyFighter.NoDamage;
                case CustomVehicleType.PorcelainCommander:
                    return configData.customVehicles.porcelainCommander.NoDamage;
                case CustomVehicleType.DuneBuggie:
                    return configData.customVehicles.duneBuggie.NoDamage;
                case CustomVehicleType.DuneTruckArmed:
                    return configData.customVehicles.duneTruckArmed.NoDamage;
                case CustomVehicleType.DuneTruckUnArmed:
                    return configData.customVehicles.duneTruckUnArmed.NoDamage;
                case CustomVehicleType.DoomsDayDiscoVan:
                    return configData.customVehicles.doomsDayDiscoVan.NoDamage;
                case CustomVehicleType.ForkLift:
                    return configData.customVehicles.forkLift.NoDamage;
                case CustomVehicleType.LawnMower:
                    return configData.customVehicles.lawnMower.NoDamage;
                case CustomVehicleType.Chariot:
                    return configData.customVehicles.chariot.NoDamage;
                case CustomVehicleType.SoulHarvester:
                    return configData.customVehicles.soulHarvester.NoDamage;
                default:
                    return false;
            }
        }

        #endregion GetSettings

        #region Permission

        private bool HasAdminPermission(BasePlayer player)
        {
            return permission.UserHasPermission(player.UserIDString, PERMISSION_ADMIN);
        }

        private bool CanViewVehicleInfo(BasePlayer player, string vehicleType, BaseVehicleSettings settings)
        {
            if (settings.Purchasable && settings.Commands.Count > 0)
            {
                return HasVehiclePermission(player, vehicleType);
            }
            return false;
        }

        private bool HasVehiclePermission(BasePlayer player, string vehicleType)
        {
            var settings = GetBaseVehicleSettings(vehicleType);
            if (!settings.UsePermission || string.IsNullOrEmpty(settings.Permission))
            {
                return true;
            }
            return permission.UserHasPermission(player.UserIDString, PERMISSION_ALL) ||
                    permission.UserHasPermission(player.UserIDString, settings.Permission);
        }

        #endregion Permission

        #region Claim

        private void TryClaimVehicle(BaseVehicle baseVehicle)
        {
            NextTick(() =>
            {
                if (baseVehicle == null)
                {
                    return;
                }
                var player = baseVehicle.creatorEntity as BasePlayer;
                if (player == null || !player.userID.IsSteamId() || !baseVehicle.OnlyOwnerAccessible())
                {
                    return;
                }
                var vehicleType = GetClaimableVehicleType(baseVehicle);
                if (vehicleType.HasValue)
                {
                    TryClaimVehicle(player, baseVehicle, vehicleType.Value.ToString());
                }
            });
        }

        private void TryClaimVehicle(BaseVehicle baseVehicle, BasePlayer player)
        {
            if (player == null || !player.userID.IsSteamId())
            {
                return;
            }
            var vehicleType = GetClaimableVehicleType(baseVehicle);
            if (vehicleType.HasValue)
            {
                TryClaimVehicle(player, baseVehicle, vehicleType.Value.ToString());
            }
        }

        private bool TryClaimVehicle(BasePlayer player, BaseEntity entity, string vehicleType)
        {
            Vehicle vehicle;
            if (!storedData.IsVehiclePurchased(player.userID, vehicleType, out vehicle))
            {
                if (!configData.global.autoUnlockFromVendor)
                {
                    return false;
                }

                storedData.AddVehicleLicense(player.userID, vehicleType);
                vehicle = storedData.GetVehicleLicense(player.userID, vehicleType);
            }
            if (vehicle.Entity == null || vehicle.Entity.IsDestroyed)
            {
                var settings = GetBaseVehicleSettings(vehicle.VehicleType);
                if (settings != null)
                {
                    settings.PreSetupVehicle(entity, vehicle, player);
                    settings.SetupVehicle(entity, vehicle, player, false);
                }
                CacheVehicleEntity(entity, vehicle, player);
                return true;
            }
            return false;
        }

        #endregion Claim

        private bool TryGetVehicle(BaseEntity entity, out Vehicle vehicle)
        {
            if (!vehiclesCache.TryGetValue(entity, out vehicle))
            {
                var vehicleModule = entity as BaseVehicleModule;
                if (vehicleModule == null)
                {
                    return false;
                }
                var parent = vehicleModule.Vehicle;
                if (parent == null || !vehiclesCache.TryGetValue(parent, out vehicle))
                {
                    return false;
                }
            }
            return true;
        }

        private IEnumerator UpdatePlayerData(double currentTimestamp)
        {
            foreach (var playerData in storedData.playerData)
            {
                foreach (var entry in playerData.Value)
                {
                    entry.Value.PlayerId = playerData.Key;
                    entry.Value.VehicleType = entry.Key;
                    if (configData.global.storeVehicle)
                    {
                        entry.Value.LastRecall = entry.Value.LastDismount = currentTimestamp;
                        if (entry.Value.EntityId == 0)
                        {
                            continue;
                        }
                        NetworkableId id = new NetworkableId(entry.Value.EntityId);
                        entry.Value.Entity = BaseNetworkable.serverEntities.Find(id) as BaseEntity;
                        if (entry.Value.Entity == null || entry.Value.Entity.IsDestroyed)
                        {
                            entry.Value.EntityId = 0;
                        }
                        else
                        {
                            vehiclesCache.Add(entry.Value.Entity, entry.Value);
                            if (entry.Value.Entity is Tugboat)
                            {
                                Tugboat vehicle = entry.Value.Entity as Tugboat;
                                vehicle.engineThrust = TUGBOAT_ENGINETHRUST * configData.normalVehicles.tugboat.speedMultiplier;
                            }
                            else if (entry.Value.Entity is ScrapTransportHelicopter)
                            {
                                ScrapTransportHelicopter vehicle = entry.Value.Entity as ScrapTransportHelicopter;
                                vehicle.liftFraction = configData.normalVehicles.transportHelicopter.liftFraction;
                                vehicle.torqueScale = SCRAP_HELICOPTER_TORQUE * configData.normalVehicles.transportHelicopter.rotationScale;
                            }
                            else if (entry.Value.Entity is Minicopter)
                            {
                                Minicopter vehicle = entry.Value.Entity as Minicopter;
                                vehicle.liftFraction = configData.normalVehicles.miniCopter.liftFraction;
                                vehicle.torqueScale = MINICOPTER_TORQUE * configData.normalVehicles.miniCopter.rotationScale;
                            }
                            else if (entry.Value.Entity is AttackHelicopter)
                            {
                                AttackHelicopter vehicle = entry.Value.Entity as AttackHelicopter;
                                vehicle.liftFraction = configData.normalVehicles.attackHelicopter.liftFraction;
                                vehicle.torqueScale = ATTACK_HELICOPTER_TORQUE * configData.normalVehicles.attackHelicopter.rotationScale;
                            }
                        }
                    }
                    // Adjust the delay duration here if needed
                    yield return new WaitForSeconds(0.1f);
                }
            }
            finishedLoading = true;
        }

        #region Helpers

        private static NormalVehicleType? GetClaimableVehicleType(BaseVehicle baseVehicle)
        {
            if (baseVehicle is Tugboat)
            {
                return NormalVehicleType.Tugboat;
            }
            if (baseVehicle is RidableHorse)
            {
                return NormalVehicleType.RidableHorse;
            }
            if (baseVehicle is ScrapTransportHelicopter)
            {
                return NormalVehicleType.TransportHelicopter;
            }
            if (baseVehicle is Minicopter)
            {
                return NormalVehicleType.MiniCopter;
            }
            if (baseVehicle is AttackHelicopter)
            {
                return NormalVehicleType.AttackHelicopter;
            }
            if (baseVehicle is RHIB)
            {
                return NormalVehicleType.RHIB;
            }
            if (baseVehicle is MotorRowboat)
            {
                return NormalVehicleType.Rowboat;
            }
            if (baseVehicle is SubmarineDuo)
            {
                return NormalVehicleType.SubmarineDuo;
            }
            if (baseVehicle is BaseSubmarine)
            {
                return NormalVehicleType.SubmarineSolo;
            }
            if (baseVehicle is Kayak)
            {
                return NormalVehicleType.Kayak;
            }
            return null;
        }

        private static bool GetDismountPosition(BaseVehicle baseVehicle, BasePlayer player, out Vector3 result)
        {
            var parentVehicle = baseVehicle.VehicleParent();
            if (parentVehicle != null)
            {
                return GetDismountPosition(parentVehicle, player, out result);
            }
            var list = Pool.Get<List<Vector3>>();
            foreach (var transform in baseVehicle.dismountPositions)
            {
                if (baseVehicle.ValidDismountPosition(player, transform.position))
                {
                    list.Add(transform.position);
                    if (baseVehicle.dismountStyle == BaseVehicle.DismountStyle.Ordered)
                    {
                        break;
                    }
                }
            }
            if (list.Count == 0)
            {
                result = Vector3.zero;
                Pool.FreeUnmanaged(ref list);
                return false;
            }
            var pos = player.transform.position;
            list.Sort((a, b) => Vector3.Distance(a, pos).CompareTo(Vector3.Distance(b, pos)));
            result = list[0];
            Pool.FreeUnmanaged(ref list);
            return true;
        }

        private static bool VehicleAnyMounted(BaseEntity entity)
        {
            var baseVehicle = entity as BaseVehicle;
            if (baseVehicle != null && baseVehicle.AnyMounted())
            {
                return true;
            }
            return entity.GetComponentsInChildren<BasePlayer>()?.Length > 0;
        }

        private static void DismountAllPlayers(BaseEntity entity)
        {
            var baseVehicle = entity as BaseVehicle;
            if (baseVehicle != null)
            {
                //(vehicle as BaseVehicle).DismountAllPlayers();
                foreach (var mountPointInfo in baseVehicle.allMountPoints)
                {
                    if (mountPointInfo != null && mountPointInfo.mountable != null)
                    {
                        var mounted = mountPointInfo.mountable.GetMounted();
                        if (mounted != null)
                        {
                            mountPointInfo.mountable.DismountPlayer(mounted);
                        }
                    }
                }
            }
            var players = entity.GetComponentsInChildren<BasePlayer>();
            foreach (var player in players)
            {
                player.SetParent(null, true, true);
            }
        }

        private static Vector3 GetGroundPositionLookingAt(BasePlayer player, float distance, bool needUp = true)
        {
            RaycastHit hitInfo;
            var headRay = player.eyes.HeadRay();
            if (Physics.Raycast(headRay, out hitInfo, distance, LAYER_GROUND))
            {
                return hitInfo.point;
            }
            return GetGroundPosition(headRay.origin + headRay.direction * distance, needUp);
        }

        private static Vector3 GetGroundPosition(Vector3 position, bool needUp = true)
        {
            RaycastHit hitInfo;
            position.y = Physics.Raycast(needUp ? position + Vector3.up * 250 : position, Vector3.down, out hitInfo, needUp ? 400f : 50f, LAYER_GROUND)
                    ? hitInfo.point.y
                    : TerrainMeta.HeightMap.GetHeight(position);
            return position;
        }

        private static bool IsInWater(Vector3 position)
        {
            var colliders = Pool.Get<List<Collider>>();
            Vis.Colliders(position, 0.5f, colliders);
            var flag = colliders.Any(x => x.gameObject.layer == (int)Layer.Water);
            Pool.FreeUnmanaged(ref colliders);
            return flag || WaterLevel.Test(position, false, false);
        }

        private static void MoveToPosition(BasePlayer player, Vector3 position)
        {
            player.Teleport(position);
            player.ForceUpdateTriggers();
            //if (player.HasParent()) player.SetParent(null, true, true);
            player.SendNetworkUpdateImmediate();
        }

        // Authorizes player and their team.
        private static void AuthTeamOnTugboat(Tugboat tug, BasePlayer player)
        {
            RelationshipManager.PlayerTeam team = RelationshipManager.ServerInstance.FindPlayersTeam(player.userID);
            VehiclePrivilege vehiclePrivilege = null;
            if (team == null || team.members.Count == 1)
            {
                foreach (BaseEntity child in tug.children)
                {
                    vehiclePrivilege = child as VehiclePrivilege;
                    if (vehiclePrivilege != null) break;
                }

                if (vehiclePrivilege == null) return;
                // I find this a bit broken to do, as it breaks gameplay and makes it OP to recall tugboat.
                // if (clear) vehiclePrivilege.authorizedPlayers.Clear();
                vehiclePrivilege.AddPlayer(player);
                return;
            }
            BasePlayer teammate;
            foreach (BaseEntity child in tug.children)
            {
                vehiclePrivilege = child as VehiclePrivilege;
                if (vehiclePrivilege == null) continue;
                // I find this a bit broken to do, as it breaks gameplay, as it breaks gameplay and makes it OP to recall tugboat.
                // if(clear) vehiclePrivilege.authorizedPlayers.Clear(); 
                vehiclePrivilege.AddPlayer(player);
                foreach (ulong id in team.members)
                {
                    teammate = BasePlayer.FindByID(id);
                    if (teammate == null) continue;
                    vehiclePrivilege.AddPlayer(teammate);
                }
            }
        }
        #region Train Car

        #endregion

        #endregion Helpers

        #endregion Methods

        #region API

        [HookMethod(nameof(IsLicensedVehicle))]
        public bool IsLicensedVehicle(BaseEntity entity)
        {
            return vehiclesCache.ContainsKey(entity);
        }

        [HookMethod(nameof(GetLicensedVehicle))]
        public BaseEntity GetLicensedVehicle(ulong playerId, string license)
        {
            return storedData.GetVehicleLicense(playerId, license)?.Entity;
        }

        [HookMethod(nameof(HasVehicleLicense))]
        public bool HasVehicleLicense(ulong playerId, string license)
        {
            return storedData.HasVehicleLicense(playerId, license);
        }

        [HookMethod(nameof(RemoveVehicleLicense))]
        public bool RemoveVehicleLicense(ulong playerId, string license)
        {
            return storedData.RemoveVehicleLicense(playerId, license);
        }

        [HookMethod(nameof(AddVehicleLicense))]
        public bool AddVehicleLicense(ulong playerId, string license)
        {
            return storedData.AddVehicleLicense(playerId, license);
        }

        [HookMethod(nameof(GetVehicleLicenses))]
        public List<string> GetVehicleLicenses(ulong playerId)
        {
            return storedData.GetVehicleLicenseNames(playerId);
        }

        [HookMethod(nameof(PurchaseAllVehicles))]
        public void PurchaseAllVehicles(ulong playerId)
        {
            storedData.PurchaseAllVehicles(playerId);
        }

        #endregion API

        #region Commands

        #region Universal Command

        private void CmdUniversal(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, PERMISSION_USE))
            {
                Print(player, Lang("NotAllowed", player.UserIDString));
                return;
            }

            string vehicleType;
            if (IsValidOption(player, command, out vehicleType))
            {
                var bypassCooldown = args.Length > 0 && IsValidBypassCooldownOption(args[0]);
                HandleUniversalCmd(player, vehicleType, bypassCooldown, command);
            }
        }

        private void HandleUniversalCmd(BasePlayer player, string vehicleType, bool bypassCooldown, string command)
        {
            if (!finishedLoading)
            {
                Print(player, Lang("PleaseWait", player.UserIDString));
                return;
            }

            // TODO: 
            // Use TerrainMeta.HeightMap.GetHeight to get height of map at a given point!

            // Debug.Log($"INFO: {player.AirFactor()}");
            // if (player.metabolism.oxygen.value == 1)
            // {
            //     
            //     Puts(Lang("NoSpawnInAir", player.UserIDString, vehicleType));
            //     return;
            // }
            Vehicle vehicle;

            string reason;
            var position = Vector3.zero;
            var rotation = Quaternion.identity;
            if (storedData.IsVehiclePurchased(player.userID, vehicleType, out vehicle))
            {
                if (vehicle.Entity != null && !vehicle.Entity.IsDestroyed)
                {
                    //recall
                    if (CanRecall(player, vehicle, bypassCooldown, command, out reason, ref position, ref rotation))
                    {
                        RecallVehicle(player, vehicle, position, rotation);
                        return;
                    }
                }
                else
                {
                    //spawn
                    if (CanSpawn(player, vehicle, bypassCooldown, command, out reason, ref position, ref rotation))
                    {
                        SpawnVehicle(player, vehicle, position, rotation);
                        return;
                    }
                }
                Print(player, reason);
                return;
            }
            //buy - Auto spawns the vehicle when attempting to spawn it via universal command
            if (!BuyVehicle(player, vehicleType)) return;
            storedData.IsVehiclePurchased(player.userID, vehicleType, out vehicle);
            if (vehicle.Entity != null && !vehicle.Entity.IsDestroyed)
            {
                //recall
                if (CanRecall(player, vehicle, bypassCooldown, command, out reason, ref position, ref rotation))
                {
                    RecallVehicle(player, vehicle, position, rotation);
                    return;
                }
            }
            else
            {
                //spawn
                if (CanSpawn(player, vehicle, bypassCooldown, command, out reason, ref position, ref rotation))
                {
                    SpawnVehicle(player, vehicle, position, rotation);
                    return;
                }
            }
            Print(player, reason);
        }

        #endregion Universal Command

        #region Custom Kill Command

        private void CmdCustomKill(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, PERMISSION_USE))
            {
                Print(player, Lang("NotAllowed", player.UserIDString));
                return;
            }
            command = command.Remove(0, configData.chat.customKillCommandPrefix.Length);
            HandleKillCmd(player, command);
        }

        #endregion Custom Kill Command

        #region Help Command

        private void CmdLicenseHelp(BasePlayer player, string command, string[] args)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine(Lang("Help", player.UserIDString));
            stringBuilder.AppendLine(Lang("HelpLicence1", player.UserIDString, configData.chat.buyCommand));
            stringBuilder.AppendLine(Lang("HelpLicence2", player.UserIDString, configData.chat.spawnCommand));
            stringBuilder.AppendLine(Lang("HelpLicence3", player.UserIDString, configData.chat.recallCommand));
            stringBuilder.AppendLine(Lang("HelpLicence4", player.UserIDString, configData.chat.killCommand));

            foreach (var entry in allVehicleSettings)
            {
                if (CanViewVehicleInfo(player, entry.Key, entry.Value))
                {
                    if (configData.chat.useUniversalCommand)
                    {
                        var firstCmd = entry.Value.Commands[0];
                        stringBuilder.AppendLine(Lang("HelpLicence5", player.UserIDString, firstCmd, entry.Value.DisplayName));
                    }
                }
            }
            Print(player, stringBuilder.ToString());
        }

        #endregion Help Command

        #region Remove Command

        [ConsoleCommand("vl.remove")]
        private void CCmdRemoveVehicle(ConsoleSystem.Arg arg)
        {
            if (arg.IsAdmin && arg.Args != null && arg.Args.Length == 2)
            {
                var option = arg.Args[0];
                string vehicleType;
                if (!IsValidVehicleType(option, out vehicleType))
                {
                    Print(arg, $"{option} is not a valid vehicle type");
                    return;
                }
                switch (arg.Args[1].ToLower())
                {
                    case "*":
                    case "all":
                        {
                            storedData.RemoveLicenseForAllPlayers(vehicleType);
                            var vehicleName = GetBaseVehicleSettings(vehicleType).DisplayName;
                            Print(arg, $"You successfully removed the vehicle({vehicleName}) of all players");
                        }
                        return;

                    default:
                        {
                            var target = RustCore.FindPlayer(arg.Args[1]);
                            if (target == null)
                            {
                                Print(arg, $"Player '{arg.Args[1]}' not found");
                                return;
                            }

                            var vehicleName = GetBaseVehicleSettings(vehicleType).DisplayName;
                            if (RemoveVehicleLicense(target.userID, vehicleType))
                            {
                                Print(arg, $"You successfully removed the vehicle({vehicleName}) of {target.displayName}");
                                return;
                            }

                            Print(arg, $"{target.displayName} has not purchased vehicle({vehicleName}) and cannot be removed");
                        }
                        return;
                }
            }
        }

        [ConsoleCommand("vl.cleardata")]
        private void CCmdClearVehicle(ConsoleSystem.Arg arg)
        {
            if (arg.IsAdmin)
            {
                foreach (var vehicle in vehiclesCache.Keys.ToArray())
                {
                    vehicle.Kill(BaseNetworkable.DestroyMode.Gib);
                }
                vehiclesCache.Clear();
                ClearData();
                Print(arg, "You successfully cleaned up all vehicle data");
            }
        }

        #endregion Remove Command

        #region Buy Command

        [ConsoleCommand("vl.buy")]
        private void CCmdBuyVehicle(ConsoleSystem.Arg arg)
        {
            if (arg.IsAdmin && arg.Args != null && arg.Args.Length == 2)
            {
                var option = arg.Args[0];
                string vehicleType;
                if (!IsValidVehicleType(option, out vehicleType))
                {
                    Print(arg, $"{option} is not a valid vehicle type");
                    return;
                }
                switch (arg.Args[1].ToLower())
                {
                    case "*":
                    case "all":
                        {
                            storedData.AddLicenseForAllPlayers(vehicleType);
                            var vehicleName = GetBaseVehicleSettings(vehicleType).DisplayName;
                            Print(arg, $"You successfully purchased the vehicle({vehicleName}) for all players");
                        }
                        return;

                    default:
                        {
                            var target = RustCore.FindPlayer(arg.Args[1]);
                            if (target == null)
                            {
                                Print(arg, $"Player '{arg.Args[1]}' not found");
                                return;
                            }

                            var vehicleName = GetBaseVehicleSettings(vehicleType).DisplayName;
                            if (AddVehicleLicense(target.userID, vehicleType))
                            {
                                Print(arg, $"You successfully purchased the vehicle({vehicleName}) for {target.displayName}");
                                return;
                            }

                            Print(arg, $"{target.displayName} has purchased vehicle({vehicleName})");
                        }
                        return;
                }
            }
            var player = arg.Player();
            if (player == null)
            {
                Print(arg, $"The server console cannot use the '{arg.cmd.FullName}' command");
            }
            else
            {
                CmdBuyVehicle(player, arg.cmd.FullName, arg.Args);
            }
        }

        private void CmdBuyVehicle(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, PERMISSION_USE))
            {
                Print(player, Lang("NotAllowed", player.UserIDString));
                return;
            }
            if (args == null || args.Length < 1)
            {
                var stringBuilder = new StringBuilder();
                stringBuilder.AppendLine(Lang("Help", player.UserIDString));
                foreach (var entry in allVehicleSettings)
                {
                    if (CanViewVehicleInfo(player, entry.Key, entry.Value))
                    {
                        var firstCmd = entry.Value.Commands[0];
                        if (entry.Value.PurchasePrices.Count > 0)
                        {
                            var prices = FormatPriceInfo(player, entry.Value.PurchasePrices);
                            stringBuilder.AppendLine(Lang("HelpBuyPrice", player.UserIDString, configData.chat.buyCommand, firstCmd, entry.Value.DisplayName, prices));
                        }
                        else
                        {
                            stringBuilder.AppendLine(Lang("HelpBuy", player.UserIDString, configData.chat.buyCommand, firstCmd, entry.Value.DisplayName));
                        }
                    }
                }
                Print(player, stringBuilder.ToString());
                return;
            }
            string vehicleType;
            if (IsValidOption(player, args[0], out vehicleType))
            {
                BuyVehicle(player, vehicleType);
            }
        }

        private bool BuyVehicle(BasePlayer player, string vehicleType, bool response = true)
        {
            var settings = GetBaseVehicleSettings(vehicleType);
            if (!settings.Purchasable)
            {
                Print(player, Lang("VehicleCannotBeBought", player.UserIDString, settings.DisplayName));
                return false;
            }
            var vehicles = storedData.GetPlayerVehicles(player.userID, false);
            if (vehicles.ContainsKey(vehicleType))
            {
                Print(player, Lang("VehicleAlreadyPurchased", player.UserIDString, settings.DisplayName));
                return false;
            }
            string resources;
            if (settings.PurchasePrices.Count > 0 && !TryPay(player, settings, settings.PurchasePrices, out resources))
            {
                Print(player, Lang("NoResourcesToPurchaseVehicle", player.UserIDString, settings.DisplayName, resources));
                return false;
            }
            vehicles.Add(vehicleType, Vehicle.Create(player.userID, vehicleType));
            SaveData();
            if (response) Print(player, Lang("VehiclePurchased", player.UserIDString, settings.DisplayName, configData.chat.spawnCommand));
            return true;
        }

        #endregion Buy Command

        #region Spawn Command

        [ConsoleCommand("vl.spawn")]
        private void CCmdSpawnVehicle(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null)
            {
                Print(arg, $"The server console cannot use the '{arg.cmd.FullName}' command");
            }
            else
            {
                CmdSpawnVehicle(player, arg.cmd.FullName, arg.Args);
            }
        }

        private void CmdSpawnVehicle(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, PERMISSION_USE))
            {
                Print(player, Lang("NotAllowed", player.UserIDString));
                return;
            }
            if (args == null || args.Length < 1)
            {
                var stringBuilder = new StringBuilder();
                stringBuilder.AppendLine(Lang("Help", player.UserIDString));
                foreach (var entry in allVehicleSettings)
                {
                    if (CanViewVehicleInfo(player, entry.Key, entry.Value))
                    {
                        var firstCmd = entry.Value.Commands[0];
                        if (entry.Value.SpawnPrices.Count > 0)
                        {
                            var prices = FormatPriceInfo(player, entry.Value.SpawnPrices);
                            stringBuilder.AppendLine(Lang("HelpSpawnPrice", player.UserIDString, configData.chat.spawnCommand, firstCmd, entry.Value.DisplayName, prices));
                        }
                        else
                        {
                            stringBuilder.AppendLine(Lang("HelpSpawn", player.UserIDString, configData.chat.spawnCommand, firstCmd, entry.Value.DisplayName));
                        }
                    }
                }
                Print(player, stringBuilder.ToString());
                return;
            }
            string vehicleType;
            if (IsValidOption(player, args[0], out vehicleType))
            {
                var bypassCooldown = args.Length > 1 && IsValidBypassCooldownOption(args[1]);
                SpawnVehicle(player, vehicleType, bypassCooldown, command + " " + args[0]);
            }
        }

        private bool SpawnVehicle(BasePlayer player, string vehicleType, bool bypassCooldown, string command)
        {
            var settings = GetBaseVehicleSettings(vehicleType);
            Vehicle vehicle;
            if (!storedData.IsVehiclePurchased(player.userID, vehicleType, out vehicle))
            {
                if (!permission.UserHasPermission(player.UserIDString, PERMISSION_BYPASS_COST))
                {
                    Print(player, Lang("VehicleNotYetPurchased", player.UserIDString, settings.DisplayName, configData.chat.buyCommand));
                    return false;
                }
                BuyVehicle(player, vehicleType);
                vehicle = storedData.GetVehicleLicense(player.userID, vehicleType);
            }
            if (vehicle.Entity != null && !vehicle.Entity.IsDestroyed)
            {
                Print(player, Lang("AlreadyVehicleOut", player.UserIDString, settings.DisplayName, configData.chat.recallCommand));
                return false;
            }
            string reason;
            var position = Vector3.zero;
            var rotation = Quaternion.identity;
            if (CanSpawn(player, vehicle, bypassCooldown, command, out reason, ref position, ref rotation))
            {
                SpawnVehicle(player, vehicle, position, rotation);
                return false;
            }
            Print(player, reason);
            return true;
        }

        private bool CanSpawn(BasePlayer player, Vehicle vehicle, bool bypassCooldown, string command, out string reason, ref Vector3 position, ref Quaternion rotation)
        {

            var settings = GetBaseVehicleSettings(vehicle.VehicleType);
            // if (player.isInAir)
            // {
            //     reason = Lang("NoSpawnInAir", player.UserIDString, settings.DisplayName);
            //     return false;
            // }
            BaseEntity randomVehicle = null;
            if (configData.global.limitVehicles > 0)
            {
                var activeVehicles = storedData.ActiveVehicles(player.userID);
                var count = activeVehicles.Count();
                if (count >= configData.global.limitVehicles)
                {
                    if (configData.global.killVehicleLimited)
                    {
                        randomVehicle = activeVehicles.ElementAt(Random.Range(0, count));
                    }
                    else
                    {
                        reason = Lang("VehiclesLimit", player.UserIDString, configData.global.limitVehicles);
                        return false;
                    }
                }
            }
            if (!CanPlayerAction(player, vehicle, settings, out reason, ref position, ref rotation))
            {
                return false;
            }
            var obj = Interface.CallHook("CanLicensedVehicleSpawn", player, vehicle.VehicleType, position, rotation);
            if (obj != null)
            {
                var s = obj as string;
                reason = s ?? Lang("SpawnWasBlocked", player.UserIDString, settings.DisplayName);
                return false;
            }

#if DEBUG
            if (player.IsAdmin)
            {
                reason = null;
                return true;
            }
#endif
            if (!CheckCooldown(player, vehicle, settings, bypassCooldown, true, command, out reason))
            {
                return false;
            }

            string resources;
            if (settings.SpawnPrices.Count > 0 && !TryPay(player, settings, settings.SpawnPrices, out resources))
            {
                reason = Lang("NoResourcesToSpawnVehicle", player.UserIDString, settings.DisplayName, resources);
                return false;
            }

            // This prevents horse spawns/recalls as well
            if (!configData.CanSpawnInZones && InZone(player))
            {
                reason = Lang("NoSpawnInZone", player.UserIDString, settings.DisplayName);
                return false;
            }

            if (randomVehicle != null)
            {
                randomVehicle.Kill(BaseNetworkable.DestroyMode.Gib);
            }
            reason = null;
            return true;
        }

        private void SpawnVehicle(BasePlayer player, Vehicle vehicle, Vector3 position, Quaternion rotation, bool response = true)
        {
            var settings = GetBaseVehicleSettings(vehicle.VehicleType);
            var entity = settings.SpawnVehicle(player, vehicle, position, rotation);
            if (entity == null)
            {
                return;
            }

            Interface.CallHook("OnLicensedVehicleSpawned", entity, player, vehicle.VehicleType);
            if (!response) return;
            Print(player, Lang("VehicleSpawned", player.UserIDString, settings.DisplayName));
        }

        private void CacheVehicleEntity(BaseEntity entity, Vehicle vehicle, BasePlayer player)
        {
            vehicle.PlayerId = player.userID;
            vehicle.VehicleType = vehicle.VehicleType;
            vehicle.Entity = entity;
            vehicle.EntityId = entity.net.ID.Value;
            vehicle.LastDismount = vehicle.LastRecall = TimeEx.currentTimestamp;
            vehiclesCache[entity] = vehicle;
        }

        #endregion Spawn Command

        #region Recall Command

        [ConsoleCommand("vl.recall")]
        private void CCmdRecallVehicle(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null)
            {
                Print(arg, $"The server console cannot use the '{arg.cmd.FullName}' command");
            }
            else
            {
                CmdRecallVehicle(player, arg.cmd.FullName, arg.Args);
            }
        }

        private void CmdRecallVehicle(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, PERMISSION_USE))
            {
                Print(player, Lang("NotAllowed", player.UserIDString));
                return;
            }
            if (args == null || args.Length < 1)
            {
                var stringBuilder = new StringBuilder();
                stringBuilder.AppendLine(Lang("Help", player.UserIDString));
                foreach (var entry in allVehicleSettings)
                {
                    if (CanViewVehicleInfo(player, entry.Key, entry.Value))
                    {
                        var firstCmd = entry.Value.Commands[0];
                        if (entry.Value.RecallPrices.Count > 0)
                        {
                            var prices = FormatPriceInfo(player, entry.Value.RecallPrices);
                            stringBuilder.AppendLine(Lang("HelpRecallPrice", player.UserIDString, configData.chat.recallCommand, firstCmd, entry.Value.DisplayName, prices));
                        }
                        else
                        {
                            stringBuilder.AppendLine(Lang("HelpRecall", player.UserIDString, configData.chat.recallCommand, firstCmd, entry.Value.DisplayName));
                        }
                    }
                }
                Print(player, stringBuilder.ToString());
                return;
            }
            string vehicleType;
            if (IsValidOption(player, args[0], out vehicleType))
            {
                var bypassCooldown = args.Length > 1 && IsValidBypassCooldownOption(args[1]);
                RecallVehicle(player, vehicleType, bypassCooldown, command + " " + args[0]);
            }
        }

        private bool RecallVehicle(BasePlayer player, string vehicleType, bool bypassCooldown, string command)
        {
            var settings = GetBaseVehicleSettings(vehicleType);
            Vehicle vehicle;
            if (!storedData.IsVehiclePurchased(player.userID, vehicleType, out vehicle))
            {
                Print(player, Lang("VehicleNotYetPurchased", player.UserIDString, settings.DisplayName, configData.chat.buyCommand));
                return false;
            }
            if (vehicle.Entity != null && !vehicle.Entity.IsDestroyed)
            {
                string reason;
                var position = Vector3.zero;
                var rotation = Quaternion.identity;
                if (CanRecall(player, vehicle, bypassCooldown, command, out reason, ref position, ref rotation))
                {
                    RecallVehicle(player, vehicle, position, rotation);
                    return true;
                }
                Print(player, reason);
                return false;
            }
            Print(player, Lang("VehicleNotOut", player.UserIDString, settings.DisplayName, configData.chat.spawnCommand));
            return false;
        }

        private bool CanRecall(BasePlayer player, Vehicle vehicle, bool bypassCooldown, string command, out string reason, ref Vector3 position, ref Quaternion rotation)
        {
            var settings = GetBaseVehicleSettings(vehicle.VehicleType);
            // if (player.isInAir)
            // {
            //     reason = Lang("NoSpawnInAir", player.UserIDString, settings.DisplayName);
            //     return false;
            // }
            if (settings.RecallMaxDistance > 0 && Vector3.Distance(player.transform.position, vehicle.Entity.transform.position) > settings.RecallMaxDistance)
            {
                reason = Lang("RecallTooFar", player.UserIDString, settings.RecallMaxDistance, settings.DisplayName);
                return false;
            }
            if (configData.global.anyMountedRecall && VehicleAnyMounted(vehicle.Entity))
            {
                reason = Lang("PlayerMountedOnVehicle", player.UserIDString, settings.DisplayName);
                return false;
            }
            if (!CanPlayerAction(player, vehicle, settings, out reason, ref position, ref rotation))
            {
                return false;
            }

            var obj = Interface.CallHook("CanLicensedVehicleRecall", vehicle.Entity, player, vehicle.VehicleType, position, rotation);
            if (obj != null)
            {
                var s = obj as string;
                reason = s ?? Lang("RecallWasBlocked", player.UserIDString, settings.DisplayName);
                return false;
            }
#if DEBUG
            if (player.IsAdmin)
            {
                reason = null;
                return true;
            }
#endif
            if (!CheckCooldown(player, vehicle, settings, bypassCooldown, false, command, out reason))
            {
                return false;
            }
            string resources;
            if (settings.RecallPrices.Count > 0 && !TryPay(player, settings, settings.RecallPrices, out resources))
            {
                reason = Lang("NoResourcesToRecallVehicle", player.UserIDString, settings.DisplayName, resources);
                return false;
            }

            // This prevents horse spawns/recalls as well
            if (!configData.CanSpawnInZones && InZone(player))
            {
                reason = Lang("NoRecallInZone", player.UserIDString, settings.DisplayName);
                return false;
            }
            reason = null;
            return true;
        }

        private void RecallVehicle(BasePlayer player, Vehicle vehicle, Vector3 position, Quaternion rotation)
        {
            var settings = GetBaseVehicleSettings(vehicle.VehicleType);
            settings.PreRecallVehicle(player, vehicle, position, rotation);
            BaseEntity vehicleEntity = vehicle.Entity;

            if (vehicleEntity.IsOn()) vehicleEntity.SetFlag(BaseEntity.Flags.On, false);
            if (vehicleEntity is TrainEngine)
            {
                TrainEngine train = vehicleEntity as TrainEngine;
                train.completeTrain.trackSpeed = 0;
            }
            else
            {
                vehicleEntity.SetVelocity(Vector3.zero);
                vehicleEntity.SetAngularVelocity(Vector3.zero);
            }
            vehicleEntity.transform.SetPositionAndRotation(position, rotation);
            vehicleEntity.transform.hasChanged = true;
            vehicleEntity.UpdateNetworkGroup();
            vehicleEntity.SendNetworkUpdateImmediate();


            settings.PostRecallVehicle(player, vehicle, position, rotation);
            vehicle.OnRecall();

            if (vehicleEntity == null || vehicleEntity.IsDestroyed)
            {
                Print(player, Lang("NotSpawnedOrRecalled", player.UserIDString, settings.DisplayName));
                return;
            }

            Interface.CallHook("OnLicensedVehicleRecalled", vehicleEntity, player, vehicle.VehicleType);
            Print(player, Lang("VehicleRecalled", player.UserIDString, settings.DisplayName));
        }

        #endregion Recall Command

        #region Kill Command

        [ConsoleCommand("vl.kill")]
        private void CCmdKillVehicle(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null)
            {
                Print(arg, $"The server console cannot use the '{arg.cmd.FullName}' command");
            }
            else
            {
                CmdKillVehicle(player, arg.cmd.FullName, arg.Args);
            }
        }

        private void CmdKillVehicle(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, PERMISSION_USE))
            {
                Print(player, Lang("NotAllowed", player.UserIDString));
                return;
            }
            if (args == null || args.Length < 1)
            {
                var stringBuilder = new StringBuilder();
                stringBuilder.AppendLine(Lang("Help", player.UserIDString));
                foreach (var entry in allVehicleSettings)
                {
                    if (CanViewVehicleInfo(player, entry.Key, entry.Value))
                    {
                        var firstCmd = entry.Value.Commands[0];
                        if (!string.IsNullOrEmpty(configData.chat.customKillCommandPrefix))
                        {
                            stringBuilder.AppendLine(Lang("HelpKillCustom", player.UserIDString, configData.chat.killCommand, firstCmd, configData.chat.customKillCommandPrefix + firstCmd, entry.Value.DisplayName));
                        }
                        else
                        {
                            stringBuilder.AppendLine(Lang("HelpKill", player.UserIDString, configData.chat.killCommand, firstCmd, entry.Value.DisplayName));
                        }
                    }
                }
                Print(player, stringBuilder.ToString());
                return;
            }

            HandleKillCmd(player, args[0]);
        }

        private void HandleKillCmd(BasePlayer player, string option)
        {
            string vehicleType;
            if (IsValidOption(player, option, out vehicleType))
            {
                KillVehicle(player, vehicleType);
            }
        }

        private bool KillVehicle(BasePlayer player, string vehicleType, bool response = true)
        {
            var settings = GetBaseVehicleSettings(vehicleType);
            Vehicle vehicle;
            if (!storedData.IsVehiclePurchased(player.userID, vehicleType, out vehicle))
            {
                Print(player, Lang("VehicleNotYetPurchased", player.UserIDString, settings.DisplayName, configData.chat.buyCommand));
                return false;
            }
            if (vehicle.Entity != null && !vehicle.Entity.IsDestroyed)
            {
                if (!CanKill(player, vehicle, settings))
                {
                    return false;
                }
                vehicle.Entity.Kill(BaseNetworkable.DestroyMode.Gib);
                if (!response) return true;
                Print(player, Lang("VehicleKilled", player.UserIDString, settings.DisplayName));
                return true;
            }
            Print(player, Lang("VehicleNotOut", player.UserIDString, settings.DisplayName, configData.chat.spawnCommand));
            return false;
        }

        private bool CanKill(BasePlayer player, Vehicle vehicle, BaseVehicleSettings settings)
        {
            if (configData.global.anyMountedKill && VehicleAnyMounted(vehicle.Entity))
            {
                Print(player, Lang("PlayerMountedOnVehicle", player.UserIDString, settings.DisplayName));
                return false;
            }
            if (settings.KillMaxDistance > 0 && Vector3.Distance(player.transform.position, vehicle.Entity.transform.position) > settings.KillMaxDistance)
            {
                Print(player, Lang("KillTooFar", player.UserIDString, settings.KillMaxDistance, settings.DisplayName));
                return false;
            }

            return true;
        }

        #endregion Kill Command

        #region Command Helpers

        private bool IsValidBypassCooldownOption(string option)
        {
            return !string.IsNullOrEmpty(configData.chat.bypassCooldownCommand) &&
                    string.Equals(option, configData.chat.bypassCooldownCommand, StringComparison.OrdinalIgnoreCase);
        }

        private bool IsValidOption(BasePlayer player, string option, out string vehicleType)
        {
            if (!commandToVehicleType.TryGetValue(option, out vehicleType))
            {
                Print(player, Lang("OptionNotFound", player.UserIDString, option));
                return false;
            }
            if (!HasVehiclePermission(player, vehicleType))
            {
                Print(player, Lang("NotAllowed", player.UserIDString));
                vehicleType = null;
                return false;
            }
            if (IsPlayerBlocked(player))
            {
                vehicleType = null;
                return false;
            }
            return true;
        }

        private bool IsValidVehicleType(string option, out string vehicleType)
        {
            foreach (var entry in allVehicleSettings)
            {
                if (string.Equals(entry.Key, option, StringComparison.OrdinalIgnoreCase))
                {
                    vehicleType = entry.Key;
                    return true;
                }
            }

            vehicleType = null;
            return false;
        }

        private string FormatPriceInfo(BasePlayer player, Dictionary<string, PriceInfo> prices)
        {
            var language = RustTranslationAPI != null ? lang.GetLanguage(player.UserIDString) : null;
            return string.Join(", ", from p in prices
                                     select Lang("PriceFormat", player.UserIDString, GetItemDisplayName(language, p.Key, p.Value.displayName), p.Value.amount));
        }

        private bool CanPlayerAction(BasePlayer player, Vehicle vehicle, BaseVehicleSettings settings, out string reason, ref Vector3 position, ref Quaternion rotation)
        {
            if (configData.global.preventBuildingBlocked && player.IsBuildingBlocked())
            {
                reason = Lang("BuildingBlocked", player.UserIDString, settings.DisplayName);
                return false;
            }
            if (configData.global.preventSafeZone && player.InSafeZone())
            {
                reason = Lang("PlayerInSafeZone", player.UserIDString, settings.DisplayName);
                return false;
            }
            if (configData.global.preventMountedOrParented && HasMountedOrParented(player, settings))
            {
                reason = Lang("MountedOrParented", player.UserIDString, settings.DisplayName);
                return false;
            }
            if (!settings.TryGetVehicleParams(player, vehicle, out reason, ref position, ref rotation))
            {
                return false;
            }
            reason = null;
            return true;
        }

        private bool HasMountedOrParented(BasePlayer player, BaseVehicleSettings settings)
        {
            if (player.GetMountedVehicle() != null)
            {
                return true;
            }
            var parentEntity = player.GetParentEntity();
            if (parentEntity != null)
            {
                if (configData.global.spawnLookingAt)
                {
                    if (LandOnCargoShip != null && parentEntity is CargoShip && settings.IsFightVehicle)
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }

        private bool CheckCooldown(BasePlayer player, Vehicle vehicle, BaseVehicleSettings settings, bool bypassCooldown, bool isSpawnCooldown, string command, out string reason)
        {
            var cooldown = settings.GetCooldown(player, isSpawnCooldown);
            if (cooldown > 0)
            {
                var timeLeft = Math.Ceiling(cooldown - (TimeEx.currentTimestamp - (isSpawnCooldown ? vehicle.LastDeath : vehicle.LastRecall)));
                if (timeLeft > 0)
                {
                    var bypassPrices = isSpawnCooldown ? settings.BypassSpawnCooldownPrices : settings.BypassRecallCooldownPrices;
                    if (bypassCooldown && bypassPrices.Count > 0)
                    {
                        string resources;
                        if (!TryPay(player, settings, bypassPrices, out resources))
                        {
                            reason = Lang(isSpawnCooldown ? "NoResourcesToSpawnVehicleBypass" : "NoResourcesToRecallVehicleBypass", player.UserIDString, settings.DisplayName, resources);
                            return false;
                        }

                        if (isSpawnCooldown)
                        {
                            vehicle.LastDeath = 0;
                        }
                        else
                        {
                            vehicle.LastRecall = 0;
                        }
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(configData.chat.bypassCooldownCommand) || bypassPrices.Count <= 0)
                        {
                            reason = Lang(isSpawnCooldown ? "VehicleOnSpawnCooldown" : "VehicleOnRecallCooldown", player.UserIDString, timeLeft, settings.DisplayName);
                        }
                        else
                        {
                            reason = Lang(isSpawnCooldown ? "VehicleOnSpawnCooldownPay" : "VehicleOnRecallCooldownPay", player.UserIDString, timeLeft, settings.DisplayName,
                                          command + " " + configData.chat.bypassCooldownCommand,
                                          FormatPriceInfo(player, isSpawnCooldown ? settings.BypassSpawnCooldownPrices : settings.BypassRecallCooldownPrices));
                        }
                        return false;
                    }
                }
            }
            reason = null;
            return true;
        }

        #endregion Command Helpers

        #endregion Commands

        #region RustTranslationAPI

        private string GetItemTranslationByShortName(string language, string itemShortName)
        {
            return (string)RustTranslationAPI.Call("GetItemTranslationByShortName", language, itemShortName);
        }

        private string GetItemDisplayName(string language, string itemShortName, string displayName)
        {
            if (RustTranslationAPI != null)
            {
                var displayName1 = GetItemTranslationByShortName(language, itemShortName);
                if (!string.IsNullOrEmpty(displayName1))
                {
                    return displayName1;
                }
            }
            return displayName;
        }

        #endregion RustTranslationAPI

        #region ConfigurationFile

        public ConfigData configData { get; private set; }

        public class ConfigData
        {
            [JsonProperty(PropertyName = "Settings")]
            public GlobalSettings global = new GlobalSettings();

            [JsonProperty(PropertyName = "Chat Settings")]
            public ChatSettings chat = new ChatSettings();

            [JsonProperty("Allow vehicles to be spawned/recalled in zones listed in prevent spawning zones")]
            public bool CanSpawnInZones = false;

            [JsonProperty(PropertyName = "Zones to prevent users from spawning/recalled vehicles within.", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<string> AntiSpawnZones = new List<string> { "KeepVehiclesOut" };

            [JsonProperty(PropertyName = "Normal Vehicle Settings")]
            public NormalVehicleSettings normalVehicles = new NormalVehicleSettings();

            [JsonProperty(PropertyName = "Modular Vehicle Settings", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public Dictionary<string, ModularVehicleSettings> modularVehicles = new Dictionary<string, ModularVehicleSettings>
            {
                ["SmallCar"] = new ModularVehicleSettings
                {
                    Purchasable = true,
                    NoDamage = false,
                    NoCollisionDamage = false,
                    DisplayName = "Small Modular Car",
                    Distance = 5,
                    MinDistanceForPlayers = 3,
                    UsePermission = true,
                    Permission = "vehiclelicence.smallmodularcar",
                    BypassCostPermission = "vehiclelicence.smallmodularcarfree",
                    Commands = new List<string> { "small", "smallcar" },
                    PurchasePrices = new Dictionary<string, PriceInfo>
                    {
                        ["scrap"] = new PriceInfo { amount = 1600, displayName = "Scrap" }
                    },
                    SpawnPrices = new Dictionary<string, PriceInfo>
                    {
                        ["metal.refined"] = new PriceInfo { amount = 10, displayName = "High Quality Metal" }
                    },
                    RecallPrices = new Dictionary<string, PriceInfo>
                    {
                        ["scrap"] = new PriceInfo { amount = 5, displayName = "Scrap" }
                    },
                    SpawnCooldown = 7200,
                    RecallCooldown = 30,
                    CooldownPermissions = new Dictionary<string, CooldownPermission>
                    {
                        ["vehiclelicence.vip"] = new CooldownPermission
                        {
                            spawnCooldown = 3600,
                            recallCooldown = 10
                        }
                    },
                    ChassisType = ChassisType.Small,
                    ModuleItems = new List<ModuleItem>
                    {
                        new ModuleItem
                        {
                            shortName = "vehicle.1mod.cockpit.with.engine", healthPercentage = 50f
                        },
                        new ModuleItem
                        {
                            shortName = "vehicle.1mod.storage", healthPercentage = 50f
                        }
                    },
                    EngineItems = new List<EngineItem>
                    {
                        new EngineItem
                        {
                            shortName = "carburetor1", conditionPercentage = 20f
                        },
                        new EngineItem
                        {
                            shortName = "crankshaft1", conditionPercentage = 20f
                        },
                        new EngineItem
                        {
                            shortName = "piston1", conditionPercentage = 20f
                        },
                        new EngineItem
                        {
                            shortName = "sparkplug1", conditionPercentage = 20f
                        },
                        new EngineItem
                        {
                            shortName = "valve1", conditionPercentage = 20f
                        }
                    }
                },
                ["MediumCar"] = new ModularVehicleSettings
                {
                    Purchasable = true,
                    NoDamage = false,
                    NoCollisionDamage = false,
                    DisplayName = "Medium Modular Car",
                    Distance = 5,
                    MinDistanceForPlayers = 3,
                    UsePermission = true,
                    Permission = "vehiclelicence.mediumodularcar",
                    BypassCostPermission = "vehiclelicence.mediumodularcarfree",
                    Commands = new List<string> { "medium", "mediumcar" },
                    PurchasePrices = new Dictionary<string, PriceInfo>
                    {
                        ["scrap"] = new PriceInfo { amount = 2400, displayName = "Scrap" }
                    },
                    SpawnPrices = new Dictionary<string, PriceInfo>
                    {
                        ["metal.refined"] = new PriceInfo { amount = 50, displayName = "High Quality Metal" }
                    },
                    RecallPrices = new Dictionary<string, PriceInfo>
                    {
                        ["scrap"] = new PriceInfo { amount = 8, displayName = "Scrap" }
                    },
                    SpawnCooldown = 9000,
                    RecallCooldown = 30,
                    CooldownPermissions = new Dictionary<string, CooldownPermission>
                    {
                        ["vehiclelicence.vip"] = new CooldownPermission
                        {
                            spawnCooldown = 4500,
                            recallCooldown = 10
                        }
                    },
                    ChassisType = ChassisType.Medium,
                    ModuleItems = new List<ModuleItem>
                    {
                        new ModuleItem
                        {
                            shortName = "vehicle.1mod.cockpit.with.engine", healthPercentage = 50f
                        },
                        new ModuleItem
                        {
                            shortName = "vehicle.1mod.rear.seats", healthPercentage = 50f
                        },
                        new ModuleItem
                        {
                            shortName = "vehicle.1mod.flatbed", healthPercentage = 50f
                        }
                    },
                    EngineItems = new List<EngineItem>
                    {
                        new EngineItem
                        {
                            shortName = "carburetor2", conditionPercentage = 20f
                        },
                        new EngineItem
                        {
                            shortName = "crankshaft2", conditionPercentage = 20f
                        },
                        new EngineItem
                        {
                            shortName = "piston2", conditionPercentage = 20f
                        },
                        new EngineItem
                        {
                            shortName = "sparkplug2", conditionPercentage = 20f
                        },
                        new EngineItem
                        {
                            shortName = "valve2", conditionPercentage = 20f
                        }
                    }
                },
                ["LargeCar"] = new ModularVehicleSettings
                {
                    Purchasable = true,
                    NoDamage = false,
                    NoCollisionDamage = false,
                    DisplayName = "Large Modular Car",
                    Distance = 6,
                    MinDistanceForPlayers = 3,
                    UsePermission = true,
                    Permission = "vehiclelicence.largemodularcar",
                    BypassCostPermission = "vehiclelicence.largemodularcarfree",
                    Commands = new List<string> { "large", "largecar" },
                    PurchasePrices = new Dictionary<string, PriceInfo>
                    {
                        ["scrap"] = new PriceInfo { amount = 3000, displayName = "Scrap" }
                    },
                    SpawnPrices = new Dictionary<string, PriceInfo>
                    {
                        ["metal.refined"] = new PriceInfo { amount = 100, displayName = "High Quality Metal" }
                    },
                    RecallPrices = new Dictionary<string, PriceInfo>
                    {
                        ["scrap"] = new PriceInfo { amount = 10, displayName = "Scrap" }
                    },
                    SpawnCooldown = 10800,
                    RecallCooldown = 30,
                    CooldownPermissions = new Dictionary<string, CooldownPermission>
                    {
                        ["vehiclelicence.vip"] = new CooldownPermission
                        {
                            spawnCooldown = 5400,
                            recallCooldown = 10
                        }
                    },
                    ChassisType = ChassisType.Large,
                    ModuleItems = new List<ModuleItem>
                    {
                        new ModuleItem
                        {
                            shortName = "vehicle.1mod.engine", healthPercentage = 50f
                        },
                        new ModuleItem
                        {
                            shortName = "vehicle.1mod.cockpit.armored", healthPercentage = 50f
                        },
                        new ModuleItem
                        {
                            shortName = "vehicle.1mod.passengers.armored", healthPercentage = 50f
                        },
                        new ModuleItem
                        {
                            shortName = "vehicle.1mod.storage", healthPercentage = 50f
                        }
                    },
                    EngineItems = new List<EngineItem>
                    {
                        new EngineItem
                        {
                            shortName = "carburetor3", conditionPercentage = 10f
                        },
                        new EngineItem
                        {
                            shortName = "crankshaft3", conditionPercentage = 10f
                        },
                        new EngineItem
                        {
                            shortName = "piston3", conditionPercentage = 10f
                        },
                        new EngineItem
                        {
                            shortName = "piston3", conditionPercentage = 10f
                        },
                        new EngineItem
                        {
                            shortName = "sparkplug3", conditionPercentage = 10f
                        },
                        new EngineItem
                        {
                            shortName = "sparkplug3", conditionPercentage = 10f
                        },
                        new EngineItem
                        {
                            shortName = "valve3", conditionPercentage = 10f
                        },
                        new EngineItem
                        {
                            shortName = "valve3", conditionPercentage = 10f
                        }
                    }
                }
            };

            [JsonProperty(PropertyName = "Train Vehicle Settings", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public Dictionary<string, TrainVehicleSettings> trainVehicles = new Dictionary<string, TrainVehicleSettings>
            {
                ["WorkCartAboveGround"] = new TrainVehicleSettings
                {
                    Purchasable = true,
                    NoDamage = false,
                    NoCollisionDamage = false,
                    DisplayName = "Work Cart Above Ground",
                    Distance = 12,
                    MinDistanceForPlayers = 6,
                    UsePermission = true,
                    Permission = "vehiclelicence.workcartaboveground",
                    BypassCostPermission = "vehiclelicence.workcartabovegroundfree",
                    Commands = new List<string>
                    {
                        "cartground", "workcartground"
                    },
                    PurchasePrices = new Dictionary<string, PriceInfo>
                    {
                        ["scrap"] = new PriceInfo { amount = 2000, displayName = "Scrap" }
                    },
                    SpawnCooldown = 1800,
                    RecallCooldown = 30,
                    CooldownPermissions = new Dictionary<string, CooldownPermission>
                    {
                        ["vehiclelicence.vip"] = new CooldownPermission
                        {
                            spawnCooldown = 900,
                            recallCooldown = 10
                        }
                    },
                    TrainComponents = new List<TrainComponent>
                    {
                        new TrainComponent { type = TrainComponentType.Engine }
                    }
                },
                ["WorkCartCovered"] = new TrainVehicleSettings
                {
                    Purchasable = true,
                    NoDamage = false,
                    NoCollisionDamage = false,
                    DisplayName = "Covered Work Cart",
                    Distance = 12,
                    MinDistanceForPlayers = 6,
                    UsePermission = true,
                    Permission = "vehiclelicence.coveredworkcart",
                    BypassCostPermission = "vehiclelicence.coveredworkcartfree",
                    Commands = new List<string>
                    {
                        "cartcovered", "coveredworkcart"
                    },
                    PurchasePrices = new Dictionary<string, PriceInfo>
                    {
                        ["scrap"] = new PriceInfo { amount = 2000, displayName = "Scrap" }
                    },
                    SpawnCooldown = 1800,
                    RecallCooldown = 30,
                    CooldownPermissions = new Dictionary<string, CooldownPermission>
                    {
                        ["vehiclelicence.vip"] = new CooldownPermission
                        {
                            spawnCooldown = 900,
                            recallCooldown = 10
                        }
                    },
                    TrainComponents = new List<TrainComponent>
                    {
                        new TrainComponent { type = TrainComponentType.CoveredEngine }
                    }
                },
                ["CompleteTrain"] = new TrainVehicleSettings
                {
                    Purchasable = true,
                    NoDamage = false,
                    NoCollisionDamage = false,
                    DisplayName = "Complete Train",
                    Distance = 12,
                    MinDistanceForPlayers = 6,
                    UsePermission = true,
                    Permission = "vehiclelicence.completetrain",
                    BypassCostPermission = "vehiclelicence.completetrainfree",
                    Commands = new List<string>
                    {
                        "ctrain", "completetrain"
                    },
                    PurchasePrices = new Dictionary<string, PriceInfo>
                    {
                        ["scrap"] = new PriceInfo { amount = 6000, displayName = "Scrap" }
                    },
                    SpawnCooldown = 3600,
                    RecallCooldown = 60,
                    CooldownPermissions = new Dictionary<string, CooldownPermission>
                    {
                        ["vehiclelicence.vip"] = new CooldownPermission
                        {
                            spawnCooldown = 900,
                            recallCooldown = 10
                        }
                    },
                    TrainComponents = new List<TrainComponent>
                    {
                        new TrainComponent
                        {
                            type = TrainComponentType.Engine
                        },
                        new TrainComponent
                        {
                            type = TrainComponentType.WagonA
                        },
                        new TrainComponent
                        {
                            type = TrainComponentType.WagonB
                        },
                        new TrainComponent
                        {
                            type = TrainComponentType.WagonC
                        },
                        new TrainComponent
                        {
                            type = TrainComponentType.Unloadable
                        },
                        new TrainComponent
                        {
                            type = TrainComponentType.UnloadableLoot
                        }
                    }
                },
                ["Locomotive"] = new TrainVehicleSettings
                {
                    Purchasable = true,
                    NoDamage = false,
                    NoCollisionDamage = false,
                    DisplayName = "Locomotive",
                    Distance = 12,
                    MinDistanceForPlayers = 6,
                    UsePermission = true,
                    Permission = "vehiclelicence.locomotive",
                    BypassCostPermission = "vehiclelicence.locomotivefree",
                    Commands = new List<string>
                    {
                        "loco", "locomotive"
                    },
                    PurchasePrices = new Dictionary<string, PriceInfo>
                    {
                        ["scrap"] = new PriceInfo { amount = 2000, displayName = "Scrap" }
                    },
                    SpawnCooldown = 1800,
                    RecallCooldown = 30,
                    CooldownPermissions = new Dictionary<string, CooldownPermission>
                    {
                        ["vehiclelicence.vip"] = new CooldownPermission
                        {
                            spawnCooldown = 900,
                            recallCooldown = 10
                        }
                    },
                    TrainComponents = new List<TrainComponent>
                    {
                        new TrainComponent { type = TrainComponentType.Locomotive }
                    }
                }
            };
            
            [DefaultValue(null)]
            [JsonProperty(PropertyName = "Custom Vehicle Settings", DefaultValueHandling = DefaultValueHandling.Ignore)]
            public CustomVehicleSettings customVehicles = null;

            [JsonProperty(PropertyName = "Version")]
            public VersionNumber version;
        }

        public class ChatSettings
        {
            [JsonProperty(PropertyName = "Use Universal Chat Command")]
            public bool useUniversalCommand = true;

            [JsonProperty(PropertyName = "Help Chat Command")]
            public string helpCommand = "license";

            [JsonProperty(PropertyName = "Buy Chat Command")]
            public string buyCommand = "buy";

            [JsonProperty(PropertyName = "Spawn Chat Command")]
            public string spawnCommand = "spawn";

            [JsonProperty(PropertyName = "Recall Chat Command")]
            public string recallCommand = "recall";

            [JsonProperty(PropertyName = "Kill Chat Command")]
            public string killCommand = "kill";

            [JsonProperty(PropertyName = "Custom Kill Chat Command Prefix")]
            public string customKillCommandPrefix = "no";

            [JsonProperty(PropertyName = "Bypass Cooldown Command")]
            public string bypassCooldownCommand = "pay";

            [JsonProperty(PropertyName = "Chat Prefix")]
            public string prefix = "<color=#00FFFF>[VehicleLicense]</color>: ";

            [JsonProperty(PropertyName = "Chat SteamID Icon")]
            public ulong steamIDIcon = 76561198924840872;
        }

        public class GlobalSettings
        {
            [JsonProperty(PropertyName = "Store Vehicle On Plugin Unloaded / Server Restart")]
            public bool storeVehicle = true;

            [JsonProperty(PropertyName = "Clear Vehicle Data On Map Wipe")]
            public bool clearVehicleOnWipe;

            [JsonProperty(PropertyName = "Interval to check vehicle for wipe (Seconds)")]
            public float checkVehiclesInterval = 300;

            [JsonProperty(PropertyName = "Spawn vehicle in the direction you are looking at")]
            public bool spawnLookingAt = true;

            [JsonProperty(PropertyName = "Automatically claim vehicles purchased from vehicle vendors")]
            public bool autoClaimFromVendor;

            [JsonProperty(PropertyName = "Vehicle vendor purchases will unlock the license for the player")]
            public bool autoUnlockFromVendor;

            [JsonProperty(PropertyName = "Limit the number of vehicles at a time")]
            public int limitVehicles;

            [JsonProperty(PropertyName = "Kill a random vehicle when the number of vehicles is limited")]
            public bool killVehicleLimited;

            [JsonProperty(PropertyName = "Prevent vehicles from damaging players")]
            public bool preventDamagePlayer = true;

            [JsonProperty(PropertyName = "Prevent vehicles from damaging NPCs")]
            public bool preventDamageNPCs = false;

            [JsonProperty(PropertyName = "Safe dismount players who jump off train")]
            public bool safeTrainDismount = true;

            [JsonProperty(PropertyName = "Prevent vehicles from shattering")]
            public bool preventShattering = true;

            [JsonProperty(PropertyName = "Prevent vehicles from spawning or recalling in safe zone")]
            public bool preventSafeZone = true;

            [JsonProperty(PropertyName = "Prevent vehicles from spawning or recalling when the player are building blocked")]
            public bool preventBuildingBlocked = true;

            [JsonProperty(PropertyName = "Prevent vehicles from spawning or recalling when the player is mounted or parented")]
            public bool preventMountedOrParented = true;

            [JsonProperty(PropertyName = "Check if any player mounted when recalling a vehicle")]
            public bool anyMountedRecall = true;

            [JsonProperty(PropertyName = "Check if any player mounted when killing a vehicle")]
            public bool anyMountedKill;

            [JsonProperty(PropertyName = "Dismount all players when a vehicle is recalled")]
            public bool dismountAllPlayersRecall = true;

            [JsonProperty(PropertyName = "Prevent other players from mounting vehicle")]
            public bool preventMounting = true;

            [JsonProperty(PropertyName = "Prevent mounting on driver's seat only")]
            public bool preventDriverSeat = true;

            [JsonProperty(PropertyName = "Prevent other players from looting fuel container and inventory")]
            public bool preventLooting = true;

            [JsonProperty(PropertyName = "Prevent other players from pushing vehicles they do not own")]
            public bool preventPushing = false;

            [JsonProperty(PropertyName = "Use Teams")]
            public bool useTeams;

            [JsonProperty(PropertyName = "Use Clans")]
            public bool useClans = true;

            [JsonProperty(PropertyName = "Use Friends")]
            public bool useFriends = true;

            [JsonProperty(PropertyName = "Vehicle No Decay")]
            public bool noDecay;

            [JsonProperty(PropertyName = "Vehicle No Fire Ball")]
            public bool noFireBall = true;

            [JsonProperty(PropertyName = "Vehicle No Server Gibs")]
            public bool noServerGibs = true;

            [JsonProperty(PropertyName = "Chinook No Map Marker")]
            public bool noMapMarker = true;

            [JsonProperty(PropertyName = "Use Raid Blocker (Need NoEscape Plugin)")]
            public bool useRaidBlocker;

            [JsonProperty(PropertyName = "Use Combat Blocker (Need NoEscape Plugin)")]
            public bool useCombatBlocker;

            [JsonProperty(PropertyName = "Populate the config with Custom Vehicles (CANNOT BE UNDONE! Will make config much larger)")]
            public bool useCustomVehicles;
        }

        public class NormalVehicleSettings
        {
            [JsonProperty(PropertyName = "Tugboat Vehicle", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public TugboatSettings tugboat = new TugboatSettings
            {
                Purchasable = true,
                NoDamage = false,
                NoCollisionDamage = false,
                DisplayName = "Tugboat",
                speedMultiplier = 1,
                autoAuth = true,
                Distance = 10,
                MinDistanceForPlayers = 3,
                UsePermission = true,
                Permission = "vehiclelicence.tug",
                BypassCostPermission = "vehiclelicence.tugfree",
                Commands = new List<string> { "tugboat", "tug" },
                PurchasePrices = new Dictionary<string, PriceInfo>
                {
                    ["scrap"] = new PriceInfo
                    {
                        amount = 10000,
                        displayName = "Scrap"
                    }
                },
                SpawnCooldown = 450,
                RecallCooldown = 30,
                CooldownPermissions = new Dictionary<string, CooldownPermission>
                {
                    ["vehiclelicence.vip"] = new CooldownPermission
                    {
                        spawnCooldown = 60,
                        recallCooldown = 10
                    }
                }
            };
            [JsonProperty(PropertyName = "Sedan Vehicle", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public SedanSettings sedan = new SedanSettings
            {
                Purchasable = true,
                NoDamage = false,
                NoCollisionDamage = false,
                DisplayName = "Sedan",
                Distance = 5,
                MinDistanceForPlayers = 3,
                UsePermission = true,
                Permission = "vehiclelicence.sedan",
                BypassCostPermission = "vehiclelicence.sedanfree",
                Commands = new List<string> { "car", "sedan" },
                PurchasePrices = new Dictionary<string, PriceInfo>
                {
                    ["scrap"] = new PriceInfo { amount = 300, displayName = "Scrap" }
                },
                SpawnCooldown = 300,
                RecallCooldown = 30,
                CooldownPermissions = new Dictionary<string, CooldownPermission>
                {
                    ["vehiclelicence.vip"] = new CooldownPermission
                    {
                        spawnCooldown = 150,
                        recallCooldown = 10
                    }
                }
            };

            [JsonProperty(PropertyName = "Chinook Vehicle", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public ChinookSettings chinook = new ChinookSettings
            {
                Purchasable = true,
                NoDamage = false,
                NoCollisionDamage = false,
                DisplayName = "Chinook",
                Distance = 15,
                MinDistanceForPlayers = 6,
                UsePermission = true,
                Permission = "vehiclelicence.chinook",
                BypassCostPermission = "vehiclelicence.chinookfree",
                Commands = new List<string> { "ch47", "chinook" },
                PurchasePrices = new Dictionary<string, PriceInfo>
                {
                    ["scrap"] = new PriceInfo { amount = 3000, displayName = "Scrap" }
                },
                SpawnCooldown = 3000,
                RecallCooldown = 30,
                CooldownPermissions = new Dictionary<string, CooldownPermission>
                {
                    ["vehiclelicence.vip"] = new CooldownPermission
                    {
                        spawnCooldown = 1500,
                        recallCooldown = 10
                    }
                }
            };

            [JsonProperty(PropertyName = "Rowboat Vehicle", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public RowboatSettings rowboat = new RowboatSettings
            {
                Purchasable = true,
                NoDamage = false,
                NoCollisionDamage = false,
                DisplayName = "Row Boat",
                Distance = 5,
                MinDistanceForPlayers = 2,
                UsePermission = true,
                Permission = "vehiclelicence.rowboat",
                BypassCostPermission = "vehiclelicence.rowboatfree",
                Commands = new List<string> { "row", "rowboat" },
                PurchasePrices = new Dictionary<string, PriceInfo>
                {
                    ["scrap"] = new PriceInfo { amount = 500, displayName = "Scrap" }
                },
                SpawnCooldown = 300,
                RecallCooldown = 30,
                CooldownPermissions = new Dictionary<string, CooldownPermission>
                {
                    ["vehiclelicence.vip"] = new CooldownPermission
                    {
                        spawnCooldown = 150,
                        recallCooldown = 10
                    }
                }
            };

            [JsonProperty(PropertyName = "RHIB Vehicle", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public RhibSettings rhib = new RhibSettings
            {
                Purchasable = true,
                NoDamage = false,
                NoCollisionDamage = false,
                DisplayName = "Rigid Hulled Inflatable Boat",
                Distance = 10,
                MinDistanceForPlayers = 3,
                UsePermission = true,
                Permission = "vehiclelicence.rhib",
                BypassCostPermission = "vehiclelicence.rhibfree",
                Commands = new List<string> { "rhib" },
                PurchasePrices = new Dictionary<string, PriceInfo>
                {
                    ["scrap"] = new PriceInfo { amount = 1000, displayName = "Scrap" }
                },
                SpawnCooldown = 450,
                RecallCooldown = 30,
                CooldownPermissions = new Dictionary<string, CooldownPermission>
                {
                    ["vehiclelicence.vip"] = new CooldownPermission
                    {
                        spawnCooldown = 225,
                        recallCooldown = 10
                    }
                }
            };

            [JsonProperty(PropertyName = "Hot Air Balloon Vehicle", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public HotAirBalloonSettings hotAirBalloon = new HotAirBalloonSettings
            {
                Purchasable = true,
                NoDamage = false,
                NoCollisionDamage = false,
                DisplayName = "Hot Air Balloon",
                Distance = 20,
                MinDistanceForPlayers = 5,
                UsePermission = true,
                Permission = "vehiclelicence.hotairballoon",
                BypassCostPermission = "vehiclelicence.hotairballoonfree",
                Commands = new List<string> { "hab", "hotairballoon" },
                PurchasePrices = new Dictionary<string, PriceInfo>
                {
                    ["scrap"] = new PriceInfo { amount = 500, displayName = "Scrap" }
                },
                SpawnCooldown = 900,
                RecallCooldown = 30,
                CooldownPermissions = new Dictionary<string, CooldownPermission>
                {
                    ["vehiclelicence.vip"] = new CooldownPermission
                    {
                        spawnCooldown = 450,
                        recallCooldown = 10
                    }
                }
            };

            [JsonProperty(PropertyName = "Armored Hot Air Balloon Vehicle", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public ArmoredHotAirBalloonSettings armoredHotAirBalloon = new ArmoredHotAirBalloonSettings
            {
                Purchasable = true,
                NoDamage = false,
                NoCollisionDamage = false,
                DisplayName = "Armored Hot Air Balloon",
                Distance = 10,
                MinDistanceForPlayers = 5,
                UsePermission = true,
                Permission = "vehiclelicence.armoredhotairballoon",
                BypassCostPermission = "vehiclelicence.armoredhotairballoonfree",
                Commands = new List<string> { "ahab", "armoredhotairballoon", "armoredballoon", "aballoon" },
                PurchasePrices = new Dictionary<string, PriceInfo>
                {
                    ["scrap"] = new PriceInfo { amount = 500, displayName = "Scrap" }
                },
                SpawnCooldown = 1000,
                RecallCooldown = 40,
                CooldownPermissions = new Dictionary<string, CooldownPermission>
                {
                    ["vehiclelicence.vip"] = new CooldownPermission
                    {
                        spawnCooldown = 550,
                        recallCooldown = 20
                    }
                }
            };

            [JsonProperty(PropertyName = "Ridable Horse Vehicle", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public RidableHorseSettings ridableHorse = new RidableHorseSettings
            {
                Purchasable = true,
                NoDamage = false,
                NoCollisionDamage = false,
                IsDoubleSaddle = false,
                DisplayName = "Ridable Horse",
                Distance = 6,
                MinDistanceForPlayers = 1,
                UsePermission = true,
                Permission = "vehiclelicence.ridablehorse",
                BypassCostPermission = "vehiclelicence.ridablehorsefree",
                Commands = new List<string> { "horse", "ridablehorse" },
                Breeds = new List<string>
                {
                    "Appalosa", "Bay", "Buckskin", "Chestnut", "Dapple Grey", "Piebald", "Pinto", "Red Roan", "White Thoroughbred", "Black Thoroughbred"
                },
                PurchasePrices = new Dictionary<string, PriceInfo>
                {
                    ["scrap"] = new PriceInfo { amount = 700, displayName = "Scrap" }
                },
                SpawnCooldown = 3000,
                RecallCooldown = 30,
                CooldownPermissions = new Dictionary<string, CooldownPermission>
                {
                    ["vehiclelicence.vip"] = new CooldownPermission
                    {
                        spawnCooldown = 1500,
                        recallCooldown = 10
                    }
                }
            };

            [JsonProperty(PropertyName = "Mini Copter Vehicle", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public MiniCopterSettings miniCopter = new MiniCopterSettings
            {
                Purchasable = true,
                NoDamage = false,
                NoCollisionDamage = false,
                DisplayName = "Mini Copter",
                Distance = 8,
                MinDistanceForPlayers = 2,
                rotationScale = 1.0f,
                flyHackPause = 0,
                liftFraction = 0.25f,
                instantTakeoff = false,
                UsePermission = true,
                Permission = "vehiclelicence.minicopter",
                BypassCostPermission = "vehiclelicence.minicopterfree",
                Commands = new List<string> { "mini", "minicopter" },
                PurchasePrices = new Dictionary<string, PriceInfo>
                {
                    ["scrap"] = new PriceInfo { amount = 4000, displayName = "Scrap" }
                },
                SpawnCooldown = 1800,
                RecallCooldown = 30,
                CooldownPermissions = new Dictionary<string, CooldownPermission>
                {
                    ["vehiclelicence.vip"] = new CooldownPermission
                    {
                        spawnCooldown = 900,
                        recallCooldown = 10
                    }
                }
            };

            [JsonProperty(PropertyName = "Attack Helicopter Vehicle", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public AttackHelicopterSettings attackHelicopter = new AttackHelicopterSettings
            {
                Purchasable = true,
                NoDamage = false,
                NoCollisionDamage = false,
                DisplayName = "Attack Helicopter",
                Distance = 8,
                MinDistanceForPlayers = 2,
                rotationScale = 1.0f,
                flyHackPause = 0,
                liftFraction = 0.33f,
                HVSpawnAmmoAmount = 0,
                IncendiarySpawnAmmoAmount = 0,
                FlareSpawnAmmoAmount = 0,
                instantTakeoff = false,
                UsePermission = true,
                Permission = "vehiclelicence.attackhelicopter",
                BypassCostPermission = "vehiclelicence.attackhelicopterfree",
                Commands = new List<string> { "attack", "aheli", "attackheli", "attackhelicopter" },
                PurchasePrices = new Dictionary<string, PriceInfo>
                {
                    ["scrap"] = new PriceInfo { amount = 4000, displayName = "Scrap" }
                },
                SpawnCooldown = 1800,
                RecallCooldown = 30,
                CooldownPermissions = new Dictionary<string, CooldownPermission>
                {
                    ["vehiclelicence.vip"] = new CooldownPermission
                    {
                        spawnCooldown = 900,
                        recallCooldown = 10
                    }
                }
            };

            [JsonProperty(PropertyName = "Transport Helicopter Vehicle", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public TransportHelicopterSettings transportHelicopter = new TransportHelicopterSettings
            {
                Purchasable = true,
                NoDamage = false,
                NoCollisionDamage = false,
                DisplayName = "Transport Copter",
                Distance = 7,
                MinDistanceForPlayers = 4,
                flyHackPause = 0,
                rotationScale = 1.0f,
                liftFraction = .25f,
                instantTakeoff = false,
                UsePermission = true,
                Permission = "vehiclelicence.transportcopter",
                BypassCostPermission = "vehiclelicence.transportcopterfree",
                Commands = new List<string>
                {
                    "tcop", "transportcopter"
                },
                PurchasePrices = new Dictionary<string, PriceInfo>
                {
                    ["scrap"] = new PriceInfo { amount = 5000, displayName = "Scrap" }
                },
                SpawnCooldown = 2400,
                RecallCooldown = 30,
                CooldownPermissions = new Dictionary<string, CooldownPermission>
                {
                    ["vehiclelicence.vip"] = new CooldownPermission
                    {
                        spawnCooldown = 1200,
                        recallCooldown = 10
                    }
                }
            };

            [JsonProperty(PropertyName = "Work Cart Vehicle", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public WorkCartSettings workCart = new WorkCartSettings
            {
                Purchasable = true,
                NoDamage = false,
                NoCollisionDamage = false,
                DisplayName = "Work Cart",
                Distance = 12,
                MinDistanceForPlayers = 6,
                UsePermission = true,
                Permission = "vehiclelicence.workcart",
                BypassCostPermission = "vehiclelicence.workcartfree",
                Commands = new List<string>
                {
                    "cart", "workcart"
                },
                PurchasePrices = new Dictionary<string, PriceInfo>
                {
                    ["scrap"] = new PriceInfo { amount = 2000, displayName = "Scrap" }
                },
                SpawnCooldown = 1800,
                RecallCooldown = 30,
                CooldownPermissions = new Dictionary<string, CooldownPermission>
                {
                    ["vehiclelicence.vip"] = new CooldownPermission
                    {
                        spawnCooldown = 900,
                        recallCooldown = 10
                    }
                }
            };

            [JsonProperty(PropertyName = "Sedan Rail Vehicle", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public WorkCartSettings sedanRail = new WorkCartSettings
            {
                Purchasable = true,
                NoDamage = false,
                NoCollisionDamage = false,
                DisplayName = "Sedan Rail",
                Distance = 6,
                MinDistanceForPlayers = 3,
                UsePermission = true,
                Permission = "vehiclelicence.sedanrail",
                BypassCostPermission = "vehiclelicence.sedanrailfree",
                Commands = new List<string>
                {
                    "carrail", "sedanrail"
                },
                PurchasePrices = new Dictionary<string, PriceInfo>
                {
                    ["scrap"] = new PriceInfo { amount = 500, displayName = "Scrap" }
                },
                SpawnCooldown = 600,
                RecallCooldown = 30,
                CooldownPermissions = new Dictionary<string, CooldownPermission>
                {
                    ["vehiclelicence.vip"] = new CooldownPermission
                    {
                        spawnCooldown = 300,
                        recallCooldown = 10
                    }
                }
            };

            [JsonProperty(PropertyName = "Magnet Crane Vehicle", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public MagnetCraneSettings magnetCrane = new MagnetCraneSettings
            {
                Purchasable = true,
                NoDamage = false,
                NoCollisionDamage = false,
                DisplayName = "Magnet Crane",
                Distance = 16,
                MinDistanceForPlayers = 8,
                UsePermission = true,
                Permission = "vehiclelicence.magnetcrane",
                BypassCostPermission = "vehiclelicence.magnetcranefree",
                Commands = new List<string>
                {
                    "crane", "magnetcrane"
                },
                PurchasePrices = new Dictionary<string, PriceInfo>
                {
                    ["scrap"] = new PriceInfo { amount = 2000, displayName = "Scrap" }
                },
                SpawnCooldown = 600,
                RecallCooldown = 30,
                CooldownPermissions = new Dictionary<string, CooldownPermission>
                {
                    ["vehiclelicence.vip"] = new CooldownPermission
                    {
                        spawnCooldown = 300,
                        recallCooldown = 10
                    }
                }
            };

            [JsonProperty(PropertyName = "Submarine Solo Vehicle", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public SubmarineSoloSettings submarineSolo = new SubmarineSoloSettings
            {
                Purchasable = true,
                NoDamage = false,
                NoCollisionDamage = false,
                DisplayName = "Submarine Solo",
                Distance = 5,
                MinDistanceForPlayers = 2,
                UsePermission = true,
                Permission = "vehiclelicence.submarinesolo",
                BypassCostPermission = "vehiclelicence.submarinesolofree",
                Commands = new List<string> { "subsolo", "solo" },
                PurchasePrices = new Dictionary<string, PriceInfo>
                {
                    ["scrap"] = new PriceInfo { amount = 600, displayName = "Scrap" }
                },
                SpawnCooldown = 300,
                RecallCooldown = 30,
                CooldownPermissions = new Dictionary<string, CooldownPermission>
                {
                    ["vehiclelicence.vip"] = new CooldownPermission
                    {
                        spawnCooldown = 150,
                        recallCooldown = 10
                    }
                }
            };

            [JsonProperty(PropertyName = "Submarine Duo Vehicle", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public SubmarineDuoSettings submarineDuo = new SubmarineDuoSettings
            {
                Purchasable = true,
                NoDamage = false,
                NoCollisionDamage = false,
                DisplayName = "Submarine Duo",
                Distance = 5,
                MinDistanceForPlayers = 2,
                UsePermission = true,
                Permission = "vehiclelicence.submarineduo",
                BypassCostPermission = "vehiclelicence.submarineduofree",
                Commands = new List<string> { "subduo", "duo" },
                PurchasePrices = new Dictionary<string, PriceInfo>
                {
                    ["scrap"] = new PriceInfo { amount = 1000, displayName = "Scrap" }
                },
                SpawnCooldown = 300,
                RecallCooldown = 30,
                CooldownPermissions = new Dictionary<string, CooldownPermission>
                {
                    ["vehiclelicence.vip"] = new CooldownPermission
                    {
                        spawnCooldown = 150,
                        recallCooldown = 10
                    }
                }
            };

            [JsonProperty(PropertyName = "Snowmobile Vehicle", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public SnowmobileSettings snowmobile = new SnowmobileSettings
            {
                Purchasable = true,
                NoDamage = false,
                NoCollisionDamage = false,
                DisplayName = "Snowmobile",
                Distance = 5,
                MinDistanceForPlayers = 2,
                UsePermission = true,
                Permission = "vehiclelicence.snowmobile",
                BypassCostPermission = "vehiclelicence.snowmobilefree",
                Commands = new List<string> { "snow", "snowmobile" },
                PurchasePrices = new Dictionary<string, PriceInfo>
                {
                    ["scrap"] = new PriceInfo { amount = 1000, displayName = "Scrap" }
                },
                SpawnCooldown = 300,
                RecallCooldown = 30,
                CooldownPermissions = new Dictionary<string, CooldownPermission>
                {
                    ["vehiclelicence.vip"] = new CooldownPermission
                    {
                        spawnCooldown = 150,
                        recallCooldown = 10
                    }
                }
            };

            [JsonProperty(PropertyName = "Tomaha Snowmobile Vehicle", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public SnowmobileSettings tomahaSnowmobile = new SnowmobileSettings
            {
                Purchasable = true,
                NoDamage = false,
                NoCollisionDamage = false,
                DisplayName = "Tomaha Snowmobile",
                Distance = 5,
                MinDistanceForPlayers = 2,
                UsePermission = true,
                Permission = "vehiclelicence.tomahasnowmobile",
                BypassCostPermission = "vehiclelicence.tomahasnowmobilefree",
                Commands = new List<string> { "tsnow", "tsnowmobile" },
                PurchasePrices = new Dictionary<string, PriceInfo>
                {
                    ["scrap"] = new PriceInfo { amount = 1000, displayName = "Scrap" }
                },
                SpawnCooldown = 300,
                RecallCooldown = 30,
                CooldownPermissions = new Dictionary<string, CooldownPermission>
                {
                    ["vehiclelicence.vip"] = new CooldownPermission
                    {
                        spawnCooldown = 150,
                        recallCooldown = 10
                    }
                }
            };

            [JsonProperty(PropertyName = "Pedal Bike Vehicle", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public PedalBikeSettings pedalBike = new PedalBikeSettings
            {
                Purchasable = true,
                NoDamage = false,
                NoCollisionDamage = false,
                DisplayName = "Pedal Bike",
                Distance = 5,
                MinDistanceForPlayers = 2,
                UsePermission = true,
                Permission = "vehiclelicence.pedalbike",
                BypassCostPermission = "vehiclelicence.pedalbikefree",
                Commands = new List<string> { "bike", "pbike" },
                PurchasePrices = new Dictionary<string, PriceInfo>
                {
                    ["scrap"] = new PriceInfo { amount = 100, displayName = "Scrap" }
                },
                SpawnCooldown = 300,
                RecallCooldown = 30,
                CooldownPermissions = new Dictionary<string, CooldownPermission>
                {
                    ["vehiclelicence.vip"] = new CooldownPermission
                    {
                        spawnCooldown = 150,
                        recallCooldown = 10
                    }
                }
            };

            [JsonProperty(PropertyName = "Pedal Trike Vehicle", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public PedalTrikeSettings pedalTrike = new PedalTrikeSettings
            {
                Purchasable = true,
                NoDamage = false,
                NoCollisionDamage = false,
                DisplayName = "Pedal Trike",
                Distance = 5,
                MinDistanceForPlayers = 2,
                UsePermission = true,
                Permission = "vehiclelicence.pedaltrike",
                BypassCostPermission = "vehiclelicence.pedaltrikefree",
                Commands = new List<string> { "trike", "ptrike" },
                PurchasePrices = new Dictionary<string, PriceInfo>
                {
                    ["scrap"] = new PriceInfo { amount = 200, displayName = "Scrap" }
                },
                SpawnCooldown = 300,
                RecallCooldown = 30,
                CooldownPermissions = new Dictionary<string, CooldownPermission>
                {
                    ["vehiclelicence.vip"] = new CooldownPermission
                    {
                        spawnCooldown = 150,
                        recallCooldown = 10
                    }
                }
            };

            [JsonProperty(PropertyName = "Motorbike Vehicle", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public MotorBikeSettings motorBike = new MotorBikeSettings
            {
                Purchasable = true,
                NoDamage = false,
                NoCollisionDamage = false,
                DisplayName = "Motorbike",
                Distance = 5,
                MinDistanceForPlayers = 2,
                UsePermission = true,
                Permission = "vehiclelicence.motorbike",
                BypassCostPermission = "vehiclelicence.motorbikefree",
                Commands = new List<string> { "mbike", "motorbike" },
                PurchasePrices = new Dictionary<string, PriceInfo>
                {
                    ["scrap"] = new PriceInfo { amount = 750, displayName = "Scrap" }
                },
                SpawnCooldown = 300,
                RecallCooldown = 30,
                CooldownPermissions = new Dictionary<string, CooldownPermission>
                {
                    ["vehiclelicence.vip"] = new CooldownPermission
                    {
                        spawnCooldown = 150,
                        recallCooldown = 10
                    }
                }
            };

            [JsonProperty(PropertyName = "Motorbike Sidecar Vehicle", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public MotorBikeSidecarSettings motorBikeSidecar = new MotorBikeSidecarSettings
            {
                Purchasable = true,
                NoDamage = false,
                NoCollisionDamage = false,
                DisplayName = "Motorbike Sidecar",
                Distance = 5,
                MinDistanceForPlayers = 2,
                UsePermission = true,
                Permission = "vehiclelicence.motorbikesidecar",
                BypassCostPermission = "vehiclelicence.motorbikesidecarfree",
                Commands = new List<string> { "mbikescar", "motorbikesidecar" },
                PurchasePrices = new Dictionary<string, PriceInfo>
                {
                    ["scrap"] = new PriceInfo { amount = 1000, displayName = "Scrap" }
                },
                SpawnCooldown = 300,
                RecallCooldown = 30,
                CooldownPermissions = new Dictionary<string, CooldownPermission>
                {
                    ["vehiclelicence.vip"] = new CooldownPermission
                    {
                        spawnCooldown = 150,
                        recallCooldown = 10
                    }
                }
            };
            
            [JsonProperty(PropertyName = "Kayak Vehicle", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public KayakSettings Kayak = new KayakSettings
            {
                Purchasable = true,
                NoDamage = false,
                NoCollisionDamage = false,
                DisplayName = "Kayak",
                Distance = 5,
                MinDistanceForPlayers = 2,
                UsePermission = true,
                Permission = "vehiclelicence.kayak",
                BypassCostPermission = "vehiclelicence.kayakfree",
                Commands = new List<string> { "kayak" },
                PurchasePrices = new Dictionary<string, PriceInfo>
                {
                    ["scrap"] = new PriceInfo { amount = 300, displayName = "Scrap" }
                },
                SpawnCooldown = 300,
                RecallCooldown = 30,
                CooldownPermissions = new Dictionary<string, CooldownPermission>
                {
                    ["vehiclelicence.vip"] = new CooldownPermission
                    {
                        spawnCooldown = 30,
                        recallCooldown = 10
                    }
                }
            };
        }

        public class CustomVehicleSettings
        {
            [JsonProperty(PropertyName = "ATV Vehicle", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public AtvSettings atv = new AtvSettings
            {
                Purchasable = true,
                NoDamage = false,
                NoCollisionDamage = false,
                DisplayName = "ATV",
                Distance = 5,
                MinDistanceForPlayers = 2,
                UsePermission = true,
                Permission = "vehiclelicence.atv",
                BypassCostPermission = "vehiclelicence.atvfree",
                Commands = new List<string> { "atv", "quad" },
                PurchasePrices = new Dictionary<string, PriceInfo>
                {
                    ["scrap"] = new PriceInfo { amount = 1000, displayName = "Scrap" }
                },
                SpawnCooldown = 300,
                RecallCooldown = 30,
                CooldownPermissions = new Dictionary<string, CooldownPermission>
                {
                    ["vehiclelicence.vip"] = new CooldownPermission
                    {
                        spawnCooldown = 150,
                        recallCooldown = 10
                    }
                }
            };

            [JsonProperty(PropertyName = "Race Sofa Vehicle", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public RaceSofaSettings raceSofa = new RaceSofaSettings
            {
                Purchasable = true,
                NoDamage = false,
                NoCollisionDamage = false,
                DisplayName = "Race Sofa",
                Distance = 5,
                MinDistanceForPlayers = 2,
                UsePermission = true,
                Permission = "vehiclelicence.sofa",
                BypassCostPermission = "vehiclelicence.sofafree",
                Commands = new List<string> { "sofa", "rsofa" },
                PurchasePrices = new Dictionary<string, PriceInfo>
                {
                    ["scrap"] = new PriceInfo { amount = 1000, displayName = "Scrap" }
                },
                SpawnCooldown = 300,
                RecallCooldown = 30,
                CooldownPermissions = new Dictionary<string, CooldownPermission>
                {
                    ["vehiclelicence.vip"] = new CooldownPermission
                    {
                        spawnCooldown = 150,
                        recallCooldown = 10
                    }
                }
            };

            [JsonProperty(PropertyName = "Water Bird Vehicle", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public WaterBirdSettings waterBird = new WaterBirdSettings
            {
                Purchasable = true,
                NoDamage = false,
                NoCollisionDamage = false,
                DisplayName = "Water Bird",
                Distance = 7,
                MinDistanceForPlayers = 2,
                UsePermission = true,
                Permission = "vehiclelicence.waterbird",
                BypassCostPermission = "vehiclelicence.waterbirdfree",
                Commands = new List<string> { "wbird", "waterbird" },
                PurchasePrices = new Dictionary<string, PriceInfo>
                {
                    ["scrap"] = new PriceInfo { amount = 5000, displayName = "Scrap" }
                },
                SpawnCooldown = 300,
                RecallCooldown = 30,
                CooldownPermissions = new Dictionary<string, CooldownPermission>
                {
                    ["vehiclelicence.vip"] = new CooldownPermission
                    {
                        spawnCooldown = 30,
                        recallCooldown = 10
                    }
                }
            };

            [JsonProperty(PropertyName = "War Bird Vehicle", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public WarBirdSettings warBird = new WarBirdSettings
            {
                Purchasable = true,
                NoDamage = false,
                NoCollisionDamage = false,
                DisplayName = "Water Bird",
                Distance = 7,
                MinDistanceForPlayers = 2,
                UsePermission = true,
                Permission = "vehiclelicence.warbird",
                BypassCostPermission = "vehiclelicence.warbirdfree",
                Commands = new List<string> { "warb", "warbird" },
                PurchasePrices = new Dictionary<string, PriceInfo>
                {
                    ["scrap"] = new PriceInfo { amount = 5000, displayName = "Scrap" }
                },
                SpawnCooldown = 300,
                RecallCooldown = 30,
                CooldownPermissions = new Dictionary<string, CooldownPermission>
                {
                    ["vehiclelicence.vip"] = new CooldownPermission
                    {
                        spawnCooldown = 30,
                        recallCooldown = 10
                    }
                }
            };

            [JsonProperty(PropertyName = "Little Bird Vehicle", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public LittleBirdSettings littleBird = new LittleBirdSettings
            {
                Purchasable = true,
                NoDamage = false,
                NoCollisionDamage = false,
                DisplayName = "Little Bird",
                Distance = 7,
                MinDistanceForPlayers = 2,
                UsePermission = true,
                Permission = "vehiclelicence.littlebird",
                BypassCostPermission = "vehiclelicence.littlebirdfree",
                Commands = new List<string> { "lbird", "littlebird" },
                PurchasePrices = new Dictionary<string, PriceInfo>
                {
                    ["scrap"] = new PriceInfo { amount = 5000, displayName = "Scrap" }
                },
                SpawnCooldown = 300,
                RecallCooldown = 30,
                CooldownPermissions = new Dictionary<string, CooldownPermission>
                {
                    ["vehiclelicence.vip"] = new CooldownPermission
                    {
                        spawnCooldown = 30,
                        recallCooldown = 10
                    }
                }
            };

            [JsonProperty(PropertyName = "Fighter Plane Vehicle", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public FighterSettings fighter = new FighterSettings
            {
                Purchasable = true,
                NoDamage = false,
                NoCollisionDamage = false,
                DisplayName = "Fighter Plane",
                Distance = 7,
                MinDistanceForPlayers = 2,
                UsePermission = true,
                Permission = "vehiclelicence.fighter",
                BypassCostPermission = "vehiclelicence.fighterfree",
                Commands = new List<string> { "fighter", "fighterplane" },
                PurchasePrices = new Dictionary<string, PriceInfo>
                {
                    ["scrap"] = new PriceInfo { amount = 5000, displayName = "Scrap" }
                },
                SpawnCooldown = 300,
                RecallCooldown = 30,
                CooldownPermissions = new Dictionary<string, CooldownPermission>
                {
                    ["vehiclelicence.vip"] = new CooldownPermission
                    {
                        spawnCooldown = 30,
                        recallCooldown = 10
                    }
                }
            };

            [JsonProperty(PropertyName = "Old Fighter Plane Vehicle", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public OldFighterSettings oldFighter = new OldFighterSettings
            {
                Purchasable = true,
                NoDamage = false,
                NoCollisionDamage = false,
                DisplayName = "Old Fighter Plane",
                Distance = 7,
                MinDistanceForPlayers = 2,
                UsePermission = true,
                Permission = "vehiclelicence.oldfighter",
                BypassCostPermission = "vehiclelicence.oldfighterfree",
                Commands = new List<string> { "ofighter", "oldfighterplane" },
                PurchasePrices = new Dictionary<string, PriceInfo>
                {
                    ["scrap"] = new PriceInfo { amount = 5000, displayName = "Scrap" }
                },
                SpawnCooldown = 300,
                RecallCooldown = 30,
                CooldownPermissions = new Dictionary<string, CooldownPermission>
                {
                    ["vehiclelicence.vip"] = new CooldownPermission
                    {
                        spawnCooldown = 30,
                        recallCooldown = 10
                    }
                }
            };

            [JsonProperty(PropertyName = "Fighter Bus Vehicle", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public FighterBusSettings fighterBus = new FighterBusSettings
            {
                Purchasable = true,
                NoDamage = false,
                NoCollisionDamage = false,
                DisplayName = "Fighter Bus",
                Distance = 7,
                MinDistanceForPlayers = 2,
                UsePermission = true,
                Permission = "vehiclelicence.fighterbus",
                BypassCostPermission = "vehiclelicence.fighterbusfree",
                Commands = new List<string> { "fbus", "fighterbus" },
                PurchasePrices = new Dictionary<string, PriceInfo>
                {
                    ["scrap"] = new PriceInfo { amount = 5000, displayName = "Scrap" }
                },
                SpawnCooldown = 300,
                RecallCooldown = 30,
                CooldownPermissions = new Dictionary<string, CooldownPermission>
                {
                    ["vehiclelicence.vip"] = new CooldownPermission
                    {
                        spawnCooldown = 30,
                        recallCooldown = 10
                    }
                }
            };

            [JsonProperty(PropertyName = "War Bus Vehicle", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public WarBusSettings warBus = new WarBusSettings
            {
                Purchasable = true,
                NoDamage = false,
                NoCollisionDamage = false,
                DisplayName = "War Bus",
                Distance = 7,
                MinDistanceForPlayers = 2,
                UsePermission = true,
                Permission = "vehiclelicence.warbus",
                BypassCostPermission = "vehiclelicence.warbusfree",
                Commands = new List<string> { "wbus", "warbus" },
                PurchasePrices = new Dictionary<string, PriceInfo>
                {
                    ["scrap"] = new PriceInfo { amount = 5000, displayName = "Scrap" }
                },
                SpawnCooldown = 300,
                RecallCooldown = 30,
                CooldownPermissions = new Dictionary<string, CooldownPermission>
                {
                    ["vehiclelicence.vip"] = new CooldownPermission
                    {
                        spawnCooldown = 30,
                        recallCooldown = 10
                    }
                }
            };

            [JsonProperty(PropertyName = "Air Bus Vehicle", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public AirBusSettings airBus = new AirBusSettings
            {
                Purchasable = true,
                NoDamage = false,
                NoCollisionDamage = false,
                DisplayName = "Air Bus",
                Distance = 7,
                MinDistanceForPlayers = 2,
                UsePermission = true,
                Permission = "vehiclelicence.airbus",
                BypassCostPermission = "vehiclelicence.airbusfree",
                Commands = new List<string> { "abus", "airbus" },
                PurchasePrices = new Dictionary<string, PriceInfo>
                {
                    ["scrap"] = new PriceInfo { amount = 5000, displayName = "Scrap" }
                },
                SpawnCooldown = 300,
                RecallCooldown = 30,
                CooldownPermissions = new Dictionary<string, CooldownPermission>
                {
                    ["vehiclelicence.vip"] = new CooldownPermission
                    {
                        spawnCooldown = 30,
                        recallCooldown = 10
                    }
                }
            };

            [JsonProperty(PropertyName = "Patrol Helicopter Vehicle", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public PatrolHelicopterSettings patrolHeli = new PatrolHelicopterSettings
            {
                Purchasable = true,
                NoDamage = false,
                NoCollisionDamage = false,
                DisplayName = "Patrol Helicopter",
                Distance = 7,
                MinDistanceForPlayers = 2,
                UsePermission = true,
                Permission = "vehiclelicence.patrolheli",
                BypassCostPermission = "vehiclelicence.patrolhelifree",
                Commands = new List<string> { "pheli", "patrolheli", "patrolhelicopter" },
                PurchasePrices = new Dictionary<string, PriceInfo>
                {
                    ["scrap"] = new PriceInfo { amount = 5000, displayName = "Scrap" }
                },
                SpawnCooldown = 300,
                RecallCooldown = 30,
                CooldownPermissions = new Dictionary<string, CooldownPermission>
                {
                    ["vehiclelicence.vip"] = new CooldownPermission
                    {
                        spawnCooldown = 30,
                        recallCooldown = 10
                    }
                }
            };

            [JsonProperty(PropertyName = "Rust Wing Vehicle", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public RustWingSettings rustWing = new RustWingSettings
            {
                Purchasable = true,
                NoDamage = false,
                NoCollisionDamage = false,
                DisplayName = "Rust Wing",
                Distance = 7,
                MinDistanceForPlayers = 2,
                UsePermission = true,
                Permission = "vehiclelicence.rustwing",
                BypassCostPermission = "vehiclelicence.rustwingfree",
                Commands = new List<string> { "rustwing" },
                PurchasePrices = new Dictionary<string, PriceInfo>
                {
                    ["scrap"] = new PriceInfo { amount = 5000, displayName = "Scrap" }
                },
                SpawnCooldown = 300,
                RecallCooldown = 30,
                CooldownPermissions = new Dictionary<string, CooldownPermission>
                {
                    ["vehiclelicence.vip"] = new CooldownPermission
                    {
                        spawnCooldown = 30,
                        recallCooldown = 10
                    }
                }
            };

            [JsonProperty(PropertyName = "Rust Wing Detailed Vehicle", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public RustWingDetailedSettings rustWingDetailed = new RustWingDetailedSettings
            {
                Purchasable = true,
                NoDamage = false,
                NoCollisionDamage = false,
                DisplayName = "Rust Wing Detailed",
                Distance = 7,
                MinDistanceForPlayers = 2,
                UsePermission = true,
                Permission = "vehiclelicence.rustwingdetailed",
                BypassCostPermission = "vehiclelicence.rustwingdetailedfree",
                Commands = new List<string> { "rustwingd" },
                PurchasePrices = new Dictionary<string, PriceInfo>
                {
                    ["scrap"] = new PriceInfo { amount = 5000, displayName = "Scrap" }
                },
                SpawnCooldown = 300,
                RecallCooldown = 30,
                CooldownPermissions = new Dictionary<string, CooldownPermission>
                {
                    ["vehiclelicence.vip"] = new CooldownPermission
                    {
                        spawnCooldown = 30,
                        recallCooldown = 10
                    }
                }
            };

            [JsonProperty(PropertyName = "Rust Wing Detailed Old Vehicle", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public RustWingDetailedOldSettings rustWingDetailedOld = new RustWingDetailedOldSettings
            {
                Purchasable = true,
                NoDamage = false,
                NoCollisionDamage = false,
                DisplayName = "Rust Wing Detailed Old",
                Distance = 7,
                MinDistanceForPlayers = 2,
                UsePermission = true,
                Permission = "vehiclelicence.rustwingdetailedold",
                BypassCostPermission = "vehiclelicence.rustwingdetailedoldfree",
                Commands = new List<string> { "rustwingo" },
                PurchasePrices = new Dictionary<string, PriceInfo>
                {
                    ["scrap"] = new PriceInfo { amount = 5000, displayName = "Scrap" }
                },
                SpawnCooldown = 300,
                RecallCooldown = 30,
                CooldownPermissions = new Dictionary<string, CooldownPermission>
                {
                    ["vehiclelicence.vip"] = new CooldownPermission
                    {
                        spawnCooldown = 30,
                        recallCooldown = 10
                    }
                }
            };

            [JsonProperty(PropertyName = "Tin Fighter Vehicle", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public TinFighterSettings tinFighter = new TinFighterSettings
            {
                Purchasable = true,
                NoDamage = false,
                NoCollisionDamage = false,
                DisplayName = "Tie Fighter",
                Distance = 7,
                MinDistanceForPlayers = 2,
                UsePermission = true,
                Permission = "vehiclelicence.tinfighter",
                BypassCostPermission = "vehiclelicence.tinfighterfree",
                Commands = new List<string> { "tin", "tfighter", "tinfighter" },
                PurchasePrices = new Dictionary<string, PriceInfo>
                {
                    ["scrap"] = new PriceInfo { amount = 5000, displayName = "Scrap" }
                },
                SpawnCooldown = 300,
                RecallCooldown = 30,
                CooldownPermissions = new Dictionary<string, CooldownPermission>
                {
                    ["vehiclelicence.vip"] = new CooldownPermission
                    {
                        spawnCooldown = 30,
                        recallCooldown = 10
                    }
                }
            };

            [JsonProperty(PropertyName = "Tin Fighter Detailed Vehicle", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public TinFighterDetailedSettings tinFighterDetailed = new TinFighterDetailedSettings
            {
                Purchasable = true,
                NoDamage = false,
                NoCollisionDamage = false,
                DisplayName = "Tie Fighter Detailed",
                Distance = 7,
                MinDistanceForPlayers = 2,
                UsePermission = true,
                Permission = "vehiclelicence.tinfighterdetailed",
                BypassCostPermission = "vehiclelicence.tinfighterdetailedfree",
                Commands = new List<string> { "tind", "tfighterd", "tinfighterd" },
                PurchasePrices = new Dictionary<string, PriceInfo>
                {
                    ["scrap"] = new PriceInfo { amount = 5000, displayName = "Scrap" }
                },
                SpawnCooldown = 300,
                RecallCooldown = 30,
                CooldownPermissions = new Dictionary<string, CooldownPermission>
                {
                    ["vehiclelicence.vip"] = new CooldownPermission
                    {
                        spawnCooldown = 30,
                        recallCooldown = 10
                    }
                }
            };

            [JsonProperty(PropertyName = "Tin Fighter Detailed Old Vehicle", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public TinFighterDetailedOldSettings tinFighterDetailedOld = new TinFighterDetailedOldSettings
            {
                Purchasable = true,
                NoDamage = false,
                NoCollisionDamage = false,
                DisplayName = "Tie Fighter Detailed Old",
                Distance = 7,
                MinDistanceForPlayers = 2,
                UsePermission = true,
                Permission = "vehiclelicence.tinfighterdetailedold",
                BypassCostPermission = "vehiclelicence.tinfighterdetailedoldfree",
                Commands = new List<string> { "tino", "tfightero", "tinfightero" },
                PurchasePrices = new Dictionary<string, PriceInfo>
                {
                    ["scrap"] = new PriceInfo { amount = 5000, displayName = "Scrap" }
                },
                SpawnCooldown = 300,
                RecallCooldown = 30,
                CooldownPermissions = new Dictionary<string, CooldownPermission>
                {
                    ["vehiclelicence.vip"] = new CooldownPermission
                    {
                        spawnCooldown = 30,
                        recallCooldown = 10
                    }
                }
            };

            [JsonProperty(PropertyName = "Mars Fighter Vehicle", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public MarsFighterSettings marsFighter = new MarsFighterSettings
            {
                Purchasable = true,
                NoDamage = false,
                NoCollisionDamage = false,
                DisplayName = "Mars Fighter",
                Distance = 7,
                MinDistanceForPlayers = 2,
                UsePermission = true,
                Permission = "vehiclelicence.marsfighter",
                BypassCostPermission = "vehiclelicence.marsfighterfree",
                Commands = new List<string> { "mars", "mfighter", "marsfighter" },
                PurchasePrices = new Dictionary<string, PriceInfo>
                {
                    ["scrap"] = new PriceInfo { amount = 5000, displayName = "Scrap" }
                },
                SpawnCooldown = 300,
                RecallCooldown = 30,
                CooldownPermissions = new Dictionary<string, CooldownPermission>
                {
                    ["vehiclelicence.vip"] = new CooldownPermission
                    {
                        spawnCooldown = 30,
                        recallCooldown = 10
                    }
                }
            };

            [JsonProperty(PropertyName = "Mars Fighter Detailed Vehicle", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public MarsFighterDetailedSettings marsFighterDetailed = new MarsFighterDetailedSettings
            {
                Purchasable = true,
                NoDamage = false,
                NoCollisionDamage = false,
                DisplayName = "Mars Fighter Detailed",
                Distance = 7,
                MinDistanceForPlayers = 2,
                UsePermission = true,
                Permission = "vehiclelicence.marsfighterdetailed",
                BypassCostPermission = "vehiclelicence.marsfighterdetailedfree",
                Commands = new List<string> { "marsd", "mfighterd", "marsfighterd" },
                PurchasePrices = new Dictionary<string, PriceInfo>
                {
                    ["scrap"] = new PriceInfo { amount = 5000, displayName = "Scrap" }
                },
                SpawnCooldown = 300,
                RecallCooldown = 30,
                CooldownPermissions = new Dictionary<string, CooldownPermission>
                {
                    ["vehiclelicence.vip"] = new CooldownPermission
                    {
                        spawnCooldown = 30,
                        recallCooldown = 10
                    }
                }
            };

            [JsonProperty(PropertyName = "Sky Plane Vehicle", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public SkyPlaneSettings skyPlane = new SkyPlaneSettings
            {
                Purchasable = true,
                NoDamage = false,
                NoCollisionDamage = false,
                DisplayName = "Sky Plane",
                Distance = 7,
                MinDistanceForPlayers = 2,
                UsePermission = true,
                Permission = "vehiclelicence.skyplane",
                BypassCostPermission = "vehiclelicence.skyplanefree",
                Commands = new List<string> { "splane", "skyplane" },
                PurchasePrices = new Dictionary<string, PriceInfo>
                {
                    ["scrap"] = new PriceInfo { amount = 5000, displayName = "Scrap" }
                },
                SpawnCooldown = 300,
                RecallCooldown = 30,
                CooldownPermissions = new Dictionary<string, CooldownPermission>
                {
                    ["vehiclelicence.vip"] = new CooldownPermission
                    {
                        spawnCooldown = 30,
                        recallCooldown = 10
                    }
                }
            };

            [JsonProperty(PropertyName = "Sky Boat Vehicle", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public SkyBoatSettings skyBoat = new SkyBoatSettings
            {
                Purchasable = true,
                NoDamage = false,
                NoCollisionDamage = false,
                DisplayName = "Sky Boat",
                Distance = 7,
                MinDistanceForPlayers = 2,
                UsePermission = true,
                Permission = "vehiclelicence.skyboat",
                BypassCostPermission = "vehiclelicence.skyboatfree",
                Commands = new List<string> { "sboat", "skyboat" },
                PurchasePrices = new Dictionary<string, PriceInfo>
                {
                    ["scrap"] = new PriceInfo { amount = 5000, displayName = "Scrap" }
                },
                SpawnCooldown = 300,
                RecallCooldown = 30,
                CooldownPermissions = new Dictionary<string, CooldownPermission>
                {
                    ["vehiclelicence.vip"] = new CooldownPermission
                    {
                        spawnCooldown = 30,
                        recallCooldown = 10
                    }
                }
            };

            [JsonProperty(PropertyName = "Twisted Truck Vehicle", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public TwistedTruckSettings twistedTruck = new TwistedTruckSettings
            {
                Purchasable = true,
                NoDamage = false,
                NoCollisionDamage = false,
                DisplayName = "Twisted Truck",
                Distance = 5,
                MinDistanceForPlayers = 2,
                UsePermission = true,
                Permission = "vehiclelicence.twistedtruck",
                BypassCostPermission = "vehiclelicence.twistedtruckfree",
                Commands = new List<string> { "ttruck", "twistedtruck" },
                PurchasePrices = new Dictionary<string, PriceInfo>
                {
                    ["scrap"] = new PriceInfo { amount = 5000, displayName = "Scrap" }
                },
                SpawnCooldown = 300,
                RecallCooldown = 30,
                CooldownPermissions = new Dictionary<string, CooldownPermission>
                {
                    ["vehiclelicence.vip"] = new CooldownPermission
                    {
                        spawnCooldown = 30,
                        recallCooldown = 10
                    }
                }
            };

            [JsonProperty(PropertyName = "Train Wreck Vehicle", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public TrainWreckSettings trainWreck = new TrainWreckSettings
            {
                Purchasable = true,
                NoDamage = false,
                NoCollisionDamage = false,
                DisplayName = "Train Wreck",
                Distance = 5,
                MinDistanceForPlayers = 2,
                UsePermission = true,
                Permission = "vehiclelicence.trainwreck",
                BypassCostPermission = "vehiclelicence.trainwreckfree",
                Commands = new List<string> { "twreck", "trainwreck" },
                PurchasePrices = new Dictionary<string, PriceInfo>
                {
                    ["scrap"] = new PriceInfo { amount = 5000, displayName = "Scrap" }
                },
                SpawnCooldown = 300,
                RecallCooldown = 30,
                CooldownPermissions = new Dictionary<string, CooldownPermission>
                {
                    ["vehiclelicence.vip"] = new CooldownPermission
                    {
                        spawnCooldown = 30,
                        recallCooldown = 10
                    }
                }
            };

            [JsonProperty(PropertyName = "Train Wrecker Vehicle", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public TrainWreckerSettings trainWrecker = new TrainWreckerSettings
            {
                Purchasable = true,
                NoDamage = false,
                NoCollisionDamage = false,
                DisplayName = "Train Wrecker",
                Distance = 5,
                MinDistanceForPlayers = 2,
                UsePermission = true,
                Permission = "vehiclelicence.trainwrecker",
                BypassCostPermission = "vehiclelicence.trainwreckerfree",
                Commands = new List<string> { "twrecker", "trainwrecker" },
                PurchasePrices = new Dictionary<string, PriceInfo>
                {
                    ["scrap"] = new PriceInfo { amount = 5000, displayName = "Scrap" }
                },
                SpawnCooldown = 300,
                RecallCooldown = 30,
                CooldownPermissions = new Dictionary<string, CooldownPermission>
                {
                    ["vehiclelicence.vip"] = new CooldownPermission
                    {
                        spawnCooldown = 30,
                        recallCooldown = 10
                    }
                }
            };

            [JsonProperty(PropertyName = "Santa Vehicle", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public SantaSettings santa = new SantaSettings
            {
                Purchasable = true,
                NoDamage = false,
                NoCollisionDamage = false,
                DisplayName = "Santa",
                Distance = 5,
                MinDistanceForPlayers = 2,
                UsePermission = true,
                Permission = "vehiclelicence.santa",
                BypassCostPermission = "vehiclelicence.santafree",
                Commands = new List<string> { "santa" },
                PurchasePrices = new Dictionary<string, PriceInfo>
                {
                    ["scrap"] = new PriceInfo { amount = 5000, displayName = "Scrap" }
                },
                SpawnCooldown = 300,
                RecallCooldown = 30,
                CooldownPermissions = new Dictionary<string, CooldownPermission>
                {
                    ["vehiclelicence.vip"] = new CooldownPermission
                    {
                        spawnCooldown = 30,
                        recallCooldown = 10
                    }
                }
            };

            [JsonProperty(PropertyName = "War Santa Vehicle", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public WarSantaSettings warSanta = new WarSantaSettings
            {
                Purchasable = true,
                NoDamage = false,
                NoCollisionDamage = false,
                DisplayName = "War Santa",
                Distance = 5,
                MinDistanceForPlayers = 2,
                UsePermission = true,
                Permission = "vehiclelicence.warsanta",
                BypassCostPermission = "vehiclelicence.warsantafree",
                Commands = new List<string> { "wsanta", "warsanta" },
                PurchasePrices = new Dictionary<string, PriceInfo>
                {
                    ["scrap"] = new PriceInfo { amount = 5000, displayName = "Scrap" }
                },
                SpawnCooldown = 300,
                RecallCooldown = 30,
                CooldownPermissions = new Dictionary<string, CooldownPermission>
                {
                    ["vehiclelicence.vip"] = new CooldownPermission
                    {
                        spawnCooldown = 30,
                        recallCooldown = 10
                    }
                }
            };

            [JsonProperty(PropertyName = "Witch Vehicle", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public WitchSettings witch = new WitchSettings
            {
                Purchasable = true,
                NoDamage = false,
                NoCollisionDamage = false,
                DisplayName = "Witch",
                Distance = 5,
                MinDistanceForPlayers = 2,
                UsePermission = true,
                Permission = "vehiclelicence.witch",
                BypassCostPermission = "vehiclelicence.witchfree",
                Commands = new List<string> { "witch" },
                PurchasePrices = new Dictionary<string, PriceInfo>
                {
                    ["scrap"] = new PriceInfo { amount = 5000, displayName = "Scrap" }
                },
                SpawnCooldown = 300,
                RecallCooldown = 30,
                CooldownPermissions = new Dictionary<string, CooldownPermission>
                {
                    ["vehiclelicence.vip"] = new CooldownPermission
                    {
                        spawnCooldown = 30,
                        recallCooldown = 10
                    }
                }
            };

            [JsonProperty(PropertyName = "Magic Carpet Vehicle", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public MagicCarpetSettings magicCarpet = new MagicCarpetSettings
            {
                Purchasable = true,
                NoDamage = false,
                NoCollisionDamage = false,
                DisplayName = "Magic Carpet",
                Distance = 5,
                MinDistanceForPlayers = 2,
                UsePermission = true,
                Permission = "vehiclelicence.magiccarpet",
                BypassCostPermission = "vehiclelicence.magiccarpetfree",
                Commands = new List<string> { "mcarpet", "magiccarpet" },
                PurchasePrices = new Dictionary<string, PriceInfo>
                {
                    ["scrap"] = new PriceInfo { amount = 5000, displayName = "Scrap" }
                },
                SpawnCooldown = 300,
                RecallCooldown = 30,
                CooldownPermissions = new Dictionary<string, CooldownPermission>
                {
                    ["vehiclelicence.vip"] = new CooldownPermission
                    {
                        spawnCooldown = 30,
                        recallCooldown = 10
                    }
                }
            };

            [JsonProperty(PropertyName = "Ah69t Vehicle", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public Ah69tSettings ah69t = new Ah69tSettings
            {
                Purchasable = true,
                NoDamage = false,
                NoCollisionDamage = false,
                DisplayName = "Ah69t",
                Distance = 7,
                MinDistanceForPlayers = 2,
                UsePermission = true,
                Permission = "vehiclelicence.ah69t",
                BypassCostPermission = "vehiclelicence.ah69tfree",
                Commands = new List<string> { "ah69t" },
                PurchasePrices = new Dictionary<string, PriceInfo>
                {
                    ["scrap"] = new PriceInfo { amount = 5000, displayName = "Scrap" }
                },
                SpawnCooldown = 300,
                RecallCooldown = 30,
                CooldownPermissions = new Dictionary<string, CooldownPermission>
                {
                    ["vehiclelicence.vip"] = new CooldownPermission
                    {
                        spawnCooldown = 30,
                        recallCooldown = 10
                    }
                }
            };

            [JsonProperty(PropertyName = "Ah69r Vehicle", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public Ah69rSettings ah69r = new Ah69rSettings
            {
                Purchasable = true,
                NoDamage = false,
                NoCollisionDamage = false,
                DisplayName = "Ah69r",
                Distance = 7,
                MinDistanceForPlayers = 2,
                UsePermission = true,
                Permission = "vehiclelicence.ah69r",
                BypassCostPermission = "vehiclelicence.ah69rfree",
                Commands = new List<string> { "ah69r" },
                PurchasePrices = new Dictionary<string, PriceInfo>
                {
                    ["scrap"] = new PriceInfo { amount = 5000, displayName = "Scrap" }
                },
                SpawnCooldown = 300,
                RecallCooldown = 30,
                CooldownPermissions = new Dictionary<string, CooldownPermission>
                {
                    ["vehiclelicence.vip"] = new CooldownPermission
                    {
                        spawnCooldown = 30,
                        recallCooldown = 10
                    }
                }
            };

            [JsonProperty(PropertyName = "Ah69a Vehicle", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public Ah69aSettings ah69a = new Ah69aSettings
            {
                Purchasable = true,
                NoDamage = false,
                NoCollisionDamage = false,
                DisplayName = "Ah69r",
                Distance = 7,
                MinDistanceForPlayers = 2,
                UsePermission = true,
                Permission = "vehiclelicence.ah69a",
                BypassCostPermission = "vehiclelicence.ah69afree",
                Commands = new List<string> { "ah69a" },
                PurchasePrices = new Dictionary<string, PriceInfo>
                {
                    ["scrap"] = new PriceInfo { amount = 5000, displayName = "Scrap" }
                },
                SpawnCooldown = 300,
                RecallCooldown = 30,
                CooldownPermissions = new Dictionary<string, CooldownPermission>
                {
                    ["vehiclelicence.vip"] = new CooldownPermission
                    {
                        spawnCooldown = 30,
                        recallCooldown = 10
                    }
                }
            };

            [JsonProperty(PropertyName = "Mavik Vehicle", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public MavikSettings mavik = new MavikSettings
            {
                Purchasable = true,
                NoDamage = false,
                NoCollisionDamage = false,
                DisplayName = "Cobat Drone",
                Distance = 7,
                MinDistanceForPlayers = 2,
                UsePermission = true,
                Permission = "vehiclelicence.mavik",
                BypassCostPermission = "vehiclelicence.mavikfree",
                Commands = new List<string> { "mavik", "cd" },
                PurchasePrices = new Dictionary<string, PriceInfo>
                {
                    ["scrap"] = new PriceInfo { amount = 5000, displayName = "Scrap" }
                },
                SpawnCooldown = 300,
                RecallCooldown = 30,
                CooldownPermissions = new Dictionary<string, CooldownPermission>
                {
                    ["vehiclelicence.vip"] = new CooldownPermission
                    {
                        spawnCooldown = 30,
                        recallCooldown = 10
                    }
                }
            };

            [JsonProperty(PropertyName = "Heavy Fighter Vehicle", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public HeavyFighterSettings heavyFighter = new HeavyFighterSettings
            {
                Purchasable = true,
                NoDamage = false,
                NoCollisionDamage = false,
                DisplayName = "Heavy Fighter",
                Distance = 7,
                MinDistanceForPlayers = 2,
                UsePermission = true,
                Permission = "vehiclelicence.heavyFighter",
                BypassCostPermission = "vehiclelicence.heavyFighterfree",
                Commands = new List<string> { "heavyfighter", "hf" },
                PurchasePrices = new Dictionary<string, PriceInfo>
                {
                    ["scrap"] = new PriceInfo { amount = 5000, displayName = "Scrap" }
                },
                SpawnCooldown = 300,
                RecallCooldown = 30,
                CooldownPermissions = new Dictionary<string, CooldownPermission>
                {
                    ["vehiclelicence.vip"] = new CooldownPermission
                    {
                        spawnCooldown = 30,
                        recallCooldown = 10
                    }
                }
            };

            [JsonProperty(PropertyName = "Porcelain Commander Vehicle", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public PorcelainCommanderSettings porcelainCommander = new PorcelainCommanderSettings
            {
                Purchasable = true,
                NoDamage = false,
                NoCollisionDamage = false,
                DisplayName = "Porcelain Commander",
                Distance = 7,
                MinDistanceForPlayers = 2,
                UsePermission = true,
                Permission = "vehiclelicence.porcelaincommander",
                BypassCostPermission = "vehiclelicence.porcelaincommanderfree",
                Commands = new List<string> { "porcelaincommander", "pc" },
                PurchasePrices = new Dictionary<string, PriceInfo>
                {
                    ["scrap"] = new PriceInfo { amount = 5000, displayName = "Scrap" }
                },
                SpawnCooldown = 300,
                RecallCooldown = 30,
                CooldownPermissions = new Dictionary<string, CooldownPermission>
                {
                    ["vehiclelicence.vip"] = new CooldownPermission
                    {
                        spawnCooldown = 30,
                        recallCooldown = 10
                    }
                }
            };

            [JsonProperty(PropertyName = "Dune Buggie Vehicle", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public DuneBuggieSettings duneBuggie = new DuneBuggieSettings
            {
                Purchasable = true,
                NoDamage = false,
                NoCollisionDamage = false,
                DisplayName = "Dune Buggie",
                Distance = 7,
                MinDistanceForPlayers = 2,
                UsePermission = true,
                Permission = "vehiclelicence.dunebuggie",
                BypassCostPermission = "vehiclelicence.dunebuggiefree",
                Commands = new List<string> { "dunebuggie", "dbug" },
                PurchasePrices = new Dictionary<string, PriceInfo>
                {
                    ["scrap"] = new PriceInfo { amount = 5000, displayName = "Scrap" }
                },
                SpawnCooldown = 300,
                RecallCooldown = 30,
                CooldownPermissions = new Dictionary<string, CooldownPermission>
                {
                    ["vehiclelicence.vip"] = new CooldownPermission
                    {
                        spawnCooldown = 30,
                        recallCooldown = 10
                    }
                }
            };

            [JsonProperty(PropertyName = "Dune Truck Armed Vehicle", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public DuneTruckArmedSettings duneTruckArmed = new DuneTruckArmedSettings
            {
                Purchasable = true,
                NoDamage = false,
                NoCollisionDamage = false,
                DisplayName = "Dune Truck Armed",
                Distance = 7,
                MinDistanceForPlayers = 2,
                UsePermission = true,
                Permission = "vehiclelicence.dunetruckarmed",
                BypassCostPermission = "vehiclelicence.dunetruckarmedfree",
                Commands = new List<string> { "dunetruckarmed", "dta" },
                PurchasePrices = new Dictionary<string, PriceInfo>
                {
                    ["scrap"] = new PriceInfo { amount = 5000, displayName = "Scrap" }
                },
                SpawnCooldown = 300,
                RecallCooldown = 30,
                CooldownPermissions = new Dictionary<string, CooldownPermission>
                {
                    ["vehiclelicence.vip"] = new CooldownPermission
                    {
                        spawnCooldown = 30,
                        recallCooldown = 10
                    }
                }
            };

            [JsonProperty(PropertyName = "Dune Truck UnArmed Vehicle", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public DuneTruckUnArmedSettings duneTruckUnArmed = new DuneTruckUnArmedSettings
            {
                Purchasable = true,
                NoDamage = false,
                NoCollisionDamage = false,
                DisplayName = "Dune Truck UnArmed",
                Distance = 7,
                MinDistanceForPlayers = 2,
                UsePermission = true,
                Permission = "vehiclelicence.dunetruckunarmed",
                BypassCostPermission = "vehiclelicence.dunetruckunarmedfree",
                Commands = new List<string> { "dunetruckunarmed", "dtua" },
                PurchasePrices = new Dictionary<string, PriceInfo>
                {
                    ["scrap"] = new PriceInfo { amount = 5000, displayName = "Scrap" }
                },
                SpawnCooldown = 300,
                RecallCooldown = 30,
                CooldownPermissions = new Dictionary<string, CooldownPermission>
                {
                    ["vehiclelicence.vip"] = new CooldownPermission
                    {
                        spawnCooldown = 30,
                        recallCooldown = 10
                    }
                }
            };

            [JsonProperty(PropertyName = "Dooms Day Disco Van Vehicle", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public DoomsDayDiscoVanSettings doomsDayDiscoVan = new DoomsDayDiscoVanSettings
            {
                Purchasable = true,
                NoDamage = false,
                NoCollisionDamage = false,
                DisplayName = "Dooms Day Disco Van",
                Distance = 7,
                MinDistanceForPlayers = 2,
                UsePermission = true,
                Permission = "vehiclelicence.doomsdaydiscovan",
                BypassCostPermission = "vehiclelicence.doomsdaydiscovanfree",
                Commands = new List<string> { "doomsdaydiscovan", "dddv" },
                PurchasePrices = new Dictionary<string, PriceInfo>
                {
                    ["scrap"] = new PriceInfo { amount = 5000, displayName = "Scrap" }
                },
                SpawnCooldown = 300,
                RecallCooldown = 30,
                CooldownPermissions = new Dictionary<string, CooldownPermission>
                {
                    ["vehiclelicence.vip"] = new CooldownPermission
                    {
                        spawnCooldown = 30,
                        recallCooldown = 10
                    }
                }
            }; 
            
            [JsonProperty(PropertyName = "Lawn Mower Vehicle", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public LawnMowerSettings lawnMower = new LawnMowerSettings
            {
                Purchasable = true,
                NoDamage = false,
                NoCollisionDamage = false,
                DisplayName = "Lawn Mower",
                Distance = 7,
                MinDistanceForPlayers = 2,
                UsePermission = true,
                Permission = "vehiclelicence.lawnmower",
                BypassCostPermission = "vehiclelicence.lawnmowerfree",
                Commands = new List<string> { "lawnmower", "lm" },
                PurchasePrices = new Dictionary<string, PriceInfo>
                {
                    ["scrap"] = new PriceInfo { amount = 5000, displayName = "Scrap" }
                },
                SpawnCooldown = 300,
                RecallCooldown = 30,
                CooldownPermissions = new Dictionary<string, CooldownPermission>
                {
                    ["vehiclelicence.vip"] = new CooldownPermission
                    {
                        spawnCooldown = 30,
                        recallCooldown = 10
                    }
                }
            };

            [JsonProperty(PropertyName = "Fork Lift Vehicle", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public ForkLiftSettings forkLift = new ForkLiftSettings
            {
                Purchasable = true,
                NoDamage = false,
                NoCollisionDamage = false,
                DisplayName = "Fork Lift",
                Distance = 7,
                MinDistanceForPlayers = 2,
                UsePermission = true,
                Permission = "vehiclelicence.forklift",
                BypassCostPermission = "vehiclelicence.forkliftfree",
                Commands = new List<string> { "forklift", "fl" },
                PurchasePrices = new Dictionary<string, PriceInfo>
                {
                    ["scrap"] = new PriceInfo { amount = 5000, displayName = "Scrap" }
                },
                SpawnCooldown = 300,
                RecallCooldown = 30,
                CooldownPermissions = new Dictionary<string, CooldownPermission>
                {
                    ["vehiclelicence.vip"] = new CooldownPermission
                    {
                        spawnCooldown = 30,
                        recallCooldown = 10
                    }
                }
            };

            [JsonProperty(PropertyName = "Chariot Vehicle", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public ChariotSettings chariot = new ChariotSettings
            {
                Purchasable = true,
                NoDamage = false,
                NoCollisionDamage = false,
                DisplayName = "Chariot",
                Distance = 7,
                MinDistanceForPlayers = 2,
                UsePermission = true,
                Permission = "vehiclelicence.chariot",
                BypassCostPermission = "vehiclelicence.chariotfree",
                Commands = new List<string> { "chariot" },
                PurchasePrices = new Dictionary<string, PriceInfo>
                {
                    ["scrap"] = new PriceInfo { amount = 5000, displayName = "Scrap" }
                },
                SpawnCooldown = 300,
                RecallCooldown = 30,
                CooldownPermissions = new Dictionary<string, CooldownPermission>
                {
                    ["vehiclelicence.vip"] = new CooldownPermission
                    {
                        spawnCooldown = 30,
                        recallCooldown = 10
                    }
                }
            };

            [JsonProperty(PropertyName = "Soul Harvester Vehicle", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public SoulHarvesterSettings soulHarvester = new SoulHarvesterSettings
            {
                Purchasable = true,
                NoDamage = false,
                NoCollisionDamage = false,
                DisplayName = "Soul Harvester",
                Distance = 7,
                MinDistanceForPlayers = 2,
                UsePermission = true,
                Permission = "vehiclelicence.soulharvester",
                BypassCostPermission = "vehiclelicence.soulharvesterfree",
                Commands = new List<string> { "soulharvester", "soul" },
                PurchasePrices = new Dictionary<string, PriceInfo>
                {
                    ["scrap"] = new PriceInfo { amount = 5000, displayName = "Scrap" }
                },
                SpawnCooldown = 300,
                RecallCooldown = 30,
                CooldownPermissions = new Dictionary<string, CooldownPermission>
                {
                    ["vehiclelicence.vip"] = new CooldownPermission
                    {
                        spawnCooldown = 30,
                        recallCooldown = 10
                    }
                }
            };
        }

        #region BaseSettings

        [JsonObject(MemberSerialization.OptIn)]
        public abstract class BaseVehicleSettings
        {
            #region Properties

            [JsonProperty(PropertyName = "Purchasable")]
            public bool Purchasable { get; set; }

            [JsonProperty(PropertyName = "No Damage")]
            public bool NoDamage { get; set; }

            [JsonProperty(PropertyName = "No Collision Damage")]
            public bool NoCollisionDamage { get; set; }

            [JsonProperty(PropertyName = "Display Name")]
            public string DisplayName { get; set; }

            [JsonProperty(PropertyName = "Use Permission")]
            public bool UsePermission { get; set; }

            [JsonProperty(PropertyName = "Permission")]
            public string Permission { get; set; }

            [JsonProperty(PropertyName = "Bypass Cost Permission")]
            public string BypassCostPermission { get; set; }

            [JsonProperty(PropertyName = "Distance To Spawn")]
            public float Distance { get; set; }

            [JsonProperty(PropertyName = "Time Before Vehicle Wipe (Seconds)")]
            public double WipeTime { get; set; }

            [JsonProperty(PropertyName = "Exclude cupboard zones when wiping")]
            public bool ExcludeCupboard { get; set; }

            [JsonProperty(PropertyName = "Maximum Health")]
            public float MaxHealth { get; set; }

            [JsonProperty(PropertyName = "Can Recall Maximum Distance")]
            public float RecallMaxDistance { get; set; }

            [JsonProperty(PropertyName = "Can Kill Maximum Distance")]
            public float KillMaxDistance { get; set; }

            [JsonProperty(PropertyName = "Minimum distance from player to recall or spawn")]
            public float MinDistanceForPlayers { get; set; } = 3f;

            [JsonProperty(PropertyName = "Remove License Once Crashed")]
            public bool RemoveLicenseOnceCrash { get; set; }

            [JsonProperty(PropertyName = "Commands")]
            public List<string> Commands { get; set; } = new List<string>();

            [JsonProperty(PropertyName = "Purchase Prices")]
            public Dictionary<string, PriceInfo> PurchasePrices { get; set; } = new Dictionary<string, PriceInfo>();

            [JsonProperty(PropertyName = "Spawn Prices")]
            public Dictionary<string, PriceInfo> SpawnPrices { get; set; } = new Dictionary<string, PriceInfo>();

            [JsonProperty(PropertyName = "Recall Prices")]
            public Dictionary<string, PriceInfo> RecallPrices { get; set; } = new Dictionary<string, PriceInfo>();

            [JsonProperty(PropertyName = "Recall Cooldown Bypass Prices")]
            public Dictionary<string, PriceInfo> BypassRecallCooldownPrices { get; set; } = new Dictionary<string, PriceInfo>();

            [JsonProperty(PropertyName = "Spawn Cooldown Bypass Prices")]
            public Dictionary<string, PriceInfo> BypassSpawnCooldownPrices { get; set; } = new Dictionary<string, PriceInfo>();

            [JsonProperty(PropertyName = "Spawn Cooldown (Seconds)")]
            public double SpawnCooldown { get; set; }

            [JsonProperty(PropertyName = "Recall Cooldown (Seconds)")]
            public double RecallCooldown { get; set; }

            [JsonProperty(PropertyName = "Cooldown Permissions")]
            public Dictionary<string, CooldownPermission> CooldownPermissions { get; set; } = new Dictionary<string, CooldownPermission>();

            // [JsonProperty(PropertyName = "Custom Vehicle")]
            // public bool CustomVehicle { get; set; } = false;

            #endregion Properties

            protected ConfigData configData => Instance.configData;

            public virtual bool IsWaterVehicle => false;
            public virtual bool IsTrainVehicle => false;
            public virtual bool IsNormalVehicle => true;
            public virtual bool IsFightVehicle => false;
            public virtual bool IsModularVehicle => false;
            public virtual bool IsConnectableVehicle => false;
            public virtual bool CustomVehicle => false;

            protected virtual IFuelSystem GetFuelSystem(BaseEntity entity)
            {
                return null;
            }

            protected virtual IEnumerable<ItemContainer> GetInventories(BaseEntity entity)
            {
                yield break;
            }

            #region Spawn

            protected virtual string GetVehiclePrefab(string vehicleType)
            {
                NormalVehicleType normalVehicleType;
                if (Enum.TryParse(vehicleType, out normalVehicleType) && Enum.IsDefined(typeof(NormalVehicleType), normalVehicleType))
                {
                    switch (normalVehicleType)
                    {
                        case NormalVehicleType.Tugboat:
                            return PREFAB_TUGBOAT;
                        case NormalVehicleType.Rowboat:
                            return PREFAB_ROWBOAT;
                        case NormalVehicleType.RHIB:
                            return PREFAB_RHIB;
                        case NormalVehicleType.Sedan:
                            return PREFAB_SEDAN;
                        case NormalVehicleType.HotAirBalloon:
                        case NormalVehicleType.ArmoredHotAirBalloon:
                            return PREFAB_HOTAIRBALLOON;
                        case NormalVehicleType.MiniCopter:
                            return PREFAB_MINICOPTER;
                        case NormalVehicleType.AttackHelicopter:
                            return PREFAB_ATTACKHELICOPTER;
                        case NormalVehicleType.TransportHelicopter:
                            return PREFAB_TRANSPORTCOPTER;
                        case NormalVehicleType.Chinook:
                            return PREFAB_CHINOOK;
                        case NormalVehicleType.RidableHorse:
                            return PREFAB_RIDABLEHORSE;
                        case NormalVehicleType.WorkCart:
                            return PREFAB_WORKCART;
                        case NormalVehicleType.SedanRail:
                            return PREFAB_SEDANRAIL;
                        case NormalVehicleType.MagnetCrane:
                            return PREFAB_MAGNET_CRANE;
                        case NormalVehicleType.SubmarineSolo:
                            return PREFAB_SUBMARINE_SOLO;
                        case NormalVehicleType.SubmarineDuo:
                            return PREFAB_SUBMARINE_DUO;
                        case NormalVehicleType.Snowmobile:
                            return PREFAB_SNOWMOBILE;
                        case NormalVehicleType.TomahaSnowmobile:
                            return PREFAB_SNOWMOBILE_TOMAHA;
                        case NormalVehicleType.PedalBike:
                            return PREFAB_PEDALBIKE;
                        case NormalVehicleType.PedalTrike:
                            return PREFAB_PEDALTRIKE;
                        case NormalVehicleType.MotorBike:
                            return PREFAB_MOTORBIKE;
                        case NormalVehicleType.MotorBike_SideCar:
                            return PREFAB_MOTORBIKE_SIDECAR;
                        case NormalVehicleType.Kayak:
                            return PREFAB_KAYAK;
                        default:
                            return null;
                    }
                }
                return null;
            }

            protected virtual string GetVehicleCustomPrefab(string vehicleType)
            {
                if (!configData.global.useCustomVehicles) return string.Empty;
                CustomVehicleType normalVehicleType;
                if (Enum.TryParse(vehicleType, out normalVehicleType) && Enum.IsDefined(typeof(CustomVehicleType), normalVehicleType))
                {
                    switch (normalVehicleType)
                    {
                        case CustomVehicleType.ATV:
                            return PREFAB_ATV;
                        case CustomVehicleType.RaceSofa:
                            return PREFAB_SOFA;
                        case CustomVehicleType.WaterBird:
                            return PREFAB_WATERBIRD;
                        case CustomVehicleType.WarBird:
                            return PREFAB_WARBIRD;
                        case CustomVehicleType.LittleBird:
                            return PREFAB_LITTLEBIRD;
                        case CustomVehicleType.Fighter:
                            return PREFAB_FIGHTER;
                        case CustomVehicleType.OldFighter:
                            return PREFAB_OLDFIGHTER;
                        case CustomVehicleType.FighterBus:
                            return PREFAB_FIGHTERBUS;
                        case CustomVehicleType.WarBus:
                            return PREFAB_WARBUS;
                        case CustomVehicleType.AirBus:
                            return PREFAB_AIRBUS;
                        case CustomVehicleType.PatrolHeli:
                            return PREFAB_PATROLHELI;
                        case CustomVehicleType.RustWing:
                            return PREFAB_RUSTWING;
                        case CustomVehicleType.RustWingDetailed:
                            return PREFAB_RUSTWINGDETAILED;
                        case CustomVehicleType.RustWingDetailedOld:
                            return PREFAB_RUSTWINGDETAILEDOLD;
                        case CustomVehicleType.TinFighter:
                            return PREFAB_TINFIGHTER;
                        case CustomVehicleType.TinFighterDetailed:
                            return PREFAB_TINFIGHTERDETAILED;
                        case CustomVehicleType.TinFighterDetailedOld:
                            return PREFAB_TINFIGHTERDETAILEDOLD;
                        case CustomVehicleType.MarsFighter:
                            return PREFAB_MARSFIGHTER;
                        case CustomVehicleType.MarsFighterDetailed:
                            return PREFAB_MARSFIGHTERDETAILED;
                        case CustomVehicleType.SkyPlane:
                            return PREFAB_SKYPLANE;
                        case CustomVehicleType.SkyBoat:
                            return PREFAB_SKYBOAT;
                        case CustomVehicleType.TwistedTruck:
                            return PREFAB_TWISTEDTRUCK;
                        case CustomVehicleType.TrainWreck:
                            return PREFAB_TRIANWRECK;
                        case CustomVehicleType.TrainWrecker:
                            return PREFAB_TRIANWRECKER;
                        case CustomVehicleType.Santa:
                            return PREFAB_SANTA;
                        case CustomVehicleType.WarSanta:
                            return PREFAB_WARSANTA;
                        case CustomVehicleType.Witch:
                            return PREFAB_WITCH;
                        case CustomVehicleType.MagicCarpet:
                            return PREFAB_MAGICCARPET;
                        case CustomVehicleType.Ah69t:
                            return PREFAB_AH69T;
                        case CustomVehicleType.Ah69r:
                            return PREFAB_AH69R;
                        case CustomVehicleType.Ah69a:
                            return PREFAB_AH69A;
                        case CustomVehicleType.Mavik:
                            return PREFAB_MAVIK;
                        case CustomVehicleType.HeavyFighter:
                            return PREFAB_HEAVYFIGHTER;
                        case CustomVehicleType.PorcelainCommander:
                            return PREFAB_PORCELAINCOMMANDER;
                        case CustomVehicleType.DuneBuggie:
                            return PREFAB_DUNEBUGGIE;
                        case CustomVehicleType.DuneTruckArmed:
                            return PREFAB_DUNETRUCKARMED;
                        case CustomVehicleType.DuneTruckUnArmed:
                            return PREFAB_DUNETRUCKUNARMED;
                        case CustomVehicleType.DoomsDayDiscoVan:
                            return PREFAB_DOOMSDAYDISCOVAN;
                        case CustomVehicleType.ForkLift:
                            return PREFAB_FORKLIFT;
                        case CustomVehicleType.LawnMower:
                            return PREFAB_LAWNMOWER;
                        case CustomVehicleType.Chariot:
                            return PREFAB_CHARIOT;
                        case CustomVehicleType.SoulHarvester:
                            return PREFAB_SOULHARVESTER;
                        default:
                            return null;
                    }
                }
                return null;
            }

            public virtual BaseEntity SpawnVehicle(BasePlayer player, Vehicle vehicle, Vector3 position, Quaternion rotation)
            {
                var prefab = GetVehiclePrefab(vehicle.VehicleType);
                if (string.IsNullOrEmpty(prefab))
                {
                    prefab = GetVehicleCustomPrefab(vehicle.VehicleType);
                    if (string.IsNullOrEmpty(prefab)) throw new ArgumentException($"Prefab not found for {vehicle.VehicleType}");
                }
                var entity = GameManager.server.CreateEntity(prefab, position, rotation);
                if (entity == null)
                {
                    return null;
                }
                PreSetupVehicle(entity, vehicle, player);
                entity.Spawn();
                SetupVehicle(entity, vehicle, player);
                if (!entity.IsDestroyed)
                {
                    Instance.CacheVehicleEntity(entity, vehicle, player);
                    return ModifyVehicle(entity, vehicle, player);
                }
                Instance.Print(player, Instance.Lang("NotSpawnedOrRecalled", player.UserIDString, DisplayName));
                return null;
            }

            #region Setup

            public virtual void PreSetupVehicle(BaseEntity entity, Vehicle vehicle, BasePlayer player)
            {
                entity.enableSaving = configData.global.storeVehicle;
                entity.OwnerID = player.userID;
            }

            public virtual void SetupVehicle(BaseEntity entity, Vehicle vehicle, BasePlayer player, bool justCreated = true)
            {
                if (MaxHealth > 0 && Math.Abs(MaxHealth - entity.MaxHealth()) > 0f)
                {
                    (entity as BaseCombatEntity)?.InitializeHealth(MaxHealth, MaxHealth);
                }

                var helicopterVehicle = entity as BaseHelicopter;
                if (helicopterVehicle != null)
                {
                    if (configData.global.noServerGibs)
                    {
                        helicopterVehicle.serverGibs.guid = string.Empty;
                    }
                    if (configData.global.noFireBall)
                    {
                        helicopterVehicle.fireBall.guid = string.Empty;
                    }
                    if (configData.global.noMapMarker)
                    {
                        var ch47Helicopter = entity as CH47Helicopter;
                        if (ch47Helicopter != null)
                        {
                            if (ch47Helicopter.mapMarkerInstance)
                            {
                                ch47Helicopter.mapMarkerInstance.Kill();
                            }
                            ch47Helicopter.mapMarkerEntityPrefab.guid = string.Empty;
                        }
                    }
                }
                if (!configData.global.preventShattering) return;
                var magnetLiftable = entity.GetComponent<MagnetLiftable>();
                if (magnetLiftable != null)
                {
                    UnityEngine.Object.Destroy(magnetLiftable);
                }
            }

            private BaseEntity ModifyVehicle(BaseEntity entity, Vehicle vehicle, BasePlayer player)
            {
                if (entity is RidableHorse) // Thanks to Beee :)
                {
                    RidableHorse ridableHorse = entity as RidableHorse;
                    string randBreed = configData.normalVehicles.ridableHorse.Breeds[Random.Range(0, configData.normalVehicles.ridableHorse.Breeds.Count)];
                    int breedIndex;
                    if (configData.normalVehicles.ridableHorse.BreedsRef.TryGetValue(randBreed, out breedIndex))
                        ridableHorse.ApplyBreed(breedIndex);
                    if (!configData.normalVehicles.ridableHorse.IsDoubleSaddle) return entity;

                    ridableHorse.SetFlag(BaseEntity.Flags.Reserved9, false, networkupdate: false);
                    ridableHorse.SetFlag(BaseEntity.Flags.Reserved10, true, networkupdate: false);
                    ridableHorse.UpdateMountFlags();

                    return entity;
                }
                if (entity is Tugboat)
                {
                    Tugboat tug = entity as Tugboat;
                    tug.engineThrust *= configData.normalVehicles.tugboat.speedMultiplier;
                    // Code for adding all teammates to tugboats.
                    if (!configData.normalVehicles.tugboat.autoAuth) return entity;
                    AuthTeamOnTugboat(tug, player);
                    return entity;
                }

                if (entity is AttackHelicopter)
                {
                    AttackHelicopter attackHelicopter = entity as AttackHelicopter;
                    attackHelicopter.torqueScale *= configData.normalVehicles.attackHelicopter.rotationScale;
                    attackHelicopter.liftFraction = configData.normalVehicles.attackHelicopter.liftFraction;
                    return entity;
                }
                if (entity is HotAirBalloon && vehicle.VehicleType.Equals(NormalVehicleType.ArmoredHotAirBalloon.ToString()))
                {
                    HotAirBalloon HAB = entity as HotAirBalloon;
                    Item armor = ItemManager.CreateByItemID(ITEMID_HOTAIRBALLOON_ARMOR); // Using int instead of string prefab.
                    if (armor == null)
                    {
                        Debug.Log("[VehicleLicence] Please report this to the developer/maintainer. PREFAB_HOTAIRBALLOON_ARMOR's item is NULL");
                        return entity;
                    }
                    ItemModHABEquipment component = armor.info.GetComponent<ItemModHABEquipment>();
                    if (component == null) return entity;
                    HotAirBalloonEquipment equipment = GameManager.server.CreateEntity(component.Prefab.resourcePath, HAB.transform.position, HAB.transform.rotation) as HotAirBalloonEquipment;
                    equipment.SetParent(HAB, true);
                    equipment.Spawn();
                    equipment.DelayNextUpgradeOnRemoveDuration = equipment.DelayNextUpgradeOnRemoveDuration;
                    armor.UseItem();
                    HAB.SendNetworkUpdateImmediate();
                    return entity;
                }

                // TODO: Maybe increase speed of other vehicles.

                if (entity is ScrapTransportHelicopter)
                {
                    ScrapTransportHelicopter scrap = entity as ScrapTransportHelicopter;
                    scrap.torqueScale *= configData.normalVehicles.transportHelicopter.rotationScale;
                    scrap.liftFraction = configData.normalVehicles.transportHelicopter.liftFraction;
                    return entity;
                }

                if (entity is Minicopter)
                {
                    Minicopter mini = entity as Minicopter;
                    // Debug.Log($"Default mini.liftDotMax: {mini.liftDotMax}\nDefault mini.altForceDotMin {mini.altForceDotMin}");
                    // mini.altForceDotMin = 0;
                    // mini.liftDotMax = 0.2f;
                    mini.torqueScale *= configData.normalVehicles.miniCopter.rotationScale;
                    mini.liftFraction = configData.normalVehicles.miniCopter.liftFraction;
                    return entity;
                }
                return entity;
            }

            #endregion Setup

            #endregion Spawn

            #region Recall

            public virtual void PreRecallVehicle(BasePlayer player, Vehicle vehicle, Vector3 position, Quaternion rotation)
            {
                if (configData.global.dismountAllPlayersRecall)
                {
                    DismountAllPlayers(vehicle.Entity);
                }

                if (CanDropInventory())
                {
                    TryDropVehicleInventory(player, vehicle);
                }

                if (vehicle.Entity.HasParent())
                {
                    vehicle.Entity.SetParent(null, true, true);
                }
            }

            public virtual void PostRecallVehicle(BasePlayer player, Vehicle vehicle, Vector3 position, Quaternion rotation)
            {
            }

            #region DropInventory

            protected virtual bool CanDropInventory()
            {
                return false;
            }

            private void TryDropVehicleInventory(BasePlayer player, Vehicle vehicle)
            {
                var droppedItemContainer = DropVehicleInventory(player, vehicle);
                if (droppedItemContainer != null)
                {
                    Instance.Print(player, Instance.Lang("VehicleInventoryDropped", player.UserIDString, DisplayName));
                }
            }

            protected virtual DroppedItemContainer DropVehicleInventory(BasePlayer player, Vehicle vehicle)
            {
                var inventories = GetInventories(vehicle.Entity);
                foreach (var inventory in inventories)
                {
                    if (inventory != null)
                    {
                        return inventory.Drop(PREFAB_ITEM_DROP, vehicle.Entity.GetDropPosition(), vehicle.Entity.transform.rotation, 0);
                    }
                }
                return null;
            }

            #endregion DropInventory

            #region Train Car

            protected bool TryGetTrainCarPositionAndRotation(BasePlayer player, Vehicle vehicle, ref string reason, ref Vector3 original, ref Quaternion rotation)
            {
                float distResult;
                TrainTrackSpline splineResult;
                if (!TrainTrackSpline.TryFindTrackNear(original, Distance, out splineResult, out distResult))
                {
                    reason = Instance.Lang("TooFarTrainTrack", player.UserIDString);
                    return false;
                }

                var position = splineResult.GetPosition(distResult);
                if (!SpaceIsClearForTrainTrack(vehicle, position, rotation))
                {
                    reason = Instance.Lang("TooCloseTrainBarricadeOrWorkCart", player.UserIDString);
                    return false;
                }

                original = position;
                reason = null;
                return true;
            }

            protected bool TryMoveToTrainTrackNear(TrainCar trainCar)
            {
                float distResult;
                TrainTrackSpline splineResult;
                if (TrainTrackSpline.TryFindTrackNear(trainCar.GetFrontWheelPos(), 2f, out splineResult, out distResult))
                {
                    trainCar.FrontWheelSplineDist = distResult;
                    Vector3 tangent;
                    var positionAndTangent = splineResult.GetPositionAndTangent(trainCar.FrontWheelSplineDist, trainCar.transform.forward, out tangent);
                    trainCar.SetTheRestFromFrontWheelData(ref splineResult, positionAndTangent, tangent, trainCar.localTrackSelection, null, true);
                    trainCar.FrontTrackSection = splineResult;
                    if (trainCar.SpaceIsClear())
                    {
                        return true;
                    }
                }
                return false;
            }

            protected bool SpaceIsClearForTrainTrack(Vehicle vehicle, Vector3 position, Quaternion rotation)
            {
                var colliders = Pool.Get<List<Collider>>();
                if (vehicle.Entity == null)
                {
                    var prefab = GetVehiclePrefab(vehicle.VehicleType);
                    
                    if (string.IsNullOrEmpty(prefab)) prefab = GetVehicleCustomPrefab(vehicle.VehicleType); // In case of custom vehicle.
                    
                    if (!string.IsNullOrEmpty(prefab))
                    {
                        var trainEngine = GameManager.server.FindPrefab(prefab)?.GetComponent<TrainEngine>();
                        if (trainEngine != null)
                        {
                            GamePhysics.OverlapOBB(new OBB(position, trainEngine.transform.lossyScale, rotation, trainEngine.bounds), colliders, Layers.Mask.Vehicle_World);
                        }
                    }
                }
                else
                {
                    GamePhysics.OverlapOBB(new OBB(position, vehicle.Entity.transform.lossyScale, rotation, vehicle.Entity.bounds), colliders, Layers.Mask.Vehicle_World);
                }
                var free = true;
                foreach (var item in colliders)
                {
                    var baseEntity = item.ToBaseEntity();
                    if (baseEntity == vehicle.Entity)
                    {
                        continue;
                    }
                    free = false;
                    break;
                }
                Pool.FreeUnmanaged(ref colliders);
                return free;
            }

            #endregion

            #endregion Recall

            #region Refund

            protected virtual bool CanRefundFuel(bool isCrash, bool isUnload)
            {
                return false;
            }

            protected virtual bool CanRefundInventory(bool isCrash, bool isUnload)
            {
                return false;
            }

            protected virtual void CollectVehicleItems(List<Item> items, Vehicle vehicle, bool isCrash, bool isUnload)
            {
                if (CanRefundFuel(isCrash, isUnload))
                {
                    var fuelSystem = GetFuelSystem(vehicle.Entity);
                    if (fuelSystem is EntityFuelSystem entityFuelSystem)
                    {
                        var fuelContainer = entityFuelSystem.GetFuelContainer();
                        if (fuelContainer != null && fuelContainer.inventory != null)
                        {
                            items.AddRange(fuelContainer.inventory.itemList);
                        }
                    }
                }
                if (CanRefundInventory(isCrash, isUnload))
                {
                    var inventories = GetInventories(vehicle.Entity);
                    foreach (var inventory in inventories)
                    {
                        items.AddRange(inventory.itemList);
                    }
                }
            }

            public void RefundVehicleItems(Vehicle vehicle, bool isCrash, bool isUnload)
            {
                var collect = Pool.Get<List<Item>>();

                CollectVehicleItems(collect, vehicle, isCrash, isUnload);

                if (collect.Count > 0)
                {
                    var player = RustCore.FindPlayerById(vehicle.PlayerId);
                    if (player == null)
                    {
                        DropItemContainer(vehicle.Entity, vehicle.PlayerId, collect);
                    }
                    else
                    {
                        for (var i = collect.Count - 1; i >= 0; i--)
                        {
                            var item = collect[i];
                            player.GiveItem(item);
                        }

                        if (player.IsConnected)
                        {
                            Instance.Print(player, Instance.Lang("RefundedVehicleItems", player.UserIDString, DisplayName));
                        }
                    }
                }
                Pool.FreeUnmanaged(ref collect);
            }

            #endregion Refund

            #region GiveFuel

            protected void TryGiveFuel(BaseEntity entity, IFuelVehicle iFuelVehicle)
            {
                if (iFuelVehicle == null || iFuelVehicle.SpawnFuelAmount <= 0)
                {
                    return;
                }
                var fuelSystem = GetFuelSystem(entity);
                if (fuelSystem is EntityFuelSystem entityFuelSystem)
                {
                    var fuelContainer = entityFuelSystem.GetFuelContainer();
                    if (fuelContainer != null && fuelContainer.inventory != null)
                    {
                        var fuelItem = ItemManager.CreateByItemID(ITEMID_FUEL, iFuelVehicle.SpawnFuelAmount);
                        if (!fuelItem.MoveToContainer(fuelContainer.inventory))
                        {
                            fuelItem.Remove();
                        }
                    }
                }
            }

            #endregion GiveFuel

            #region Permission

            public double GetCooldown(BasePlayer player, bool isSpawn)
            {
                var cooldown = isSpawn ? SpawnCooldown : RecallCooldown;
                foreach (var entry in CooldownPermissions)
                {
                    var currentCooldown = isSpawn ? entry.Value.spawnCooldown : entry.Value.recallCooldown;
                    if (cooldown > currentCooldown && Instance.permission.UserHasPermission(player.UserIDString, entry.Key))
                    {
                        cooldown = currentCooldown;
                    }
                }
                return cooldown;
            }

            #endregion Permission

            #region TryGetVehicleParams

            public virtual bool TryGetVehicleParams(BasePlayer player, Vehicle vehicle, out string reason, ref Vector3 spawnPos, ref Quaternion spawnRot)
            {
                Vector3 original;
                Quaternion rotation;
                if (!TryGetPositionAndRotation(player, vehicle, out reason, out original, out rotation))
                {
                    return false;
                }

                CorrectPositionAndRotation(player, vehicle, original, rotation, out spawnPos, out spawnRot);
                return true;
            }

            protected virtual float GetSpawnRotationAngle()
            {
                return 90f;
            }

            protected virtual Vector3 GetOriginalPosition(BasePlayer player)
            {
                if (configData.global.spawnLookingAt || IsWaterVehicle || IsTrainVehicle)
                {
                    return GetGroundPositionLookingAt(player, Distance, !IsTrainVehicle);
                }

                return player.transform.position;
            }

            protected virtual bool TryGetPositionAndRotation(BasePlayer player, Vehicle vehicle, out string reason, out Vector3 original, out Quaternion rotation)
            {
                original = GetOriginalPosition(player);
                rotation = Quaternion.identity;
                if (MinDistanceForPlayers > 0)
                {
                    var nearbyPlayers = Pool.Get<List<BasePlayer>>();
                    Vis.Entities(original, MinDistanceForPlayers, nearbyPlayers, Layers.Mask.Player_Server);
                    var flag = nearbyPlayers.Any(x => x.userID.IsSteamId() && x != player);
                    Pool.FreeUnmanaged(ref nearbyPlayers);
                    if (flag)
                    {
                        reason = Instance.Lang("PlayersOnNearby", player.UserIDString, DisplayName);
                        return false;
                    }
                }
                if (IsWaterVehicle && !IsInWater(original))
                {
                    reason = Instance.Lang("NotLookingAtWater", player.UserIDString, DisplayName);
                    return false;
                }
                RaycastHit hit;
                if (IsWaterVehicle && Physics.Raycast(original, player.eyes.MovementForward(), out hit, 100))
                {
                    if (hit.GetEntity() is PaddlingPool)
                    {
                        reason = Instance.Lang("NotLookingAtWater", player.UserIDString, DisplayName);
                        return false;
                    }
                    List<BaseEntity> pools = Pool.Get<List<BaseEntity>>();
                    Vis.Entities(original, 0.5f, pools, Layers.Mask.Deployed);
                    if (pools.Any(x => x is PaddlingPool))
                    {
                        reason = Instance.Lang("NotLookingAtWater", player.UserIDString, DisplayName);
                        Pool.FreeUnmanaged(ref pools);
                        return false;
                    }
                    Pool.FreeUnmanaged(ref pools);
                }
                reason = null;
                return true;
            }

            protected virtual void CorrectPositionAndRotation(BasePlayer player, Vehicle vehicle, Vector3 original, Quaternion rotation, out Vector3 spawnPos, out Quaternion spawnRot)
            {
                spawnPos = original;
                if (IsTrainVehicle)
                {
                    var forward = player.eyes.HeadForward().WithY(0);
                    spawnRot = forward != Vector3.zero ? Quaternion.LookRotation(forward) : Quaternion.identity;
                    return;
                }
                if (configData.global.spawnLookingAt)
                {
                    var needGetGround = true;
                    if (IsWaterVehicle)
                    {
                        RaycastHit hit;
                        if (Physics.Raycast(spawnPos, Vector3.up, out hit, 100, LAYER_GROUND) && hit.GetEntity() is StabilityEntity)
                        {
                            needGetGround = false; //At the dock
                        }

                        if (IsWaterVehicle && (int)player.transform.position.y >= -1)
                        {
                            if (vehicle.VehicleType == "Tugboat" && Vector3.Distance(spawnPos, player.transform.position) < configData.normalVehicles.tugboat.Distance
                               && spawnPos.y - player.transform.position.y < configData.normalVehicles.tugboat.Distance)
                            {
                                spawnPos += player.eyes.MovementForward() * configData.normalVehicles.tugboat.Distance;
                            }
                            spawnPos.y = player.transform.position.y;
                        }
                        else if (IsWaterVehicle && (int)player.transform.position.y < -1)
                        {
                            // Math.Abs(Math.Abs(spawnPos.y) - Math.Abs(player.transform.position.y)) >= configData.normalVehicles.tugboat.Distance
                            if (vehicle.VehicleType == "Tugboat" && Vector3.Distance(spawnPos, player.transform.position)
                                    < configData.normalVehicles.tugboat.Distance && spawnPos.y - player.transform.position.y < configData.normalVehicles.tugboat.Distance)
                            {
                                spawnPos += player.eyes.MovementForward() * configData.normalVehicles.tugboat.Distance;
                            }
                            spawnPos.y = player.transform.position.y - 3;
                        }
                    }
                    else
                    {
                        if (TryGetCenterOfFloorNearby(ref spawnPos))
                        {
                            needGetGround = false; //At the floor
                            if (vehicle.VehicleType == "TransportHelicopter" && Vector3.Distance(spawnPos, player.transform.position)
                               < configData.normalVehicles.transportHelicopter.Distance)
                            { spawnPos += player.eyes.MovementForward() * configData.normalVehicles.transportHelicopter.Distance; }
                        }
                    }
                    if (needGetGround)
                    {
                        spawnPos = GetGroundPosition(spawnPos);
                        if (IsWaterVehicle && (int)player.transform.position.y >= -1 && spawnPos.y <= -1)
                        {
                            if (vehicle.VehicleType == "Tugboat" && Vector3.Distance(spawnPos, player.transform.position)
                                < configData.normalVehicles.tugboat.Distance && spawnPos.y - player.transform.position.y < configData.normalVehicles.tugboat.Distance)
                            {
                                spawnPos += player.eyes.MovementForward() * configData.normalVehicles.tugboat.Distance;
                            }
                            spawnPos.y = -1;
                        }
                        else if (IsWaterVehicle && (int)player.transform.position.y < -1)
                        {
                            if (vehicle.VehicleType == "Tugboat" && Vector3.Distance(spawnPos, player.transform.position)
                                < configData.normalVehicles.tugboat.Distance && spawnPos.y - player.transform.position.y < configData.normalVehicles.tugboat.Distance)
                            {
                                spawnPos += player.eyes.MovementForward() * configData.normalVehicles.tugboat.Distance;
                            }
                            spawnPos.y = player.transform.position.y;
                        }
                        if (vehicle.VehicleType == "TransportHelicopter" && Vector3.Distance(spawnPos, player.transform.position)
                            < configData.normalVehicles.transportHelicopter.Distance)
                        { spawnPos += player.eyes.MovementForward() * configData.normalVehicles.transportHelicopter.Distance; }
                    }
                }
                else
                {
                    GetPositionWithNoPlayersNearby(player, ref spawnPos);
                }

                var normalized = (spawnPos - player.transform.position).normalized;
                var angle = normalized != Vector3.zero ? Quaternion.LookRotation(normalized).eulerAngles.y : Random.Range(0f, 360f);
                var rotationAngle = GetSpawnRotationAngle();
                spawnRot = Quaternion.Euler(Vector3.up * (angle + rotationAngle));

            }

            private void GetPositionWithNoPlayersNearby(BasePlayer player, ref Vector3 spawnPos)
            {
                var minDistance = Mathf.Min(MinDistanceForPlayers, 2.5f);
                var maxDistance = Mathf.Max(Distance, minDistance);

                var players = new BasePlayer[1];
                var sourcePos = spawnPos;
                for (var i = 0; i < 10; i++)
                {
                    spawnPos.x = sourcePos.x + Random.Range(minDistance, maxDistance) * (Random.value >= 0.5f ? 1 : -1);
                    spawnPos.z = sourcePos.z + Random.Range(minDistance, maxDistance) * (Random.value >= 0.5f ? 1 : -1);

                    if (BaseEntity.Query.Server.GetPlayersInSphere(spawnPos, minDistance, players, p => p.userID.IsSteamId() && p != player) == 0)
                    {
                        break;
                    }
                }
                spawnPos = GetGroundPosition(spawnPos);
            }

            private bool TryGetCenterOfFloorNearby(ref Vector3 spawnPos)
            {
                var buildingBlocks = Pool.Get<List<BuildingBlock>>();
                Vis.Entities(spawnPos, 2f, buildingBlocks, Layers.Mask.Construction);
                if (buildingBlocks.Count > 0)
                {
                    var position = spawnPos;
                    var closestBuildingBlock = buildingBlocks
                            .Where(x => !x.ShortPrefabName.Contains("wall"))
                            .OrderBy(x => (x.transform.position - position).magnitude).FirstOrDefault();
                    if (closestBuildingBlock != null)
                    {
                        var worldSpaceBounds = closestBuildingBlock.WorldSpaceBounds();
                        spawnPos = worldSpaceBounds.position;
                        spawnPos.y += worldSpaceBounds.extents.y;
                        Pool.FreeUnmanaged(ref buildingBlocks);
                        return true;
                    }
                }
                Pool.FreeUnmanaged(ref buildingBlocks);
                return false;
            }

            #endregion TryGetVehicleParams
        }

        public abstract class FuelVehicleSettings : BaseVehicleSettings, IFuelVehicle
        {
            public int SpawnFuelAmount { get; set; }
            public bool RefundFuelOnKill { get; set; } = true;
            public bool RefundFuelOnCrash { get; set; } = true;

            public override void SetupVehicle(BaseEntity entity, Vehicle vehicle, BasePlayer player, bool justCreated = true)
            {
                if (justCreated)
                {
                    TryGiveFuel(entity, this);
                }
                base.SetupVehicle(entity, vehicle, player, justCreated);
            }

            protected override bool CanRefundFuel(bool isCrash, bool isUnload)
            {
                return isUnload || (isCrash ? RefundFuelOnCrash : RefundFuelOnKill);
            }
        }

        public abstract class InventoryVehicleSettings : BaseVehicleSettings, IInventoryVehicle
        {
            public bool RefundInventoryOnKill { get; set; } = true;
            public bool RefundInventoryOnCrash { get; set; } = true;
            public bool DropInventoryOnRecall { get; set; }

            protected override bool CanDropInventory()
            {
                return DropInventoryOnRecall;
            }

            protected override bool CanRefundInventory(bool isCrash, bool isUnload)
            {
                return isUnload || (isCrash ? RefundInventoryOnCrash : RefundInventoryOnKill);
            }
        }

        public abstract class InvFuelVehicleSettings : BaseVehicleSettings, IFuelVehicle, IInventoryVehicle
        {
            public int SpawnFuelAmount { get; set; }
            public bool RefundFuelOnKill { get; set; } = true;
            public bool RefundFuelOnCrash { get; set; } = true;
            public bool RefundInventoryOnKill { get; set; } = true;
            public bool RefundInventoryOnCrash { get; set; } = true;
            public bool DropInventoryOnRecall { get; set; }

            public override void SetupVehicle(BaseEntity entity, Vehicle vehicle, BasePlayer player, bool justCreated = true)
            {
                if (justCreated)
                {
                    TryGiveFuel(entity, this);
                }
                base.SetupVehicle(entity, vehicle, player, justCreated);
            }

            protected override bool CanDropInventory()
            {
                return DropInventoryOnRecall;
            }

            protected override bool CanRefundInventory(bool isCrash, bool isUnload)
            {
                return isUnload || (isCrash ? RefundInventoryOnCrash : RefundInventoryOnKill);
            }

            protected override bool CanRefundFuel(bool isCrash, bool isUnload)
            {
                return isUnload || (isCrash ? RefundFuelOnCrash : RefundFuelOnKill);
            }
        }

        #endregion BaseSettings

        #region Interface

        public interface IFuelVehicle
        {
            [JsonProperty(PropertyName = "Amount Of Fuel To Spawn", Order = 20)]
            int SpawnFuelAmount { get; set; }

            [JsonProperty(PropertyName = "Refund Fuel On Kill", Order = 21)]
            bool RefundFuelOnKill { get; set; }

            [JsonProperty(PropertyName = "Refund Fuel On Crash", Order = 22)]
            bool RefundFuelOnCrash { get; set; }
        }

        public interface IInventoryVehicle
        {
            [JsonProperty(PropertyName = "Refund Inventory On Kill", Order = 30)]
            bool RefundInventoryOnKill { get; set; }

            [JsonProperty(PropertyName = "Refund Inventory On Crash", Order = 31)]
            bool RefundInventoryOnCrash { get; set; }

            [JsonProperty(PropertyName = "Drop Inventory Items When Vehicle Recall", Order = 49)]
            bool DropInventoryOnRecall { get; set; }
        }

        public interface IModularVehicle
        {
            [JsonProperty(PropertyName = "Refund Engine Items On Kill", Order = 40)]
            bool RefundEngineOnKill { get; set; }

            [JsonProperty(PropertyName = "Refund Engine Items On Crash", Order = 41)]
            bool RefundEngineOnCrash { get; set; }

            [JsonProperty(PropertyName = "Refund Module Items On Kill", Order = 42)]
            bool RefundModuleOnKill { get; set; }

            [JsonProperty(PropertyName = "Refund Module Items On Crash", Order = 43)]
            bool RefundModuleOnCrash { get; set; }
        }

        public interface IAmmoVehicle
        {
            [JsonProperty(PropertyName = "Amount Of Ammo To Spawn", Order = 20)]
            int SpawnAmmoAmount { get; set; }
        }

        public interface ITrainVehicle
        {
        }

        #endregion Interface

        #region Struct

        public struct CooldownPermission
        {
            public double spawnCooldown;
            public double recallCooldown;
        }

        public struct ModuleItem
        {
            public string shortName;
            public float healthPercentage;
        }

        public struct EngineItem
        {
            public string shortName;
            public float conditionPercentage;
        }

        public struct PriceInfo
        {
            public int amount;
            public string displayName;
        }

        public struct TrainComponent
        {
            public TrainComponentType type;
        }

        #endregion Struct

        #region VehicleSettings

        public class PedalBikeSettings : BaseVehicleSettings
        {
        }

        public class PedalTrikeSettings : BaseVehicleSettings
        {
        }

        public class MotorBikeSettings : FuelVehicleSettings
        {
            protected override IFuelSystem GetFuelSystem(BaseEntity entity)
            {
                return (entity as Bike)?.GetFuelSystem();
            }
        }

        public class MotorBikeSidecarSettings : FuelVehicleSettings
        {
            protected override IFuelSystem GetFuelSystem(BaseEntity entity)
            {
                return (entity as Bike)?.GetFuelSystem();
            }
        }

        public class AtvSettings : FuelVehicleSettings
        {
            protected override IFuelSystem GetFuelSystem(BaseEntity entity)
            {
                return (entity as Bike)?.GetFuelSystem();
            }
        }

        public class RaceSofaSettings : FuelVehicleSettings
        {
            protected override IFuelSystem GetFuelSystem(BaseEntity entity)
            {
                return (entity as Bike)?.GetFuelSystem();
            }
        }

        public class KayakSettings : BaseVehicleSettings
        {
            public override bool IsWaterVehicle => true;
        }

        public class SedanSettings : BaseVehicleSettings
        {
        }

        public class ChinookSettings : BaseVehicleSettings
        {
        }

        public class RowboatSettings : InvFuelVehicleSettings
        {
            public override bool IsWaterVehicle => true;

            protected override IFuelSystem GetFuelSystem(BaseEntity entity)
            {
                return (entity as MotorRowboat)?.GetFuelSystem();
            }

            protected override IEnumerable<ItemContainer> GetInventories(BaseEntity entity)
            {
                yield return (entity as MotorRowboat)?.storageUnitInstance.Get(true)?.inventory;
            }
        }

        public class RhibSettings : RowboatSettings
        {
        }

        public class TugboatSettings : FuelVehicleSettings
        {
            public override bool IsWaterVehicle => true;

            [JsonProperty(PropertyName = "Speed Multiplier")]
            public float speedMultiplier { get; set; } = 1;

            [JsonProperty(PropertyName = "Auto Auth Teammates on spawn/recall")]
            public bool autoAuth { get; set; } = true;

            protected override IFuelSystem GetFuelSystem(BaseEntity entity)
            {
                return (entity as MotorRowboat)?.GetFuelSystem();
            }
        }


        public class HotAirBalloonSettings : InvFuelVehicleSettings
        {
            protected override float GetSpawnRotationAngle()
            {
                return 180f;
            }

            protected override IFuelSystem GetFuelSystem(BaseEntity entity)
            {
                return (entity as HotAirBalloon)?.fuelSystem;
            }

            protected override IEnumerable<ItemContainer> GetInventories(BaseEntity entity)
            {
                yield return (entity as HotAirBalloon)?.storageUnitInstance.Get(true)?.inventory;
            }
        }

        public class ArmoredHotAirBalloonSettings : HotAirBalloonSettings
        {
        }

        public class MiniCopterSettings : FuelVehicleSettings
        {
            public override bool IsFightVehicle => true;

            [JsonProperty("Rotation Scale")]
            public float rotationScale = 1.0f;

            [JsonProperty("Lift Fraction")]
            public float liftFraction = 0.25f;

            [JsonProperty("Seconds to pause flyhack when dismount from Mini Copter.")]
            public int flyHackPause;

            [JsonProperty("Instant Engine Start-up (instant take-off)")]
            public bool instantTakeoff;

            protected override IFuelSystem GetFuelSystem(BaseEntity entity)
            {
                return (entity as Minicopter)?.GetFuelSystem();
            }
        }

        public class AttackHelicopterSettings : InvFuelVehicleSettings
        {
            private const int HV_AMMO_ITEM_ID = -1841918730;
            private const int INCENDIARY_AMMO_ITEM_ID = 1638322904;
            private const int FLARE_ITEM_ID = 304481038;

            [JsonProperty("HV Rocket Spawn Amount")]
            public int HVSpawnAmmoAmount { get; set; }

            [JsonProperty("Incendiary Rocket Spawn Amount")]
            public int IncendiarySpawnAmmoAmount { get; set; }

            [JsonProperty("Flare Spawn Amount")]
            public int FlareSpawnAmmoAmount { get; set; }

            public override bool IsFightVehicle => true;

            [JsonProperty("Rotation Scale")]
            public float rotationScale = 1.0f;

            [JsonProperty("Lift Fraction")]
            public float liftFraction = 0.33f;

            [JsonProperty("Seconds to pause flyhack when dismount from Attack Helicopter.")]
            public int flyHackPause;

            [JsonProperty("Instant Engine Start-up (instant take-off)")]
            public bool instantTakeoff;

            protected override IFuelSystem GetFuelSystem(BaseEntity entity)
            {
                return (entity as AttackHelicopter)?.GetFuelSystem();
            }

            protected override IEnumerable<ItemContainer> GetInventories(BaseEntity entity)
            {
                yield return (entity as AttackHelicopter)?.GetRockets().inventory;
                yield return (entity as AttackHelicopter)?.GetTurret().inventory;
            }

            public override void SetupVehicle(BaseEntity entity, Vehicle vehicle, BasePlayer player, bool justCreated = true)
            {
                if (justCreated)
                {
                    TryGiveAmmo(entity);
                }
                base.SetupVehicle(entity, vehicle, player, justCreated);
            }

            private void TryGiveAmmo(BaseEntity entity)
            {
                if (entity == null || (HVSpawnAmmoAmount <= 0 && IncendiarySpawnAmmoAmount <= 0 && FlareSpawnAmmoAmount <= 0))
                {
                    return;
                }

                AttackHelicopterRockets ammoContainer = (entity as AttackHelicopter)?.GetRockets();

                if (ammoContainer == null || ammoContainer.inventory == null) return;

                Item ammoItem = ItemManager.CreateByItemID(HV_AMMO_ITEM_ID, HVSpawnAmmoAmount);
                if (!ammoItem.MoveToContainer(ammoContainer.inventory))
                {
                    ammoItem.Remove();
                }

                ammoItem = ItemManager.CreateByItemID(INCENDIARY_AMMO_ITEM_ID, IncendiarySpawnAmmoAmount);
                if (!ammoItem.MoveToContainer(ammoContainer.inventory))
                {
                    ammoItem.Remove();
                }

                ammoItem = ItemManager.CreateByItemID(FLARE_ITEM_ID, FlareSpawnAmmoAmount);
                if (!ammoItem.MoveToContainer(ammoContainer.inventory))
                {
                    ammoItem.Remove();
                }
            }
        }

        public class WaterBirdSettings : BaseVehicleSettings
        {
            public override bool IsFightVehicle => true;

            public override bool IsWaterVehicle => true;
            
            public override bool CustomVehicle => true;
        }

        public class WarBirdSettings : BaseVehicleSettings
        {
            public override bool IsFightVehicle => true;
            
            public override bool CustomVehicle => true;
        }

        public class LittleBirdSettings : BaseVehicleSettings
        {
            public override bool IsFightVehicle => true;
            
            public override bool CustomVehicle => true;
        }

        public class FighterSettings : BaseVehicleSettings
        {
            public override bool IsFightVehicle => true;
            
            public override bool CustomVehicle => true;
        }

        public class OldFighterSettings : BaseVehicleSettings
        {
            public override bool IsFightVehicle => true;
            
            public override bool CustomVehicle => true;
        }

        public class FighterBusSettings : BaseVehicleSettings
        {
            public override bool IsFightVehicle => true;
            
            public override bool CustomVehicle => true;
        }

        public class WarBusSettings : BaseVehicleSettings
        {
            public override bool IsFightVehicle => true;
            
            public override bool CustomVehicle => true;
        }

        public class AirBusSettings : BaseVehicleSettings
        {
            public override bool IsFightVehicle => true;
            
            public override bool CustomVehicle => true;
        }

        public class PatrolHelicopterSettings : BaseVehicleSettings
        {
            public override bool IsFightVehicle => true;
            
            public override bool CustomVehicle => true;
        }

        public class RustWingSettings : BaseVehicleSettings
        {
            public override bool IsFightVehicle => true;
            
            public override bool CustomVehicle => true;
        }

        public class RustWingDetailedSettings : BaseVehicleSettings
        {
            public override bool IsFightVehicle => true;
            
            public override bool CustomVehicle => true;
        }

        public class RustWingDetailedOldSettings : BaseVehicleSettings
        {
            public override bool IsFightVehicle => true;
            
            public override bool CustomVehicle => true;
        }

        public class TinFighterSettings : BaseVehicleSettings
        {
            public override bool IsFightVehicle => true;
            
            public override bool CustomVehicle => true;
        }

        public class TinFighterDetailedSettings : BaseVehicleSettings
        {
            public override bool IsFightVehicle => true;
            
            public override bool CustomVehicle => true;
        }

        public class TinFighterDetailedOldSettings : BaseVehicleSettings
        {
            public override bool IsFightVehicle => true;
            
            public override bool CustomVehicle => true;
        }

        public class MarsFighterSettings : BaseVehicleSettings
        {
            public override bool IsFightVehicle => true;
            
            public override bool CustomVehicle => true;
        }

        public class MarsFighterDetailedSettings : BaseVehicleSettings
        {
            public override bool IsFightVehicle => true;
            
            public override bool CustomVehicle => true;
        }

        public class SkyPlaneSettings : BaseVehicleSettings
        {
            public override bool IsFightVehicle => true;
            
            public override bool CustomVehicle => true;
        }

        public class SkyBoatSettings : BaseVehicleSettings
        {
            public override bool IsFightVehicle => true;

            public override bool IsWaterVehicle => true;
            
            public override bool CustomVehicle => true;
        }

        public class TwistedTruckSettings : BaseVehicleSettings
        {
            public override bool CustomVehicle => true;
        }

        public class TrainWreckSettings : BaseVehicleSettings
        {
            public override bool CustomVehicle => true;
        }

        public class TrainWreckerSettings : BaseVehicleSettings
        {
            public override bool CustomVehicle => true;
        }

        public class SantaSettings : BaseVehicleSettings
        {
            public override bool IsFightVehicle => true;
            
            public override bool CustomVehicle => true;
        }

        public class WarSantaSettings : BaseVehicleSettings
        {
            public override bool IsFightVehicle => true;
            
            public override bool CustomVehicle => true;
        }

        public class WitchSettings : BaseVehicleSettings
        {
            public override bool IsFightVehicle => true;
            
            public override bool CustomVehicle => true;
        }

        public class MagicCarpetSettings : BaseVehicleSettings
        {
            public override bool IsFightVehicle => true;
            
            public override bool CustomVehicle => true;
        }

        public class Ah69tSettings : BaseVehicleSettings
        {
            public override bool IsFightVehicle => true;
            
            public override bool CustomVehicle => true;
        }

        public class Ah69rSettings : BaseVehicleSettings
        {
            public override bool IsFightVehicle => true;
            
            public override bool CustomVehicle => true;
        }

        public class Ah69aSettings : BaseVehicleSettings
        {
            public override bool IsFightVehicle => true;
            
            public override bool CustomVehicle => true;
        }

        public class MavikSettings : BaseVehicleSettings
        {
            public override bool IsFightVehicle => true;
            
            public override bool CustomVehicle => true;
        }

        public class HeavyFighterSettings : BaseVehicleSettings
        {
            public override bool IsFightVehicle => true;
            
            public override bool CustomVehicle => true;
        }

        public class PorcelainCommanderSettings : BaseVehicleSettings
        {
            public override bool IsFightVehicle => true;
            
            public override bool CustomVehicle => true;
        }

        public class DuneBuggieSettings : BaseVehicleSettings
        {
            public override bool CustomVehicle => true;
        }

        public class DuneTruckArmedSettings : BaseVehicleSettings
        {
            public override bool CustomVehicle => true;
        }

        public class DuneTruckUnArmedSettings : BaseVehicleSettings
        {
            public override bool CustomVehicle => true;
        }

        public class DoomsDayDiscoVanSettings : BaseVehicleSettings
        {
            public override bool CustomVehicle => true;
        }

        public class ForkLiftSettings : BaseVehicleSettings
        {
            public override bool CustomVehicle => true;
        }

        public class LawnMowerSettings : BaseVehicleSettings
        {
            public override bool CustomVehicle => true;
        }

        public class ChariotSettings : BaseVehicleSettings
        {
            public override bool CustomVehicle => true;
        }

        public class SoulHarvesterSettings : BaseVehicleSettings
        {
            public override bool CustomVehicle => true;
        }

        public class TransportHelicopterSettings : FuelVehicleSettings
        {
            public override bool IsFightVehicle => true;

            [JsonProperty("Lift Fraction")]
            public float liftFraction = 0.25f;

            [JsonProperty("Rotation Scale")]
            public float rotationScale = 1.0f;

            [JsonProperty("Seconds to pause flyhack when dismount from Transport Scrap Helicopter.")]
            public int flyHackPause;
            
            [JsonProperty("Instant Engine Start-up (instant take-off)")]
            public bool instantTakeoff;

            protected override IFuelSystem GetFuelSystem(BaseEntity entity)
            {
                return (entity as ScrapTransportHelicopter)?.GetFuelSystem();
            }
        }

        public class RidableHorseSettings : InventoryVehicleSettings
        {
            [JsonProperty("Spawn with Double Saddle")]
            public bool IsDoubleSaddle { get; set; }

            [JsonProperty("Breeds")]
            public List<string> Breeds { get; set; }

            [JsonIgnore]
            public Dictionary<string, int> BreedsRef = new Dictionary<string, int>()
            {
                ["Appalosa"] = 0,
                ["Bay"] = 1,
                ["Buckskin"] = 2,
                ["Chestnut"] = 3,
                ["Dapple Grey"] = 4,
                ["Piebald"] = 5,
                ["Pinto"] = 6,
                ["Red Roan"] = 7,
                ["White Thoroughbred"] = 8,
                ["Black Thoroughbred"] = 9
            };

            protected override IEnumerable<ItemContainer> GetInventories(BaseEntity entity)
            {
                yield return (entity as RidableHorse)?.storageInventory;
            }

            public override void PostRecallVehicle(BasePlayer player, Vehicle vehicle, Vector3 position, Quaternion rotation)
            {
                base.PostRecallVehicle(player, vehicle, position, rotation);

                var ridableHorse = vehicle.Entity as RidableHorse;
                if (ridableHorse != null)
                {
                    ridableHorse.TryLeaveHitch();
                    // Broke on update, not sure what the replacement is or if one is needed
                    // ridableHorse.DropToGround(ridableHorse.transform.position, ridableHorse.transform.rotation, true); //ridableHorse.UpdateDropToGroundForDuration(2f);
                }
            }

            protected override void CorrectPositionAndRotation(BasePlayer player, Vehicle vehicle, Vector3 original, Quaternion rotation, out Vector3 spawnPos, out Quaternion spawnRot)
            {
                base.CorrectPositionAndRotation(player, vehicle, original, rotation, out spawnPos, out spawnRot);
                spawnPos += Vector3.up * 0.3f;
            }
        }

        // Only work cart (TrainEngine)
        public class WorkCartSettings : FuelVehicleSettings
        {
            public override bool IsTrainVehicle => true;

            public bool IsConnectableEngine(TrainEngine trainEngine)
            {
                return trainEngine.frontCoupling != null && trainEngine.rearCoupling != null;
            }

            protected override IFuelSystem GetFuelSystem(BaseEntity entity)
            {
                return (entity as TrainEngine)?.GetFuelSystem();
            }

            public override void PostRecallVehicle(BasePlayer player, Vehicle vehicle, Vector3 position, Quaternion rotation)
            {
                base.PostRecallVehicle(player, vehicle, position, rotation);
                var trainEngine = vehicle.Entity as TrainEngine;
                if (trainEngine != null)
                {
                    TryMoveToTrainTrackNear(trainEngine);
                }
            }

            protected override bool TryGetPositionAndRotation(BasePlayer player, Vehicle vehicle, out string reason, out Vector3 original, out Quaternion rotation)
            {
                return !base.TryGetPositionAndRotation(player, vehicle, out reason, out original, out rotation)
                       || TryGetTrainCarPositionAndRotation(player, vehicle, ref reason, ref original, ref rotation);
            }
        }

        public class MagnetCraneSettings : FuelVehicleSettings
        {
            protected override IFuelSystem GetFuelSystem(BaseEntity entity)
            {
                return (entity as MagnetCrane)?.GetFuelSystem();
            }
        }

        public class SubmarineSoloSettings : InvFuelVehicleSettings, IAmmoVehicle
        {
            private const int AMMO_ITEM_ID = -1671551935;

            public int SpawnAmmoAmount { get; set; }
            public override bool IsWaterVehicle => true;

            protected override IFuelSystem GetFuelSystem(BaseEntity entity)
            {
                return (entity as BaseSubmarine)?.GetFuelSystem();
            }

            protected override IEnumerable<ItemContainer> GetInventories(BaseEntity entity)
            {
                yield return (entity as BaseSubmarine)?.GetItemContainer()?.inventory;
                yield return (entity as BaseSubmarine)?.GetTorpedoContainer()?.inventory;
            }

            public override void SetupVehicle(BaseEntity entity, Vehicle vehicle, BasePlayer player, bool justCreated = true)
            {
                if (justCreated)
                {
                    TryGiveAmmo(entity);
                }
                base.SetupVehicle(entity, vehicle, player, justCreated);
            }

            private void TryGiveAmmo(BaseEntity entity)
            {
                if (entity == null || SpawnAmmoAmount <= 0)
                {
                    return;
                }
                var ammoContainer = (entity as BaseSubmarine)?.GetTorpedoContainer();

                if (ammoContainer == null || ammoContainer.inventory == null) return;

                var ammoItem = ItemManager.CreateByItemID(AMMO_ITEM_ID, SpawnAmmoAmount);
                if (!ammoItem.MoveToContainer(ammoContainer.inventory))
                {
                    ammoItem.Remove();
                }
            }
        }

        public class SubmarineDuoSettings : SubmarineSoloSettings
        {
        }

        public class SnowmobileSettings : InvFuelVehicleSettings
        {
            protected override IFuelSystem GetFuelSystem(BaseEntity entity)
            {
                return (entity as Snowmobile)?.GetFuelSystem();
            }

            protected override IEnumerable<ItemContainer> GetInventories(BaseEntity entity)
            {
                yield return (entity as Snowmobile)?.GetItemContainer()?.inventory;
            }
        }

        public class ModularVehicleSettings : InvFuelVehicleSettings, IModularVehicle
        {
            #region Properties

            public bool RefundEngineOnKill { get; set; } = true;
            public bool RefundEngineOnCrash { get; set; } = true;
            public bool RefundModuleOnKill { get; set; } = true;
            public bool RefundModuleOnCrash { get; set; } = true;

            [JsonProperty(PropertyName = "Chassis Type (Small, Medium, Large)", Order = 50)]
            public ChassisType ChassisType { get; set; } = ChassisType.Small;

            [JsonProperty(PropertyName = "Vehicle Module Items", Order = 51)]
            public List<ModuleItem> ModuleItems { get; set; } = new List<ModuleItem>();

            [JsonProperty(PropertyName = "Vehicle Engine Items", Order = 52)]
            public List<EngineItem> EngineItems { get; set; } = new List<EngineItem>();

            #endregion Properties

            #region ModuleItems

            private List<ModuleItem> _validModuleItems;

            public IEnumerable<ModuleItem> ValidModuleItems
            {
                get
                {
                    if (_validModuleItems == null)
                    {
                        _validModuleItems = new List<ModuleItem>();
                        foreach (var modularItem in ModuleItems)
                        {
                            var itemDefinition = ItemManager.FindItemDefinition(modularItem.shortName);
                            if (itemDefinition != null)
                            {
                                var itemModVehicleModule = itemDefinition.GetComponent<ItemModVehicleModule>();
                                if (itemModVehicleModule == null || !itemModVehicleModule.entityPrefab.isValid)
                                {
                                    Instance.PrintError($"'{modularItem}' is not a valid vehicle module");
                                    continue;
                                }
                                _validModuleItems.Add(modularItem);
                            }
                        }
                    }
                    return _validModuleItems;
                }
            }

            public IEnumerable<Item> CreateModuleItems()
            {
                foreach (var moduleItem in ValidModuleItems)
                {
                    var item = ItemManager.CreateByName(moduleItem.shortName);
                    if (item != null)
                    {
                        item.condition = item.maxCondition * (moduleItem.healthPercentage / 100f);
                        item.MarkDirty();
                        yield return item;
                    }
                }
            }

            #endregion ModuleItems

            #region EngineItems

            private List<EngineItem> _validEngineItems;

            public IEnumerable<EngineItem> ValidEngineItems
            {
                get
                {
                    if (_validEngineItems == null)
                    {
                        _validEngineItems = new List<EngineItem>();
                        foreach (var modularItem in EngineItems)
                        {
                            var itemDefinition = ItemManager.FindItemDefinition(modularItem.shortName);
                            if (itemDefinition != null)
                            {
                                var itemModEngineItem = itemDefinition.GetComponent<ItemModEngineItem>();
                                if (itemModEngineItem == null)
                                {
                                    Instance.PrintError($"'{modularItem}' is not a valid engine item");
                                    continue;
                                }
                                _validEngineItems.Add(modularItem);
                            }
                        }
                    }
                    return _validEngineItems;
                }
            }

            public IEnumerable<Item> CreateEngineItems()
            {
                foreach (var engineItem in ValidEngineItems)
                {
                    var item = ItemManager.CreateByName(engineItem.shortName);
                    if (item != null)
                    {
                        item.condition = item.maxCondition * (engineItem.conditionPercentage / 100f);
                        item.MarkDirty();
                        yield return item;
                    }
                }
            }

            #endregion EngineItems

            public override bool IsNormalVehicle => false;
            public override bool IsModularVehicle => true;

            protected override IFuelSystem GetFuelSystem(BaseEntity entity)
            {
                return (entity as ModularCar)?.GetFuelSystem();
            }

            #region Spawn

            protected override string GetVehiclePrefab(string vehicleType)
            {
                switch (ChassisType)
                {
                    case ChassisType.Small:
                        return PREFAB_CHASSIS_SMALL;
                    case ChassisType.Medium:
                        return PREFAB_CHASSIS_MEDIUM;
                    case ChassisType.Large:
                        return PREFAB_CHASSIS_LARGE;
                    default:
                        return null;
                }
            }

            #region Setup

            public override void SetupVehicle(BaseEntity entity, Vehicle vehicle, BasePlayer player, bool justCreated = true)
            {
                var modularCar = entity as ModularCar;
                if (modularCar != null)
                {
                    if (ValidModuleItems.Any())
                    {
                        AttacheVehicleModules(modularCar, vehicle);
                    }
                    if (ValidEngineItems.Any())
                    {
                        Instance.NextTick(() =>
                        {
                            AddItemsToVehicleEngine(modularCar, vehicle);
                        });
                    }
                }
                base.SetupVehicle(entity, vehicle, player, justCreated);
            }

            #endregion Setup

            #endregion Spawn

            #region Recall

            public override void PreRecallVehicle(BasePlayer player, Vehicle vehicle, Vector3 position, Quaternion rotation)
            {
                base.PreRecallVehicle(player, vehicle, position, rotation);

                if (vehicle.Entity is ModularCar)
                {
                    var modularCarGarages = Pool.Get<List<ModularCarGarage>>();
                    Vis.Entities(vehicle.Entity.transform.position, 3f, modularCarGarages, Layers.Mask.Deployed | Layers.Mask.Default);
                    var modularCarGarage = modularCarGarages.FirstOrDefault(x => x.carOccupant == vehicle.Entity);
                    Pool.FreeUnmanaged(ref modularCarGarages);
                    if (modularCarGarage != null)
                    {
                        modularCarGarage.enabled = false;
                        modularCarGarage.ReleaseOccupant();
                        modularCarGarage.Invoke(() => modularCarGarage.enabled = true, 0.25f);
                    }
                }
            }

            #region DropInventory

            protected override DroppedItemContainer DropVehicleInventory(BasePlayer player, Vehicle vehicle)
            {
                var modularCar = vehicle.Entity as ModularCar;
                if (modularCar != null)
                {
                    foreach (var moduleEntity in modularCar.AttachedModuleEntities)
                    {
                        if (moduleEntity is VehicleModuleEngine)
                        {
                            continue;
                        }
                        var moduleStorage = moduleEntity as VehicleModuleStorage;
                        if (moduleStorage != null)
                        {
                            return moduleStorage.GetContainer()?.inventory?.Drop(PREFAB_ITEM_DROP, vehicle.Entity.GetDropPosition(), vehicle.Entity.transform.rotation, 0);
                        }
                    }
                }
                return null;
            }

            #endregion DropInventory

            #endregion Recall

            #region Refund

            private void GetRefundStatus(bool isCrash, bool isUnload, out bool refundFuel, out bool refundInventory, out bool refundEngine, out bool refundModule)
            {
                if (isUnload)
                {
                    refundFuel = refundInventory = refundEngine = refundModule = true;
                    return;
                }
                refundFuel = isCrash ? RefundFuelOnCrash : RefundFuelOnKill;
                refundInventory = isCrash ? RefundInventoryOnCrash : RefundInventoryOnKill;
                refundEngine = isCrash ? RefundEngineOnCrash : RefundEngineOnKill;
                refundModule = isCrash ? RefundModuleOnCrash : RefundModuleOnKill;
            }

            protected override void CollectVehicleItems(List<Item> items, Vehicle vehicle, bool isCrash, bool isUnload)
            {
                var modularCar = vehicle.Entity as ModularCar;
                if (modularCar != null)
                {
                    bool refundFuel, refundInventory, refundEngine, refundModule;
                    GetRefundStatus(isCrash, isUnload, out refundFuel, out refundInventory, out refundEngine, out refundModule);

                    foreach (var moduleEntity in modularCar.AttachedModuleEntities)
                    {
                        if (refundEngine)
                        {
                            var moduleEngine = moduleEntity as VehicleModuleEngine;
                            if (moduleEngine != null)
                            {
                                var engineContainer = moduleEngine.GetContainer()?.inventory;
                                if (engineContainer != null)
                                {
                                    items.AddRange(engineContainer.itemList);
                                }
                                continue;
                            }
                        }
                        if (refundInventory)
                        {
                            var moduleStorage = moduleEntity as VehicleModuleStorage;
                            if (moduleStorage != null && !(moduleEntity is VehicleModuleEngine))
                            {
                                var storageContainer = moduleStorage.GetContainer()?.inventory;
                                if (storageContainer != null)
                                {
                                    items.AddRange(storageContainer.itemList);
                                }
                            }
                        }
                    }
                    if (refundFuel)
                    {
                        var fuelSystem = GetFuelSystem(modularCar);
                        if (fuelSystem is EntityFuelSystem entityFuelSystem)
                        {
                            var fuelContainer = entityFuelSystem.GetFuelContainer()?.inventory;
                            if (fuelContainer != null)
                            {
                                items.AddRange(fuelContainer.itemList);
                            }
                        }
                    }
                    if (refundModule)
                    {
                        var moduleContainer = modularCar.Inventory?.ModuleContainer;
                        if (moduleContainer != null)
                        {
                            items.AddRange(moduleContainer.itemList);
                        }
                    }
                    //var chassisContainer = modularCar.Inventory?.ChassisContainer;
                    //if (chassisContainer != null)
                    //{
                    //    collect.AddRange(chassisContainer.itemList);
                    //}
                }
            }

            #endregion Refund

            #region VehicleModules

            private void AttacheVehicleModules(ModularCar modularCar, Vehicle vehicle)
            {
                foreach (var moduleItem in CreateModuleItems())
                {
                    if (!modularCar.TryAddModule(moduleItem))
                    {
                        Instance?.PrintError($"Module item '{moduleItem.info.shortname}' in '{vehicle.VehicleType}' cannot be attached to the vehicle");
                        moduleItem.Remove();
                    }
                }
            }

            private void AddItemsToVehicleEngine(ModularCar modularCar, Vehicle vehicle)
            {
                if (modularCar == null || modularCar.IsDestroyed)
                {
                    return;
                }
                foreach (var moduleEntity in modularCar.AttachedModuleEntities)
                {
                    var vehicleModuleEngine = moduleEntity as VehicleModuleEngine;
                    if (vehicleModuleEngine != null)
                    {
                        var engineInventory = vehicleModuleEngine.GetContainer()?.inventory;
                        if (engineInventory != null)
                        {
                            foreach (var engineItem in CreateEngineItems())
                            {
                                var moved = false;
                                for (var i = 0; i < engineInventory.capacity; i++)
                                {
                                    if (engineItem.MoveToContainer(engineInventory, i, false))
                                    {
                                        moved = true;
                                        break;
                                    }
                                }
                                if (!moved)
                                {
                                    Instance?.PrintError($"Engine item '{engineItem.info.shortname}' in '{vehicle.VehicleType}' cannot be move to the vehicle engine inventory");
                                    engineItem.Remove();
                                    engineItem.DoRemove();
                                }
                            }
                        }
                    }
                }
            }

            #endregion VehicleModules
        }

        public class TrainVehicleSettings : FuelVehicleSettings, ITrainVehicle
        {
            #region Properties

            [JsonProperty(PropertyName = "Train Components", Order = 50)]
            public List<TrainComponent> TrainComponents { get; set; } = new List<TrainComponent>();

            #endregion Properties

            public override bool IsNormalVehicle => false;
            public override bool IsTrainVehicle => true;
            public override bool IsConnectableVehicle => true;

            protected override IFuelSystem GetFuelSystem(BaseEntity entity)
            {
                return (entity as TrainCar)?.GetFuelSystem();
            }

            protected override string GetVehiclePrefab(string vehicleType)
            {
                return TrainComponents.Count > 0 ? GetTrainVehiclePrefab(TrainComponents[0].type) : base.GetVehiclePrefab(vehicleType);
            }

            protected override string GetVehicleCustomPrefab(string vehicleType)
            {
                if (!configData.global.useCustomVehicles) return string.Empty;
                return TrainComponents.Count > 0 ? GetTrainVehiclePrefab(TrainComponents[0].type) : base.GetVehicleCustomPrefab(vehicleType);
            }

            #region Spawn

            private static string GetTrainVehiclePrefab(TrainComponentType componentType)
            {
                switch (componentType)
                {
                    case TrainComponentType.Engine:
                        return PREFAB_TRAINENGINE;
                    case TrainComponentType.CoveredEngine:
                        return PREFAB_TRAINENGINE_COVERED;
                    case TrainComponentType.Locomotive:
                        return PREFAB_TRAINENGINE_LOCOMOTIVE;
                    case TrainComponentType.WagonA:
                        return PREFAB_TRAINWAGON_A;
                    case TrainComponentType.WagonB:
                        return PREFAB_TRAINWAGON_B;
                    case TrainComponentType.WagonC:
                        return PREFAB_TRAINWAGON_C;
                    case TrainComponentType.Unloadable:
                        return PREFAB_TRAINWAGON_UNLOADABLE;
                    case TrainComponentType.UnloadableLoot:
                        return PREFAB_TRAINWAGON_UNLOADABLE_LOOT;
                    case TrainComponentType.UnloadableFuel:
                        return PREFAB_TRAINWAGON_UNLOADABLE_FUEL;
                    case TrainComponentType.Caboose:
                        return PREFAB_CABOOSE;
                    default:
                        return null;
                }
            }

            public override BaseEntity SpawnVehicle(BasePlayer player, Vehicle vehicle, Vector3 position, Quaternion rotation)
            {
                TrainCar prevTrainCar = null, primaryTrainCar = null;
                foreach (var component in TrainComponents)
                {
                    var prefab = GetTrainVehiclePrefab(component.type);
                    if (string.IsNullOrEmpty(prefab))
                    {
                        throw new ArgumentException($"Prefab not found for {vehicle.VehicleType}({component.type})");
                    }
                    float distResult;
                    TrainTrackSpline splineResult;
                    if (prevTrainCar == null)
                    {
                        if (TrainTrackSpline.TryFindTrackNear(position, 20f, out splineResult, out distResult))
                        {
                            position = splineResult.GetPosition(distResult);
                            prevTrainCar = GameManager.server.CreateEntity(prefab, position, rotation) as TrainCar;
                            if (prevTrainCar == null)
                            {
                                continue;
                            }
                            PreSetupVehicle(prevTrainCar, vehicle, player);
                            prevTrainCar.Spawn();
                            prevTrainCar.CancelInvoke(prevTrainCar.KillMessage);
                            SetupVehicle(prevTrainCar, vehicle, player);
                        }
                    }
                    else
                    {
                        var newTrainCar = GameManager.server.CreateEntity(prefab, prevTrainCar.transform.position, prevTrainCar.transform.rotation) as TrainCar;
                        if (newTrainCar == null)
                        {
                            continue;
                        }

                        position += prevTrainCar.transform.rotation * (newTrainCar.bounds.center - Vector3.forward * (newTrainCar.bounds.extents.z + prevTrainCar.bounds.extents.z));
                        if (TrainTrackSpline.TryFindTrackNear(position, 20f, out splineResult, out distResult))
                        {
                            position = splineResult.GetPosition(distResult);
                            newTrainCar.transform.position = position;

                            PreSetupVehicle(newTrainCar, vehicle, player);
                            newTrainCar.Spawn();
                            newTrainCar.CancelInvoke(newTrainCar.KillMessage);
                            SetupVehicle(newTrainCar, vehicle, player);

                            float minSplineDist;
                            var distance = prevTrainCar.RearTrackSection.GetDistance(position, 1f, out minSplineDist);
                            var preferredAltTrack = prevTrainCar.RearTrackSection != prevTrainCar.FrontTrackSection ? prevTrainCar.RearTrackSection : null;
                            newTrainCar.MoveFrontWheelsAlongTrackSpline(prevTrainCar.RearTrackSection, minSplineDist, distance, preferredAltTrack, TrainTrackSpline.TrackSelection.Default);

                            newTrainCar.coupling.frontCoupling.TryCouple(prevTrainCar.coupling.rearCoupling, true);
                            prevTrainCar = newTrainCar;
                        }
                    }
                    if (primaryTrainCar == null)
                    {
                        primaryTrainCar = prevTrainCar;
                    }
                }
                if (primaryTrainCar == null || primaryTrainCar.IsDestroyed)
                {
                    Instance.Print(player, Instance.Lang("NotSpawnedOrRecalled", player.UserIDString, DisplayName));
                    return null;
                }
                Instance.CacheVehicleEntity(primaryTrainCar, vehicle, player);
                return primaryTrainCar;
            }

            #endregion Spawn

            #region Recall

            public override void PreRecallVehicle(BasePlayer player, Vehicle vehicle, Vector3 position, Quaternion rotation)
            {
                base.PreRecallVehicle(player, vehicle, position, rotation);
                var trainCar = vehicle.Entity as TrainCar;
                if (trainCar != null)
                {
                    trainCar.coupling.Uncouple(true);
                    trainCar.coupling.Uncouple(false);
                }
            }

            public override void PostRecallVehicle(BasePlayer player, Vehicle vehicle, Vector3 position, Quaternion rotation)
            {
                base.PostRecallVehicle(player, vehicle, position, rotation);
                var trainCar = vehicle.Entity as TrainCar;
                if (trainCar != null)
                {
                    TryMoveToTrainTrackNear(trainCar);
                }
            }

            #endregion Recall

            #region Refund

            protected override void CollectVehicleItems(List<Item> items, Vehicle vehicle, bool isCrash, bool isUnload)
            {
                // Refund primary engine fuel only
                if (!CanRefundFuel(isCrash, isUnload)) return;

                var trainCar = vehicle.Entity as TrainCar;

                if (trainCar == null) return;
                var fuelSystem = GetFuelSystem(trainCar);

                if (fuelSystem is EntityFuelSystem entityFuelSystem)
                {
                    var fuelContainer = entityFuelSystem.GetFuelContainer()?.inventory;

                    if (fuelContainer != null)
                    {
                        items.AddRange(fuelContainer.itemList);
                    }
                }
            }

            #endregion Refund

            #region TryGetVehicleParams

            protected override bool TryGetPositionAndRotation(BasePlayer player, Vehicle vehicle, out string reason, out Vector3 original, out Quaternion rotation)
            {
                if (!base.TryGetPositionAndRotation(player, vehicle, out reason, out original, out rotation)) return true;

                return TryGetTrainCarPositionAndRotation(player, vehicle, ref reason, ref original, ref rotation);
            }

            // protected override void CorrectPositionAndRotation(BasePlayer player, Vehicle vehicle, Vector3 original, Quaternion rotation, out Vector3 spawnPos, out Quaternion spawnRot)
            // {
            //     base.CorrectPositionAndRotation(player, vehicle, original, rotation, out spawnPos, out spawnRot);
            //     // No rotation on recall
            //     if (vehicle.Entity != null)
            //     {
            //         spawnRot = vehicle.Entity.transform.rotation;
            //     } 
            // }

            #endregion TryGetVehicleParams
        }

        #endregion VehicleSettings

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                PreprocessOldConfig();
                configData = Config.ReadObject<ConfigData>();
                if (configData == null)
                {
                    LoadDefaultConfig();
                }
                else
                {
                    UpdateConfigValues();
                }
            }
            catch (Exception ex)
            {
                PrintError($"The configuration file is corrupted. \n{ex}");
                LoadDefaultConfig();
            }
            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            PrintWarning("Creating a new configuration file");
            configData = new ConfigData();
            configData.version = Version;
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(configData);
        }

        private void UpdateConfigValues()
        {
            if (configData.version >= Version) return;
            if (configData.version <= default(VersionNumber))
            {
                string prefix, prefixColor;
                if (GetConfigValue(out prefix, "Chat Settings", "Chat Prefix") && GetConfigValue(out prefixColor, "Chat Settings", "Chat Prefix Color"))
                {
                    configData.chat.prefix = $"<color={prefixColor}>{prefix}</color>: ";
                }
            }
            if (configData.version <= new VersionNumber(1, 7, 3))
            {
                configData.normalVehicles.sedan.MinDistanceForPlayers = 3f;
                configData.normalVehicles.chinook.MinDistanceForPlayers = 5f;
                configData.normalVehicles.rowboat.MinDistanceForPlayers = 2f;
                configData.normalVehicles.rhib.MinDistanceForPlayers = 3f;
                configData.normalVehicles.hotAirBalloon.MinDistanceForPlayers = 4f;
                configData.normalVehicles.armoredHotAirBalloon.MinDistanceForPlayers = 4f;
                configData.normalVehicles.ridableHorse.MinDistanceForPlayers = 1f;
                configData.normalVehicles.miniCopter.MinDistanceForPlayers = 2f;
                configData.normalVehicles.attackHelicopter.MinDistanceForPlayers = 2f;
                configData.normalVehicles.transportHelicopter.MinDistanceForPlayers = 4f;
                foreach (var entry in configData.modularVehicles)
                {
                    switch (entry.Value.ChassisType)
                    {
                        case ChassisType.Small:
                            entry.Value.MinDistanceForPlayers = 2f;
                            break;

                        case ChassisType.Medium:
                            entry.Value.MinDistanceForPlayers = 2.5f;
                            break;

                        case ChassisType.Large:
                            entry.Value.MinDistanceForPlayers = 3f;
                            break;

                        default:
                            continue;
                    }
                }
            }
            if (configData.version >= new VersionNumber(1, 7, 17) && configData.version <= new VersionNumber(1, 7, 18))
            {
                LoadData();
                foreach (var data in storedData.playerData)
                {
                    Vehicle vehicle;
                    if (data.Value.TryGetValue("SubmarineDouble", out vehicle))
                    {
                        data.Value.Remove("SubmarineDouble");
                        data.Value.Add(nameof(NormalVehicleType.SubmarineDuo), vehicle);
                    }
                }
                SaveData();
            }

            if (configData.version < new VersionNumber(1, 8, 0))
            {
                configData.normalVehicles.ridableHorse.Breeds = new List<string>
                {
                    "Appalosa", "Bay", "Buckskin", "Chestnut", "Dapple Grey", "Piebald", "Pinto", "Red Roan", "White Thoroughbred", "Black Thoroughbred"
                };
                configData.normalVehicles.ridableHorse.IsDoubleSaddle = false;
            }

            if (configData.version < new VersionNumber(1, 8, 3))
            {
                configData.normalVehicles.tugboat.BypassCostPermission = "vehiclelicence.tugfree";
                configData.normalVehicles.sedan.BypassCostPermission = "vehiclelicence.sedanfree";
                configData.normalVehicles.chinook.BypassCostPermission = "vehiclelicence.chinookfree";
                configData.normalVehicles.rowboat.BypassCostPermission = "vehiclelicence.rowboatfree";
                configData.normalVehicles.rhib.BypassCostPermission = "vehiclelicence.rhibfree";
                configData.normalVehicles.hotAirBalloon.BypassCostPermission = "vehiclelicence.hotairballoonfree";
                configData.normalVehicles.armoredHotAirBalloon.BypassCostPermission = "vehiclelicence.armoredhotairballoonfree";
                configData.normalVehicles.ridableHorse.BypassCostPermission = "vehiclelicence.ridablehorsefree";
                configData.normalVehicles.miniCopter.BypassCostPermission = "vehiclelicence.minicopterfree";
                configData.normalVehicles.attackHelicopter.BypassCostPermission = "vehiclelicence.attackhelicopterfree";
                configData.normalVehicles.transportHelicopter.BypassCostPermission = "vehiclelicence.transportcopterfree";
                configData.normalVehicles.workCart.BypassCostPermission = "vehiclelicence.workcartfree";
                configData.normalVehicles.sedanRail.BypassCostPermission = "vehiclelicence.sedanrailfree";
                configData.normalVehicles.magnetCrane.BypassCostPermission = "vehiclelicence.magnetcranefree";
                configData.normalVehicles.submarineSolo.BypassCostPermission = "vehiclelicence.submarinesolofree";
                configData.normalVehicles.submarineDuo.BypassCostPermission = "vehiclelicence.submarineduofree";
                configData.normalVehicles.snowmobile.BypassCostPermission = "vehiclelicence.snowmobilefree";
                configData.normalVehicles.tomahaSnowmobile.BypassCostPermission = "vehiclelicence.tomahasnowmobilefree";

                configData.modularVehicles["SmallCar"].BypassCostPermission = "vehiclelicence.smallmodularcarfree";
                configData.modularVehicles["MediumCar"].BypassCostPermission = "vehiclelicence.mediumodularcarfree";
                configData.modularVehicles["LargeCar"].BypassCostPermission = "vehiclelicence.largemodularcarfree";

                configData.trainVehicles["WorkCartAboveGround"].BypassCostPermission = "vehiclelicence.workcartabovegroundfree";
                configData.trainVehicles["WorkCartCovered"].BypassCostPermission = "vehiclelicence.coveredworkcartfree";
                configData.trainVehicles["CompleteTrain"].BypassCostPermission = "vehiclelicence.completetrainfree";
                configData.trainVehicles["Locomotive"].BypassCostPermission = "vehiclelicence.locomotivefree";
            }

            if (configData.version < new VersionNumber(1, 8, 6))
            {
                configData.normalVehicles.transportHelicopter.instantTakeoff = false;
                configData.global.preventPushing = false;
                configData.global.useCustomVehicles = false;
            }

            configData.version = Version;
            SaveConfig();
        }

        private bool GetConfigValue<T>(out T value, params string[] path)
        {
            var configValue = Config.Get(path);
            if (configValue != null)
            {
                if (configValue is T)
                {
                    value = (T)configValue;
                    return true;
                }
                try
                {
                    value = Config.ConvertValue<T>(configValue);
                    return true;
                }
                catch (Exception ex)
                {
                    PrintError($"GetConfigValue ERROR: path: {string.Join("\\", path)}\n{ex}");
                }
            }

            value = default(T);
            return false;
        }

        private void SetConfigValue(params object[] pathAndTrailingValue)
        {
            Config.Set(pathAndTrailingValue);
        }

        #region Preprocess Old Config

        private void PreprocessOldConfig()
        {
            var config = Config.ReadObject<JObject>();
            if (config == null)
            {
                return;
            }
            //Interface.Oxide.DataFileSystem.WriteObject(Name + "_old", jObject);
            VersionNumber oldVersion;
            if (!GetConfigVersionPre(config, out oldVersion)) return;
            if (oldVersion >= Version) return;
            if (oldVersion < new VersionNumber(1, 7, 35))
            {
                try
                {
                    if (config["Train Vehicle Settings"] == null)
                    {
                        config["Train Vehicle Settings"] = JObject.FromObject(new ConfigData().trainVehicles);
                    }
                    var workCartAboveGround = GetConfigValue(config, "Normal Vehicle Settings", "Work Cart Above Ground Vehicle");
                    if (workCartAboveGround != null)
                    {
                        var settings = workCartAboveGround.ToObject<TrainVehicleSettings>();
                        settings.TrainComponents = new List<TrainComponent>
                        {
                            new TrainComponent
                            {
                                type = TrainComponentType.Engine
                            }
                        };
                        config["Train Vehicle Settings"]["WorkCartAboveGround"] = JObject.FromObject(settings);
                    }
                    var coveredWorkCart = GetConfigValue(config, "Normal Vehicle Settings", "Covered Work Cart Vehicle");
                    if (coveredWorkCart != null)
                    {
                        var settings = coveredWorkCart.ToObject<TrainVehicleSettings>();
                        settings.TrainComponents = new List<TrainComponent>
                        {
                            new TrainComponent
                            {
                                type = TrainComponentType.CoveredEngine
                            }
                        };
                        config["Train Vehicle Settings"]["WorkCartCovered"] = JObject.FromObject(settings);
                    }
                }
                catch
                {
                    // ignored
                }
            }

            if (oldVersion < new VersionNumber(1, 7, 48))
            {
                try
                {
                    var locomotive = GetConfigValue(config, "Train Vehicle Settings", "Locomotive");
                    if (locomotive == null)
                    {
                        var settings = new TrainVehicleSettings
                        {
                            Purchasable = true,
                            DisplayName = "Locomotive",
                            Distance = 12,
                            MinDistanceForPlayers = 6,
                            UsePermission = true,
                            Permission = "vehiclelicence.locomotive",
                            Commands = new List<string>
                            {
                                "loco", "locomotive"
                            },
                            PurchasePrices = new Dictionary<string, PriceInfo>
                            {
                                ["scrap"] = new PriceInfo { amount = 2000, displayName = "Scrap" }
                            },
                            SpawnCooldown = 1800,
                            RecallCooldown = 30,
                            CooldownPermissions = new Dictionary<string, CooldownPermission>
                            {
                                ["vehiclelicence.vip"] = new CooldownPermission
                                {
                                    spawnCooldown = 900,
                                    recallCooldown = 10
                                }
                            },
                            TrainComponents = new List<TrainComponent>
                            {
                                new TrainComponent
                                {
                                    type = TrainComponentType.Locomotive
                                }
                            }
                        };
                        config["Train Vehicle Settings"]["Locomotive"] = JObject.FromObject(settings);
                    }
                }
                catch
                {
                    // Still ignored.
                }
            }
            Config.WriteObject(config);
            // Interface.Oxide.DataFileSystem.WriteObject(Name + "_new", jObject);
        }

        private JObject GetConfigValue(JObject config, params string[] path)
        {
            if (path.Length < 1)
            {
                throw new ArgumentException("path is empty");
            }

            try
            {
                JToken jToken;
                if (!config.TryGetValue(path[0], out jToken))
                {
                    return null;
                }

                for (var i = 1; i < path.Length; i++)
                {
                    var jObject = jToken as JObject;
                    if (jObject == null || !jObject.TryGetValue(path[i], out jToken))
                    {
                        return null;
                    }
                }
                return jToken as JObject;
            }
            catch (Exception ex)
            {
                PrintError($"GetConfigValue ERROR: path: {string.Join("\\", path)}\n{ex}");
            }
            return null;
        }

        private bool GetConfigValuePre<T>(JObject config, out T value, params string[] path)
        {
            if (path.Length < 1)
            {
                throw new ArgumentException("path is empty");
            }

            try
            {
                JToken jToken;
                if (!config.TryGetValue(path[0], out jToken))
                {
                    value = default(T);
                    return false;
                }

                for (var i = 1; i < path.Length; i++)
                {
                    var jObject = jToken.ToObject<JObject>();

                    if (jObject != null && jObject.TryGetValue(path[i], out jToken)) continue;

                    value = default(T);
                    return false;
                }
                value = jToken.ToObject<T>();
                return true;
            }
            catch (Exception ex)
            {
                PrintError($"GetConfigValuePre ERROR: path: {string.Join("\\", path)}\n{ex}");
            }
            value = default(T);
            return false;
        }

        private void SetConfigValuePre(JObject config, object value, params string[] path)
        {
            if (path.Length < 1)
            {
                throw new ArgumentException("path is empty");
            }

            try
            {
                JToken jToken;
                if (!config.TryGetValue(path[0], out jToken))
                {
                    if (path.Length == 1)
                    {
                        jToken = JToken.FromObject(value);
                        config.Add(path[0], jToken);
                        return;
                    }
                    jToken = new JObject();
                    config.Add(path[0], jToken);
                }

                for (var i = 1; i < path.Length - 1; i++)
                {
                    var jObject = jToken as JObject;
                    if (jObject == null || !jObject.TryGetValue(path[i], out jToken))
                    {
                        jToken = new JObject();
                        jObject?.Add(path[i], jToken);
                    }
                }
                var targetToken = jToken as JObject;
                if (targetToken != null)
                {
                    targetToken[path[path.Length - 1]] = JToken.FromObject(value);
                }
                // (jToken as JObject)?.TryAdd(path[path.Length - 1], JToken.FromObject(value));
            }
            catch (Exception ex)
            {
                PrintError($"SetConfigValuePre ERROR: value: {value} path: {string.Join("\\", path)}\n{ex}");
            }
        }

        private bool GetConfigVersionPre(JObject config, out VersionNumber version)
        {
            try
            {
                JToken jToken;
                if (config.TryGetValue("Version", out jToken))
                {
                    version = jToken.ToObject<VersionNumber>();
                    return true;
                }
            }
            catch
            {
                // ignored
            }
            version = default(VersionNumber);
            return false;
        }

        #endregion Preprocess Old Config

        #endregion ConfigurationFile

        #region DataFile

        public StoredData storedData { get; private set; }

        public class StoredData
        {
            public readonly Dictionary<ulong, Dictionary<string, Vehicle>> playerData = new Dictionary<ulong, Dictionary<string, Vehicle>>();

            public IEnumerable<BaseEntity> ActiveVehicles(ulong playerId)
            {
                Dictionary<string, Vehicle> vehicles;
                if (!playerData.TryGetValue(playerId, out vehicles))
                {
                    yield break;
                }

                foreach (var vehicle in vehicles.Values)
                {
                    if (vehicle.Entity != null && !vehicle.Entity.IsDestroyed)
                    {
                        yield return vehicle.Entity;
                    }
                }
            }

            public Dictionary<string, Vehicle> GetPlayerVehicles(ulong playerId, bool readOnly = true)
            {
                Dictionary<string, Vehicle> vehicles;
                if (!playerData.TryGetValue(playerId, out vehicles))
                {
                    if (!readOnly)
                    {
                        vehicles = new Dictionary<string, Vehicle>();
                        playerData.Add(playerId, vehicles);
                        return vehicles;
                    }
                    return null;
                }
                return vehicles;
            }

            public bool IsVehiclePurchased(ulong playerId, string vehicleType, out Vehicle vehicle)
            {
                vehicle = GetVehicleLicense(playerId, vehicleType);
                if (vehicle == null)
                {
                    return false;
                }
                return true;
            }

            public Vehicle GetVehicleLicense(ulong playerId, string vehicleType)
            {
                Dictionary<string, Vehicle> vehicles;
                if (!playerData.TryGetValue(playerId, out vehicles))
                {
                    return null;
                }
                Vehicle vehicle;
                if (!vehicles.TryGetValue(vehicleType, out vehicle))
                {
                    return null;
                }
                return vehicle;
            }

            public bool HasVehicleLicense(ulong playerId, string vehicleType)
            {
                Dictionary<string, Vehicle> vehicles;
                if (!playerData.TryGetValue(playerId, out vehicles))
                {
                    return false;
                }
                return vehicles.ContainsKey(vehicleType);
            }

            public bool AddVehicleLicense(ulong playerId, string vehicleType)
            {
                Dictionary<string, Vehicle> vehicles;
                if (!playerData.TryGetValue(playerId, out vehicles))
                {
                    vehicles = new Dictionary<string, Vehicle>();
                    playerData.Add(playerId, vehicles);
                }
                if (vehicles.ContainsKey(vehicleType))
                {
                    return false;
                }
                vehicles.Add(vehicleType, Vehicle.Create(playerId, vehicleType));
                Instance.SaveData();
                return true;
            }

            public bool RemoveVehicleLicense(ulong playerId, string vehicleType)
            {
                Dictionary<string, Vehicle> vehicles;
                if (!playerData.TryGetValue(playerId, out vehicles))
                {
                    return false;
                }

                if (!vehicles.Remove(vehicleType))
                {
                    return false;
                }
                Instance.SaveData();
                return true;
            }

            public List<string> GetVehicleLicenseNames(ulong playerId)
            {
                Dictionary<string, Vehicle> vehicles;
                if (!playerData.TryGetValue(playerId, out vehicles))
                {
                    return new List<string>();
                }
                return vehicles.Keys.ToList();
            }

            public void PurchaseAllVehicles(ulong playerId)
            {
                var changed = false;
                Dictionary<string, Vehicle> vehicles;
                if (!playerData.TryGetValue(playerId, out vehicles))
                {
                    vehicles = new Dictionary<string, Vehicle>();
                    playerData.Add(playerId, vehicles);
                }
                foreach (var vehicleType in Instance.allVehicleSettings.Keys)
                {
                    if (!vehicles.ContainsKey(vehicleType))
                    {
                        vehicles.Add(vehicleType, Vehicle.Create(playerId, vehicleType));
                        changed = true;
                    }
                }

                if (changed)
                {
                    Instance.SaveData();
                }
            }

            public void AddLicenseForAllPlayers(string vehicleType)
            {
                foreach (var entry in playerData)
                {
                    if (!entry.Value.ContainsKey(vehicleType))
                    {
                        entry.Value.Add(vehicleType, Vehicle.Create(entry.Key, vehicleType));
                    }
                }
            }

            public void RemoveLicenseForAllPlayers(string vehicleType)
            {
                foreach (var entry in playerData)
                {
                    entry.Value.Remove(vehicleType);
                }
            }

            public void ResetPlayerData()
            {
                foreach (var vehicleEntries in playerData)
                {
                    foreach (var vehicleEntry in vehicleEntries.Value)
                    {
                        vehicleEntry.Value.Reset();
                    }
                }
            }
        }

        [JsonObject(MemberSerialization.OptIn)]
        public class Vehicle
        {
            [JsonProperty("entityID")]
            public ulong EntityId { get; set; }

            [JsonProperty("lastDeath")]
            public double LastDeath { get; set; }

            public ulong PlayerId { get; set; }
            public BaseEntity Entity { get; set; }
            public string VehicleType { get; set; }
            public double LastRecall { get; set; }
            public double LastDismount { get; set; }

            public void OnDismount()
            {
                LastDismount = TimeEx.currentTimestamp;
            }

            public void OnRecall()
            {
                LastRecall = TimeEx.currentTimestamp;
            }

            public void OnDeath()
            {
                Entity = null;
                EntityId = 0;
                LastDeath = TimeEx.currentTimestamp;
            }

            public void Reset()
            {
                EntityId = 0;
                LastDeath = 0;
            }

            public static Vehicle Create(ulong playerId, string vehicleType)
            {
                var vehicle = new Vehicle();
                vehicle.VehicleType = vehicleType;
                vehicle.PlayerId = playerId;
                return vehicle;
            }
        }

        private void LoadData()
        {
            try
            {
                storedData = Interface.Oxide.DataFileSystem.ReadObject<StoredData>(Name);
            }
            catch
            {
                storedData = null;
            }
            if (storedData == null)
            {
                ClearData();
            }
        }

        private void ClearData()
        {
            storedData = new StoredData();
            SaveData();
        }

        private void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject(Name, storedData);
        }

        private void OnNewSave()
        {
            if (configData.global.clearVehicleOnWipe)
            {
                ClearData();
            }
            else
            {
                storedData.ResetPlayerData();
                SaveData();
            }
        }

        #endregion DataFile

        #region LanguageFile

        private void Print(BasePlayer player, string message)
        {
            Player.Message(player, message, configData.chat.prefix, configData.chat.steamIDIcon);
        }

        private void Print(ConsoleSystem.Arg arg, string message)
        {
            var player = arg.Player();
            if (player == null)
            {
                Puts(message);
            }
            else
            {
                PrintToConsole(player, message);
            }
        }

        private string Lang(string key, string id = null, params object[] args)
        {
            try
            {
                return string.Format(lang.GetMessage(key, this, id), args);
            }
            catch (Exception)
            {
                PrintError($"Error in the language formatting of '{key}'. (userid: {id}. lang: {lang.GetLanguage(id)}. args: {string.Join(" ,", args)})");
                throw;
            }
        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["Help"] = "These are the available commands:",
                ["HelpLicence1"] = "<color=#4DFF4D>/{0}</color> -- To buy a vehicle",
                ["HelpLicence2"] = "<color=#4DFF4D>/{0}</color> -- To spawn a vehicle",
                ["HelpLicence3"] = "<color=#4DFF4D>/{0}</color> -- To recall a vehicle",
                ["HelpLicence4"] = "<color=#4DFF4D>/{0}</color> -- To kill a vehicle",
                ["HelpLicence5"] = "<color=#4DFF4D>/{0}</color> -- To buy, spawn or recall a <color=#009EFF>{1}</color>",

                ["PriceFormat"] = "<color=#FF1919>{0}</color> x{1}",
                ["HelpBuy"] = "<color=#4DFF4D>/{0} {1}</color> -- To buy a <color=#009EFF>{2}</color>",
                ["HelpBuyPrice"] = "<color=#4DFF4D>/{0} {1}</color> -- To buy a <color=#009EFF>{2}</color>. Price: {3}",
                ["HelpSpawn"] = "<color=#4DFF4D>/{0} {1}</color> -- To spawn a <color=#009EFF>{2}</color>",
                ["HelpSpawnPrice"] = "<color=#4DFF4D>/{0} {1}</color> -- To spawn a <color=#009EFF>{2}</color>. Price: {3}",
                ["HelpRecall"] = "<color=#4DFF4D>/{0} {1}</color> -- To recall a <color=#009EFF>{2}</color>",
                ["HelpRecallPrice"] = "<color=#4DFF4D>/{0} {1}</color> -- To recall a <color=#009EFF>{2}</color>. Price: {3}",
                ["HelpKill"] = "<color=#4DFF4D>/{0} {1}</color> -- To kill a <color=#009EFF>{2}</color>",
                ["HelpKillCustom"] = "<color=#4DFF4D>/{0} {1}</color> or <color=#4DFF4D>/{2}</color>  -- To kill a <color=#009EFF>{3}</color>",

                ["NotAllowed"] = "You do not have permission to use this command.",
                ["PleaseWait"] = "Please wait a little bit before using this command.",
                ["RaidBlocked"] = "<color=#FF1919>You may not do that while raid blocked</color>.",
                ["CombatBlocked"] = "<color=#FF1919>You may not do that while combat blocked</color>.",
                ["OptionNotFound"] = "This <color=#009EFF>{0}</color> option doesn't exist.",
                ["VehiclePurchased"] = "You have purchased a <color=#009EFF>{0}</color>, type <color=#4DFF4D>/{1}</color> for more information.",
                ["VehicleAlreadyPurchased"] = "You have already purchased <color=#009EFF>{0}</color>.",
                ["VehicleCannotBeBought"] = "<color=#009EFF>{0}</color> is unpurchasable",
                ["VehicleNotOut"] = "<color=#009EFF>{0}</color> is not out, type <color=#4DFF4D>/{1}</color> for more information.",
                ["AlreadyVehicleOut"] = "You already have a <color=#009EFF>{0}</color> outside, type <color=#4DFF4D>/{1}</color> for more information.",
                ["VehicleNotYetPurchased"] = "You have not yet purchased a <color=#009EFF>{0}</color>, type <color=#4DFF4D>/{1}</color> for more information.",
                ["VehicleSpawned"] = "You spawned your <color=#009EFF>{0}</color>.",
                ["VehicleRecalled"] = "You recalled your <color=#009EFF>{0}</color>.",
                ["VehicleKilled"] = "You killed your <color=#009EFF>{0}</color>.",
                ["VehicleOnSpawnCooldown"] = "You must wait <color=#FF1919>{0}</color> seconds before you can spawn your <color=#009EFF>{1}</color>.",
                ["VehicleOnRecallCooldown"] = "You must wait <color=#FF1919>{0}</color> seconds before you can recall your <color=#009EFF>{1}</color>.",
                ["VehicleOnSpawnCooldownPay"] = "You must wait <color=#FF1919>{0}</color> seconds before you can spawn your <color=#009EFF>{1}</color>. You can bypass this cooldown by using the <color=#FF1919>/{2}</color> command to pay <color=#009EFF>{3}</color>",
                ["VehicleOnRecallCooldownPay"] = "You must wait <color=#FF1919>{0}</color> seconds before you can recall your <color=#009EFF>{1}</color>. You can bypass this cooldown by using the <color=#FF1919>/{2}</color> command to pay <color=#009EFF>{3}</color>",
                ["NotLookingAtWater"] = "You must be looking at water to spawn or recall a <color=#009EFF>{0}</color>.",
                ["BuildingBlocked"] = "You can't spawn a <color=#009EFF>{0}</color> if you don't have the building privileges.",
                ["RefundedVehicleItems"] = "Your <color=#009EFF>{0}</color> vehicle items was refunded to your inventory.",
                ["PlayerMountedOnVehicle"] = "It cannot be recalled or killed when players mounted on your <color=#009EFF>{0}</color>.",
                ["PlayerInSafeZone"] = "You cannot spawn or recall your <color=#009EFF>{0}</color> in the safe zone.",
                ["VehicleInventoryDropped"] = "Your <color=#009EFF>{0}</color> vehicle inventory cannot be recalled, it have dropped to the ground.",
                ["NoResourcesToPurchaseVehicle"] = "You don't have enough resources to buy a <color=#009EFF>{0}</color>. You are missing: \n{1}",
                ["NoResourcesToSpawnVehicle"] = "You don't have enough resources to spawn a <color=#009EFF>{0}</color>. You are missing: \n{1}",
                ["NoResourcesToSpawnVehicleBypass"] = "You don't have enough resources to bypass the cooldown to spawn a <color=#009EFF>{0}</color>. You are missing: \n{1}",
                ["NoResourcesToRecallVehicle"] = "You don't have enough resources to recall a <color=#009EFF>{0}</color>. You are missing: \n{1}",
                ["NoResourcesToRecallVehicleBypass"] = "You don't have enough resources to bypass the cooldown to recall a <color=#009EFF>{0}</color>. You are missing: \n{1}",
                ["MountedOrParented"] = "You cannot spawn or recall a <color=#009EFF>{0}</color> when mounted or parented.",
                ["RecallTooFar"] = "You must be within <color=#FF1919>{0}</color> meters of <color=#009EFF>{1}</color> to recall.",
                ["KillTooFar"] = "You must be within <color=#FF1919>{0}</color> meters of <color=#009EFF>{1}</color> to kill.",
                ["PlayersOnNearby"] = "You cannot spawn or recall a <color=#009EFF>{0}</color> when there are players near the position you are looking at.",
                ["RecallWasBlocked"] = "An external plugin blocked you from recalling a <color=#009EFF>{0}</color>.",
                ["NoRecallInZone"] = "No recalling a <color=#009EFF>{0}</color> in the zone.",
                ["NoSpawnInZone"] = "No spawning a <color=#009EFF>{0}</color> in the zone.",
                ["NoSpawnInAir"] = "No spawning a <color=#009EFF>{0}</color> in the air.",
                ["SpawnWasBlocked"] = "An external plugin blocked you from spawning a <color=#009EFF>{0}</color>.",
                ["VehiclesLimit"] = "You can have up to <color=#009EFF>{0}</color> vehicles at a time.",
                ["TooFarTrainTrack"] = "You are too far from the train track.",
                ["TooCloseTrainBarricadeOrWorkCart"] = "You are too close to the train barricade or work cart.",
                ["NotSpawnedOrRecalled"] = "For some reason, your <color=#009EFF>{0}</color> vehicle was not spawned/recalled",

                ["CantUse"] = "Sorry! This {0} belongs to {1}. You cannot use it.",
                ["CantPush"] = "Sorry! This {0} belongs to {1}. You cannot push it.",
            }, this);
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["Help"] = "可用命令列表:",
                ["HelpLicence1"] = "<color=#4DFF4D>/{0}</color> -- 购买一辆载具",
                ["HelpLicence2"] = "<color=#4DFF4D>/{0}</color> -- 生成一辆载具",
                ["HelpLicence3"] = "<color=#4DFF4D>/{0}</color> -- 召回一辆载具",
                ["HelpLicence4"] = "<color=#4DFF4D>/{0}</color> -- 摧毁一辆载具",
                ["HelpLicence5"] = "<color=#4DFF4D>/{0}</color> -- 购买，生成，召回一辆 <color=#009EFF>{1}</color>",

                ["PriceFormat"] = "<color=#FF1919>{0}</color> x{1}",
                ["HelpBuy"] = "<color=#4DFF4D>/{0} {1}</color> -- 购买一辆 <color=#009EFF>{2}</color>",
                ["HelpBuyPrice"] = "<color=#4DFF4D>/{0} {1}</color> -- 购买一辆 <color=#009EFF>{2}</color>，价格: {3}",
                ["HelpSpawn"] = "<color=#4DFF4D>/{0} {1}</color> -- 生成一辆 <color=#009EFF>{2}</color>",
                ["HelpSpawnPrice"] = "<color=#4DFF4D>/{0} {1}</color> -- 生成一辆 <color=#009EFF>{2}</color>，价格: {3}",
                ["HelpRecall"] = "<color=#4DFF4D>/{0} {1}</color> -- 召回一辆 <color=#009EFF>{2}</color>",
                ["HelpRecallPrice"] = "<color=#4DFF4D>/{0} {1}</color> -- 召回一辆 <color=#009EFF>{2}</color>，价格: {3}",
                ["HelpKill"] = "<color=#4DFF4D>/{0} {1}</color> -- 摧毁一辆 <color=#009EFF>{2}</color>",
                ["HelpKillCustom"] = "<color=#4DFF4D>/{0} {1}</color> 或者 <color=#4DFF4D>/{2}</color>  -- 摧毁一辆 <color=#009EFF>{3}</color>",

                ["NotAllowed"] = "您没有权限使用该命令",
                ["PleaseWait"] = "使用此命令之前请稍等一下",
                ["RaidBlocked"] = "<color=#FF1919>您被突袭阻止了，不能使用该命令</color>",
                ["CombatBlocked"] = "<color=#FF1919>您被战斗阻止了，不能使用该命令</color>",
                ["OptionNotFound"] = "选项 <color=#009EFF>{0}</color> 不存在",
                ["VehiclePurchased"] = "您购买了 <color=#009EFF>{0}</color>, 输入 <color=#4DFF4D>/{1}</color> 了解更多信息",
                ["VehicleAlreadyPurchased"] = "您已经购买了 <color=#009EFF>{0}</color>",
                ["VehicleCannotBeBought"] = "<color=#009EFF>{0}</color> 是不可购买的",
                ["VehicleNotOut"] = "您还没有生成您的 <color=#009EFF>{0}</color>, 输入 <color=#4DFF4D>/{1}</color> 了解更多信息",
                ["AlreadyVehicleOut"] = "您已经生成了您的 <color=#009EFF>{0}</color>, 输入 <color=#4DFF4D>/{1}</color> 了解更多信息",
                ["VehicleNotYetPurchased"] = "您还没有购买 <color=#009EFF>{0}</color>, 输入 <color=#4DFF4D>/{1}</color> 了解更多信息",
                ["VehicleSpawned"] = "您生成了您的 <color=#009EFF>{0}</color>",
                ["VehicleRecalled"] = "您召回了您的 <color=#009EFF>{0}</color>",
                ["VehicleKilled"] = "您摧毁了您的 <color=#009EFF>{0}</color>",
                ["VehicleOnSpawnCooldown"] = "您必须等待 <color=#FF1919>{0}</color> 秒，才能生成您的 <color=#009EFF>{1}</color>",
                ["VehicleOnRecallCooldown"] = "您必须等待 <color=#FF1919>{0}</color> 秒，才能召回您的 <color=#009EFF>{1}</color>",
                ["VehicleOnSpawnCooldownPay"] = "您必须等待 <color=#FF1919>{0}</color> 秒，才能生成您的 <color=#009EFF>{1}</color>。你可以使用 <color=#FF1919>/{2}</color> 命令支付 <color=#009EFF>{3}</color> 来绕过这个冷却时间",
                ["VehicleOnRecallCooldownPay"] = "您必须等待 <color=#FF1919>{0}</color> 秒，才能召回您的 <color=#009EFF>{1}</color>。你可以使用 <color=#FF1919>/{2}</color> 命令支付 <color=#009EFF>{3}</color> 来绕过这个冷却时间",
                ["NotLookingAtWater"] = "您必须看着水面才能生成您的 <color=#009EFF>{0}</color>",
                ["BuildingBlocked"] = "您没有领地柜权限，无法生成您的 <color=#009EFF>{0}</color>",
                ["RefundedVehicleItems"] = "您的 <color=#009EFF>{0}</color> 载具物品已经归还回您的库存",
                ["PlayerMountedOnVehicle"] = "您的 <color=#009EFF>{0}</color> 上坐着玩家，无法被召回或摧毁",
                ["PlayerInSafeZone"] = "您不能在安全区域内生成或召回您的 <color=#009EFF>{0}</color>",
                ["VehicleInventoryDropped"] = "您的 <color=#009EFF>{0}</color> 载具物品不能召回，它已经掉落在地上了",
                ["NoResourcesToPurchaseVehicle"] = "您没有足够的资源购买 <color=#009EFF>{0}</color>，还需要: \n{1}",
                ["NoResourcesToSpawnVehicle"] = "您没有足够的资源生成 <color=#009EFF>{0}</color>，还需要: \n{1}",
                ["NoResourcesToSpawnVehicleBypass"] = "您没有足够的资源绕过冷却时间来生成 <color=#009EFF>{0}</color>，还需要: \n{1}",
                ["NoResourcesToRecallVehicle"] = "您没有足够的资源召回 <color=#009EFF>{0}</color>，还需要: \n{1}",
                ["NoResourcesToRecallVehicleBypass"] = "您没有足够的资源绕过冷却时间来召回 <color=#009EFF>{0}</color>，还需要: \n{1}",
                ["MountedOrParented"] = "当您坐着或者在附着在实体上时无法生成或召回 <color=#009EFF>{0}</color>",
                ["RecallTooFar"] = "您必须在 <color=#FF1919>{0}</color> 米内才能召回您的 <color=#009EFF>{1}</color>",
                ["KillTooFar"] = "您必须在 <color=#FF1919>{0}</color> 米内才能摧毁您的 <color=#009EFF>{1}</color>",
                ["PlayersOnNearby"] = "您正在看着的位置附近有玩家时无法生成或召回 <color=#009EFF>{0}</color>",
                ["RecallWasBlocked"] = "有其他插件阻止您召回 <color=#009EFF>{0}</color>.",
                ["NoRecallInZone"] = "不召回该区域中的<color=#009EFF>{0}</color>.",
                ["NoSpawnInZone"] = "不会在该区域生成 <color=#009EFF>{0}</color>.",
                ["NoSpawnInAir"] = "在空中时不会生成 <color=#009EFF>{0}</color>.",
                ["SpawnWasBlocked"] = "有其他插件阻止您生成 <color=#009EFF>{0}</color>.",
                ["VehiclesLimit"] = "您在同一时间内最多可以拥有 <color=#009EFF>{0}</color> 辆载具",
                ["TooFarTrainTrack"] = "您距离铁路轨道太远了",
                ["TooCloseTrainBarricadeOrWorkCart"] = "您距离铁轨障碍物或其它火车太近了",
                ["NotSpawnedOrRecalled"] = "由于某些原因，您的 <color=#009EFF>{0}</color> 载具无法生成或召回",

                ["CantUse"] = "您不能使用它，这个 {0} 属于 {1}",
                ["CantPush"] = "您无法推送此内容，它 {0} 属于 {1}.",
            }, this, "zh-CN");
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["Help"] = "Список доступных команд:",
                ["HelpLicence1"] = "<color=#4DFF4D>/{0}</color> -- Купить транспорт",
                ["HelpLicence2"] = "<color=#4DFF4D>/{0}</color> -- Создать транспорт",
                ["HelpLicence3"] = "<color=#4DFF4D>/{0}</color> -- Вызвать транспорт",
                ["HelpLicence4"] = "<color=#4DFF4D>/{0}</color> -- Уничтожить транспорт",
                ["HelpLicence5"] = "<color=#4DFF4D>/{0}</color> -- Купить, создать, или вызвать <color=#009EFF>{1}</color>",

                ["PriceFormat"] = "<color=#FF1919>{0}</color> x{1}",
                ["HelpBuy"] = "<color=#4DFF4D>/{0} {1}</color> -- Купить <color=#009EFF>{2}</color>.",
                ["HelpBuyPrice"] = "<color=#4DFF4D>/{0} {1}</color> -- Купить <color=#009EFF>{2}</color>. Цена: {3}",
                ["HelpSpawn"] = "<color=#4DFF4D>/{0} {1}</color> -- Создать <color=#009EFF>{2}</color>",
                ["HelpSpawnPrice"] = "<color=#4DFF4D>/{0} {1}</color> -- Вызывать <color=#009EFF>{2}</color>. Цена: {3}",
                ["HelpRecall"] = "<color=#4DFF4D>/{0} {1}</color> -- Вызвать <color=#009EFF>{2}</color>",
                ["HelpRecallPrice"] = "<color=#4DFF4D>/{0} {1}</color> -- Вызвать <color=#009EFF>{2}</color>. Цена: {3}",
                ["HelpKill"] = "<color=#4DFF4D>/{0} {1}</color> -- Уничтожить <color=#009EFF>{2}</color>",
                ["HelpKillCustom"] = "<color=#4DFF4D>/{0} {1}</color> или же <color=#4DFF4D>/{2}</color>  -- Уничтожить <color=#009EFF>{3}</color>",

                ["NotAllowed"] = "У вас нет разрешения для использования данной команды.",
                ["PleaseWait"] = "Пожалуйста, подождите немного, прежде чем использовать эту команду.",
                ["RaidBlocked"] = "<color=#FF1919>Вы не можете это сделать из-за блокировки (рейд)</color>.",
                ["CombatBlocked"] = "<color=#FF1919>Вы не можете это сделать из-за блокировки (бой)</color>.",
                ["OptionNotFound"] = "Опция <color=#009EFF>{0}</color> не существует.",
                ["VehiclePurchased"] = "Вы приобрели <color=#009EFF>{0}</color>, напишите <color=#4DFF4D>/{1}</color> для получения дополнительной информации.",
                ["VehicleAlreadyPurchased"] = "Вы уже приобрели <color=#009EFF>{0}</color>.",
                ["VehicleCannotBeBought"] = "<color=#009EFF>{0}</color> приобрести невозможно",
                ["VehicleNotOut"] = "<color=#009EFF>{0}</color> отсутствует. Напишите <color=#4DFF4D>/{1}</color> для получения дополнительной информации.",
                ["AlreadyVehicleOut"] = "У вас уже есть <color=#009EFF>{0}</color>, напишите <color=#4DFF4D>/{1}</color>  для получения дополнительной информации.",
                ["VehicleNotYetPurchased"] = "Вы ещё не приобрели <color=#009EFF>{0}</color>. Напишите <color=#4DFF4D>/{1}</color> для получения дополнительной информации.",
                ["VehicleSpawned"] = "Вы создали ваш <color=#009EFF>{0}</color>.",
                ["VehicleRecalled"] = "Вы вызвали ваш <color=#009EFF>{0}</color>.",
                ["VehicleKilled"] = "Вы уничтожили ваш <color=#009EFF>{0}</color>.",
                ["VehicleOnSpawnCooldown"] = "Вам необходимо подождать <color=#FF1919>{0}</color> секунд прежде, чем создать свой <color=#009EFF>{1}</color>.",
                ["VehicleOnRecallCooldown"] = "Вам необходимо подождать <color=#FF1919>{0}</color> секунд прежде, чем вызвать свой <color=#009EFF>{1}</color>.",
                ["VehicleOnSpawnCooldownPay"] = "Вам необходимо подождать <color=#FF1919>{0}</color> секунд прежде, чем создать свой <color=#009EFF>{1}</color>. Вы можете обойти это время восстановления, используя команду <color=#FF1919>/{2}</color>, чтобы заплатить <color=#009EFF>{3}</color>",
                ["VehicleOnRecallCooldownPay"] = "Вам необходимо подождать <color=#FF1919>{0}</color> секунд прежде, чем вызвать свой <color=#009EFF>{1}</color>. Вы можете обойти это время восстановления, используя команду <color=#FF1919>/{2}</color>, чтобы заплатить <color=#009EFF>{3}</color>",
                ["NotLookingAtWater"] = "Вы должны смотреть на воду, чтобы создать или вызвать <color=#009EFF>{0}</color>.",
                ["BuildingBlocked"] = "Вы не можете создать <color=#009EFF>{0}</color> если отсутствует право строительства.",
                ["RefundedVehicleItems"] = "Запчасти от вашего <color=#009EFF>{0}</color> были возвращены в ваш инвентарь.",
                ["PlayerMountedOnVehicle"] = "Нельзя вызвать, когда игрок находится в вашем <color=#009EFF>{0}</color>.",
                ["PlayerInSafeZone"] = "Вы не можете создать, или вызвать ваш <color=#009EFF>{0}</color> в безопасной зоне.",
                ["VehicleInventoryDropped"] = "Инвентарь из вашего <color=#009EFF>{0}</color> не может быть вызван, он выброшен на землю.",
                ["NoResourcesToPurchaseVehicle"] = "У вас недостаточно ресурсов для покупки <color=#009EFF>{0}</color>. Вам не хватает: \n{1}",
                ["NoResourcesToSpawnVehicle"] = "У вас недостаточно ресурсов для покупки <color=#009EFF>{0}</color>. Вам не хватает: \n{1}",
                ["NoResourcesToSpawnVehicleBypass"] = "У вас недостаточно ресурсов для покупки <color=#009EFF>{0}</color>. Вам не хватает: \n{1}",
                ["NoResourcesToRecallVehicle"] = "У вас недостаточно ресурсов для покупки <color=#009EFF>{0}</color>. Вам не хватает: \n{1}",
                ["NoResourcesToRecallVehicleBypass"] = "У вас недостаточно ресурсов для покупки <color=#009EFF>{0}</color>. Вам не хватает: \n{1}",
                ["MountedOrParented"] = "Вы не можете создать <color=#009EFF>{0}</color> когда сидите или привязаны к объекту.",
                ["RecallTooFar"] = "Вы должны быть в пределах <color=#FF1919>{0}</color> метров от <color=#009EFF>{1}</color>, чтобы вызывать.",
                ["KillTooFar"] = "Вы должны быть в пределах <color=#FF1919>{0}</color> метров от <color=#009EFF>{1}</color>, уничтожить.",
                ["PlayersOnNearby"] = "Вы не можете создать <color=#009EFF>{0}</color> когда рядом с той позицией, на которую вы смотрите, есть игроки.",
                ["RecallWasBlocked"] = "Внешний плагин заблокировал вам вызвать <color=#009EFF>{0}</color>.",
                ["NoRecallInZone"] = "Нет отзыва <color=#009EFF>{0}</color> в зоне.",
                ["NoSpawnInZone"] = "В зоне не создается <color=#009EFF>{0}</color>.",
                ["NoSpawnInAir"] = "Не создавать <color=#009EFF>{0}</color> в воздухе.",
                ["SpawnWasBlocked"] = "Внешний плагин заблокировал вам создать <color=#009EFF>{0}</color>.",
                ["VehiclesLimit"] = "У вас может быть до <color=#009EFF>{0}</color> автомобилей одновременно",
                ["TooFarTrainTrack"] = "Вы слишком далеко от железнодорожных путей",
                ["TooCloseTrainBarricadeOrWorkCart"] = "Вы слишком близко к железнодорожной баррикаде или рабочей тележке",
                ["NotSpawnedOrRecalled"] = "По какой-то причине ваш <color=#009EFF>{0}</color>  автомобилей не был вызван / отозван",

                ["CantUse"] = "Простите! Этот {0} принадлежит {1}. Вы не можете его использовать.",
                ["CantPush"] = "Простите! Этот {0} принадлежит {1}. Вы не можете его подтолкнуть.",
            }, this, "ru");
        }

        #endregion LanguageFile
    }
}