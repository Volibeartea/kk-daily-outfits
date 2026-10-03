using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using ExtensibleSaveFormat;
using KKAPI.MainGame;

namespace KKDailyOutfits
{
    [BepInPlugin(Guid, "KK Daily Outfits", "0.4.0")]
    [BepInProcess("Koikatu.exe")]
    [BepInDependency("marco.kkapi")]
    [BepInDependency("com.bepis.bepinex.extendedsave")]
    [BepInDependency("com.bepis.bepinex.sideloader")]
    public sealed class DailyOutfitsPlugin : BaseUnityPlugin
    {
        public const string Guid = "local.kk.dailyoutfits";
        internal static ManualLogSource Log;
        internal static ConfigEntry<bool> Enabled;
        internal static ConfigEntry<string> Folder;
        internal static ConfigEntry<bool> IncludeSwim;
        internal static ConfigEntry<int> NoRepeatDays;
        internal static ConfigEntry<bool> ShowStoryCharacters;
        internal static ConfigFile Settings;

        private void Awake()
        {
            Log = Logger;
            Settings = Config;
            try { WardrobePaths.EnsureDefaultCloset(Paths.GameRootPath); }
            catch (Exception ex) { Log.LogWarning("Could not create DefaultCloset: " + ex.Message); }
            Enabled = Config.Bind("General", "Enabled", true, "Enable daily underwear changes in story mode.");
            Folder = Config.Bind("General", "OutfitFolder", "UserData/DailyOutfits",
                new ConfigDescription("Root folder of coordinate PNG cards. Relative paths are relative to the game folder. Each character can use a separate subfolder. Refresh folders here after adding or removing folders. Only direct PNG files in the selected folder are used.",
                    null, new ConfigurationManagerAttributes { CustomDrawer = WardrobeUi.DrawRoot }));
            IncludeSwim = Config.Bind("General", "IncludeSwimCoordinate", false,
                "Also change underwear in the swimming coordinate. Default: keep swimwear unchanged.");
            NoRepeatDays = Config.Bind("General", "NoRepeatDays", 1,
                new ConfigDescription("Number of in-game days before an outfit can repeat. 1 prevents yesterday's outfit; 0 disables the limit. If all outfits are unavailable, choose the least recently worn. History is stored when you save the game.",
                    new AcceptableValueRange<int>(0, 365)));
            ShowStoryCharacters = Config.Bind("General", "ShowStoryCharacters", false,
                "Show fixed story characters in Character wardrobes. This only filters the settings list; existing daily outfit behavior is unchanged.");
        }

        internal static void RefreshSettingsWindow()
        {
            BepInEx.PluginInfo manager;
            if (!BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue("com.bepis.bepinex.configurationmanager", out manager)) return;
            try
            {
                var method = manager.Instance.GetType().GetMethod("BuildSettingList");
                if (method != null) method.Invoke(manager.Instance, null);
            }
            catch (Exception ex) { Log.LogDebug("Settings refresh deferred until reopening F1: " + ex.Message); }
        }

        private void Start()
        {
            GameAPI.RegisterExtraBehaviour<DailyOutfitsController>(Guid);
            Log.LogInfo("Ready. Underwear changes run on story day changes only.");
        }

