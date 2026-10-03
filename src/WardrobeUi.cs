using System;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;

namespace KKDailyOutfits
{
    internal sealed class WardrobeUi
    {
        private readonly Func<string, bool, string> status;
        private bool expanded;
        private bool customPath;
        private bool showStatus;
        private Vector2 scroll;
        private Vector2 statusScroll;
        private string statusError;
        private static string cachedRoot;
        private static string[] folders = new string[0];
        private static string folderError;

        internal WardrobeUi(Func<string, bool, string> statusReader) { status = statusReader; }

        // One shared refresh control, next to OutfitFolder in General.
        internal static void DrawRoot(ConfigEntryBase setting)
        {
            var entry = (ConfigEntry<string>)setting;
            GUILayout.BeginVertical();
            string edited = GUILayout.TextField(entry.Value);
            if (edited != entry.Value) { entry.Value = edited; cachedRoot = null; }
            if (GUILayout.Button("Refresh folders")) RefreshFolders();
            if (folderError != null) GUILayout.Label(folderError);
            GUILayout.EndVertical();
        }

        internal void Draw(ConfigEntryBase setting)
        {
            var entry = (ConfigEntry<string>)setting;
            GUILayout.BeginVertical();
            if (GUILayout.Button((WardrobePaths.IsDisabled(entry.Value) ? "Disabled" : entry.Value) + (expanded ? " ▲" : " ▼")))
            {
                expanded = !expanded;
                if (expanded) { EnsureFolders(); showStatus = false; }
            }
            if (expanded)
            {
                EnsureFolders();
                if (folderError != null) GUILayout.Label(folderError);
                int count = WardrobePaths.IsDisabled(entry.Value) ? 0 : 1;
                foreach (var folder in folders)
                    if (!string.Equals(folder, entry.Value, StringComparison.OrdinalIgnoreCase)) count++;
                if (count > 0)
                {
                    // Explicit content-sized height prevents the empty stretched scroll area.
                    scroll = GUILayout.BeginScrollView(scroll, GUILayout.Height(Math.Min(180, count * 28 + 8)));
                    if (!WardrobePaths.IsDisabled(entry.Value) && GUILayout.Button("Disabled")) Select(entry, "");
                    foreach (var folder in folders)
                        if (!string.Equals(folder, entry.Value, StringComparison.OrdinalIgnoreCase) && GUILayout.Button(folder)) Select(entry, folder);
                    GUILayout.EndScrollView();
                }
                else if (folderError == null) GUILayout.Label("No other folders. Add folders under OutfitFolder.");
                if (GUILayout.Button(customPath ? "Hide custom path" : "Custom path...")) customPath = !customPath;
                if (customPath)
                {
                    string edited = GUILayout.TextField(entry.Value);
                    if (edited != entry.Value) entry.Value = edited;
                }
            }
            if (GUILayout.Button(showStatus ? "Hide wardrobe status ▲" : "Show wardrobe status ▼"))
            {
                showStatus = !showStatus;
                if (showStatus) { expanded = false; customPath = false; ReadStatus(entry.Value, false); }
            }
            if (showStatus)
            {
                string text = ReadStatus(entry.Value, false);
                float height = Mathf.Clamp(GUI.skin.label.CalcHeight(new GUIContent(text), 260) + 12, 40, 220);
                statusScroll = GUILayout.BeginScrollView(statusScroll, GUILayout.Height(height));
                GUILayout.Label(text);
                GUILayout.EndScrollView();
                if (GUILayout.Button("Refresh status")) ReadStatus(entry.Value, true);
            }
            GUILayout.EndVertical();
        }

        private void Select(ConfigEntry<string> entry, string folder)
        {
            entry.Value = folder;
            expanded = false;
            customPath = false;
            showStatus = false;
            statusError = null;
        }

        private string ReadStatus(string folder, bool refresh)
        {
            try
            {
                string text = status(folder, refresh);
                statusError = null;
                return text;
            }
            catch (Exception ex) { statusError = "Unable to read: " + ex.Message; return statusError; }
        }

        private static void EnsureFolders()
        {
            if (!string.Equals(cachedRoot, DailyOutfitsPlugin.Folder.Value, StringComparison.Ordinal)) RefreshFolders();
        }

        private static void RefreshFolders()
        {
            cachedRoot = DailyOutfitsPlugin.Folder.Value;
            try
            {
                string root = WardrobePaths.Resolve(Paths.GameRootPath, cachedRoot, "");
                folders = WardrobePaths.Subfolders(root);
                folderError = Directory.Exists(root) ? null : "Wardrobe root does not exist. Create it or update OutfitFolder.";
            }
            catch (Exception ex) { folders = new string[0]; folderError = "Scan failed: " + ex.Message; }
        }
    }
}

