// Single-file global state, backed by AsyncStorage via @/src/utils/storage.
// No redux, no zustand — just a tiny pub/sub. Mobile app is small enough.

import { useEffect, useState } from "react";

import { storage } from "@/src/utils/storage";
import { todayStr, tomorrowStr } from "@/src/utils/date";
import {
  Badges,
  DayFormatId,
  DayRecord,
  DEFAULT_SETTINGS,
  Settings,
  Task,
} from "./types";

const KEYS = {
  onboarded: "tdp.onboarded",
  tasks: "tdp.tasks",
  tomorrowTasks: "tdp.tomorrowTasks",
  dayFormat: "tdp.dayFormat",
  dropDate: "tdp.dropDate",
  streak: "tdp.streak",
  lastClearDate: "tdp.lastClearDate",
  badges: "tdp.badges",
  history: "tdp.history",
  settings: "tdp.settings",
  assets: "tdp.assets",
};

export type AppState = {
  ready: boolean;
  onboarded: boolean;
  tasks: Task[];
  tomorrowTasks: Task[];
  dayFormat: DayFormatId | null;
  dropDate: string | null; // YYYY-MM-DD when the drop happened
  streak: number;
  lastClearDate: string | null;
  badges: Badges;
  history: DayRecord[];
  settings: Settings;
  assets: Record<string, string>; // assetKey -> base64 PNG
};

const initial: AppState = {
  ready: false,
  onboarded: false,
  tasks: [],
  tomorrowTasks: [],
  dayFormat: null,
  dropDate: null,
  streak: 0,
  lastClearDate: null,
  badges: { dayClearCount: 0, fullSprintWins: 0, onFire: false, weekChampion: false },
  history: [],
  settings: DEFAULT_SETTINGS,
  assets: {},
};

let state: AppState = initial;
const listeners = new Set<() => void>();

function emit() {
  listeners.forEach((l) => l());
}

function setState(partial: Partial<AppState>) {
  state = { ...state, ...partial };
  emit();
}

export function useAppState(): AppState {
  const [, force] = useState(0);
  useEffect(() => {
    const l = () => force((n) => n + 1);
    listeners.add(l);
    return () => {
      listeners.delete(l);
    };
  }, []);
  return state;
}

export function getState(): AppState {
  return state;
}

// ---------------- Persistence ----------------

async function readJSON<T>(key: string, fallback: T): Promise<T> {
  const raw = await storage.getItem(key, "" as string);
  if (!raw) return fallback;
  try {
    return JSON.parse(raw) as T;
  } catch {
    return fallback;
  }
}

async function writeJSON<T>(key: string, value: T): Promise<void> {
  await storage.setItem(key, JSON.stringify(value));
}

export async function loadState() {
  const [
    onboarded,
    tasks,
    tomorrowTasks,
    dayFormat,
    dropDate,
    streak,
    lastClearDate,
    badges,
    history,
    settings,
    assets,
  ] = await Promise.all([
    storage.getItem(KEYS.onboarded, false as boolean),
    readJSON<Task[]>(KEYS.tasks, []),
    readJSON<Task[]>(KEYS.tomorrowTasks, []),
    storage.getItem(KEYS.dayFormat, "" as string),
    storage.getItem(KEYS.dropDate, "" as string),
    storage.getItem(KEYS.streak, 0 as number),
    storage.getItem(KEYS.lastClearDate, "" as string),
    readJSON<Badges>(KEYS.badges, initial.badges),
    readJSON<DayRecord[]>(KEYS.history, []),
    readJSON<Settings>(KEYS.settings, DEFAULT_SETTINGS),
    readJSON<Record<string, string>>(KEYS.assets, {}),
  ]);

  let nextTasks = tasks || [];
  let nextFormat = (dayFormat || null) as DayFormatId | null;
  let nextDropDate = dropDate || null;

  // Day rollover: if dropDate is not today, archive yesterday and prepare today.
  const today = todayStr();
  if (nextDropDate && nextDropDate !== today) {
    // Archive previous day if not already in history.
    if (nextFormat && !(history || []).some((h) => h.date === nextDropDate)) {
      const total = nextTasks.length;
      const done = nextTasks.filter((t) => t.done).length;
      const cleared = computeCleared(nextFormat, total, done);
      (history || []).unshift({
        date: nextDropDate!,
        formatId: nextFormat,
        totalTasks: total,
        doneTasks: done,
        cleared,
      });
      await writeJSON(KEYS.history, history);
    }
    // Carry over unfinished tasks + tomorrow-staged tasks.
    const carryover = nextTasks.filter((t) => !t.done);
    nextTasks = [...carryover, ...(tomorrowTasks || [])];
    await writeJSON(KEYS.tasks, nextTasks);
    await writeJSON(KEYS.tomorrowTasks, []);
    nextFormat = null;
    nextDropDate = null;
    await storage.setItem(KEYS.dayFormat, "");
    await storage.setItem(KEYS.dropDate, "");
  }

  setState({
    ready: true,
    onboarded: !!onboarded,
    tasks: nextTasks,
    tomorrowTasks: tomorrowTasks || [],
    dayFormat: nextFormat,
    dropDate: nextDropDate,
    streak: streak || 0,
    lastClearDate: lastClearDate || null,
    badges: badges || initial.badges,
    history: history || [],
    settings: settings || DEFAULT_SETTINGS,
    assets: assets || {},
  });
}