        internal static bool HasUnsupportedData(Dictionary<string, PluginData> data)
        {
            if (data == null) return false;
            // Conservative initial support: do not partially transfer or erase these plugins' data.
            return data.Any(entry => entry.Value != null && entry.Value.data != null &&
                entry.Value.data.Count > 0 &&
                (entry.Key.IndexOf("material", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 entry.Key.IndexOf("overlay", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 entry.Key.IndexOf("clothes", StringComparison.OrdinalIgnoreCase) >= 0));
        }
    }

    public sealed class DailyOutfitsController : GameCustomFunctionController
    {
        internal static DailyOutfitsController Active;
        private readonly Dictionary<string, WardrobeSnapshot> snapshots = new Dictionary<string, WardrobeSnapshot>(StringComparer.OrdinalIgnoreCase);
        private readonly Random random = new Random();
        private readonly WardrobeRegistry wardrobes = new WardrobeRegistry();
        private WearHistory history = new WearHistory();
        private SaveData loadedSave;
        private float nextRosterCheck;

        private void Update()
        {
            if (UnityEngine.Time.unscaledTime < nextRosterCheck) return;
            nextRosterCheck = UnityEngine.Time.unscaledTime + 1f;
            if (KKAPI.Maker.MakerAPI.InsideMaker) return;
            var game = Manager.Game.Instance;
            if (game == null || loadedSave == null || !ReferenceEquals(loadedSave, game.saveData)) return;
            try { RegisterWardrobes(); }
            catch (Exception ex) { DailyOutfitsPlugin.Log.LogDebug("Roster refresh deferred: " + ex.Message); }
        }

        protected override void OnGameLoad(GameSaveLoadEventArgs args)
        {
            snapshots.Clear();
            wardrobes.Reset();
            try
            {
                var saved = GetExtendedData();
                object value;
                history = WearHistory.Load(saved != null && saved.data.TryGetValue("wearHistory", out value) ? value as string : null);
            }
            catch (Exception ex)
            {
                history = new WearHistory();
                DailyOutfitsPlugin.Log.LogWarning("Could not restore wear history; starting fresh: " + ex.Message);
            }
            if (Manager.Game.Instance != null && Manager.Game.Instance.saveData != null)
                history.SetWeek(Manager.Game.Instance.saveData.week);
            loadedSave = Manager.Game.Instance == null ? null : Manager.Game.Instance.saveData;
            RegisterWardrobes();
        }

        protected override void OnNewGame()
        {
            snapshots.Clear();
            wardrobes.Reset();
            history = new WearHistory();
            if (Manager.Game.Instance != null && Manager.Game.Instance.saveData != null)
                history.SetWeek(Manager.Game.Instance.saveData.week);
            loadedSave = Manager.Game.Instance == null ? null : Manager.Game.Instance.saveData;
            RegisterWardrobes();
        }

        protected override void OnPeriodChange(ActionGame.Cycle.Type period) { RegisterWardrobes(); }
        protected override void OnGameSave(GameSaveLoadEventArgs args)
        {
            RegisterWardrobes();
            var saved = GetExtendedData() ?? new PluginData();
            saved.version = 1;
            saved.data["wearHistory"] = history.Save();
            SetExtendedData(saved);
        }

        private void RegisterWardrobes()
        {
            Active = this;
            var game = Manager.Game.Instance;
            if (game == null || game.HeroineList == null) return;
            wardrobes.Synchronize(game.HeroineList, history);
        }

        protected override void OnDayChange(ActionGame.Cycle.Week day)
        {
            snapshots.Clear();
            history.Advance((int)day);
            if (!DailyOutfitsPlugin.Enabled.Value || KKAPI.Maker.MakerAPI.InsideMaker || GameAPI.InsideHScene) return;
            try { ChangeAll(day); }
            catch (Exception ex) { DailyOutfitsPlugin.Log.LogError("Daily change failed: " + ex); }
        }

        private void ChangeAll(ActionGame.Cycle.Week day)
        {
            var game = Manager.Game.Instance;
            if (game == null || game.saveData == null || game.HeroineList == null) return;
            var pools = new Dictionary<string, List<OutfitCard>>(StringComparer.OrdinalIgnoreCase);
            int changed = 0, skipped = 0;
            foreach (var heroine in game.HeroineList)
            {
                if (heroine == null) continue;
                try
                {
                    var folder = WardrobePaths.ResolveSelected(Paths.GameRootPath, DailyOutfitsPlugin.Folder.Value, wardrobes.GetFolder(heroine));
                    if (folder == null) { skipped++; continue; }
                    List<OutfitCard> cards;
                    if (!pools.TryGetValue(folder, out cards))
                    {
                        cards = LoadCards(folder);
                        pools.Add(folder, cards);
                    }
                    var identity = wardrobes.GetIdentity(heroine);
                    int selected = history.Choose(identity, cards.Select(x => x.Id).ToArray(), DailyOutfitsPlugin.NoRepeatDays.Value, random);
                    if (selected >= 0 && ChangeHeroine(heroine, cards[selected].Coordinate))
                    {
                        history.Record(identity, cards[selected].Id);
                        changed++;
                        DailyOutfitsPlugin.Log.LogInfo("Changed " + heroine.charFile.parameter.fullname + " to " + cards[selected].FileName);
                    }
                    else skipped++;
                }
                catch (Exception ex)
                {
                    skipped++;
                    DailyOutfitsPlugin.Log.LogWarning("Skipped character at class/seat " + heroine.schoolClass + "/" + heroine.schoolClassIndex + ": " + ex.Message);
                }
            }
            DailyOutfitsPlugin.Log.LogInfo("Day " + day + ": " + changed + " characters changed, " + skipped + " skipped.");
        }

        private sealed class OutfitCard
        {
            internal string Id;
            internal string FileName;
            internal ChaFileCoordinate Coordinate;
        }

        private sealed class WardrobeSnapshot
        {
            internal List<OutfitCard> Cards;
            internal readonly List<string> Errors = new List<string>();
        }

        internal string DescribeWardrobe(string identity, string folder, bool refresh)
        {
            if (WardrobePaths.IsDisabled(folder)) return "Disabled - select a wardrobe to enable daily outfit changes.";
            var path = WardrobePaths.Resolve(Paths.GameRootPath, DailyOutfitsPlugin.Folder.Value, folder);
            WardrobeSnapshot snapshot;
            if (refresh || !snapshots.TryGetValue(path, out snapshot))
            {
                snapshot = new WardrobeSnapshot();
                if (!Directory.Exists(path)) snapshot.Errors.Add("Folder does not exist.");
                else snapshot.Cards = LoadCards(path, snapshot.Errors);
                if (snapshot.Cards == null) snapshot.Cards = new List<OutfitCard>();
                snapshots[path] = snapshot;
            }
            var result = new StringBuilder();
            string current = history.Current(identity);
            var worn = snapshot.Cards.FirstOrDefault(x => x.Id == current);
            result.AppendLine("Today (last applied): " + (current == null ? "No recorded outfit yet" : worn == null ? "Not found in this wardrobe" : worn.FileName));
            result.AppendLine("Cooldown: " + DailyOutfitsPlugin.NoRepeatDays.Value + " day(s)");
            foreach (var card in snapshot.Cards)
            {
                int remaining = history.DaysUntilAvailable(identity, card.Id, DailyOutfitsPlugin.NoRepeatDays.Value);
                string state = card.Id == current ? "Wearing today" : remaining == 0 ? "Available" : "Washing - " + remaining + " day(s) until eligible";
                result.AppendLine(card.FileName + " - " + state);
            }
            if (snapshot.Cards.Count == 0) result.AppendLine("No usable coordinate cards.");
            if (snapshot.Cards.Count > 0 && snapshot.Cards.All(x => history.DaysUntilAvailable(identity, x.Id, DailyOutfitsPlugin.NoRepeatDays.Value) > 0))
                result.AppendLine("All outfits are on cooldown. If still unavailable at the next change, the least recently worn outfit will be used.");
            foreach (var error in snapshot.Errors) result.AppendLine("Skipped: " + error);
            return result.ToString();
        }

        private static List<OutfitCard> LoadCards(string folder, List<string> errors = null)
        {
            var cards = new List<OutfitCard>();
            if (!Directory.Exists(folder))
            {
                DailyOutfitsPlugin.Log.LogWarning("Outfit folder does not exist: " + folder);
                return cards;
            }
            foreach (var path in WardrobePaths.Cards(folder))
            {
                try
                {
                    // The normal coordinate loader raises ExtendedSave's event and Sideloader resolves IDs.
                    // This detached coordinate is never loaded onto a character.
                    var card = new ChaFileCoordinate();
                    if (!card.LoadFile(path)) throw new IOException("Not a supported coordinate card.");
                    if (DailyOutfitsPlugin.HasUnsupportedData(ExtendedSave.GetAllExtendedData(card)))
                        throw new IOException("Material/overlay/clothes extension data is not supported in v0.1.");
                    ValidateParts(card);
                    string id;
                    using (var stream = File.OpenRead(path))
                    using (var hash = SHA256.Create()) id = Convert.ToBase64String(hash.ComputeHash(stream));
                    cards.Add(new OutfitCard { Id = id, FileName = Path.GetFileName(path), Coordinate = card });
                }
                catch (Exception ex)
                {
                    if (errors != null) errors.Add(Path.GetFileName(path) + ": " + ex.Message);
                    DailyOutfitsPlugin.Log.LogWarning("Skipped card " + Path.GetFileName(path) + ": " + ex.Message);
                }
            }
            if (cards.Count == 0)
            {
                DailyOutfitsPlugin.Log.LogWarning("No usable coordinate cards in " + folder);
            }
            return cards;
        }

        private static void ValidateParts(ChaFileCoordinate card)
        {
            if (card.clothes == null || card.clothes.parts == null || card.clothes.parts.Length <= UnderwearCopy.Shorts)
                throw new IOException("Missing clothing parts.");
            var list = Manager.Character.Instance.chaListCtrl;
            var bra = card.clothes.parts[UnderwearCopy.Bra];
            var shorts = card.clothes.parts[UnderwearCopy.Shorts];
            if (bra == null || shorts == null ||
                list.GetListInfo(ChaListDefine.CategoryNo.co_bra, bra.id) == null ||
                list.GetListInfo(ChaListDefine.CategoryNo.co_shorts, shorts.id) == null)
                throw new IOException("Underwear item is missing (possibly a missing zipmod).");
            // Deep-copy validation before any target is changed.
            UnderwearCopy.Apply(card.clothes, new ChaFileClothes());
        }

        private static bool ChangeHeroine(SaveData.Heroine heroine, ChaFileCoordinate source)
        {
            var files = heroine.GetRelatedChaFiles().Where(x => x != null).Distinct().ToArray();
            var targets = new HashSet<ChaFileCoordinate>();
            foreach (var file in files)
            {
                if (DailyOutfitsPlugin.HasUnsupportedData(ExtendedSave.GetAllExtendedData(file)))
                    throw new IOException("Character has material/overlay/clothes extension data; v0.1 leaves it unchanged.");
                for (int i = 0; i < file.coordinate.Length; i++)
                {
                    if (!DailyOutfitsPlugin.IncludeSwim.Value && i == (int)ChaFileDefine.CoordinateType.Swim) continue;
                    if (file.coordinate[i] != null) targets.Add(file.coordinate[i]);
                }
            }
            var control = heroine.chaCtrl;
            bool updateVisible = control != null && control.nowCoordinate != null &&
                (DailyOutfitsPlugin.IncludeSwim.Value || control.fileStatus.coordinateType != (int)ChaFileDefine.CoordinateType.Swim);
            if (updateVisible) targets.Add(control.nowCoordinate);
            // Check every target before modifying any of this character's coordinates.
            foreach (var target in targets)
                if (target.clothes == null || target.clothes.parts == null || target.clothes.parts.Length <= UnderwearCopy.Shorts ||
                    DailyOutfitsPlugin.HasUnsupportedData(ExtendedSave.GetAllExtendedData(target)))
                    throw new IOException("Unsupported target coordinate; character left unchanged.");
            foreach (var target in targets) UnderwearCopy.Apply(source.clothes, target.clothes);
            if (updateVisible)
            {
                control.ChangeClothesBra(control.nowCoordinate.clothes.parts[UnderwearCopy.Bra].id, true);
                control.ChangeClothesShorts(control.nowCoordinate.clothes.parts[UnderwearCopy.Shorts].id, true);
            }
            return targets.Count > 0;
        }
    }
}

