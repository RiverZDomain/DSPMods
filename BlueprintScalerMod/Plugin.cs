using BepInEx;
using BepInEx.Configuration;

namespace BlueprintScalerMod
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.francisco.dsp.blueprintscalermod";
        public const string PluginName = "Blueprint Scaler Mod";
        public const string PluginVersion = "0.1.0";

        public static ConfigEntry<int> SourceScale { get; private set; }

        private void Awake()
        {
            SourceScale = Config.Bind("Blueprint Scaling", "SourceScale", 200, "Blueprint source planet scale.");
            gameObject.AddComponent<BlueprintScalePanel>();
            Logger.LogInfo(PluginName + " loaded.");
        }
    }
}