using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class AppData
{
    const string SAVE_KEY = "tdp.save.v2";
    static AppSaveData s_Data;

    public static AppSaveData Data
    {
        get { if (s_Data == null) Load(); return s_Data; }
    }

    // =====================================================================
    //  Persistence
    // =====================================================================
    static void Load()
    {
        string json = PlayerPrefs.GetString(SAVE_KEY, "");
        if (!string.IsNullOrEmpty(json))
        {
            try { s_Data = JsonUtility.FromJson<AppSaveData>(json); }
            catch { s_Data = new AppSaveData(); }
        }
        if (s_Data == null) s_Data = new AppSaveData();
        EnsureValid();

        if (!s_Data.seeded) Seed();
        Rollover();
    }

    static void EnsureValid()
    {
        if (s_Data.tasks == null) s_Data.tasks = new List<TaskItem>();
        if (s_Data.tomorrowTasks == null) s_Data.tomorrowTasks = new List<TaskItem>();
        if (s_Data.history == null) s_Data.history = new List<DayRecord>();
        if (s_Data.badges == null) s_Data.badges = new Badges();
        if (s_Data.settings == null) s_Data.settings = new AppSettings();
        if (s_Data.dayFormat == null) s_Data.dayFormat = "";
        if (s_Data.dropDate == null) s_Data.dropDate = "";
        if (s_Data.lastClearDate == null) s_Data.lastClearDate = "";
    }

    public static void Save()
    {
        PlayerPrefs.SetString(SAVE_KEY, JsonUtility.ToJson(s_Data));
        PlayerPrefs.Save();
    }

    static string NewId()
    {
        long ms = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        string rand = UnityEngine.Random.Range(0, 1 << 24).ToString("x6");
        return $"{ms}-{rand}";
    }

    // =====================================================================
    //  Day rollover (mirrors loadState in state.ts)
    // =====================================================================
    static void Rollover()
    {
        string today = AppConstants.TodayStr();
        if (string.IsNullOrEmpty(s_Data.dropDate) || s_Data.dropDate == today) return;

        // Archive previous day if not already in history
        if (!string.IsNullOrEmpty(s_Data.dayFormat) &&
            !s_Data.history.Any(h => h.date == s_Data.dropDate))
        {
            int total = s_Data.tasks.Count;
            int done = s_Data.tasks.Count(t => t.done);
            bool cleared = AppConstants.ComputeCleared(s_Data.dayFormat, total, done);
            s_Data.history.Insert(0, new DayRecord
            {
                date = s_Data.dropDate,
                formatId = s_Data.dayFormat,
                totalTasks = total,
                doneTasks = done,
                cleared = cleared,
            });
        }

        // Carry over unfinished tasks + tomorrow-staged tasks
        var carryover = s_Data.tasks.Where(t => !t.done).ToList();
        carryover.AddRange(s_Data.tomorrowTasks);
        s_Data.tasks = carryover;
        s_Data.tomorrowTasks = new List<TaskItem>();
        s_Data.dayFormat = "";
        s_Data.dropDate = "";
        Save();
    }

    // =====================================================================
    //  First run — start completely empty (no sample data).
    //  The mark prevents re-initialization on later loads.
    // =====================================================================
    static void Seed()
    {
        s_Data.seeded = true;
        Save();
    }

    static TaskItem MakeTask(string title, string priority, bool done)
    {
        return new TaskItem
        {
            id = NewId(),
            title = title,
            priority = priority,
            done = done,
            createdAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
        };
    }

    // =====================================================================
    //  Actions
    // =====================================================================
    public static void MarkOnboarded()
    {
        Data.onboarded = true;
        Save();
    }

    public static void AddTask(string title, string priority)
    {
        Data.tasks.Add(MakeTask(title.Trim(), priority, false));
        Save();
    }

    public static void ToggleTask(string id)
    {
        var t = Data.tasks.FirstOrDefault(x => x.id == id);
        if (t != null) { t.done = !t.done; Save(); }
    }

    public static void RemoveTask(string id)
    {
        Data.tasks.RemoveAll(x => x.id == id);
        Save();
    }

    public static void AddTomorrowTask(string title, string priority)
    {
        Data.tomorrowTasks.Add(MakeTask(title.Trim(), priority, false));
        Save();
    }

    public static void RemoveTomorrowTask(string id)
    {
        Data.tomorrowTasks.RemoveAll(x => x.id == id);
        Save();
    }

    public static void SetDayFormat(string formatId)
    {
        Data.dayFormat = formatId;
        Data.dropDate = AppConstants.TodayStr();
        Save();
    }

    public static void ClearDayFormat()
    {
        Data.dayFormat = "";
        Data.dropDate = "";
        Save();
    }

    public static void RecordDayClear(string formatId)
    {
        string today = AppConstants.TodayStr();
        if (Data.lastClearDate == today) return;

        bool isConsecutive = false;
        if (!string.IsNullOrEmpty(Data.lastClearDate))
        {
            string yesterday = AppConstants.TodayStr(DateTime.Now.Date.AddDays(-1));
            isConsecutive = yesterday == Data.lastClearDate;
        }
        int newStreak = isConsecutive ? Data.streak + 1 : 1;

        var badges = new Badges
        {
            dayClearCount = Data.badges.dayClearCount + 1,
            fullSprintWins = Data.badges.fullSprintWins + (formatId == "full-sprint" ? 1 : 0),
            onFire = newStreak >= 3,
            weekChampion = false,
        };
        int last7 = Data.history.Take(7).Count(h => h.cleared) + 1;
        if (last7 >= 5) badges.weekChampion = true;

        Data.streak = newStreak;
        Data.lastClearDate = today;
        Data.badges = badges;
        Save();
    }

    public static void UpdateSetting(string key, string value)
    {
        switch (key)
        {
            case "morningDrop": Data.settings.morningDrop = value; break;
            case "sprintDeadline": Data.settings.sprintDeadline = value; break;
            case "eveningReminder": Data.settings.eveningReminder = value; break;
            case "riskLevel": Data.settings.riskLevel = value; break;
        }
        Save();
    }

    public static void ResetAll()
    {
        var fresh = new AppSaveData
        {
            seeded = true,   // keep seeded so we don't re-seed sample content
            onboarded = true,
        };
        s_Data = fresh;
        EnsureValid();
        Save();
    }
}
