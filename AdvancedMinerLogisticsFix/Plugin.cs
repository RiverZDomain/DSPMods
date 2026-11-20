using System;
using System.Reflection;
using BepInEx;
using HarmonyLib;

namespace AdvancedMinerLogisticsFix
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInProcess("DSPGAME.exe")]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.francisco.dsp.advancedminerlogisticsfix";
        public const string PluginName = "Advanced Miner Logistics Fix";
        public const string PluginVersion = "0.1.0";

        private Harmony _harmony;

        private void Awake()
        {
            _harmony = new Harmony(PluginGuid);
            MethodInfo updateVeinCollection = AccessTools.Method(typeof(StationComponent), nameof(StationComponent.UpdateVeinCollection));
            if (updateVeinCollection == null)
            {
                Logger.LogError("Could not find StationComponent.UpdateVeinCollection; collector logistics will not be fixed.");
                return;
            }

            _harmony.Patch(
                updateVeinCollection,
                prefix: new HarmonyMethod(typeof(VeinCollectorPatch), nameof(VeinCollectorPatch.Prefix)));

            MethodInfo internalTickLocal = AccessTools.Method(typeof(StationComponent), nameof(StationComponent.InternalTickLocal));
            if (internalTickLocal == null)
            {
                Logger.LogError("Could not find StationComponent.InternalTickLocal; single-item collector delivery will not be enabled.");
                return;
            }

            _harmony.Patch(
                internalTickLocal,
                prefix: new HarmonyMethod(typeof(VeinCollectorDispatchPatch), nameof(VeinCollectorDispatchPatch.Prefix)),
                finalizer: new HarmonyMethod(typeof(VeinCollectorDispatchPatch), nameof(VeinCollectorDispatchPatch.Finalizer)));
            Logger.LogInfo(PluginName + " v" + PluginVersion + " loaded.");
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }
    }

    internal static class VeinCollectorPatch
    {
        internal static void Prefix(StationComponent __instance, PlanetFactory factory)
        {
            if (__instance == null || !__instance.isVeinCollector ||
                factory?.factorySystem?.minerPool == null ||
                __instance.minerId <= 0 || __instance.minerId >= factory.factorySystem.minerPool.Length ||
                __instance.storage == null || __instance.storage.Length == 0 ||
                __instance.collectionIds == null || __instance.collectionIds.Length == 0)
                return;

            ref MinerComponent miner = ref factory.factorySystem.minerPool[__instance.minerId];
            if (miner.id != __instance.minerId || miner.productCount <= 0 || miner.productId <= 0)
                return;

            lock (__instance.storage)
            {
                ref StationStore slot = ref __instance.storage[0];
                if (slot.count > 0 && slot.itemId != miner.productId)
                    return;

                bool itemChanged = slot.itemId != miner.productId || __instance.collectionIds[0] != miner.productId;
                if (!itemChanged)
                    return;

                slot.localOrder = 0;
                slot.remoteOrder = 0;
                slot.itemId = miner.productId;
                __instance.collectionIds[0] = miner.productId;
            }

            __instance.UpdateNeeds();
        }
    }

    internal static class VeinCollectorDispatchPatch
    {
        internal static void Prefix(StationComponent __instance, out int __state)
        {
            __state = -1;
            if (__instance == null || !__instance.isVeinCollector)
                return;

            __state = __instance.deliveryDrones;
            __instance.deliveryDrones = 0;
        }

        internal static Exception Finalizer(StationComponent __instance, int __state, Exception __exception)
        {
            if (__instance != null && __state >= 0)
                __instance.deliveryDrones = __state;

            return __exception;
        }
    }
}