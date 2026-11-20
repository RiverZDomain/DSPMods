using BepInEx;
using HarmonyLib;

namespace AdvancedMinerLinearPower
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInProcess("DSPGAME.exe")]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.francisco.dsp.advanceminerlinearpower";
        public const string PluginName = "Advanced Miner Linear Power";
        public const string PluginVersion = "0.1.0";

        internal static BepInEx.Logging.ManualLogSource ModLogger { get; private set; }
        private Harmony _harmony;

        private void Awake()
        {
            ModLogger = Logger;
            _harmony = new Harmony(PluginGuid);
            _harmony.PatchAll(typeof(GameLifecyclePatch));
            Logger.LogInfo(PluginName + " loaded.");
        }

        private void OnDestroy()
        {
            GameLifecyclePatch.Unsubscribe();
            _harmony?.UnpatchSelf();
        }
    }
}