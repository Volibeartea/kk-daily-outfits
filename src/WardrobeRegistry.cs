using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using ExtensibleSaveFormat;
using KKAPI.MainGame;

namespace KKDailyOutfits
{
    internal sealed class WardrobeRegistry
    {
        private readonly Dictionary<string, ConfigEntry<string>> entries = new Dictionary<string, ConfigEntry<string>>();
        private readonly Dictionary<SaveData.Heroine, string> identities = new Dictionary<SaveData.Heroine, string>();
        private readonly Dictionary<string, ConfigurationManagerAttributes> display = new Dictionary<string, ConfigurationManagerAttributes>();
        private bool dirty;

        internal void Reset()
        {
            identities.Clear();
            foreach (var tag in display.Values) tag.Browsable = false;
            dirty = true;
        }

        internal void Synchronize(ICollection<SaveData.Heroine> roster, WearHistory history)
        {
            // Only delete confirmed departures from this loaded save. Other saves' entries
            // stay hidden rather than being mistaken for transferred-out characters.
            foreach (var pair in identities.ToArray())
            {
                if (roster.Contains(pair.Key)) continue;
                identities.Remove(pair.Key);
                ConfigEntry<string> old;
                if (entries.TryGetValue(pair.Value, out old))
                {
                    DailyOutfitsPlugin.Settings.Remove(old.Definition);
                    entries.Remove(pair.Value);
                    display.Remove(pair.Value);
                }
                history.Forget(pair.Value);
                dirty = true;
            }
            foreach (var heroine in roster)
            {
                if (heroine == null) continue;
                try { GetFolder(heroine); }
                catch (Exception ex) { DailyOutfitsPlugin.Log.LogDebug("Wardrobe registration deferred: " + ex.Message); }
            }
            int rank = 0;
            foreach (var heroine in roster.Where(x => x != null).OrderBy(x => x.fixCharaID != 0)
                .ThenBy(x => x.schoolClass).ThenBy(x => x.schoolClassIndex))
            {
                string id;
                ConfigurationManagerAttributes tag;
                if (!identities.TryGetValue(heroine, out id) || !display.TryGetValue(id, out tag)) continue;
                // Configuration Manager sorts Order descending, so earlier seats get higher values.
                int order = -rank++;
                if (tag.Order != order) { tag.Order = order; dirty = true; }
            }
            if (dirty)
            {
                DailyOutfitsPlugin.Settings.Save();
                dirty = false;
                DailyOutfitsPlugin.RefreshSettingsWindow();
            }
        }

        internal string GetIdentity(SaveData.Heroine heroine) { return identities[heroine]; }

        internal string GetFolder(SaveData.Heroine heroine)
        {
            string id;
            var files = heroine.GetRelatedChaFiles().Where(x => x != null).Distinct().ToArray();
            if (files.Length == 0) throw new InvalidOperationException("Character data is not ready.");
            if (!identities.TryGetValue(heroine, out id))
            {
                var data = ExtendedSave.GetExtendedDataById(files[0], DailyOutfitsPlugin.Guid);
                object stored;
                id = data != null && data.data != null && data.data.TryGetValue("wardrobeId", out stored) ? stored as string : null;
                if (!IsIdentity(id) || identities.ContainsValue(id))
                    id = System.Guid.NewGuid().ToString("N");
                identities.Add(heroine, id);
            }
            // Synchronize both the persistent character and its currently spawned copy.
            foreach (var file in files)
            {
                var data = ExtendedSave.GetExtendedDataById(file, DailyOutfitsPlugin.Guid) ?? new PluginData();
                object existing;
                if (!data.data.TryGetValue("wardrobeId", out existing) || !Equals(existing, id))
                {
                    data.version = 1;
                    data.data["wardrobeId"] = id;
                    ExtendedSave.SetExtendedDataById(file, DailyOutfitsPlugin.Guid, data);
                }
            }
            ConfigEntry<string> entry;
            if (!entries.TryGetValue(id, out entry))
            {
                var name = files[0].parameter.fullname;
                string identity = id;
                var ui = new WardrobeUi((folder, refresh) =>
                {
                    if (!identities.ContainsValue(identity)) return "This character is not in the current save.";
                    var controller = DailyOutfitsController.Active;
                    return controller == null ? "Load a story save first." : controller.DescribeWardrobe(identity, folder, refresh);
                });
                var tag = new ConfigurationManagerAttributes { DispName = name + " [" + id.Substring(0, 8) + "]", Category = "Character wardrobes", CustomDrawer = ui.Draw };
                entry = DailyOutfitsPlugin.Settings.Bind("Character wardrobes", "Wardrobe_" + id, "",
                    new ConfigDescription("Character: " + name + ". Disabled by default. Select a wardrobe or enter a custom path to enable daily changes. Clear the path to disable. Only direct PNG files are used. Save the game after first setup to retain the character identity.",
                        null, tag));
                entries.Add(id, entry);
                display.Add(id, tag);
                dirty = true;
            }
            var attributes = display[id];
            bool visible = heroine.fixCharaID == 0 || DailyOutfitsPlugin.ShowStoryCharacters.Value;
            string classroom = Manager.Game.ClassRoomNameIndexPair.FirstOrDefault(x => x.Value == heroine.schoolClass).Key ?? "?";
            string label = files[0].parameter.fullname + (heroine.fixCharaID != 0 ? " (Story)" : " (" + classroom + ", Seat " + heroine.schoolClassIndex + ")");
            if (attributes.Browsable != visible || attributes.DispName != label) dirty = true;
            attributes.Browsable = visible;
            attributes.DispName = label;
            return entry.Value;
        }

        private static bool IsIdentity(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;
            try { return new System.Guid(id).ToString("N") == id; }
            catch (FormatException) { return false; }
        }

    }
}
