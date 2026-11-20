using System;
using System.Reflection;
using System.Threading;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;

namespace DistributeSpray
{
    [BepInPlugin(GUID, NAME, VERSION)]
    [BepInProcess("DSPGAME.exe")]
    public class DistributeSprayPlugin : BaseUnityPlugin
    {
        public const string GUID = "com.Valoneu.DistributeSpray";
        public const string NAME = "DistributeSpray (Personal MK3)";
        public const string VERSION = "1.0.4";
        public static ConfigEntry<bool> ModEnabled;
        public static ConfigEntry<bool> RemoveProliferatorPowerSurcharge;

        private void Awake()
        {
            ModEnabled = Config.Bind("General", "Enabled", true, "Enable/disable automatic spraying.");
            RemoveProliferatorPowerSurcharge = Config.Bind("General", "RemoveProliferatorPowerSurcharge", true, "Remove the extra power cost from proliferator effects while retaining their production bonuses.");
            var harmony = new Harmony(GUID);
            harmony.PatchAll(typeof(GameBeginPatch));
            harmony.PatchAll(typeof(PowerSurchargePatches));
            MethodInfo insertUint = AccessTools.Method(typeof(PlanetFactory), "InsertInto", new Type[] { typeof(uint), typeof(int), typeof(int), typeof(byte), typeof(byte), typeof(byte).MakeByRefType() });
            if (insertUint != null) harmony.Patch(insertUint, prefix: new HarmonyMethod(typeof(InsertPatches), nameof(InsertPatches.InsertInto_Prefix)), postfix: new HarmonyMethod(typeof(InsertPatches), nameof(InsertPatches.InsertInto_Postfix)));
            MethodInfo insertInt = AccessTools.Method(typeof(PlanetFactory), "InsertInto", new Type[] { typeof(int), typeof(int), typeof(int), typeof(byte), typeof(byte), typeof(byte).MakeByRefType() });
            if (insertInt != null) harmony.Patch(insertInt, prefix: new HarmonyMethod(typeof(InsertPatches), nameof(InsertPatches.InsertInto_Prefix)), postfix: new HarmonyMethod(typeof(InsertPatches), nameof(InsertPatches.InsertInto_Postfix)));
            MethodInfo insertStorage = AccessTools.Method(typeof(PlanetFactory), nameof(PlanetFactory.InsertIntoStorage), new Type[] { typeof(int), typeof(int), typeof(int), typeof(int), typeof(int).MakeByRefType(), typeof(bool) });
            if (insertStorage != null) harmony.Patch(insertStorage, prefix: new HarmonyMethod(typeof(InsertPatches), nameof(InsertPatches.InsertIntoStorage_Prefix)), postfix: new HarmonyMethod(typeof(InsertPatches), nameof(InsertPatches.InsertIntoStorage_Postfix)));
            MethodInfo addItem = AccessTools.Method(typeof(StationComponent), nameof(StationComponent.AddItem), new Type[] { typeof(int), typeof(int), typeof(int) });
            if (addItem != null) harmony.Patch(addItem, prefix: new HarmonyMethod(typeof(StationPatches), nameof(StationPatches.AddItem_Prefix)), postfix: new HarmonyMethod(typeof(StationPatches), nameof(StationPatches.AddItem_Postfix)));
            Logger.LogInfo($"{NAME} v{VERSION} loaded!");
        }
    }

