using System;
using KKDailyOutfits;

public static class HistoryTests
{
    private static int checks;
    private static void Check(bool value, string message)
    {
        if (!value) throw new Exception(message);
        checks++;
    }

    public static string Run()
    {
        checks = 0;
        var random = new Random(123);
        var history = new WearHistory();
        history.SetWeek(0);
        history.Record("Alice", "A");
        Check(history.Current("Alice") == "A", "Status must identify current outfit.");
        Check(history.Current("Unknown") == null, "Unknown character must not show another character's outfit.");
        Check(history.DaysUntilAvailable("Alice", "A", 1) == 2, "Today's outfit needs two day changes with a one-day exclusion.");
        Check(history.DaysUntilAvailable("Alice", "B", 1) == 0, "Unworn outfit must be available.");
        Check(history.DaysUntilAvailable("Alice", "A", 0) == 0, "Status must honor disabled cooldown.");
        history.Advance(1);
        Check(history.DaysUntilAvailable("Alice", "A", 1) == 1, "Yesterday's outfit needs one more day.");
        Check(history.Choose("Alice", new[] { "A", "B" }, 1, random) == 1, "Yesterday must be excluded.");
        Check(history.Choose("Alice", new[] { "A" }, 365, random) == 0, "A single outfit must remain usable.");
        Check(history.Choose("Alice", new string[0], 1, random) == -1, "An empty wardrobe must skip.");
        history.Record("Bob", "B");
        Check(history.Choose("Bob", new[] { "A", "B" }, 1, random) == 0, "Histories must be per-character.");
        history.Record("Alice", "B");
        // Round trip into a new instance to represent quitting and reloading the save.
        history = WearHistory.Load(history.Save());
        Check(history.Current("Alice") == "B", "Status must survive save/load.");
        history.Advance(2);
        Check(history.DaysUntilAvailable("Alice", "A", 1) == 0, "Status and eligibility must agree after cooldown.");
        Check(history.Choose("Alice", new[] { "A", "B", "C" }, 2, random) == 2, "Reload must retain both blocked days.");
        Check(history.Choose("Alice", new[] { "A", "B" }, 2, random) == 0, "Insufficient pool must choose least recently worn.");
        Check(history.Choose("Alice", new[] { "A", "B" }, 1, random) == 0, "Default permits day-before-yesterday.");
        history.Record("Alice", "C");
        history.Advance(3);
        Check(history.Choose("Alice", new[] { "A", "B", "C" }, 2, random) == 0, "A must become eligible after two blocked days.");
        history.Record("Alice", "A");
        history.Advance(4);
        Check(history.Choose("Alice", new[] { "A", "A", "B" }, 1, random) == 2, "Duplicate card copies must not bypass cooldown.");
        bool gotA = false, gotB = false;
        for (int i = 0; i < 80; i++)
        {
            int pick = history.Choose("Alice", new[] { "A", "B" }, 0, random);
            gotA |= pick == 0; gotB |= pick == 1;
        }
        Check(gotA && gotB, "Zero days must disable exclusions.");
        // Failed/disabled changes do not replace the current outfit; it was still worn yesterday.
        history.Advance(5);
        Check(history.Choose("Alice", new[] { "A", "B" }, 1, random) == 1, "Skipped changes must not make current outfit eligible.");
        var saved = history.Save();
        var copy = WearHistory.Load(saved);
        copy.Advance(5); // Same weekday is an actual seven-day jump in the game's Change(Week).
        Check(copy.Choose("Alice", new[] { "A", "B", "C" }, 5, random) != 0, "Current outfit remains worn across a skipped week.");
        Check(history.Save() == saved, "Loading another session must not share mutable history.");
        bool bad = false;
        try { WearHistory.Load("not base64!"); } catch (FormatException) { bad = true; }
        Check(bad, "Malformed history must be detected.");
        var clean = WearHistory.Load(null);
        Check(clean.Choose("Alice", new[] { "A" }, 1, random) == 0, "Legacy saves need an empty history.");
        var weekEnd = new WearHistory();
        weekEnd.SetWeek(6);
        weekEnd.Record("Alice", "A");
        weekEnd.Advance(0);
        Check(weekEnd.Choose("Alice", new[] { "A", "B" }, 1, random) == 1, "Week rollover is one day.");
        // A long run checks the rolling window rather than just individual examples.
        var rolling = new WearHistory();
        rolling.SetWeek(0);
        var recent = new System.Collections.Generic.Queue<int>();
        for (int d = 1; d <= 100; d++)
        {
            rolling.Advance(d % 7);
            int choice = rolling.Choose("Alice", new[] { "A", "B", "C", "D" }, 2, random);
            Check(!recent.Contains(choice), "Repeated within rolling two-day window at day " + d);
            rolling.Record("Alice", new[] { "A", "B", "C", "D" }[choice]);
            recent.Enqueue(choice);
            if (recent.Count > 2) recent.Dequeue();
            rolling = WearHistory.Load(rolling.Save());
        }
        var transfers = new WearHistory();
        transfers.Record("Alice", "A");
        transfers.Record("Alice2", "B");
        transfers.Forget("Alice");
        transfers = WearHistory.Load(transfers.Save());
        Check(transfers.Current("Alice") == null, "Transferred character current outfit must be removed persistently.");
        Check(transfers.DaysUntilAvailable("Alice", "A", 100) == 0, "Transferred character cooldown must be removed.");
        Check(transfers.Current("Alice2") == "B", "Removing a character must not match another identity's prefix.");
        Check(transfers.DaysUntilAvailable("Alice2", "B", 1) == 2, "Other characters must retain cooldown.");
        transfers.Forget("Unknown");
        Check(transfers.Current("Alice2") == "B", "Removing an unknown identity must be harmless.");
        return checks + " wear-history checks passed (including 100 days with save/load).";
    }
}
