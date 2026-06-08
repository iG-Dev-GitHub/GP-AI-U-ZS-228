// Shared types for the day-planner state.

export type Priority = "low" | "med" | "high";

export type Task = {
  id: string;
  title: string;
  priority: Priority;
  done: boolean;
  createdAt: number;
};

export type DayFormatId = "steady" | "quick-win" | "deep-work" | "full-sprint";

export type RiskLevel = "easy" | "hardcore";

// A "day" is identified by a local YYYY-MM-DD string (no auth, no timezone games).
export type DayRecord = {
  date: string;
  formatId: DayFormatId;
  totalTasks: number;
  doneTasks: number;
  cleared: boolean; // mandatory tasks for the format were all completed
};

export type Badges = {
  dayClearCount: number;
  fullSprintWins: number;
  onFire: boolean; // 3-day streak active
  weekChampion: boolean; // 5 day-clears in last 7 days
};

export type Settings = {
  morningDrop: string; // "HH:mm"
  sprintDeadline: string; // "HH:mm" (default 18:00)
  eveningReminder: string; // "HH:mm"
  riskLevel: RiskLevel;
};

export const DEFAULT_SETTINGS: Settings = {
  morningDrop: "08:00",
  sprintDeadline: "18:00",
  eveningReminder: "22:00",
  riskLevel: "easy",
};
