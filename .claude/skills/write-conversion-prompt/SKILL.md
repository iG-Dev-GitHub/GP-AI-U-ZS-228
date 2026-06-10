---
name: write-conversion-prompt
description: |
  Write conversion prompts for new React Native → Unity app conversions.
  Use when a new sample app is in samples/ and needs a conversion-prompt-vN.md
  file in prompts/. Covers source app exploration, prompt structure, avoiding
  duplication with SKILL.md and app-builder.md, and a mandatory self-check
  pass to catch factual errors before delivering.
---

# Write Conversion Prompt

Generate a `prompts/conversion-prompt-vN.md` file for a new React Native source app. The prompt drives the `app-builder` agent to convert the app into Unity 6 using the `react-to-unity` skill.

## Key References

These three files define the conversion system. Read them before writing any prompt:

- **Prompt guide** — `claude-4-prompt-guide.md` (project root) — Claude 4.x prompting best practices
- **Conversion skill** — `.claude/skills/react-to-unity/SKILL.md` — complete React→Unity pattern library
- **Builder agent** — `.claude/agents/app-builder.md` — agent system prompt with work phases and pitfall list

## Core Principle: No Duplication

The conversion prompt is **app-specific only**. General patterns already live in the skill and agent:

| Already covered (do NOT repeat) | Belongs in the prompt |
|---|---|
| UXML/USS/C# patterns, file structure | App's screens, navigation, data model |
| PanelSettings setup, safe area handling | App's color palette as USS custom properties |
| Scrollbar hiding, perfect circles recipe | App-specific sphere/marker/card dimensions |
| Work phases (read→USS→UXML→C#→compile→screenshot) | Source file reading order with batches |
| Common pitfalls (scrollbars, ovals, hardcoded hex) | App-specific quality and screenshot checks |
| Bootstrap, navigation, tab bar patterns | App-specific data logic (streaks, scoring, etc.) |

## Workflow

### Step 1 — Explore the Source App

Read every file in the source app directory. Use parallel tool calls per batch.

**Batch 1** — Data layer: types, constants/theme, storage/persistence, context/state management
**Batch 2** — Navigation + screens: root layout, tab layout, each screen component
**Batch 3** — Shared components: cards, modals, pickers, list items

Capture these details:
- App name, purpose, visual theme
- All screens and navigation structure (tabs, stacks, modals)
- Data model shapes (interfaces, storage keys, nested structures)
- Complete color palette (every hex value from theme/constants)
- All interactive features (toggles, games, pickers, animations)
- Computational logic (streaks, scores, rates, date calculations)
- Icon library used (Ionicons, Lucide, etc.) → map to emoji replacements
- Libraries to skip (haptics, Reanimated animations, linear gradient)

### Step 2 — Read the Latest Existing Prompt

Read the highest-numbered `prompts/conversion-prompt-vN.md` for format reference. Match its structure exactly — the builder agent expects this format.

### Step 3 — Write the Prompt

Create `prompts/conversion-prompt-v{N+1}.md` with these sections in order. See [references/prompt-section-guide.md](references/prompt-section-guide.md) for detailed specs per section.

**Structure:**

```
Opening paragraph (1-3 sentences: what to convert, where to write, read skill first)

<source_app>
  App name, theme, key features, navigation structure, data persistence approach
</source_app>

<app_specific_rules>
  Numbered rules: source reading, file manifest, colors, visual specs,
  data model, logic porting, interactive features, seed data, icon mapping
</app_specific_rules>

<source_files_reading_order>
  <use_parallel_tool_calls>...</use_parallel_tool_calls>
  Batched file list with descriptions
</source_files_reading_order>

<app_quality_checks>
  Bulleted verification items specific to this app
</app_quality_checks>

<app_screenshot_checks>
  Per-screen visual checks with specific colors, sizes, element names
</app_screenshot_checks>
```

**Prompt guide principles to apply:**

1. **Be explicit** — specify exact pixel sizes, color hex values, element dimensions. "42×42px sphere with border-radius: 21px" not "a circle".
2. **Add context/motivation** — explain WHY a rule matters. "This ensures data shapes and logic are ported accurately."
3. **Use XML tags** — structure with `<source_app>`, `<app_specific_rules>`, etc.
4. **Include parallel tool calls directive** — add `<use_parallel_tool_calls>` in the source reading section.
5. **Positive framing** — "use emoji characters for icons" not "don't use Ionicons". Avoid aggressive language ("CRITICAL", "MUST") — use normal phrasing.
6. **Provide the color list** — list every `--color-*` custom property with its hex value. The builder needs these to write Theme.uss without re-reading source files.
7. **Include concrete C# code examples** for dynamic styling patterns. The builder follows examples more precisely than descriptions. When a rule describes setting borders, colors, or sizes dynamically in C#, provide an actual code snippet showing the correct API calls.

**Lessons from production runs — include these in every prompt:**

- **C# IStyle border shorthands don't exist.** If any element needs dynamic border styling in C#, add a warning: use `borderTopColor`/`borderRightColor`/`borderBottomColor`/`borderLeftColor` (not `borderColor`), same for radius and width. Include `SetBorderColor()`/`SetBorderWidth()`/`SetBorderRadius()` helper methods with code example. Omitting this causes compile errors.
- **C# inline hex colors.** The "no hardcoded hex" rule should cover C# too — reference `AppConstants` color strings or `SphereColors` array for dynamic inline styles, not literal hex values.
- **Event handler cleanup.** When describing reusable overlays (delete confirmation, modals), mention that `clicked +=` accumulates handlers. Use `clicked -= previous` or recreate the button element each time.
- **Scrollbar hiding needs reinforcement.** The USS `display: none` on `.unity-scroller--vertical`/`.unity-scroller--horizontal` may not suffice at runtime. Add `width: 0` / `height: 0` alongside `display: none` and verify scrollbar visibility in screenshot checks.

### Step 4 — Self-Check (Mandatory)

Re-read the written prompt AND the source files it references. Verify each item below. Fix any issues found before delivering.

**Factual accuracy checks:**

- [ ] **Streak/score logic** — compare your description word-by-word against the source function. Common error: describing fallback behavior that doesn't exist (e.g., "if today not done, start from yesterday" when the code returns 0 immediately).
- [ ] **C# dynamic styling** — any rule that describes setting borders/colors dynamically in C# includes the IStyle individual property warning and a code example. Missing this caused compile failures in production.
- [ ] **Data model types** — every type listed in the file manifest is defined and referenced elsewhere in the prompt. No phantom types that appear in the manifest but nowhere else.
- [ ] **Color completeness** — every hex color used in the source app's theme/constants appears as a `--color-*` property in the prompt. Check accent colors, status colors, background variants, border colors, overlay colors.
- [ ] **Default values** — default selections (e.g., default color in picker, default emoji) match the source code's `useState` initial values, not assumptions.
- [ ] **Navigation structure** — tab count, screen names, modal vs. stack presentation all match the source's layout files.
- [ ] **Date logic** — Monday-based vs. Sunday-based weeks, date format strings, month calculation (0-indexed vs 1-indexed) all match source.
- [ ] **Stats formulas** — denominators correct (days elapsed vs. days in month, habits.length in denominator), rounding behavior matches.

**Structural checks:**

- [ ] No duplication with SKILL.md or app-builder.md content
- [ ] Every numbered rule in `<app_specific_rules>` provides motivation (why it matters)
- [ ] Source file reading order covers ALL source files, batched by dependency
- [ ] Quality checks cover app-specific logic, not generic conversion checks
- [ ] Screenshot checks name specific colors, element names, and layout details per screen

**Prompt guide compliance:**

- [ ] Instructions are explicit and specific (not vague)
- [ ] Context/motivation provided for key rules
- [ ] XML tags used for structure
- [ ] `<use_parallel_tool_calls>` present in source reading section
- [ ] No aggressive language ("CRITICAL", "MUST") — use normal phrasing per prompt guide's tool triggering advice