// ---------------- Actions ----------------

export async function markOnboarded() {
  setState({ onboarded: true });
  await storage.setItem(KEYS.onboarded, true);
}

export async function addTask(title: string, priority: Task["priority"]) {
  const t: Task = {
    id: `${Date.now()}-${Math.random().toString(36).slice(2, 8)}`,
    title: title.trim(),
    priority,
    done: false,
    createdAt: Date.now(),
  };
  const next = [...state.tasks, t];
  setState({ tasks: next });
  await writeJSON(KEYS.tasks, next);
}

export async function toggleTask(id: string) {
  const next = state.tasks.map((t) => (t.id === id ? { ...t, done: !t.done } : t));
  setState({ tasks: next });
  await writeJSON(KEYS.tasks, next);
}

export async function removeTask(id: string) {
  const next = state.tasks.filter((t) => t.id !== id);
  setState({ tasks: next });
  await writeJSON(KEYS.tasks, next);
}

export async function addTomorrowTask(title: string, priority: Task["priority"]) {
  const t: Task = {
    id: `${Date.now()}-${Math.random().toString(36).slice(2, 8)}`,
    title: title.trim(),
    priority,
    done: false,
    createdAt: Date.now(),
  };
  const next = [...state.tomorrowTasks, t];
  setState({ tomorrowTasks: next });
  await writeJSON(KEYS.tomorrowTasks, next);
}

export async function removeTomorrowTask(id: string) {
  const next = state.tomorrowTasks.filter((t) => t.id !== id);
  setState({ tomorrowTasks: next });
  await writeJSON(KEYS.tomorrowTasks, next);
}

export async function setDayFormat(formatId: DayFormatId) {
  const today = todayStr();
  setState({ dayFormat: formatId, dropDate: today });
  await storage.setItem(KEYS.dayFormat, formatId);
  await storage.setItem(KEYS.dropDate, today);
}

export async function clearDayFormat() {
  setState({ dayFormat: null, dropDate: null });
  await storage.setItem(KEYS.dayFormat, "");
  await storage.setItem(KEYS.dropDate, "");
}

export function computeCleared(
  formatId: DayFormatId,
  total: number,
  done: number,
): boolean {
  if (total === 0) return false;
  switch (formatId) {
    case "steady":
      return done >= 3;
    case "quick-win":
      return done >= 5;
    case "deep-work":
      return done >= 2;
    case "full-sprint":
      return done >= total && total > 0;
  }
}

export async function recordDayClear(formatId: DayFormatId) {
  const today = todayStr();
  if (state.lastClearDate === today) return; // already recorded
  const yesterday = state.lastClearDate;
  const isConsecutive = (() => {
    if (!yesterday) return false;
    const d = new Date(today);
    d.setDate(d.getDate() - 1);
    return todayStr(d) === yesterday;
  })();
  const newStreak = isConsecutive ? state.streak + 1 : 1;

  // Update badges
  const badges: Badges = {
    dayClearCount: state.badges.dayClearCount + 1,
    fullSprintWins:
      state.badges.fullSprintWins + (formatId === "full-sprint" ? 1 : 0),
    onFire: newStreak >= 3,
    weekChampion: false, // computed below
  };
  // Perfect Week: 5 clears in last 7 days (including today)
  const last7 = state.history.slice(0, 7).filter((h) => h.cleared).length + 1;
  if (last7 >= 5) badges.weekChampion = true;

  setState({ streak: newStreak, lastClearDate: today, badges });
  await Promise.all([
    storage.setItem(KEYS.streak, newStreak),
    storage.setItem(KEYS.lastClearDate, today),
    writeJSON(KEYS.badges, badges),
  ]);
}

export async function updateSettings(patch: Partial<Settings>) {
  const next = { ...state.settings, ...patch };
  setState({ settings: next });
  await writeJSON(KEYS.settings, next);
}

export async function resetAll() {
  setState({
    ...initial,
    ready: true,
    assets: state.assets, // keep generated assets so we don't re-fetch
  });
  await Promise.all([
    storage.removeItem(KEYS.onboarded),
    storage.removeItem(KEYS.tasks),
    storage.removeItem(KEYS.tomorrowTasks),
    storage.removeItem(KEYS.dayFormat),
    storage.removeItem(KEYS.dropDate),
    storage.removeItem(KEYS.streak),
    storage.removeItem(KEYS.lastClearDate),
    storage.removeItem(KEYS.badges),
    storage.removeItem(KEYS.history),
    storage.removeItem(KEYS.settings),
  ]);
}

export async function saveAsset(name: string, dataB64: string) {
  const next = { ...state.assets, [name]: dataB64 };
  setState({ assets: next });
  await writeJSON(KEYS.assets, next);
}

// dev/debug aid — also forces tomorrow to today (used nowhere in prod UI)
export async function _devSetDropDate(date: string) {
  setState({ dropDate: date });
  await storage.setItem(KEYS.dropDate, date);
}

export const __KEYS = KEYS;
export { tomorrowStr };
