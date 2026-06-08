import { DayFormatId, RiskLevel } from "@/src/store/types";

export type DayFormat = {
  id: DayFormatId;
  title: string;
  color: string;
  description: string;
  // Number of tasks required to "clear" the day
  required: number | "all";
  cellIndices: number[]; // which bottom cells map to this format
  assetKey: string;
};

// Bottom row of 7 cells (left to right):
// [Red, Yellow, Green, Blue, Green, Yellow, Red]
export const CELL_FORMATS: DayFormatId[] = [
  "full-sprint",
  "deep-work",
  "quick-win",
  "steady",
  "quick-win",
  "deep-work",
  "full-sprint",
];

export const DAY_FORMATS: Record<DayFormatId, DayFormat> = {
  steady: {
    id: "steady",
    title: "Steady Day",
    color: "#3B82F6",
    description: "Complete 3 tasks at your own pace.",
    required: 3,
    cellIndices: [3],
    assetKey: "card-steady-day",
  },
  "quick-win": {
    id: "quick-win",
    title: "Quick Win",
    color: "#10B981",
    description: "Close 5 small tasks today.",
    required: 5,
    cellIndices: [2, 4],
    assetKey: "card-quick-win",
  },
  "deep-work": {
    id: "deep-work",
    title: "Deep Work",
    color: "#F59E0B",
    description: "Two tasks. 90 minutes each. No distractions.",
    required: 2,
    cellIndices: [1, 5],
    assetKey: "card-deep-work",
  },
  "full-sprint": {
    id: "full-sprint",
    title: "Full Sprint",
    color: "#EF4444",
    description: "Finish every task before 18:00. All or nothing.",
    required: "all",
    cellIndices: [0, 6],
    assetKey: "card-full-sprint",
  },
};

// Probabilities per cell index (0..6) for each risk level.
// User-specified totals:
//   Easy:    70% Steady/QuickWin, 25% DeepWork, 5% FullSprint
//   Hardcore: 30% Steady, 30% DeepWork, 40% FullSprint
const EASY_WEIGHTS = [0.025, 0.125, 0.25, 0.2, 0.25, 0.125, 0.025];
const HARDCORE_WEIGHTS = [0.2, 0.15, 0.0, 0.3, 0.0, 0.15, 0.2];

export function pickTargetCell(risk: RiskLevel): number {
  const w = risk === "easy" ? EASY_WEIGHTS : HARDCORE_WEIGHTS;
  const r = Math.random();
  let acc = 0;
  for (let i = 0; i < w.length; i++) {
    acc += w[i];
    if (r < acc) return i;
  }
  return w.length - 1;
}

export function formatForCell(cellIndex: number): DayFormat {
  return DAY_FORMATS[CELL_FORMATS[cellIndex]];
}
