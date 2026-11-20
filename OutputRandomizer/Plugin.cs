using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using HarmonyLib;

namespace OutputRandomizer
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInProcess("DSPGAME.exe")]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.francisco.dsp.outputrandomizer";
        public const string PluginName = "Output Randomizer";
        public const string PluginVersion = "1.0.0";

        internal static BepInEx.Logging.ManualLogSource ModLogger { get; private set; }
        internal static BepInEx.Configuration.ConfigEntry<bool> DefaultEnabled { get; private set; }
        private Harmony _harmony;

        private void Awake()
        {
            ModLogger = Logger;
            DefaultEnabled = Config.Bind("General", "Enabled", true, "Default for newly created saves.");
            NewGameMenuIntegration.PendingEnabled = DefaultEnabled.Value;
            _harmony = new Harmony(PluginGuid);

            MethodInfo galaxySelectOpen = AccessTools.Method(typeof(UIGalaxySelect), "_OnOpen");
            if (galaxySelectOpen != null)
                _harmony.Patch(galaxySelectOpen, postfix: new HarmonyMethod(typeof(NewGameMenuIntegration), nameof(NewGameMenuIntegration.OnOpen)));
            MethodInfo galaxySelectClose = AccessTools.Method(typeof(UIGalaxySelect), "_OnClose");
            if (galaxySelectClose != null)
                _harmony.Patch(galaxySelectClose, postfix: new HarmonyMethod(typeof(NewGameMenuIntegration), nameof(NewGameMenuIntegration.OnClose)));
            MethodInfo newGame = AccessTools.Method(typeof(GameData), nameof(GameData.NewGame), new[] { typeof(GameDesc) });
            if (newGame != null)
                _harmony.Patch(newGame, prefix: new HarmonyMethod(typeof(RandomizerSaveState), nameof(RandomizerSaveState.OnNewGame)));

            MethodInfo saveCurrentGame = AccessTools.Method(typeof(GameSave), nameof(GameSave.SaveCurrentGame), new[] { typeof(string) });
            if (saveCurrentGame != null)
                _harmony.Patch(saveCurrentGame, postfix: new HarmonyMethod(typeof(RandomizerSaveState), nameof(RandomizerSaveState.AfterSave)));

            MethodInfo loadCurrentGame = AccessTools.Method(typeof(GameSave), nameof(GameSave.LoadCurrentGame), new[] { typeof(string) });
            if (loadCurrentGame != null)
                _harmony.Patch(loadCurrentGame, prefix: new HarmonyMethod(typeof(RandomizerSaveState), nameof(RandomizerSaveState.BeforeLoad)));

            MethodInfo updateMethod = AccessTools.Method(typeof(MinerComponent), "InternalUpdate");
            if (updateMethod == null)
            {
                ModLogger.LogError("Could not find MinerComponent.InternalUpdate; miner outputs will not be randomized.");
                return;
            }

            _harmony.Patch(
                updateMethod,
                prefix: new HarmonyMethod(typeof(MinerOutputPatch), nameof(MinerOutputPatch.Prefix)),
                postfix: new HarmonyMethod(typeof(MinerOutputPatch), nameof(MinerOutputPatch.Postfix)),
                finalizer: new HarmonyMethod(typeof(MinerOutputPatch), nameof(MinerOutputPatch.Finalizer)));

            MethodInfo gameBegin = AccessTools.Method(typeof(GameMain), nameof(GameMain.Begin));
            if (gameBegin != null)
                _harmony.Patch(gameBegin, postfix: new HarmonyMethod(typeof(MinerOutputPatch), nameof(MinerOutputPatch.OnGameBegin)));

            MethodInfo newMiner = AccessTools.Method(typeof(FactorySystem), nameof(FactorySystem.NewMinerComponent));
            if (newMiner != null)
                _harmony.Patch(newMiner, postfix: new HarmonyMethod(typeof(MinerOutputPatch), nameof(MinerOutputPatch.RegisterNewCollector)));

            MethodInfo insertInt = AccessTools.Method(typeof(PlanetFactory), nameof(PlanetFactory.InsertInto), new[]
            {
                typeof(int), typeof(int), typeof(int), typeof(byte), typeof(byte), typeof(byte).MakeByRefType()
            });
            MethodInfo insertUint = AccessTools.Method(typeof(PlanetFactory), nameof(PlanetFactory.InsertInto), new[]
            {
                typeof(uint), typeof(int), typeof(int), typeof(byte), typeof(byte), typeof(byte).MakeByRefType()
            });
            if (insertInt != null)
                _harmony.Patch(insertInt, prefix: new HarmonyMethod(typeof(MinerOutputPatch), nameof(MinerOutputPatch.InsertIntoPrefix)));
            if (insertUint != null)
                _harmony.Patch(insertUint, prefix: new HarmonyMethod(typeof(MinerOutputPatch), nameof(MinerOutputPatch.InsertIntoPrefix)));

            MethodInfo veinCollection = AccessTools.Method(typeof(StationComponent), nameof(StationComponent.UpdateVeinCollection));
            if (veinCollection != null)
                _harmony.Patch(veinCollection, prefix: new HarmonyMethod(typeof(MinerOutputPatch), nameof(MinerOutputPatch.EnforceDepotPrefix)));

            ModLogger.LogInfo(PluginName + " v" + PluginVersion + " loaded.");
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }
    }

    internal static class MinerOutputPatch
    {
        private static readonly object RandomLock = new object();
        private static readonly Random Random = new Random();
        private static readonly object OutputItemsLock = new object();
        private static readonly Dictionary<long, int> OutputItems = new Dictionary<long, int>();
        private static readonly HashSet<long> CollectorMiners = new HashSet<long>();
        private static int[] _itemIds;

        [ThreadStatic] private static bool _hasActiveMiner;
        [ThreadStatic] private static int _activeFactoryIndex;
        [ThreadStatic] private static int _activeEntityId;
        [ThreadStatic] private static int _activeOutputItemId;

        internal sealed class UpdateState
        {
            internal bool PreviousHasActiveMiner;
            internal int PreviousFactoryIndex;
            internal int PreviousEntityId;
            internal int PreviousOutputItemId;
            internal int FactoryIndex;
            internal int EntityId;
            internal int OutputItemId;
        }

        internal static void Prefix(ref MinerComponent __instance, PlanetFactory factory, ref bool __runOriginal, out UpdateState __state)
        {
            __state = null;
            if (!RandomizerSaveState.Enabled || factory == null)
                return;

            int factoryIndex = factory.index;
            int entityId = __instance.entityId;
            long minerKey = GetMinerKey(factoryIndex, entityId);
            if (__instance.productCount > 0 && CollectorMiners.Contains(minerKey))
            {
                __runOriginal = false;
                return;
            }

            if (__instance.productCount <= 0)
                RemoveOutputItem(factoryIndex, entityId);

            int outputItemId = GetOutputItemId(factoryIndex, entityId);
            if (outputItemId <= 0)
                return;

            __state = new UpdateState
            {
                PreviousHasActiveMiner = _hasActiveMiner,
                PreviousFactoryIndex = _activeFactoryIndex,
                PreviousEntityId = _activeEntityId,
                PreviousOutputItemId = _activeOutputItemId,
                FactoryIndex = factoryIndex,
                EntityId = entityId,
                OutputItemId = outputItemId
            };

            _hasActiveMiner = true;
            _activeFactoryIndex = factoryIndex;
            _activeEntityId = entityId;
            _activeOutputItemId = outputItemId;
        }

        internal static void Postfix(ref MinerComponent __instance, UpdateState __state)
        {
            if (__state == null)
                return;

            if (__instance.productCount > 0)
            {
                __instance.productId = __state.OutputItemId;
            }
            else
            {
                RemoveOutputItem(__state.FactoryIndex, __state.EntityId);
            }
        }

        internal static Exception Finalizer(Exception __exception, UpdateState __state)
        {
            RestoreThreadContext(__state);
            return __exception;
        }

        internal static void InsertIntoPrefix(PlanetFactory __instance, ref int __2, ref byte __3)
        {
            if (!RandomizerSaveState.Enabled || !_hasActiveMiner || __instance == null || __instance.index != _activeFactoryIndex)
                return;

            __2 = _activeOutputItemId;
            if (__3 > 1)
                __3 = 1;
        }

        internal static void EnforceDepotPrefix(StationComponent __instance, PlanetFactory factory, ref bool __runOriginal)
        {
            if (!RandomizerSaveState.Enabled || __instance == null || !__instance.isVeinCollector ||
                __instance.storage == null || __instance.storage.Length == 0 ||
                __instance.collectionIds == null || __instance.collectionIds.Length == 0 ||
                factory?.factorySystem?.minerPool == null || __instance.minerId <= 0 ||
                __instance.minerId >= factory.factorySystem.minerPool.Length)
                return;

            EnforceDepotMode(__instance, factory);

            ref MinerComponent miner = ref factory.factorySystem.minerPool[__instance.minerId];
            if (miner.id != __instance.minerId || miner.productId <= 0 || miner.productCount <= 0)
                return;

            bool itemChanged;
            lock (__instance.storage)
            {
                ref StationStore slot = ref __instance.storage[0];
                itemChanged = slot.itemId != miner.productId || __instance.collectionIds[0] != miner.productId;
                slot.max = 1;

                if (itemChanged && (slot.count > 0 || slot.localOrder != 0 || slot.remoteOrder != 0))
                {
                    __runOriginal = false;
                    return;
                }

                if (itemChanged)
                {
                    slot.itemId = miner.productId;
                    __instance.collectionIds[0] = miner.productId;
                }
            }

            if (itemChanged)
            {
                __instance.UpdateNeeds();
                factory.transport?.RefreshStationTraffic(__instance.id);
            }
        }

        internal static void OnGameBegin()
        {
            lock (OutputItemsLock)
                OutputItems.Clear();
            _itemIds = null;

            var data = GameMain.data;
            if (data?.factories == null)
                return;

            CollectorMiners.Clear();
            for (int factoryIndex = 0; factoryIndex < data.factoryCount; factoryIndex++)
            {
                var factory = data.factories[factoryIndex];
                var transport = factory?.transport;
                var minerPool = factory?.factorySystem?.minerPool;
                if (transport?.stationPool == null || minerPool == null)
                    continue;

                for (int stationIndex = 1; stationIndex < transport.stationCursor; stationIndex++)
                {
                    var station = transport.stationPool[stationIndex];
                    if (station == null || station.id != stationIndex || !station.isVeinCollector ||
                        station.minerId <= 0 || station.minerId >= minerPool.Length)
                        continue;

                    ref MinerComponent miner = ref minerPool[station.minerId];
                    if (miner.id == station.minerId)
                    {
                        CollectorMiners.Add(GetMinerKey(factoryIndex, miner.entityId));
                        EnforceDepotMode(station, factory);
                    }
                }
            }
        }

        internal static void RegisterNewCollector(FactorySystem __instance, int entityId, PrefabDesc desc, int __result)
        {
            if (__result <= 0 || desc == null || !desc.isVeinCollector || __instance?.factory == null ||
                __instance.minerPool == null || __result >= __instance.minerPool.Length)
                return;

            ref MinerComponent miner = ref __instance.minerPool[__result];
            if (miner.id == __result)
                CollectorMiners.Add(GetMinerKey(__instance.factory.index, miner.entityId));
        }

        private static void EnforceDepotMode(StationComponent station, PlanetFactory factory)
        {
            bool changed = false;
            lock (station.storage)
            {
                ref StationStore slot = ref station.storage[0];
                if (slot.localLogic != ELogisticStorage.None)
                {
                    slot.localLogic = ELogisticStorage.None;
                    changed = true;
                }

                if (slot.remoteLogic != ELogisticStorage.None)
                {
                    slot.remoteLogic = ELogisticStorage.None;
                    changed = true;
                }
            }

            if (changed)
            {
                station.UpdateNeeds();
                factory?.transport?.RefreshStationTraffic(station.id);
            }
        }

        internal static void RestoreThreadContext(UpdateState state)
        {
            if (state == null)
                return;

            _hasActiveMiner = state.PreviousHasActiveMiner;
            _activeFactoryIndex = state.PreviousFactoryIndex;
            _activeEntityId = state.PreviousEntityId;
            _activeOutputItemId = state.PreviousOutputItemId;
        }

        private static int GetOutputItemId(int factoryIndex, int entityId)
        {
            long key = GetMinerKey(factoryIndex, entityId);
            lock (OutputItemsLock)
            {
                if (!OutputItems.TryGetValue(key, out int itemId))
                {
                    itemId = GetRandomItemId();
                    if (itemId > 0)
                        OutputItems[key] = itemId;
                }

                return itemId;
            }
        }

        private static void RemoveOutputItem(int factoryIndex, int entityId)
        {
            long key = GetMinerKey(factoryIndex, entityId);
            lock (OutputItemsLock)
                OutputItems.Remove(key);
        }

        private static long GetMinerKey(int factoryIndex, int entityId)
        {
            return ((long)factoryIndex << 32) | (uint)entityId;
        }

        private static int GetRandomItemId()
        {
            if (_itemIds == null)
            {
                var itemPrototypes = LDB.items?.dataArray;
                if (itemPrototypes == null)
                    return 0;

                var itemIds = new List<int>(itemPrototypes.Length);
                foreach (var itemPrototype in itemPrototypes)
                {
                    if (itemPrototype != null && itemPrototype.ID > 0 && RandomizerSaveState.SelectedItemIds.Contains(itemPrototype.ID))
                        itemIds.Add(itemPrototype.ID);
                }

                _itemIds = itemIds.ToArray();
            }

            if (_itemIds.Length == 0)
                return 0;

            lock (RandomLock)
                return _itemIds[Random.Next(_itemIds.Length)];
        }
    }

}