    [HarmonyPatch]
    public static class PowerSurchargePatches
    {
        private static bool IsEnabled() =>
            DistributeSprayPlugin.ModEnabled?.Value == true &&
            DistributeSprayPlugin.RemoveProliferatorPowerSurcharge?.Value == true;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(AssemblerComponent), nameof(AssemblerComponent.SetPCState))]
        public static void RemoveAssemblerPowerSurcharge(AssemblerComponent __instance, PowerConsumerComponent[] pcPool)
        {
            if (!IsEnabled() || __instance.extraPowerRatio <= 0 || pcPool == null || __instance.pcId <= 0 || __instance.pcId >= pcPool.Length)
                return;

            pcPool[__instance.pcId].SetRequiredEnergy(__instance.replicating, 1000);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(LabComponent), nameof(LabComponent.SetPCState))]
        public static void RemoveLabPowerSurcharge(LabComponent __instance, PowerConsumerComponent[] pcPool)
        {
            if (!IsEnabled() || __instance.extraPowerRatio <= 0 || pcPool == null || __instance.pcId <= 0 || __instance.pcId >= pcPool.Length)
                return;

            pcPool[__instance.pcId].SetRequiredEnergy(__instance.replicating, 1000);
        }
    }

    public static class StationPatches
    {
        public static void AddItem_Prefix(StationComponent __instance, int itemId, int count, ref int inc, ref bool __state)
        {
            if (!DistributeSprayPlugin.ModEnabled.Value || count <= 0 || itemId <= 0 || GameMain.data == null) return;
            PlanetFactory factory = null;
            if (GameMain.data.factories != null)
            {
                foreach (var candidate in GameMain.data.factories)
                {
                    if (candidate != null && candidate.planetId == __instance.planetId) { factory = candidate; break; }
                }
            }
            if (factory == null) return;
            var status = SprayLogic.GetStatus(factory.index);
            if (status == null || status.incLevel <= 0) return;
            int deficit = count * status.incLevel - inc;
            if (deficit <= 0) return;
            __state = true;
            inc += deficit;
        }

        public static void AddItem_Postfix(StationComponent __instance, int __result, bool __state)
        {
            if (!__state || __result <= 0) return;
            PlanetFactory factory = null;
            if (GameMain.data?.factories != null)
            {
                foreach (var candidate in GameMain.data.factories)
                {
                    if (candidate != null && candidate.planetId == __instance.planetId) { factory = candidate; break; }
                }
            }
            if (factory == null) return;
            var status = SprayLogic.GetStatus(factory.index);
            if (status != null) Interlocked.Add(ref status.incDebt, __result);
        }
    }

    public class SprayStatus
    {
        public int incLevel;
        public volatile int incDebt;
        public int bestProlIdx;
    }

    [HarmonyPatch]
    public static class GameBeginPatch
    {
        private static bool _subscribed;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameMain), "Begin")]
        public static void OnGameBegin()
        {
            try
            {
                SprayLogic.InitStatusArray(GameMain.data.factories.Length);
                if (_subscribed)
                {
                    GameMain.logic.onFactoryFrameEnd -= SprayLogic.OnFactoryFrameEnd;
                    _subscribed = false;
                }
                GameMain.logic.onFactoryFrameEnd += SprayLogic.OnFactoryFrameEnd;
                _subscribed = true;
            }
            catch { }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameMain), "End")]
        public static void OnGameEnd()
        {
            try
            {
                if (_subscribed)
                {
                    GameMain.logic.onFactoryFrameEnd -= SprayLogic.OnFactoryFrameEnd;
                    _subscribed = false;
                }
            }
            catch { }
        }
    }

    public static class SprayLogic
    {
        private static SprayStatus[] _statusArr;
        public static readonly int[] ProlIds = { 1143 };
        public static readonly int[] ProlInc = { 4 };
        public static readonly int[] ProlSpray = { 240 };

        public static void InitStatusArray(int size)
        {
            _statusArr = new SprayStatus[size];
            for (int i = 0; i < size; i++) _statusArr[i] = new SprayStatus();
        }

        public static SprayStatus GetStatus(int factoryIndex) =>
            (_statusArr != null && factoryIndex >= 0 && factoryIndex < _statusArr.Length) ? _statusArr[factoryIndex] : null;

        public static void OnFactoryFrameEnd()
        {
            try
            {
                if (!DistributeSprayPlugin.ModEnabled.Value || GameMain.data == null || _statusArr == null) return;
                NexusLogic.Init();
                var data = GameMain.data;
                var doneStars = new System.Collections.Generic.HashSet<int>();
                for (int factoryIndex = 0; factoryIndex < data.factoryCount; factoryIndex++)
                {
                    var factory = data.factories[factoryIndex];
                    if (factory?.transport == null || factoryIndex >= _statusArr.Length) continue;
                    var status = _statusArr[factoryIndex];
                    bool fullScan = GameMain.instance.timei % 60 == 0;
                    if (fullScan)
                    {
                        int bestLevel = 0;
                        int bestIndex = -1;
                        for (int stationIndex = 1; stationIndex < factory.transport.stationCursor; stationIndex++)
                        {
                            var station = factory.transport.stationPool[stationIndex];
                            if (station == null || station.id != stationIndex || station.isCollector) continue;
                            for (int slot = 0; slot < station.storage.Length; slot++)
                            {
                                int itemId = station.storage[slot].itemId;
                                int count = station.storage[slot].count;
                                if (itemId <= 0 || count <= 0) continue;
                                for (int proliferatorIndex = 0; proliferatorIndex < ProlIds.Length; proliferatorIndex++)
                                {
                                    if (itemId == ProlIds[proliferatorIndex] && ProlInc[proliferatorIndex] > bestLevel)
                                    {
                                        bestLevel = ProlInc[proliferatorIndex];
                                        bestIndex = proliferatorIndex;
                                    }
                                }
                                if (status.incLevel > 0)
                                {
                                    int targetInc = count * status.incLevel;
                                    if (station.storage[slot].inc < targetInc)
                                    {
                                        int diff = targetInc - station.storage[slot].inc;
                                        int itemsToSpray = (diff + status.incLevel - 1) / status.incLevel;
                                        station.storage[slot].inc = targetInc;
                                        Interlocked.Add(ref status.incDebt, itemsToSpray);
                                    }
                                }
                            }
                        }
                        status.incLevel = bestLevel;
                        status.bestProlIdx = bestIndex;
                    }
                    if (status.incLevel > 0)
                    {
                        int starIndex = factory.planetId / 100 - 1;
                        if (doneStars.Add(starIndex))
                        {
                            int nexusDebt = 0;
                            NexusLogic.SprayNexus(starIndex, status.incLevel, ref nexusDebt);
                            if (nexusDebt > 0) Interlocked.Add(ref status.incDebt, nexusDebt);
                        }
                    }
                    if (status.incDebt > 0 && status.bestProlIdx >= 0)
                    {
                        int targetId = ProlIds[status.bestProlIdx];
                        int sprayPer = ProlSpray[status.bestProlIdx];
                        int[] consumeRegister = GameMain.statistics?.production?.factoryStatPool?[factory.index]?.consumeRegister;
                        for (int stationIndex = 1; stationIndex < factory.transport.stationCursor && status.incDebt > 0; stationIndex++)
                        {
                            var station = factory.transport.stationPool[stationIndex];
                            if (station == null || station.id != stationIndex || station.isCollector) continue;
                            for (int slot = 0; slot < station.storage.Length && status.incDebt > 0; slot++)
                            {
                                if (station.storage[slot].itemId != targetId || station.storage[slot].count <= 0) continue;
                                int take = Math.Min((status.incDebt + sprayPer - 1) / sprayPer, station.storage[slot].count);
                                station.storage[slot].count -= take;
                                Interlocked.Add(ref status.incDebt, -take * sprayPer);
                                if (consumeRegister != null)
                                {
                                    lock (consumeRegister) consumeRegister[targetId] += take;
                                }
                            }
                        }
                    }
                }
            }
            catch { }
        }
    }

    public static class NexusLogic
    {
        private static Type _starAssemblyType;
        private static FieldInfo _productStorageField;
        private static FieldInfo _productStorageIncField;
        private static bool _initialized;

        public static void Init()
        {
            if (_initialized) return;
            _starAssemblyType = AccessTools.TypeByName("MoreMegaStructure.StarAssembly");
            if (_starAssemblyType != null)
            {
                _productStorageField = AccessTools.Field(_starAssemblyType, "productStorage");
                _productStorageIncField = AccessTools.Field(_starAssemblyType, "productStorageInc");
            }
            _initialized = true;
        }

        public static void SprayNexus(int starIndex, int incLevel, ref int incDebt)
        {
            if (_starAssemblyType == null || _productStorageField == null || _productStorageIncField == null) return;
            var storage = _productStorageField.GetValue(null) as System.Collections.Generic.Dictionary<int, System.Collections.Generic.Dictionary<int, int>>;
            var storageInc = _productStorageIncField.GetValue(null) as System.Collections.Generic.Dictionary<int, System.Collections.Generic.Dictionary<int, int>>;
            if (storage == null || !storage.TryGetValue(starIndex, out var items)) return;
            if (!storageInc.TryGetValue(starIndex, out var increments)) return;
            foreach (var itemId in items.Keys.System_Collections_Generic_ICollection_int_ToArray())
            {
                int count = items[itemId];
                if (itemId <= 0 || count <= 0) continue;
                int targetInc = count * incLevel;
                increments.TryGetValue(itemId, out int currentInc);
                if (currentInc < targetInc)
                {
                    int diff = targetInc - currentInc;
                    increments[itemId] = targetInc;
                    incDebt += (diff + incLevel - 1) / incLevel;
                }
            }
        }
    }

    internal static class Extensions
    {
        public static T[] System_Collections_Generic_ICollection_int_ToArray<T>(this System.Collections.Generic.ICollection<T> collection)
        {
            T[] array = new T[collection.Count];
            collection.CopyTo(array, 0);
            return array;
        }
    }

    public static class InsertPatches
    {
        public static void InsertInto_Prefix(PlanetFactory __instance, byte itemCount, ref byte itemInc, ref bool __state)
        {
            if (!DistributeSprayPlugin.ModEnabled.Value) return;
            var status = SprayLogic.GetStatus(__instance.index);
            if (status == null || status.incLevel <= 0) return;
            int deficit = itemCount * status.incLevel - itemInc;
            if (deficit <= 0) return;
            deficit = Math.Min(deficit, 255 - itemInc);
            __state = true;
            itemInc += (byte)deficit;
        }

        public static void InsertInto_Postfix(PlanetFactory __instance, int __result, bool __state)
        {
            if (__state && __result > 0)
            {
                var status = SprayLogic.GetStatus(__instance.index);
                if (status != null) Interlocked.Add(ref status.incDebt, __result);
            }
        }

        public static void InsertIntoStorage_Prefix(PlanetFactory __instance, int count, ref int inc, ref bool __state)
        {
            if (!DistributeSprayPlugin.ModEnabled.Value) return;
            var status = SprayLogic.GetStatus(__instance.index);
            if (status == null || status.incLevel <= 0) return;
            int deficit = count * status.incLevel - inc;
            if (deficit <= 0) return;
            __state = true;
            inc += deficit;
        }

        public static void InsertIntoStorage_Postfix(PlanetFactory __instance, int __result, bool __state)
        {
            if (__state && __result > 0)
            {
                var status = SprayLogic.GetStatus(__instance.index);
                if (status != null) Interlocked.Add(ref status.incDebt, __result);
            }
        }
    }
}
