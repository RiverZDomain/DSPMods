using UnityEngine;

namespace BlueprintScalerMod
{
    internal sealed class BlueprintScalePanel : MonoBehaviour
    {
        private const int WindowId = 61428;
        private const int MinimumScale = 5;
        private const int MaximumScale = 510;
        private const int ScaleStep = 10;

        private Rect windowRect;
        private bool isPlacementActive;

        private void Awake()
        {
            windowRect = new Rect(24f, Screen.height - 150f, 290f, 112f);
        }

        private void Update()
        {
            Player player = GameMain.mainPlayer;
            isPlacementActive = player != null
                && player.controller != null
                && player.controller.actionBuild != null
                && player.controller.actionBuild.blueprintMode == EBlueprintMode.Paste
                && player.controller.actionBuild.blueprintPasteTool != null
                && player.controller.actionBuild.blueprintPasteTool.blueprint != null;
        }

        private void OnGUI()
        {
            if (!isPlacementActive)
            {
                return;
            }

            windowRect = GUI.Window(WindowId, windowRect, DrawWindow, "Blueprint Scaler");
            windowRect.x = Mathf.Clamp(windowRect.x, 0f, Mathf.Max(0f, Screen.width - windowRect.width));
            windowRect.y = Mathf.Clamp(windowRect.y, 0f, Mathf.Max(0f, Screen.height - windowRect.height));
        }

        private void DrawWindow(int windowId)
        {
            GUILayout.Label("Choose Scale (Default 200)");
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("-10", GUILayout.Width(54f)))
            {
                AdjustScale(-ScaleStep);
            }

            GUILayout.Label("Selected scale: " + Plugin.SourceScale.Value, GUILayout.ExpandWidth(true));

            if (GUILayout.Button("+10", GUILayout.Width(54f)))
            {
                AdjustScale(ScaleStep);
            }

            GUILayout.EndHorizontal();
            GUI.DragWindow(new Rect(0f, 0f, 10000f, 22f));
        }

        private static void AdjustScale(int delta)
        {
            Plugin.SourceScale.Value = Mathf.Clamp(Plugin.SourceScale.Value + delta, MinimumScale, MaximumScale);
        }
    }
}