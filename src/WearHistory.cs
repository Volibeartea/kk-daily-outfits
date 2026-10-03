using System;
using System.Collections.Generic;
using System.IO;

namespace KKDailyOutfits
{
    // Saved with the game, not in the global configuration or source coordinate cards.
    internal sealed class WearHistory
    {
        private long day;
        private int previousWeek = -1;
        private readonly Dictionary<string, long> lastWorn = new Dictionary<string, long>(StringComparer.Ordinal);
        private readonly Dictionary<string, string> current = new Dictionary<string, string>(StringComparer.Ordinal);

        internal void SetWeek(int week) { previousWeek = week; }

        internal void Forget(string character)
        {
            current.Remove(character);
            var remove = new List<string>();
            foreach (var key in lastWorn.Keys)
                if (key.StartsWith(character + "|", StringComparison.Ordinal)) remove.Add(key);
            foreach (var key in remove) lastWorn.Remove(key);
        }

        internal string Current(string character)
        {
            string card;
            return current.TryGetValue(character, out card) ? card : null;
        }

        internal int DaysUntilAvailable(string character, string card, int cooldown)
        {
            long worn;
            if (cooldown <= 0 || !lastWorn.TryGetValue(Key(character, card), out worn)) return 0;
            return (int)Math.Max(0L, cooldown - (day - worn) + 1);
        }

        internal void Advance(int week)
        {
            // KK's Cycle.Change(Week) treats the same weekday as seven days later.
            int elapsed = previousWeek < 0 ? 1 : (week - previousWeek + 7) % 7;
            if (elapsed == 0) elapsed = 7;
            day += elapsed;
            previousWeek = week;
            // If changing was disabled, failed, or skipped, the last outfit stayed in use.
            foreach (var pair in current) lastWorn[Key(pair.Key, pair.Value)] = day - 1;
        }

        internal int Choose(string character, string[] candidates, int cooldown, Random random)
        {
            if (candidates.Length == 0) return -1;
            var eligible = new List<int>();
            var oldest = new List<int>();
            long oldestDay = long.MaxValue;
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < candidates.Length; i++)
            {
                // Identical copies of a card must not bypass cooldown or gain extra weight.
                if (!seen.Add(candidates[i])) continue;
                long worn;
                bool known = lastWorn.TryGetValue(Key(character, candidates[i]), out worn);
                if (!known || cooldown <= 0 || day - worn > cooldown) eligible.Add(i);
                long rank = known ? worn : long.MinValue;
                if (rank < oldestDay) { oldestDay = rank; oldest.Clear(); }
                if (rank == oldestDay) oldest.Add(i);
            }
            var pool = eligible.Count > 0 ? eligible : oldest;
            return pool[random.Next(pool.Count)];
        }

        internal void Record(string character, string card)
        {
            lastWorn[Key(character, card)] = day;
            current[character] = card;
        }

        private static string Key(string character, string card) { return character + "|" + card; }

        internal string Save()
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(1);
                writer.Write(day);
                writer.Write(previousWeek);
                writer.Write(lastWorn.Count);
                foreach (var pair in lastWorn) { writer.Write(pair.Key); writer.Write(pair.Value); }
                writer.Write(current.Count);
                foreach (var pair in current) { writer.Write(pair.Key); writer.Write(pair.Value); }
                writer.Flush();
                return Convert.ToBase64String(stream.ToArray());
            }
        }

        internal static WearHistory Load(string value)
        {
            var result = new WearHistory();
            if (string.IsNullOrEmpty(value)) return result;
            using (var stream = new MemoryStream(Convert.FromBase64String(value)))
            using (var reader = new BinaryReader(stream))
            {
                if (reader.ReadInt32() != 1) throw new IOException("Unsupported wear history version.");
                result.day = reader.ReadInt64();
                result.previousWeek = reader.ReadInt32();
                if (result.day < 0 || result.previousWeek < -1 || result.previousWeek > 6) throw new IOException("Invalid history date.");
                int count = ReadCount(reader);
                for (int i = 0; i < count; i++)
                {
                    string key = reader.ReadString();
                    long worn = reader.ReadInt64();
                    if (worn < 0 || worn > result.day) throw new IOException("Invalid wear date.");
                    result.lastWorn.Add(key, worn);
                }
                count = ReadCount(reader);
                for (int i = 0; i < count; i++) result.current.Add(reader.ReadString(), reader.ReadString());
                if (stream.Position != stream.Length) throw new IOException("Unexpected history data.");
            }
            return result;
        }

        private static int ReadCount(BinaryReader reader)
        {
            int count = reader.ReadInt32();
            if (count < 0 || count > 100000) throw new IOException("Invalid history entry count.");
            return count;
        }
    }
}
