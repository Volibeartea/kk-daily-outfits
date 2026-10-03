using System;
using System.IO;
using KKDailyOutfits;
using UnityEngine;

public static class CopyTests
{
    private static int checks;
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
        checks++;
    }

    public static string Run(string scratch)
    {
        checks = 0;
        var source = new ChaFileClothes();
        var target = new ChaFileClothes();
        source.parts[2].id = 120;
        source.parts[3].id = 121;
        source.parts[2].emblemeId = 31;
        source.parts[2].emblemeId2 = 32;
        source.parts[2].colorInfo[0].baseColor = new Color(.1f, .2f, .3f, .4f);
        source.parts[2].colorInfo[0].pattern = 55;
        source.parts[2].colorInfo[0].tiling = new Vector2(2, 3);
        source.parts[2].colorInfo[0].patternColor = Color.red;
        source.parts[2].hideOpt = new[] { true, false };
        var originalParts = (ChaFileClothes.PartsInfo[])target.parts.Clone();
        var mask = target.hideBraOpt;
        var subParts = target.subPartsId;
        target.hideBraOpt[0] = true;
        UnderwearCopy.Apply(source, target);
        Check(target.parts[2].id == 120 && target.parts[3].id == 121, "Both underwear parts must come from the source.");
        for (int i = 0; i < originalParts.Length; i++)
            if (i != 2 && i != 3) Check(ReferenceEquals(originalParts[i], target.parts[i]), "Other clothing slot changed: " + i);
        Check(ReferenceEquals(mask, target.hideBraOpt) && target.hideBraOpt[0], "Outer-garment masking changed.");
        Check(ReferenceEquals(subParts, target.subPartsId), "Outer-garment subparts changed.");
        Check(target.parts[2].emblemeId == 31 && target.parts[2].emblemeId2 == 32, "Emblems lost.");
        Check(target.parts[2].colorInfo[0].baseColor.a == .4f && target.parts[2].colorInfo[0].pattern == 55 && target.parts[2].colorInfo[0].tiling.x == 2, "Color/pattern lost.");
        source.parts[2].id = 999;
        source.parts[2].colorInfo[0].pattern = 999;
        source.parts[2].hideOpt[0] = false;
        Check(target.parts[2].id == 120 && target.parts[2].colorInfo[0].pattern == 55 && target.parts[2].hideOpt[0], "Source and target share mutable data.");
        var second = new ChaFileClothes();
        UnderwearCopy.Apply(target, second);
        second.parts[3].id = 456;
        Check(target.parts[3].id == 121, "Characters share mutable underwear data.");
        var before = target.parts[2];
        source.parts[3] = null;
        bool failed = false;
        try { UnderwearCopy.Apply(source, target); } catch (ArgumentException) { failed = true; }
        Check(failed && ReferenceEquals(before, target.parts[2]), "Malformed source caused partial replacement.");

        Directory.CreateDirectory(scratch);
        var root = Path.Combine(scratch, "DailyOutfits");
        var alice = Path.Combine(root, "Alice");
        var bob = Path.Combine(root, "Bob");
        Directory.CreateDirectory(alice);
        Directory.CreateDirectory(bob);
        File.WriteAllText(Path.Combine(root, "shared.png"), "test fixture, not a card");
        File.WriteAllText(Path.Combine(alice, "alice.png"), "fixture");
        File.WriteAllText(Path.Combine(bob, "bob.png"), "fixture");
        Check(WardrobePaths.Resolve(scratch, "DailyOutfits", "Alice") == alice, "Relative wardrobe resolution failed.");
        Check(WardrobePaths.Resolve(scratch, root, bob) == bob, "Absolute wardrobe resolution failed.");
        Check(WardrobePaths.Resolve(scratch, root, "") == root, "Shared wardrobe resolution failed.");
        Check(WardrobePaths.Cards(root).Length == 1 && Path.GetFileName(WardrobePaths.Cards(root)[0]) == "shared.png", "Shared pool includes private wardrobes.");
        Check(WardrobePaths.Cards(alice).Length == 1 && Path.GetFileName(WardrobePaths.Cards(alice)[0]) == "alice.png", "Character pool includes another wardrobe.");
        var nested = Path.Combine(alice, "Special");
        Directory.CreateDirectory(nested);
        var folders = WardrobePaths.Subfolders(root);
        Check(folders.Length == 3, "Folder picker must include nested wardrobes.");
        Check(Array.IndexOf(folders, Path.Combine("Alice", "Special")) >= 0, "Folder picker must use relative paths.");
        Check(WardrobePaths.Subfolders(Path.Combine(root, "Missing")).Length == 0, "Missing root must have no folder choices.");
        Check(WardrobePaths.ResolveSelected(scratch, root, "") == null, "Empty/legacy shared wardrobe must be disabled.");
        Check(WardrobePaths.ResolveSelected(scratch, "", "  ") == null, "Disabled must bypass even an invalid root.");
        Check(WardrobePaths.ResolveSelected(scratch, root, "Alice") == alice, "Assigned wardrobes must remain enabled.");
        Check(WardrobePaths.ResolveSelected(scratch, root, bob) == bob, "Existing absolute paths must remain enabled.");
        string defaultCloset = WardrobePaths.EnsureDefaultCloset(scratch);
        Check(Directory.Exists(defaultCloset), "DefaultCloset must be created under the game root.");
        File.WriteAllText(Path.Combine(defaultCloset, "existing.png"), "keep");
        Check(WardrobePaths.EnsureDefaultCloset(scratch) == defaultCloset && File.ReadAllText(Path.Combine(defaultCloset, "existing.png")) == "keep", "Startup must preserve existing files.");
        Check(Array.IndexOf(WardrobePaths.Subfolders(Path.GetDirectoryName(defaultCloset)), "DefaultCloset") >= 0, "DefaultCloset must be selectable but not auto-assigned.");
        return checks + " checks passed using real game clothing types.";
    }
}
