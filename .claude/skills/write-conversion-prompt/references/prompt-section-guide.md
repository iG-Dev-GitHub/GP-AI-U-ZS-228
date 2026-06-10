# Conversion Prompt Section Guide

Detailed spec for each section of a conversion prompt. Every section is required.

## Table of Contents

- [Opening Paragraph](#opening-paragraph)
- [source_app](#source_app)
- [app_specific_rules](#app_specific_rules)
- [source_files_reading_order](#source_files_reading_order)
- [app_quality_checks](#app_quality_checks)
- [app_screenshot_checks](#app_screenshot_checks)

---

## Opening Paragraph

1-3 sentences. State what to convert, where to write output, and to read the skill first.

**Good:**
```
Convert the React Native app in `samples/AppName/frontend/` into a Unity 6 project
by writing UXML templates, USS stylesheets, and C# scripts into the `<output-dir>/`
directory. Read the `react-to-unity` skill fully before writing any code. This is a
complex, multi-file conversion — plan your work systematically and proceed
incrementally through each phase, completing each file fully before moving to the next.
```

Always use `<output-dir>/` as placeholder — the builder agent receives the actual path at runtime.

---

## source_app

Brief overview for the builder to understand what it's building before reading source files.

**Include:**
- App name and one-line purpose
- Visual theme/aesthetic (dark cyberpunk, warm pastel, minimal, etc.)
- Number of tabs and their names
- Non-tab screens (modals, detail views, stack screens)
- Key interactive mechanic (habit spheres, card flipping, timer, etc.)
- Data persistence approach ("AsyncStorage with two keys: habits list, logs object")
- Navigation summary (tab + stack/modal, what's visible where)

**Omit:** Implementation details, exact colors, file paths — those go in rules.

---

## app_specific_rules

Numbered rules. Each rule should be self-contained and provide motivation where non-obvious.

### Rule categories (typical order):

**1. Source reading directive** — always rule 1. Explain why reading first matters.

**2. Output directory** — where to write, what not to touch.

**3. File manifest** — complete list of files to create, grouped by type:
- USS files (Theme.uss, Components.uss) with what each contains
- UXML screen templates (one per screen) with element inventory
- UXML component templates (reusable items) with element inventory
- C# scripts (5 files: AppTypes, AppConstants, AppData, GameBootstrap, GameManager)

For each file, list its key contents in a parenthetical. This is the builder's roadmap.

**4. Color palette** — list every `--color-*` custom property with hex value in a code block. Include all background variants, text colors, accent colors, status colors, border colors, overlay colors. Add motivation: "Every hex color in the USS/UXML codebase must reference a var(--color-*) property."

**5-N. App-specific visual/behavioral rules** — one rule per concern:
- Element dimensions with exact pixel sizes and border-radius values
- Data model with field names and types (public fields for JsonUtility)
- Data logic ported from source (date helpers, streak calculation, scoring)
- Interactive features (modals, pickers, grids, delete confirmation)
- Game mechanics (multipliers, badges, progress tracking)
- Seed data for first-run (specific habits with realistic log patterns)
- Icon mapping (library → emoji)
- Libraries to skip (haptics, animations, gradients)
- Tab bar labels (always mention: both emoji icon AND text label)

### Common mistakes in rules:

| Mistake | Fix |
|---|---|
| Describing streak logic from memory | Copy the algorithm step-by-step from source, verify edge cases |
| Listing data types not used elsewhere | Every type in manifest must appear in at least one other rule |
| Missing overlay/modal backdrop color | Check source for rgba overlay values |
| Assuming Monday-based weeks | Check source's day-of-week calculation |
| Wrong default values for pickers | Check `useState` initial values in source |
| Vague element sizes ("a circle") | Exact px: "42×42px, border-radius: 21px" |
| Missing C# IStyle border warning | If any element uses dynamic borders in C#, add warning + helper code example. `style.borderColor`/`borderRadius`/`borderWidth` don't exist — causes compile errors |
| Describing dynamic styling without code example | Include actual C# snippet — builder follows examples more precisely than descriptions |
| "No hardcoded hex" only covers USS/UXML | Extend to C# too — use AppConstants color strings, not literal hex |
| Missing event handler cleanup on reusable overlays | `clicked +=` accumulates — mention cleanup when describing delete/confirm overlays |

---

## source_files_reading_order

Batched file list. Always include the parallel tool calls directive.

**Structure:**
```xml
<source_files_reading_order>
<use_parallel_tool_calls>
Read all source files using parallel tool calls — send all files in each batch
as a single message with multiple Read tool calls. This speeds up the reading
phase significantly.
</use_parallel_tool_calls>

**Batch 1** (data layer — read in parallel):
1. `samples/AppName/design_guidelines.json` (description)
2. `samples/AppName/frontend/src/types.ts` (description)
...

**Batch 2** (navigation + screens — read in parallel):
...

**Batch 3** (components — read in parallel, if needed):
...
</source_files_reading_order>
```

**Batching rules:**
- Batch 1: design guidelines, types, theme/colors, storage/persistence, context/state
- Batch 2: root layout, tab layout, all screen components
- Batch 3: shared components (cards, modals, pickers, list items) — only if there are standalone component files
- Each file gets a parenthetical description of what it contains
- Cover ALL source files — missing files cause the builder to guess

---

## app_quality_checks

Bulleted list of verification items specific to this app's logic and data.

**Good checks** (app-specific, verifiable):
- "Streak calculation matches storage.ts exactly (consecutive days backward from today, break on first gap)"
- "getWeeklyData returns 7 bools for last 7 days (today-6 through today), not Monday-based week"
- "Stats monthly rate denominator is habits.Count × daysElapsedThisMonth, not daysInMonth"
- "X2 badge appears at streak ≥ 7 in all 3 screens"
- "Grep USS/UXML for hardcoded hex — should find zero"

**Bad checks** (generic, already in SKILL.md):
- "Data classes use public fields" — already in skill's conversion checklist
- "PanelSettings is 390×844" — already in skill
- "ScrollView wraps all content" — already in skill

Only include checks that verify THIS app's unique logic, not general conversion correctness.

---

## app_screenshot_checks

Per-screen visual checks with specific, observable details.

**For each screen, list:**
- Background color (hex)
- Key text elements (title text, specific labels)
- Interactive elements (buttons, badges, FAB) with colors
- Data-driven elements (lists, grids, cards) that should show seed data
- Layout details (number of columns, row arrangement)

**End with an "All screens" entry** covering cross-cutting concerns:
- Theme consistency (dark/light throughout)
- No visible scrollbars
- Circles are circles (not ovals)
- Tab bar labels visible
- Accent colors present

**Example entry:**
```
- **Today**: near-black background (#050505), "HabitGravity" title in white bold,
  date subtitle in gray, cyan counter badge, habit rows with colored spheres
  (filled=done, outline=not), streak flame 🔥 + "Xd" text, X2 lime badges,
  cyan FAB bottom-right, seed data habits visible
```
