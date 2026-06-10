# Conversion Progress — Daily Drop (ZS-GP-1.6.2)

Source: React Native Expo "Task Drop Day Plinko" (shown generically as "Daily Drop" / "Plan My Day").
Output dir: `Assets/!_App - ZS-GP-1.6.2/`

## App structure
- Tabs (tab bar visible): Today, Tomorrow, Stats, Settings
- Modals/stack (tab bar hidden): Welcome (onboarding 3 slides), Drop, FullSprint
- Start: onboarded ? Today : Welcome

## Data model
- Task{id,title,priority(low/med/high),done,createdAt}
- DayFormatId: steady|quick-win|deep-work|full-sprint
- Settings{morningDrop,sprintDeadline,eveningReminder,riskLevel}
- Badges{dayClearCount,fullSprintWins,onFire,weekChampion}
- DayRecord{date,formatId,totalTasks,doneTasks,cleared}
- streak, lastClearDate, history, onboarded
- PlayerPrefs persistence (mirror tdp.* keys, JsonUtility)

## Format clear rules: steady>=3, quick-win>=5, deep-work>=2, full-sprint = all(>0)
## Risk weights (cell 0..6): easy [.025,.125,.25,.2,.25,.125,.025]; hardcore [.2,.15,0,.3,0,.15,.2]
## Bottom cells L->R: full-sprint, deep-work, quick-win, steady, quick-win, deep-work, full-sprint

## Rules honored
- Tab bar flush to device bottom (paddingBottom 0, no gap)
- No offline/local-storage text in UI
- Emoji only from NotoEmoji-Bold (panel emoji fallback)
- Never show "Task Drop Day Plinko"

## Phases
- [x] Phase 1: read all sources
- [x] Phase 2: Theme.uss, Components.uss (0 hardcoded hex verified)
- [x] Phase 3: UXML screens (7) + components (8)
- [x] Phase 4: C# (AppTypes, AppConstants, AppData, GameBootstrap, GameManager)
- [x] Phase 5: Panel Settings -> 390x844 ScaleWithScreenSize; Panel Text Settings emoji on; AppSceneBuilder editor script
- [x] Phase 6: static review (cross-checked Q() names vs UXML; renamed Task->TaskItem to avoid clash with System.Threading.Tasks.Task in StartupLoadingController)

