using System;
using System.Collections.Generic;
using HarmonyLib;

namespace AdvancedMinerLinearPower
{
    [HarmonyPatch]
    internal static class GameLifecyclePatch
    {
        private static readonly Dictionary<PowerConsumerComponent[], HashSet<int>> CollectorEntities = new Dictionary<PowerConsumerComponent[], HashSet<int>>();
        private static bool _loggedFailure;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameMain), nameof(GameMain.Begin))]
        private static void OnGameBegin()
        {
            CollectorEntities.Clear();
            RegisterExistingCollectors();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameMain), nameof(GameMain.End))]
        private static void OnGameEnd()
        {
            Unsubscribe();
        }

        internal static void Unsubscribe()
        {
            CollectorEntities.Clear();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(FactorySystem), nameof(FactorySystem.NewMinerComponent))]
        private static void RegisterNewMiner(FactorySystem __instance, int entityId, PrefabDesc desc, int __result)
        {
            if (__result <= 0 || desc == null || !desc.isVeinCollector || __instance.factory?.powerSystem == null)
                return;

            var miner = __instance.minerPool[__result];
            RegisterCollector(__instance.factory.powerSystem.consumerPool, miner.entityId);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(MinerComponent), nameof(MinerComponent.SetPCState))]
        private static void SetMinerPowerState(MinerComponent __instance, PowerConsumerComponent[] pcPool)
        {
            int consumerId = __instance.pcId;
            if (pcPool == null || consumerId <= 0 || consumerId >= pcPool.Length)
                return;
            if (!CollectorEntities.TryGetValue(pcPool, out var entityIds) || !entityIds.Contains(__instance.entityId))
                return;

            double linearRatio = __instance.workstate > EWorkState.Idle
                ? (double)__instance.speedDamper * __instance.speed / 10000.0
                : 0.0;
            pcPool[consumerId].SetRequiredEnergy(linearRatio);
        }

        private static void RegisterExistingCollectors()
        {
            try
            {
                var data = GameMain.data;
                if (data == null)
                    return;

                for (int factoryIndex = 0; factoryIndex < data.factoryCount; factoryIndex++)
                {
                    var factory = data.factories[factoryIndex];
                    if (factory?.planet == null || factory.transport == null || factory.factorySystem == null || factory.powerSystem == null)
                        continue;

                    var stationPool = factory.transport.stationPool;
                    var minerPool = factory.factorySystem.minerPool;
                    var consumerPool = factory.powerSystem.consumerPool;

                    for (int stationIndex = 1; stationIndex < factory.transport.stationCursor; stationIndex++)
                    {
                        var station = stationPool[stationIndex];
                        if (station == null || station.id != stationIndex || !station.isVeinCollector)
                            continue;
                        if (station.minerId <= 0 || station.minerId >= minerPool.Length)
                            continue;

                        ref MinerComponent miner = ref minerPool[station.minerId];
                        if (miner.id != station.minerId || miner.pcId <= 0 || miner.pcId >= consumerPool.Length)
                            continue;
                        if (consumerPool[miner.pcId].id != miner.pcId)
                            continue;

                        RegisterCollector(consumerPool, miner.entityId);
                        double linearRatio = miner.workstate > EWorkState.Idle
                            ? (double)miner.speedDamper * miner.speed / 10000.0
                            : 0.0;
                        consumerPool[miner.pcId].SetRequiredEnergy(linearRatio);
                    }
                }
            }
            catch (Exception exception)
            {
                if (_loggedFailure)
                    return;

                _loggedFailure = true;
                Plugin.ModLogger?.LogError("Failed to update advanced miner power scaling: " + exception);
            }
        }

        private static void RegisterCollector(PowerConsumerComponent[] consumerPool, int entityId)
        {
            if (consumerPool == null || entityId <= 0)
                return;

            if (!CollectorEntities.TryGetValue(consumerPool, out var entityIds))
            {
                entityIds = new HashSet<int>();
                CollectorEntities.Add(consumerPool, entityIds);
            }

            entityIds.Add(entityId);
        }
    }
}