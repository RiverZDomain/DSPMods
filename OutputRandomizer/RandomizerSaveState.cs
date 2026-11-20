using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OutputRandomizer
{
    internal static class RandomizerSaveState
    {
        private const string MetadataSuffix = ".outputrandomizer";
        private const string Header = "OutputRandomizerV1";

        internal static bool Enabled { get; private set; } = true;
        internal static HashSet<int> SelectedItemIds { get; private set; } = new HashSet<int>();

        internal static void OnNewGame(GameDesc __0)
        {
            Enabled = NewGameMenuIntegration.PendingEnabled;
            SelectedItemIds = new HashSet<int>(NewGameMenuIntegration.PendingItemIds);
        }

        internal static void BeforeLoad(string __0)
        {
            Enabled = false;
            SelectedItemIds = ItemFilterPicker.GetCandidateItemIds();

            string path = GetMetadataPath(__0);
            if (path == null || !File.Exists(path))
                return;

            try
            {
                string[] lines = File.ReadAllLines(path);
                if (lines.Length < 2 || lines[0].Trim() != Header)
                    return;

                // Line 1: "1" or "0"
                Enabled = lines[1].Trim() == "1";

                // Line 2: comma-separated item IDs
                HashSet<int> availableIds = ItemFilterPicker.GetCandidateItemIds();
                var selected = new HashSet<int>();
                if (!string.IsNullOrWhiteSpace(lines[2]))
                {
                    foreach (string value in lines[2].Split(','))
                    {
                        if (int.TryParse(value, out int id) && availableIds.Contains(id))
                            selected.Add(id);
                    }
                }

                if (selected.Count > 0)
                    SelectedItemIds = selected;
            }
            catch (Exception ex)
            {
                Plugin.ModLogger?.LogWarning("[OR] Failed to load save state: " + ex.Message);
            }
        }

        internal static void AfterSave(bool __result, string __0)
        {
            if (!__result)
                return;

            string path = GetMetadataPath(__0);
            if (path == null)
                return;

            try
            {
                string ids = string.Join(",", SelectedItemIds.OrderBy(x => x));
                File.WriteAllText(path, Header + "\n" + (Enabled ? "1" : "0") + "\n" + ids);
            }
            catch (Exception ex)
            {
                Plugin.ModLogger?.LogWarning("[OR] Failed to write save state: " + ex.Message);
            }
        }

        private static string GetMetadataPath(string saveName)
        {
            if (string.IsNullOrEmpty(saveName))
                return null;
            try
            {
                return GameSave.SavePath(saveName) + MetadataSuffix;
            }
            catch
            {
                return null;
            }
        }
    }
}   