## Files written
USS: Resources/UI/Styles/Theme.uss, Components.uss
UXML screens: Welcome, Today, Tomorrow, Stats, Settings, Drop, FullSprint (Resources/UI/Screens/*Screen.uxml)
UXML components: TabBar, TaskCard, DayFormatBadge, StagedRow, HistoryRow, AddTaskSheet, TimePicker, ConfirmReset
C#: Scripts/AppTypes, AppConstants, AppData, GameBootstrap, GameManager
Editor: Assets/Editor/!_App - ZS-GP-1.6.2/AppSceneBuilder.cs (menu: Tools/ZS-GP/Build App Scene)

## Key decisions
- Reused existing Panel Settings.asset (loaded at runtime by GameManager), reconfigured to 390x844.
- Text: rely on UnityDefaultRuntimeTheme default Latin font; emoji via PanelTextSettings NotoEmoji fallback (EnableEmojiSupport=1).
- Plinko board built programmatically in C# (dynamic peg geometry); ball drop animated via coroutine.
- No usable source PNGs (backend-generated, absent) -> emoji icons used throughout.
- Tab bar flush to bottom (paddingBottom 0), height 64.

## Post-review fixes (round 2)
- First launch is now EMPTY (removed all seed/sample data); bumped SAVE_KEY tdp.save.v1 -> v2 so existing installs reset. onboarded defaults false -> Welcome shows, then empty Today.
- Fixed runtime "Add Task" NullReferenceException: an editable UI Toolkit TextField NREs when the resolved font asset is null (PanelTextSettings default font asset was null; Labels render via theme default but editable fields do not). GameManager.ApplyRootFont() now builds a TextCore FontAsset from the built-in Latin font (LegacyRuntime/Arial) with NotoEmoji-Bold as fallback and sets it on the root via unityFontDefinition.
- Settings tab emoji: ⚙ (U+2699, text-presentation, not in default font) -> 🔧 (emoji-block, routes to NotoEmoji fallback).

## Cannot run here
- Unity is running a different project (WA-GP) and ZS lockfile present; no build/screenshot scripts in repo.
- To finalize in Unity: run menu "Tools/ZS-GP/Build App Scene", then Play. Screenshots via existing Tools/Screenshots Tool.
- Verified by static review instead: name cross-check, hex grep, type-clash check, USS/UXML/C# API review.

## Notes
- Plinko board built programmatically in C# (dynamic peg positions) into a named container.
- Ball drop animated via coroutine in Drop screen.
- Countdown timer via coroutine (1s tick) on FullSprint.

## Post-review fixes (round 3 — match Source tutorial, emoji, tab bar)
- Welcome/tutorial now matches Source `welcome.tsx`:
  - Added the static "waiting" ball (white circle + 🕐) at top-center on slides 2 & 3
    (Source PlinkoBoard with animateDrop=false renders a static ball when targetCell != null).
    BuildPlinkoBoard gained an optional `staticBall` param; welcome passes it when WTarget[p] >= 0.
  - `.welcome-slide` changed `justify-content: center` -> `flex-start` so slide content is
    top-aligned like the Source (board → badge → title → body from the top, footer pinned bottom).
- Tab bar made a bit larger ("чуть шире"): height 64 -> 76, padding-top 6 -> 9,
  padding-bottom 0 -> 6, tab-icon 20 -> 23.
- Tofu/square glyphs replaced with characters that exist in NotoEmoji-Bold.ttf (verified via cmap):
  - ✓ U+2713 (MISSING in NotoEmoji) -> ✔ U+2714  (TaskCard, HistoryRow, DropScreen continue, GameManager x3)
  - ✕ U+2715 (MISSING in NotoEmoji) -> ✖ U+2716  (Drop close, FullSprint close)
  - Confirmed every other emoji in use (☀ 🌙 📊 🔧 🔥 🕐 🗑 🚀 🏆 🏅 ⚡ 🎯 🧭 ✅ etc.) IS in NotoEmoji-Bold.
  - NotoEmoji-Bold SDF asset is AtlasPopulationMode=Dynamic and its sourceFontFileGUID matches the
    TTF, so present glyphs rasterize on demand via the root font's emoji fallback.
- Fixed NullReferenceException when pressing the "+" (Add Task) button. The AddTaskSheet has an
  editable TextField; editable fields resolve their font from PanelSettings.textSettings
  .defaultFontAsset (NOT the inherited unityFontDefinition that round-2 set on the root). The
  Panel Text Settings asset shipped with m_DefaultFontAsset = null, so the field NRE'd on show.
  ApplyRootFont() now also assigns the runtime FontAsset to _panelSettings.textSettings
  .defaultFontAsset (when null). Same root cause affects every TextField (TimePicker etc.).
- Round-4 hardening (crash persisted): the round-3 assignment was guarded behind an early
  `return` that fired because FontAsset.CreateFontAsset(builtinFont) RETURNS NULL at runtime on
  this setup (Latin Labels still render via the theme's default font, which masked it). Rewrote
  ApplyRootFont to (1) try several font-build strategies via TryCreateFont + the
  CreateFontAsset("Arial","Regular") OS overload, (2) ALWAYS assign a non-null
  textSettings.defaultFontAsset (Latin if available, else emoji so the field can never NRE),
  (3) add emoji to textSettings.fallbackFontAssets, (4) keep the asset referenced in a field so it
  is not GC'd, and (5) log the outcome ("Font setup: latin=.., emoji=.., panelDefaultAssigned=..").
  Also hardened MountModal to log + no-op if a modal UXML fails to load instead of NRE'ing.
- ROOT CAUSE of the "+ / Add task / Reset All Data" NRE (round-5, confirmed by managed stack):
  it was NOT the font. UI Toolkit's StyleVariableResolver.ResolveVariable threw a
  NullReferenceException inside VisualTreeAsset.Instantiate() → CloneSetupRecursively →
  ApplyInlineStyles. Cause: those UXML used a custom property in a UXML *inline* style attribute,
  e.g. style="color: var(--color-text-secondary);". var() is only resolvable in USS class
  selectors — in an inline style="" it crashes at clone time (no variable context yet). The tab
  screens never crashed because none of them used inline var().
  Fix: removed every inline var() from UXML (AddTaskSheet, ConfirmReset, FullSprintScreen — 6 sites)
  and replaced with USS utility classes .text-inverse/.text-white/.text-sprint-light (added to the
  end of Components.uss so they win same-specificity) plus the existing .text-secondary.
  Rule: NEVER put var() in a UXML inline style attribute — only literal values or USS classes.
- Safe-area top: corrected the inset formula to the notch (top) inset
  (Screen.height - safe.yMax) * scale, and raised the floor 12f -> 44f so the device notch /
  status bar never overlaps top content even when the platform under-reports Screen.safeArea
  (editor Game view returns the full screen). A larger real reported inset still wins via Mathf.Max.
  Applied per-screen in Navigate() as screen-content paddingTop = _topInset + 8.
  (The font-default fix from round-4 is kept — it was a real latent bug, just not this crash.
  Removed the temporary stack-trace capture/diagnostic logging once the cause was found.)
- Images: re-confirmed Source has NO usable UI images. App art (ball-clock, badge-*, card-*) is
  backend/Gemini-generated and absent at rest; AssetImage falls back to empty circles / Ionicons
  (trophy). assets/images/* are only Expo template defaults (react-logo, icon, splash). Nothing to import.
