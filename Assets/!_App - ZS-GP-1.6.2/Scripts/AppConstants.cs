using System;
using System.Collections.Generic;
using UnityEngine;

public class DayFormat
{
    public string id;
    public string title;
    public string colorHex;
    public string description;
    public int required;   // -1 means "all"
    public string emoji;
}

public static class AppConstants
{
    // ── Dynamic color constants (static design lives in USS) ──
    public const string ACCENT = "#FACC15";
    public const string SURFACE = "#1E243A";
    public const string DEEP = "#0B1020";
    public const string WHITE = "#FFFFFF";
    public const string TEXT_SECONDARY = "#94A3B8";
    public const string TEXT_INVERSE = "#0B1020";
    public const string COMPLETED = "#475569";
    public const string STEADY = "#3B82F6";
    public const string QUICK = "#10B981";
    public const string DEEP_AMBER = "#F59E0B";
    public const string SPRINT = "#EF4444";
    public const string TASK_MORNING = "#0F1E3D";
    public const string TASK_EVENING = "#451A03";
    public const string BG_BOARD = "#13182B";
    public const string BG_BOARD_RED = "#3B0A0A";

    // ── Day formats ──
    public static readonly Dictionary<string, DayFormat> Formats = new Dictionary<string, DayFormat>
    {
        { "steady", new DayFormat { id = "steady", title = "Steady Day", colorHex = "#3B82F6",
            description = "Complete 3 tasks at your own pace.", required = 3, emoji = "🧭" } },
        { "quick-win", new DayFormat { id = "quick-win", title = "Quick Win", colorHex = "#10B981",
            description = "Close 5 small tasks today.", required = 5, emoji = "⚡" } },
        { "deep-work", new DayFormat { id = "deep-work", title = "Deep Work", colorHex = "#F59E0B",
            description = "Two tasks. 90 minutes each. No distractions.", required = 2, emoji = "🎯" } },
        { "full-sprint", new DayFormat { id = "full-sprint", title = "Full Sprint", colorHex = "#EF4444",
            description = "Finish every task before 18:00. All or nothing.", required = -1, emoji = "🔥" } },
    };

    // Bottom row of 7 cells (left -> right)
    public static readonly string[] CELL_FORMATS =
    {
        "full-sprint", "deep-work", "quick-win", "steady", "quick-win", "deep-work", "full-sprint",
    };

    // Probabilities per cell index (0..6)
    static readonly float[] EASY_WEIGHTS = { 0.025f, 0.125f, 0.25f, 0.2f, 0.25f, 0.125f, 0.025f };
    static readonly float[] HARDCORE_WEIGHTS = { 0.2f, 0.15f, 0.0f, 0.3f, 0.0f, 0.15f, 0.2f };

    // Settings time options
    public static readonly string[] TIMES =
    {
        "06:00", "07:00", "08:00", "09:00", "10:00",
        "16:00", "17:00", "18:00", "19:00", "20:00",
        "21:00", "22:00", "23:00",
    };

    public static DayFormat GetFormat(string id)
    {
        if (!string.IsNullOrEmpty(id) && Formats.TryGetValue(id, out var f)) return f;
        return null;
    }

    public static DayFormat FormatForCell(int cellIndex)
    {
        int i = Mathf.Clamp(cellIndex, 0, CELL_FORMATS.Length - 1);
        return Formats[CELL_FORMATS[i]];
    }

    public static int PickTargetCell(string risk)
    {
        float[] w = risk == "easy" ? EASY_WEIGHTS : HARDCORE_WEIGHTS;
        float r = UnityEngine.Random.value;
        float acc = 0f;
        for (int i = 0; i < w.Length; i++)
        {
            acc += w[i];
            if (r < acc) return i;
        }
        return w.Length - 1;
    }

    // ── Date helpers ──
    public static string TodayStr() => TodayStr(DateTime.Now);
    public static string TodayStr(DateTime d) => d.ToString("yyyy-MM-dd");
    public static string TomorrowStr() => TodayStr(DateTime.Now.AddDays(1));

    public static int SecondsUntilHHmm(string hhmm)
    {
        var now = DateTime.Now;
        var parts = hhmm.Split(':');
        int h = int.Parse(parts[0]);
        int m = int.Parse(parts[1]);
        var target = new DateTime(now.Year, now.Month, now.Day, h, m, 0);
        int diff = (int)Math.Floor((target - now).TotalSeconds);
        return diff > 0 ? diff : 0;
    }

    public static bool IsMorning() => DateTime.Now.Hour < 17;

    public static string FmtCountdown(int totalSeconds)
    {
        int s = Mathf.Max(0, totalSeconds);
        int h = s / 3600;
        int m = (s % 3600) / 60;
        int sec = s % 60;
        return $"{h:D2}:{m:D2}:{sec:D2}";
    }

    // ── Clear rule ──
    public static bool ComputeCleared(string formatId, int total, int done)
    {
        if (total == 0) return false;
        switch (formatId)
        {
            case "steady": return done >= 3;
            case "quick-win": return done >= 5;
            case "deep-work": return done >= 2;
            case "full-sprint": return done >= total && total > 0;
            default: return false;
        }
    }

    // ── Color helpers ──
    public static Color FromHex(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out Color c);
        return c;
    }

    public static Color WithAlpha(Color c, float a)
    {
        c.a = a;
        return c;
    }

    public static Color PriorityColor(string priority)
    {
        switch (priority)
        {
            case "high": return FromHex(SPRINT);
            case "med": return FromHex(DEEP_AMBER);
            case "low": return FromHex(STEADY);
            default: return FromHex(DEEP_AMBER);
        }
    }

    public static string PriorityLabel(string priority)
    {
        switch (priority)
        {
            case "high": return "High";
            case "med": return "Med";
            case "low": return "Low";
            default: return "Med";
        }
    }
}
