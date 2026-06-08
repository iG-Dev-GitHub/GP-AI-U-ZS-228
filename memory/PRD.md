# Task Drop Day Plinko — PRD

## Concept
Offline Android-first day planner (Expo React Native) with a Plinko ball that assigns the user's day format. Up to 10 tasks per day; a single ball drop picks one of 4 day formats:

- **Steady Day** (blue) — complete 3 tasks
- **Quick Win** (green) — close 5 small tasks
- **Deep Work** (yellow) — 2 focus tasks × 90 min
- **Full Sprint** (red) — finish everything by 18:00

Plinko board has 7 bottom cells: [Red, Yellow, Green, Blue, Green, Yellow, Red].

## Risk Levels
- **Easy**: 70% Steady/Quick Win, 25% Deep Work, 5% Full Sprint
- **Hardcore**: 30% Steady, 30% Deep Work, 40% Full Sprint

## Achievements
- **Day Clear** — mandatory tasks complete (per format)
- **On Fire** — 3 consecutive Day Clears
- **Week Champion** — 5 Day Clears in last 7 days

## Screens
- Welcome (3-slide tutorial) → `/welcome`
- Today tab → `/(tabs)/index` (board summary, format card, tasks)
- Tomorrow tab → `/(tabs)/tomorrow` (stage tasks, inactive board)
- Stats tab → `/(tabs)/stats` (streak, badges grid, history)
- Settings tab → `/(tabs)/settings` (times, risk level, reset)
- Drop modal → `/drop`
- Full Sprint modal → `/full-sprint`

## Tech
- **Frontend**: Expo Router, react-native-reanimated for ball animation, AsyncStorage via `@/src/utils/storage`. No authentication. No push notifications.
- **Backend**: FastAPI only used for one-time AI asset generation via Gemini Nano Banana (`gemini-3.1-flash-image-preview`). Frontend caches all 8 PNGs (ball-clock, 3 badges, 4 format cards) in AsyncStorage after first launch.
- **Storage**: Local-only. Day rollover at midnight automatically archives history and carries over unfinished + tomorrow-staged tasks.

## App identity
The app name **"Task Drop Day Plinko"** is never shown in the UI or on the icon — per product spec.
