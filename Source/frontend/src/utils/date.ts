// Local-date helpers. No timezone shenanigans.

export function todayStr(d: Date = new Date()): string {
  const y = d.getFullYear();
  const m = String(d.getMonth() + 1).padStart(2, "0");
  const day = String(d.getDate()).padStart(2, "0");
  return `${y}-${m}-${day}`;
}

export function tomorrowStr(d: Date = new Date()): string {
  const t = new Date(d);
  t.setDate(t.getDate() + 1);
  return todayStr(t);
}

// "HH:mm" → seconds remaining until that time today (or 0 if already past).
export function secondsUntilHHmm(hhmm: string, now: Date = new Date()): number {
  const [h, m] = hhmm.split(":").map((v) => parseInt(v, 10));
  const target = new Date(now);
  target.setHours(h, m, 0, 0);
  const diff = Math.floor((target.getTime() - now.getTime()) / 1000);
  return diff > 0 ? diff : 0;
}

export function isMorning(now: Date = new Date()): boolean {
  return now.getHours() < 17;
}

export function fmtCountdown(totalSeconds: number): string {
  const s = Math.max(0, Math.floor(totalSeconds));
  const h = Math.floor(s / 3600);
  const m = Math.floor((s % 3600) / 60);
  const sec = s % 60;
  return `${String(h).padStart(2, "0")}:${String(m).padStart(2, "0")}:${String(sec).padStart(2, "0")}`;
}
