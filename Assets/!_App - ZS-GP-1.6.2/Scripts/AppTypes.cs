using System;
using System.Collections.Generic;

// ── Navigation screens ──
public enum AppScreen
{
    Welcome,
    Today,
    Tomorrow,
    Stats,
    Settings,
    Drop,
    FullSprint,
}

// ── Serializable data classes (JsonUtility -> public fields only) ──

[Serializable]
public class TaskItem
{
    public string id;
    public string title;
    public string priority;   // "low" | "med" | "high"
    public bool done;
    public long createdAt;    // unix ms
}

[Serializable]
public class DayRecord
{
    public string date;       // YYYY-MM-DD
    public string formatId;   // steady | quick-win | deep-work | full-sprint
    public int totalTasks;
    public int doneTasks;
    public bool cleared;
}

[Serializable]
public class Badges
{
    public int dayClearCount;
    public int fullSprintWins;
    public bool onFire;        // 3-day streak active
    public bool weekChampion;  // 5 day-clears in last 7 days
}

[Serializable]
public class AppSettings
{
    public string morningDrop = "08:00";
    public string sprintDeadline = "18:00";
    public string eveningReminder = "22:00";
    public string riskLevel = "easy"; // easy | hardcore
}

[Serializable]
public class AppSaveData
{
    public bool seeded;
    public bool onboarded;
    public List<TaskItem> tasks = new List<TaskItem>();
    public List<TaskItem> tomorrowTasks = new List<TaskItem>();
    public string dayFormat = "";     // "" = none
    public string dropDate = "";      // YYYY-MM-DD when the drop happened
    public int streak;
    public string lastClearDate = "";
    public Badges badges = new Badges();
    public List<DayRecord> history = new List<DayRecord>();
    public AppSettings settings = new AppSettings();
}
