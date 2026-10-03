using System;
using System.IO;

namespace KKDailyOutfits
{
    internal static class WardrobePaths
    {
        internal static bool IsDisabled(string wardrobe)
        {
            return string.IsNullOrEmpty(wardrobe) || wardrobe.Trim().Length == 0;
        }

        internal static string ResolveSelected(string gameRoot, string root, string wardrobe)
        {
            return IsDisabled(wardrobe) ? null : Resolve(gameRoot, root, wardrobe);
        }

        internal static string EnsureDefaultCloset(string gameRoot)
        {
            string path = Path.Combine(gameRoot, "UserData/DailyOutfits/DefaultCloset");
            Directory.CreateDirectory(path);
            return path;
        }
        internal static string[] Subfolders(string root)
        {
            var result = new System.Collections.Generic.List<string>();
            if (!Directory.Exists(root)) return result.ToArray();
            // Do not follow junctions/symlinks: they can escape the root or form cycles.
            var pending = new System.Collections.Generic.Stack<string>();
            pending.Push(root);
            string prefix = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            while (pending.Count > 0)
                foreach (var path in Directory.GetDirectories(pending.Pop()))
                {
                    if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0) continue;
                    result.Add(Path.GetFullPath(path).Substring(prefix.Length));
                    pending.Push(path);
                }
            result.Sort(StringComparer.OrdinalIgnoreCase);
            return result.ToArray();
        }
        internal static string Resolve(string gameRoot, string root, string wardrobe)
        {
            if (string.IsNullOrEmpty(root)) throw new ArgumentException("OutfitFolder is empty.");
            var basePath = Path.GetFullPath(Path.IsPathRooted(root) ? root : Path.Combine(gameRoot, root));
            if (string.IsNullOrEmpty(wardrobe) || wardrobe.Trim().Length == 0) return basePath;
            return Path.GetFullPath(Path.IsPathRooted(wardrobe) ? wardrobe : Path.Combine(basePath, wardrobe));
        }

        internal static string[] Cards(string folder)
        {
            // Never include sibling/child wardrobes in a character's selection pool.
            var paths = Directory.GetFiles(folder, "*.png", SearchOption.TopDirectoryOnly);
            Array.Sort(paths, StringComparer.OrdinalIgnoreCase);
            return paths;
        }
    }
}
