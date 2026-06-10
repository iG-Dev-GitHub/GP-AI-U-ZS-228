---
name: react-to-unity
description: Converts React Native / React apps to Unity 6 using UI Toolkit with UXML templates for layout, USS stylesheets for styling, and C# for logic and data binding. Covers screen navigation, tab bars, coroutines, data persistence, responsive scaling, safe area handling, and the complete conversion workflow.
allowed-tools: Read, Grep, Write, Bash, Edit, Glob, WebFetch
---

# React Native → Unity Conversion Skill

Convert React Native / React apps to Unity 6 (UI Toolkit) using **UXML for layout**, **USS for styling**, and **C# for logic and data binding**.

## When to Use This Skill

- Converting a React Native or React web app to a Unity mobile app
- Building runtime UI in Unity using UI Toolkit (not uGUI Canvas)
- Creating UI-driven apps/games with screen navigation, data, and interactivity

## Target Platform

- Unity 6 (6000.x) — the template is 6000.3.8f1
- UI Toolkit (`com.unity.modules.uielements`) — already in the template
- No Inspector assignments — everything loaded from Resources at runtime
- Scene pre-configured — `Assets/Scenes/SampleScene.unity`; the bootstrap runs automatically

---

## Reference Resolution: Point-Based (390×844)

The reference resolution is **390×844** — matches iOS point dimensions (~393pt) and Android dp (~390dp). React Native point values can be used directly in Unity without conversion.

### How It Works

With PanelSettings `ScaleWithScreenSize` and `match = 0f` (width only), Unity scales all reference pixels by `screen_width / 390`:

| Device | Screen width | Scale factor | `16px` renders at | Equivalent |
|---|---|---|---|---|
| iPhone 15 Pro | 1179px | 3.02× | 48.4px | 16.1pt |
| iPhone SE | 750px | 1.92× | 30.8px | 15.4pt |
| Android 1080p | 1080px | 2.77× | 44.3px | 16.1dp |
| Android 1440p | 1440px | 3.69× | 59.1px | 16.9dp |

When you see `fontSize: 16` in React Native, write `font-size: 16px` in USS. No conversion needed.

### Standard Sizes (iOS HIG)

| Element | Size (px at 390 ref) | Notes |
|---|---|---|
| Body text | 16-17 | Standard readable size |
| Caption | 12-13 | Small labels, timestamps |
| Section header | 20 | Emphasized text |
| Screen title | 28 | Large title |
| Hero/large title | 34 | Extra large display |
| Button label | 16-18 | Bold, centered |
| Tab icon (emoji) | 22 | Emoji characters |
| Tab label | 11-12 | Small text under icon |
| Touch target | 44×44 minimum | Apple HIG minimum |
| Primary button height | 50 | Comfortable tap target |
| Tab bar height | 50-65 | Content area (safe area bottom adds more) |
| Card padding | 16 | Standard content padding |
| Screen padding (horizontal) | 16 | Standard screen margin |
| Screen padding (top) | 20+ | Combined with safe area inset |

---

## File Structure

All generated files go into the output directory. Do not modify template files (ProjectSettings, Packages, Scenes, TextMesh Pro).

```
Assets/
├── Resources/
│   ├── Fonts/
│   │   └── LiberationSans.ttf          ← Copy from TextMesh Pro/Fonts/
│   └── UI/
│       ├── Styles/
│       │   ├── Theme.uss               ← Colors, typography, base styles
│       │   └── Components.uss          ← Cards, buttons, badges, tab bar
│       ├── Screens/
│       │   ├── HomeScreen.uxml         ← One UXML per screen
│       │   ├── SettingsScreen.uxml
│       │   └── DetailScreen.uxml
│       └── Components/
│           ├── TabBar.uxml             ← Tab bar component
│           ├── StatCard.uxml           ← Reusable item templates
│           └── ListItem.uxml
└── Scripts/
    ├── AppTypes.cs                     ← Enums and [Serializable] data classes
    ├── AppConstants.cs                 ← Config values, display helpers
    ├── AppData.cs                      ← PlayerPrefs persistence layer
    ├── GameBootstrap.cs                ← [RuntimeInitializeOnLoadMethod] auto-start
    └── GameManager.cs                  ← Navigation, data binding, game logic
```

### Why This Structure

- UXML files in `Assets/Resources/UI/` → loaded at runtime via `Resources.Load<VisualTreeAsset>()`
- USS files in `Assets/Resources/UI/Styles/` → loaded at runtime via `Resources.Load<StyleSheet>()` and applied to root (cascades to all children)
- UXML can be edited in Unity's UI Builder or any text editor — no recompilation needed
- USS can be edited to change colors/sizes/spacing without touching C# code
- C# contains only logic — no layout creation, no inline styling

### File Dependency Order

Write in this order to avoid forward references:

1. `Theme.uss` → `Components.uss` (USS files first)
2. UXML screen templates + component templates (use the USS classes)
3. `AppTypes.cs` → `AppConstants.cs` → `AppData.cs` → `GameBootstrap.cs` → `GameManager.cs`

---

## USS Differences from CSS

USS looks like CSS but has different defaults and missing features. These cause the most layout bugs:

| Topic | CSS | USS |
|-------|-----|-----|
| Default flex-direction | `row` | **`column`** — set `flex-direction: row` explicitly for horizontal layouts |
| `gap` | Supported | **Not supported** — use margins on children |
| `flex-wrap` | `wrap` | **`wrap`** (lowercase — `Wrap` fails silently) |
| `display` values | `flex`, `block`, `grid`, `none` | **Only `flex` and `none`** |
| `z-index` | Supported | **Not supported** — element order = draw order |
| Units | px, em, rem, vh, vw, % | **Only px and %** |
| `calc()` | Supported | **Not supported** |
| `@media` | Supported | **Not supported** — handle in C# |
| Font weight | `100`–`900` | **Only**: `normal`, `bold`, `italic`, `bold-and-italic` |
| Text alignment | `text-align: center` | **`-unity-text-align: middle-center`** (2D grid) |
| Box model | `content-box` default | **Always `border-box`** |
| Shorthand `margin`/`padding` | Supported | **Supported in USS** (unlike C# inline styles) |
| `border-radius` shorthand | Supported | **Supported in USS** |
| Custom properties | `--var` / `var(--var)` | **Supported** (same syntax) |
| `:hover` / `:active` | Supported | **Supported** |
| `:nth-child`, `:first-child`, `:last-child`, `::before` | Supported | **Not supported** — use uniform margins on all children |
| Selectors | Full CSS | **Limited**: `.class`, `#name`, `Type`, `:pseudo`, `>` child, space descendant |
| `text-overflow` | `text-overflow: ellipsis` | **Supported** — use with `overflow: hidden` |
| `aspect-ratio` | Supported | **Supported** (Unity 2022.2+) |
| `text-shadow` | Supported | **Supported** — `text-shadow: 2px 2px 4px rgba(0,0,0,0.5)` |
| `letter-spacing` | Supported | **Supported** — `letter-spacing: 2px` |
| Background scale | `background-size` | **`-unity-background-scale-mode`**: `scale-and-crop`, `stretch-to-fill`, `scale-to-fit` |

### Always Write Explicit `px` Units on Length Values

USS requires every length/size value to include a `px` or `%` unit. The Unity 6 USS parser silently ignores bare numbers (unlike CSS which accepts unitless values in some contexts), so omitting units produces no error but causes broken layout — elements collapse to zero size or default positioning.

Always write units on: `font-size`, `margin`, `padding`, `width`, `height`, `border-radius`, `border-width`, `min-height`, `max-width`, `letter-spacing`, `top`, `left`, `right`, `bottom`, and all other length properties.

```css
/* Every length value gets an explicit px unit */
font-size: 16px;
margin-top: 8px;
border-radius: 20px;
padding: 24px;
width: 44px;
border-width: 2px;
```

Unitless values that are correct as-is (these are ratios/factors, not lengths):
- `flex-grow: 1`
- `opacity: 0.5`
- `0` (zero is valid with or without a unit)

When porting from React Native, add `px` to every numeric style value: RN `StyleSheet.create({ fontSize: 16 })` becomes USS `font-size: 16px`.

### Text Overflow & Wrapping

Grid card labels use `white-space: nowrap` to prevent ugly line breaks. Without it, text like "Diamond Egg" wraps to "Dia\nmo\nnd\nEg\ng" when the container is narrow. Only use `white-space: normal` on body text and descriptions that are intended to wrap.

```css
/* Wrap text normally (default for long content) */
.text-wrap { white-space: normal; }

/* Prevent wrapping — single line, truncate with ellipsis */
.text-truncate {
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
}

/* Short labels in grids/cards */
.card-label {
    white-space: nowrap;
    -unity-text-align: middle-center;
    font-size: 12px;
}
```

### Perfect Circles in USS

In CSS, `border-radius: 50%` on any rectangle makes an ellipse that inscribes the rectangle — a square becomes a circle, a rectangle becomes an oval. USS works the same way. If your element's `width ≠ height`, you get an oval, not a circle.

For circular elements (emoji avatars, color picker dots, icon badges), always enforce equal width and height, then set `border-radius` to half the size:

```css
/* Correct: explicit square + pixel border-radius = perfect circle */
.avatar-circle {
    width: 48px;
    height: 48px;
    border-radius: 24px;
    align-items: center;
    justify-content: center;
}

/* WRONG: unequal dimensions = oval */
.avatar-oval {
    width: 48px;
    height: 56px;
    border-radius: 50%;  /* This creates an oval, not a circle */
}
```

For intentionally egg-shaped elements (like day markers on a road), unequal dimensions are correct:
```css
.day-marker-egg {
    width: 44px;
    height: 52px;
    border-radius: 22px;  /* Creates the egg/oval shape */
}
```

### Grid Sizing

In flex-wrap grids, verify that `(items_per_row × width%) + (items_per_row × 2 × margin%) ≤ 100%`. USS uses `border-box` sizing — `width` includes padding and border, but not margin.

```css
/* 4-column grid — each item ~22% + 3% margins = 25% × 4 = 100% */
.grid-4col { flex-direction: row; flex-wrap: wrap; }
.grid-4col > * { width: 22%; margin: 1.5%; }

/* 2-column grid — each item ~46% + 4% margins = 50% × 2 = 100% */
.grid-2col { flex-direction: row; flex-wrap: wrap; }
.grid-2col > * { width: 46%; margin: 2%; }
```

---

## Pattern 1: USS Theme & Components

### Theme.uss — Global Design Tokens

Port **all** colors from the source app's color constants as USS custom properties — this includes primary colors, border variants, tints, state colors, and any secondary shades used for card backgrounds, markers, or badges. Every hex color value in the entire USS/UXML codebase must reference a `var(--color-*)` property. Also define a typography scale as `--font-size-*` custom properties.

```css
/* ============================================
   Theme.uss — Design tokens and base styles
   ============================================ */

/* ── Custom Properties (from colors.ts / design_guidelines.json) ── */
:root {
    /* Primary palette */
    --color-bg: #FFFBF0;
    --color-card: #FFFDF5;
    --color-border: #E4D5B7;
    --color-primary: #FFB300;
    --color-primary-dark: #FF8F00;
    --color-text: #212121;
    --color-text-muted: #9E9E9E;
    --color-text-light: #FFFFFF;
    --color-success: #66BB6A;
    --color-danger: #FF5252;
    --color-white: #FFFFFF;

    /* Include ALL secondary shades, border variants, tints, and state colors
       used anywhere in the app — Components.uss must never contain hardcoded hex */
    --color-success-light: #E8F5E9;
    --color-danger-light: #FFEBEE;
    --color-highlight: #FFF8E1;
    --color-disabled-bg: #FAFAFA;
    --color-disabled-border: #E0E0E0;

    /* Typography scale (RN point values map directly) */
    --font-size-xs: 11px;
    --font-size-sm: 13px;
    --font-size-md: 16px;
    --font-size-lg: 20px;
    --font-size-xl: 24px;
    --font-size-2xl: 32px;
    --font-size-3xl: 34px;
}

/* ── Base Layout ── */
.screen {
    flex-grow: 1;
    background-color: var(--color-bg);
}

.screen-scroll {
    flex-grow: 1;
}

.screen-content {
    padding: 16px;
    padding-bottom: 24px;
    /* paddingTop is set dynamically in C# for safe area */
}

/* ── Typography Scale (RN points map directly) ── */
.text-hero {
    font-size: 34px;
    -unity-font-style: bold;
    color: var(--color-text);
}

.text-title {
    font-size: 28px;
    -unity-font-style: bold;
    color: var(--color-text);
}

.text-heading {
    font-size: 20px;
    -unity-font-style: bold;
    color: var(--color-text);
}

.text-body {
    font-size: 16px;
    color: var(--color-text);
    white-space: normal;
}

.text-caption {
    font-size: 12px;
    color: var(--color-text-muted);
}

.text-center {
    -unity-text-align: middle-center;
}

.text-bold {
    -unity-font-style: bold;
}

.text-white {
    color: var(--color-text-light);
}

.text-muted {
    color: var(--color-text-muted);
}

/* ── Layout Utilities ── */
.row {
    flex-direction: row;
}

.center {
    align-items: center;
    justify-content: center;
}

.grow {
    flex-grow: 1;
}

.hidden {
    display: none;
}

.wrap {
    flex-wrap: wrap;
}

.space-between {
    justify-content: space-between;
}

.align-center {
    align-items: center;
}
```

### Components.uss — Reusable Component Styles

```css
/* ============================================
   Components.uss — Cards, buttons, badges, tab bar
   ============================================ */

/* ── Card ── */
.card {
    background-color: var(--color-card);
    border-radius: 16px;
    border-width: 1px;
    border-color: var(--color-border);
    padding: 16px;
    margin-bottom: 12px;
}

.card-centered {
    align-items: center;
}

/* ── Primary Button ── */
.btn-primary {
    height: 50px;
    background-color: var(--color-primary);
    color: var(--color-text-light);
    font-size: 18px;
    -unity-font-style: bold;
    -unity-text-align: middle-center;
    border-radius: 25px;
    border-width: 0;
    margin-top: 16px;
    transition: background-color 0.15s ease, scale 0.1s ease;
}

.btn-primary:active {
    background-color: var(--color-primary-dark);
    scale: 0.97 0.97;
}

/* ── Secondary / Outline Button ── */
.btn-secondary {
    height: 44px;
    background-color: rgba(0, 0, 0, 0);
    color: var(--color-primary);
    font-size: 16px;
    -unity-font-style: bold;
    -unity-text-align: middle-center;
    border-radius: 22px;
    border-width: 2px;
    border-color: var(--color-primary);
}

/* ── Back / Close Button ── */
.btn-back {
    width: 44px;
    height: 44px;
    background-color: rgba(0, 0, 0, 0);
    color: var(--color-text);
    font-size: 20px;
    -unity-text-align: middle-center;
    border-width: 0;
    margin-right: 8px;
}

/* ── Tab Bar ── */
.tab-bar {
    flex-direction: row;
    min-height: 65px;
    background-color: var(--color-white);
    border-top-width: 1px;
    border-top-color: var(--color-border);
    /* paddingBottom set dynamically in C# for safe area _bottomInset */
}

.tab-btn {
    flex-grow: 1;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    background-color: rgba(0, 0, 0, 0);
    border-width: 0;
    padding: 6px 0;
}

.tab-btn.active {
    background-color: #F3E5AB;
}

.tab-icon {
    font-size: 22px;
    color: var(--color-text);
    margin-bottom: 2px;
}

.tab-label {
    font-size: 11px;
    color: var(--color-text-muted);
}

.tab-btn.active .tab-label {
    color: #F57C00;
}

/* ── Stat Card (for grids of metrics) ── */
.stat-card {
    flex-grow: 1;
    align-items: center;
    padding: 12px 8px;
}

.stat-value {
    font-size: 24px;
    -unity-font-style: bold;
    color: var(--color-text);
}

.stat-label {
    font-size: 12px;
    color: var(--color-text-muted);
    margin-top: 4px;
}

/* ── History / List Item ── */
.list-item {
    flex-direction: row;
    align-items: center;
    padding: 12px;
}

.list-item-emoji {
    font-size: 28px;
    margin-right: 12px;
}

.list-item-value {
    font-size: 20px;
    -unity-font-style: bold;
}

/* ── Badge ── */
.badge {
    padding: 4px 12px;
    border-radius: 12px;
    font-size: 12px;
    -unity-font-style: bold;
}

/* ── Empty State ── */
.empty-state {
    align-items: center;
    padding: 32px 16px;
}

.empty-emoji {
    font-size: 44px;
    margin-bottom: 8px;
}

/* ── Timer / Progress Bar ── */
.timer-bar-bg {
    height: 6px;
    background-color: var(--color-border);
    border-radius: 3px;
    margin: 8px 0;
}

.timer-bar-fill {
    height: 6px;
    width: 100%;
    background-color: var(--color-success);
    border-radius: 3px;
}

/* ── Test Phase Container ── */
.test-phase {
    flex-grow: 1;
    align-items: center;
    justify-content: center;
    padding: 24px;
}

.phase-text-large {
    font-size: 32px;
    -unity-font-style: bold;
    -unity-text-align: middle-center;
}

/* ── Text Overflow (for short labels in grids/cards) ── */
.text-truncate {
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
}

/* ── Grid Layouts ── */
.grid-2col {
    flex-direction: row;
    flex-wrap: wrap;
}

.grid-2col > * {
    width: 46%;
    margin: 2%;
}

.grid-4col {
    flex-direction: row;
    flex-wrap: wrap;
}

.grid-4col > * {
    width: 22%;
    margin: 1.5%;
}

.grid-3x3 {
    width: 260px;
    height: 260px;
    flex-direction: row;
    flex-wrap: wrap;
    align-self: center;
}

.grid-3x3-cell {
    width: 80px;
    height: 80px;
    margin: 3px;
    border-radius: 8px;
}

/* ── Header Bar (for stack screens) ── */
.header-bar {
    flex-direction: row;
    align-items: center;
    padding: 8px;
    background-color: var(--color-white);
    border-bottom-width: 1px;
    border-bottom-color: var(--color-border);
    /* paddingTop set dynamically in C# for safe area */
}
```

---

## Pattern 2: UXML Screen Templates

### Screen Template Structure

Every screen UXML follows this pattern:

```xml
<ui:UXML xmlns:ui="UnityEngine.UIElements">
    <ui:ScrollView class="screen-scroll"
        vertical-scroller-visibility="Hidden"
        horizontal-scroller-visibility="Hidden">
        <ui:VisualElement name="screen-content" class="screen-content">

            <!-- Screen title -->
            <ui:Label text="Screen Title" class="text-title" />

            <!-- Hero card -->
            <ui:VisualElement class="card card-centered">
                <ui:Label name="hero-emoji" text="🐔" style="font-size: 54px;" />
                <ui:Label name="hero-title" class="text-heading" />
                <ui:Label name="hero-subtitle" class="text-body text-muted text-center" />
            </ui:VisualElement>

            <!-- Dynamic content container (populated in C#) -->
            <ui:VisualElement name="stats-grid" class="grid-2col" />

            <!-- Action button -->
            <ui:Button name="btn-action" text="Start" class="btn-primary" />

        </ui:VisualElement>
    </ui:ScrollView>
</ui:UXML>
```

### Stack Screen with Header Bar

```xml
<ui:UXML xmlns:ui="UnityEngine.UIElements">
    <ui:VisualElement class="screen">
        <!-- Header with back button -->
        <ui:VisualElement name="header" class="header-bar">
            <ui:Button name="btn-back" text="✕" class="btn-back" />
            <ui:Label name="header-title" text="Detail" class="text-heading" />
        </ui:VisualElement>

        <ui:ScrollView class="screen-scroll"
            vertical-scroller-visibility="Hidden"
            horizontal-scroller-visibility="Hidden">
            <ui:VisualElement name="screen-content" class="screen-content">
                <!-- Screen content here -->
            </ui:VisualElement>
        </ui:ScrollView>
    </ui:VisualElement>
</ui:UXML>
```

### Game Screen with Multiple Phases

For screens with distinct phases (intro → playing → done), define all phases in the UXML and show/hide them via CSS classes:

```xml
<ui:UXML xmlns:ui="UnityEngine.UIElements">
    <ui:VisualElement name="game-screen" class="screen">

        <!-- Intro phase (visible by default) -->
        <ui:VisualElement name="phase-intro" class="test-phase">
            <ui:Label text="⚡" style="font-size: 54px;" />
            <ui:Label text="Reaction Speed" class="text-title text-center" />
            <ui:Label text="Tap when the screen turns green!" class="text-body text-center text-muted" />
            <ui:Button name="btn-start" text="Start" class="btn-primary" />
        </ui:VisualElement>

        <!-- Waiting phase (hidden initially) -->
        <ui:VisualElement name="phase-waiting" class="test-phase hidden">
            <ui:Label name="waiting-text" text="Wait..." class="phase-text-large text-white" />
        </ui:VisualElement>

        <!-- Ready phase (hidden initially) -->
        <ui:VisualElement name="phase-ready" class="test-phase hidden">
            <ui:Label text="TAP NOW!" class="phase-text-large text-white" />
        </ui:VisualElement>

        <!-- Feedback phase (hidden initially) -->
        <ui:VisualElement name="phase-feedback" class="test-phase hidden">
            <ui:Label name="feedback-text" class="phase-text-large" />
            <ui:Label name="feedback-detail" class="text-body text-center" />
        </ui:VisualElement>

        <!-- Done phase (hidden initially) -->
        <ui:VisualElement name="phase-done" class="test-phase hidden">
            <ui:Label name="done-text" class="text-title text-center" />
            <ui:Button name="btn-results" text="See Results" class="btn-primary" />
        </ui:VisualElement>

    </ui:VisualElement>
</ui:UXML>
```

### Reusable Component Templates

For repeated items (list rows, grid cards), create component UXML files:

```xml
<!-- Components/StatCard.uxml -->
<ui:UXML xmlns:ui="UnityEngine.UIElements">
    <ui:VisualElement class="card stat-card">
        <ui:Label name="stat-value" class="stat-value" />
        <ui:Label name="stat-label" class="stat-label" />
    </ui:VisualElement>
</ui:UXML>
```

```xml
<!-- Components/ListItem.uxml -->
<ui:UXML xmlns:ui="UnityEngine.UIElements">
    <ui:VisualElement class="card list-item">
        <ui:Label name="item-emoji" class="list-item-emoji" />
        <ui:VisualElement class="grow">
            <ui:Label name="item-title" class="text-body text-bold" />
            <ui:Label name="item-subtitle" class="text-caption" />
        </ui:VisualElement>
        <ui:Label name="item-value" class="list-item-value" />
    </ui:VisualElement>
</ui:UXML>
```

### UXML Rules

1. Always use `ui:` namespace prefix: `<ui:VisualElement>`, `<ui:Label>`, `<ui:Button>`
2. Name interactive elements (`name="btn-start"`) for C# querying
3. Use USS classes for styling (`class="card text-body"`) — avoid inline `style=""` except for one-off values
4. Every screen uses a ScrollView with `vertical-scroller-visibility="Hidden"` and `horizontal-scroller-visibility="Hidden"` (see Scrollbar Hiding recipe). Exception: game screens with fixed-layout phases
5. Leave dynamic containers empty (`<ui:VisualElement name="grid" class="grid-2col" />`) — C# populates them
6. Use `class="hidden"` on phases that start hidden
7. Self-close empty elements: `<ui:VisualElement name="grid" class="grid-2col" />`
8. Available elements: `VisualElement`, `Label`, `Button`, `ScrollView`, `TextField`, `Toggle`, `Slider`, `ListView`, `ProgressBar`, `DropdownField`, `Foldout`, `Image`, `RadioButton`, `RadioButtonGroup`

---

## Pattern 3: Bootstrap (Auto-Start Without Scene Setup)

```csharp
using UnityEngine;

public class GameBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        var go = new GameObject("GameManager");
        Object.DontDestroyOnLoad(go);
        go.AddComponent<GameManager>();
    }
}
```

This runs automatically on any scene. No Inspector configuration needed.

---

## Pattern 4: GameManager (PanelSettings + Navigation)

The GameManager creates the UIDocument, loads stylesheets globally, and swaps UXML screen templates on navigation.

### PanelSettings Setup

PanelSettings is created programmatically with `HideFlags.HideAndDontSave`. Without this flag, clicking the GameManager in Unity's hierarchy during Play mode nulls the runtime-created PanelSettings reference, causing a black screen. Store it in a `_panelSettings` field and add `LateUpdate()` recovery.

The font file `LiberationSans.ttf` must be copied to `Assets/Resources/Fonts/` and loaded in `SetupUI()`. Without explicit font loading all Label text may be invisible.

The template includes `Assets/Resources/UI/UnityDefaultRuntimeTheme.tss` — a Theme Style Sheet that must be loaded and assigned to PanelSettings in `SetupUI()` via `_panelSettings.themeStyleSheet = Resources.Load<ThemeStyleSheet>("UI/UnityDefaultRuntimeTheme")`. Without this, Unity logs "No Theme Style Sheet set to PanelSettings" and UI may not render properly (missing fonts, broken styles).

### Safe Area Handling

Compute `_topInset` and `_bottomInset` from `Screen.safeArea` in `SetupUI()`, converting to reference coordinates: `float scale = 390f / (float)Screen.width`. Apply `_topInset` as paddingTop on content containers (per-screen in `Navigate()`), `_bottomInset` as paddingBottom on the tab bar. Do not add safe area padding to the root element or the `_contentArea` wrapper.

**Common safe area mistakes** (all three have occurred in past conversions — verify your code against the pattern below):
- Scale must use **width**: `390f / Screen.width`. Using `844f / Screen.height` breaks on non-16:9 devices.
- `_topInset = safeArea.y * scale` (distance from screen top to safe area top). Do NOT use `Screen.height - safeArea.yMax` — that formula gives the bottom inset.
- Apply `_topInset` **per-screen in `Navigate()`** to `screen-content` or `header` elements. Do NOT apply it to `_contentArea` in `SetupUI()` — that causes double padding on stack screens (like detail screens) that have their own header with safe area padding.

```csharp
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class GameManager : MonoBehaviour
{
    // ── UI root references ──
    PanelSettings _panelSettings;
    UIDocument    _doc;
    VisualElement _root;
    VisualElement _contentArea;
    VisualElement _tabBar;

    // ── Safe area insets (in reference pixels) ──
    float _topInset;
    float _bottomInset;

    // ── Navigation state ──
    AppScreen _currentScreen = AppScreen.Home;

    // =========================================================================
    //  Unity Lifecycle
    // =========================================================================
    void Start()
    {
        SetupUI();
        Navigate(AppScreen.Home);
    }

    // =========================================================================
    //  UI Setup — creates PanelSettings, UIDocument, loads global stylesheets
    // =========================================================================
    void SetupUI()
    {
        // Create PanelSettings programmatically
        _panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
        _panelSettings.hideFlags         = HideFlags.HideAndDontSave;
        _panelSettings.referenceResolution = new Vector2Int(390, 844);
        _panelSettings.scaleMode           = PanelScaleMode.ScaleWithScreenSize;
        _panelSettings.screenMatchMode     = PanelScreenMatchMode.MatchWidthOrHeight;
        _panelSettings.match               = 0f; // WIDTH only — correct for portrait apps

        // Load and assign Theme Style Sheet (prevents "No Theme Style Sheet" warning)
        var theme = Resources.Load<ThemeStyleSheet>("UI/UnityDefaultRuntimeTheme");
        if (theme != null)
            _panelSettings.themeStyleSheet = theme;

        // Create UIDocument
        _doc               = gameObject.AddComponent<UIDocument>();
        _doc.panelSettings = _panelSettings;

        // Compute safe area insets in reference coordinates
        float scale = 390f / (float)UnityEngine.Screen.width;
        var safeArea = UnityEngine.Screen.safeArea;
        _topInset    = Mathf.Max(safeArea.y * scale, 20f);
        _bottomInset = Mathf.Max(
            (UnityEngine.Screen.height - (safeArea.y + safeArea.height)) * scale, 0f);

        // Root setup
        _root = _doc.rootVisualElement;
        _root.style.flexDirection   = FlexDirection.Column;
        _root.style.width           = new Length(100, LengthUnit.Percent);
        _root.style.height          = new Length(100, LengthUnit.Percent);

        // Load global stylesheets — these cascade to ALL children
        _root.styleSheets.Add(Resources.Load<StyleSheet>("UI/Styles/Theme"));
        _root.styleSheets.Add(Resources.Load<StyleSheet>("UI/Styles/Components"));
        _root.AddToClassList("screen");

        // Load font and apply to root — required for text rendering
        var font = Resources.Load<Font>("Fonts/LiberationSans");
        if (font != null)
        {
            _root.style.unityFontDefinition = StyleKeyword.Null;
            _root.style.unityFont = new StyleFont(font);
        }

        // Content area (screens render here)
        _contentArea = new VisualElement();
        _contentArea.style.flexGrow   = 1;
        _contentArea.style.flexShrink = 1;
        _contentArea.style.overflow   = Overflow.Hidden;
        _root.Add(_contentArea);

        // Tab bar (loaded from UXML component template)
        var tabBarTemplate = Resources.Load<VisualTreeAsset>("UI/Components/TabBar");
        _tabBar = tabBarTemplate.Instantiate();
        _tabBar.style.paddingBottom = _bottomInset; // safe area
        BindTabBar();
        _root.Add(_tabBar);
    }

    // =========================================================================
    //  PanelSettings Recovery
    // =========================================================================
    void LateUpdate()
    {
        if (_doc != null && _doc.panelSettings == null && _panelSettings != null)
        {
            _doc.panelSettings = _panelSettings;
            Navigate(_currentScreen);
        }
    }

    // =========================================================================
    //  Navigation — load UXML template, bind data and events
    // =========================================================================
    void Navigate(AppScreen screen)
    {
        StopAllCoroutines();      // cancel any running timers/animations
        _currentScreen = screen;
        _contentArea.Clear();     // remove current screen

        // Load and instantiate the screen's UXML template
        var template = Resources.Load<VisualTreeAsset>($"UI/Screens/{screen}Screen");
        var screenElement = template.Instantiate();
        screenElement.style.flexGrow = 1;
        _contentArea.Add(screenElement);

        // Apply safe area top inset to screen content
        var content = screenElement.Q("screen-content");
        if (content != null)
            content.style.paddingTop = _topInset + 12;

        // Apply safe area to header bar (for stack screens)
        var header = screenElement.Q("header");
        if (header != null)
            header.style.paddingTop = _topInset + 8;

        // Show/hide tab bar
        bool showTabs = screen == AppScreen.Home
                     || screen == AppScreen.History
                     || screen == AppScreen.Settings;
        _tabBar.style.display = showTabs ? DisplayStyle.Flex : DisplayStyle.None;

        // Bind data and events for this screen
        switch (screen)
        {
            case AppScreen.Home:
                BindHomeScreen(screenElement);
                if (showTabs) UpdateTabHighlight(screen);
                break;
            // ... other screens ...
        }
    }

    // =========================================================================
    //  Tab Bar Binding
    // =========================================================================
    void BindTabBar()
    {
        _tabBar.Q<Button>("tab-home").clicked += () => Navigate(AppScreen.Home);
        _tabBar.Q<Button>("tab-history").clicked += () => Navigate(AppScreen.History);
        _tabBar.Q<Button>("tab-settings").clicked += () => Navigate(AppScreen.Settings);
    }

    void UpdateTabHighlight(AppScreen screen)
    {
        var tabNames = new[] { "tab-home", "tab-history", "tab-settings" };
        string activeTab = screen switch
        {
            AppScreen.Home     => "tab-home",
            AppScreen.History  => "tab-history",
            AppScreen.Settings => "tab-settings",
            _ => null
        };

        foreach (var name in tabNames)
        {
            var btn = _tabBar.Q<Button>(name);
            btn.EnableInClassList("active", name == activeTab);
        }
    }

    // =========================================================================
    //  Screen Data Binding (like React component rendering)
    // =========================================================================
    void BindHomeScreen(VisualElement screen)
    {
        // Query named elements and set their data
        var summary = AppData.GetSummary();
        var level = AppConstants.GetLevel(summary.AverageScore);

        screen.Q<Label>("hero-emoji").text = level.Emoji;
        screen.Q<Label>("hero-title").text = level.Label;
        screen.Q<Label>("hero-subtitle").text = level.Message;

        // Populate dynamic content using component templates
        var statsGrid = screen.Q("stats-grid");
        var statTemplate = Resources.Load<VisualTreeAsset>("UI/Components/StatCard");

        AddStat(statsGrid, statTemplate, summary.TotalTests.ToString(), "Tests");
        AddStat(statsGrid, statTemplate, $"{summary.AverageScore:F0}", "Average");

        // Bind button events
        screen.Q<Button>("btn-action").clicked += () => Navigate(AppScreen.TestSelect);
    }

    void AddStat(VisualElement container, VisualTreeAsset template, string value, string label)
    {
        var stat = template.Instantiate();
        stat.Q<Label>("stat-value").text = value;
        stat.Q<Label>("stat-label").text = label;
        container.Add(stat);
    }

    // =========================================================================
    //  Game Phase Management (for multi-phase screens)
    // =========================================================================
    void ShowPhase(VisualElement screen, string phaseName)
    {
        foreach (var phase in screen.Query(className: "test-phase").ToList())
            phase.AddToClassList("hidden");
        screen.Q(name: phaseName)?.RemoveFromClassList("hidden");
    }

    // =========================================================================
    //  Helpers
    // =========================================================================
    static Color FromHex(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out Color c);
        return c;
    }
}
```

### Key Points

- `Resources.Load<VisualTreeAsset>("UI/Screens/HomeScreen")` loads the UXML template at runtime
- `template.Instantiate()` creates a clone — like React component instantiation
- `screen.Q<Label>("hero-title")` queries by name — like `document.getElementById()`
- `EnableInClassList("active", condition)` toggles USS classes — triggers USS transitions
- `ShowPhase(screen, "phase-ready")` shows/hides UXML sections via CSS class toggling
- Global USS is loaded once on `_root` — cascades to all instantiated templates
- Safe area insets are applied dynamically in C# since they depend on runtime screen dimensions
- `StopAllCoroutines()` on every Navigate() prevents stale coroutine writes
- `ClickEvent` (not `PointerUpEvent`) for tap handling — more reliable on mobile

---

## Pattern 5: Tab Bar Component

### TabBar.uxml

```xml
<ui:UXML xmlns:ui="UnityEngine.UIElements">
    <ui:VisualElement class="tab-bar">
        <ui:Button name="tab-home" class="tab-btn">
            <ui:Label text="🏠" class="tab-icon" />
            <ui:Label text="Home" class="tab-label" />
        </ui:Button>
        <ui:Button name="tab-history" class="tab-btn">
            <ui:Label text="📊" class="tab-icon" />
            <ui:Label text="History" class="tab-label" />
        </ui:Button>
        <ui:Button name="tab-collection" class="tab-btn">
            <ui:Label text="🥚" class="tab-icon" />
            <ui:Label text="Collection" class="tab-label" />
        </ui:Button>
    </ui:VisualElement>
</ui:UXML>
```

The tab bar is loaded once in `SetupUI()` and persists across navigations. Active state is toggled via the `active` USS class. The tab bar is shown on tab screens and hidden on stack screens.

---

## Pattern 6: Dynamic Content from Templates

For lists, grids, and data-driven UI, load a component template and instantiate it per item:

```csharp
void BindHistoryScreen(VisualElement screen)
{
    var history = AppData.GetHistory();
    var listContainer = screen.Q("history-list");
    var itemTemplate = Resources.Load<VisualTreeAsset>("UI/Components/ListItem");

    foreach (var record in history)
    {
        var item = itemTemplate.Instantiate();
        item.Q<Label>("item-emoji").text = AppConstants.GetTestEmoji(record.TestType);
        item.Q<Label>("item-title").text = AppConstants.GetTestName(record.TestType);
        item.Q<Label>("item-subtitle").text = record.CreatedAt;
        item.Q<Label>("item-value").text = record.Score.ToString();

        // Make items tappable
        item.RegisterCallback<ClickEvent>(_ => ShowDetail(record));

        listContainer.Add(item);
    }

    // Show empty state if no data
    if (history.Count == 0)
    {
        screen.Q("empty-state")?.RemoveFromClassList("hidden");
    }
}
```

### 2-Column Grid from Templates

```csharp
void PopulateGrid(VisualElement container, VisualTreeAsset cardTemplate, List<ItemData> items)
{
    container.Clear();
    foreach (var item in items)
    {
        var card = cardTemplate.Instantiate();
        card.Q<Label>("card-title").text = item.Name;
        card.Q<Label>("card-value").text = item.Value;
        container.Add(card);
    }
}
```

---

## Pattern 7: Coroutines (replaces async/await + setTimeout + setInterval)

Use coroutines for timed, sequenced, or animated behavior.

```csharp
    IEnumerator RunReactionRound(VisualElement screen)
    {
        // Show waiting phase
        ShowPhase(screen, "phase-waiting");
        screen.Q("phase-waiting").style.backgroundColor = new StyleColor(FromHex("#FF5252"));

        // Random delay (2-5 seconds)
        float waitTime = UnityEngine.Random.Range(2f, 5f);
        yield return new WaitForSeconds(waitTime);

        // Show ready phase — green
        ShowPhase(screen, "phase-ready");
        screen.Q("phase-ready").style.backgroundColor = new StyleColor(FromHex("#66BB6A"));

        // Wait for tap (frame-by-frame loop)
        _tapped = false;
        float startTime = Time.time;

        while (!_tapped && (Time.time - startTime) < 4f)
        {
            yield return null; // wait one frame
        }

        float reactionMs = (Time.time - startTime) * 1000f;

        // Show feedback
        ShowPhase(screen, "phase-feedback");
        var feedbackLabel = screen.Q<Label>("feedback-text");
        if (feedbackLabel != null)
            feedbackLabel.text = _tapped ? $"{reactionMs:F0} ms" : "Too slow!";

        yield return new WaitForSeconds(1.2f);
    }
```

### Coroutine + Callback Communication

When a coroutine waits for user input, use shared fields. Both coroutines and UI callbacks run on Unity's main thread — no locking needed.

```csharp
    bool _tapped;

    void BindReactionTest(VisualElement screen)
    {
        screen.Q("phase-ready").RegisterCallback<ClickEvent>(_ => { _tapped = true; });
        screen.Q<Button>("btn-start").clicked += () => StartCoroutine(RunReactionSequence(screen));
    }
```

### Delayed Execution Without Coroutine

```csharp
    // Like setTimeout — runs once after delay
    element.schedule.Execute(() =>
    {
        element.AddToClassList("visible");
    }).StartingIn(300); // milliseconds

    // Like setInterval — runs repeatedly
    var handle = element.schedule.Execute(() => { /* update */ }).Every(500);
    handle.Pause();  // cancel
    handle.Resume(); // restart
```

### Transition Gotcha: 1-Frame Delay

Adding a USS class in the same frame that an element is created or shown won't trigger a transition — the start and end values are computed in the same layout pass. Use a short delay:

```csharp
    // Won't animate — class applied in same frame as creation:
    var panel = template.Instantiate();
    _contentArea.Add(panel);
    panel.AddToClassList("visible");

    // Will animate — 1-frame delay:
    var panel = template.Instantiate();
    _contentArea.Add(panel);
    panel.schedule.Execute(() =>
    {
        panel.AddToClassList("visible");
    }).StartingIn(50); // 50ms is enough for one layout pass
```

### Listening for Transition End

```csharp
    element.RegisterCallback<TransitionEndEvent>(evt =>
    {
        // Runs when any USS transition on this element completes
    });
```

---

## Pattern 8: Data Persistence (replaces AsyncStorage)

### Types (AppTypes.cs)

```csharp
using System;
using System.Collections.Generic;

[Serializable]
public class AppRecord
{
    public string id;
    public string record_type;
    public float  score;
    public string created_at;
}

[Serializable]
public class AppSaveData
{
    public List<AppRecord> records = new List<AppRecord>();
}
```

`JsonUtility` only serializes public fields — not properties, not `Dictionary<>`, not private fields.

### Persistence (AppData.cs)

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class AppData
{
    const string SAVE_KEY = "app_data_v1";
    static AppSaveData s_Data;

    public static AppSaveData Data
    {
        get { if (s_Data == null) Load(); return s_Data; }
    }

    static void Load()
    {
        string json = PlayerPrefs.GetString(SAVE_KEY, "");
        if (!string.IsNullOrEmpty(json))
        {
            try   { s_Data = JsonUtility.FromJson<AppSaveData>(json); }
            catch { s_Data = new AppSaveData(); }
        }
        else { s_Data = new AppSaveData(); }
    }

    public static void Save()
    {
        PlayerPrefs.SetString(SAVE_KEY, JsonUtility.ToJson(s_Data));
        PlayerPrefs.Save();
    }

    public static AppRecord AddRecord(string recordType, float score)
    {
        var rec = new AppRecord
        {
            id          = Guid.NewGuid().ToString(),
            record_type = recordType,
            score       = Mathf.Round(score),
            created_at  = DateTime.UtcNow.ToString("O")
        };
        Data.records.Add(rec);
        Save();
        return rec;
    }

    public static List<AppRecord> RecentRecords(int count = 20) =>
        Data.records.OrderByDescending(r => r.created_at).Take(count).ToList();
}
```

---

## Pattern 9: Constants (replaces Colors.ts + Config)

```csharp
using UnityEngine;

public static class AppConst
{
    // Colors used dynamically in C# (data-dependent backgrounds, level theming)
    // Static colors live in Theme.uss as custom properties — no need to duplicate here
    public const string COLOR_BG         = "#FFFBF0";
    public const string COLOR_PRIMARY    = "#FFB300";
    public const string COLOR_TEXT       = "#212121";
    public const string COLOR_SUCCESS    = "#66BB6A";
    public const string COLOR_DANGER     = "#FF5252";
}
```

Only keep C# constants for colors applied dynamically via inline style overrides (e.g., setting a card's background based on score/level data). Static colors live in USS.

---

## Pattern 10: Event Handling

### Button Clicks

```csharp
// Query button from UXML and bind event
screen.Q<Button>("btn-start").clicked += () => Navigate(AppScreen.Game);

// Or with RegisterCallback for more control
screen.Q<Button>("btn-start").RegisterCallback<ClickEvent>(evt =>
{
    StartGame();
});
```

Use `ClickEvent` for tap handling — it's more reliable on mobile than `PointerUpEvent`.

### Non-Button Element Taps

```csharp
var card = screen.Q("my-card");
card.RegisterCallback<ClickEvent>(_ => Navigate(AppScreen.Detail));
```

### Value Change (TextField, Slider, Toggle)

```csharp
var textField = screen.Q<TextField>("search-input");
textField.RegisterValueChangedCallback(evt => { string val = evt.newValue; });

var slider = screen.Q<Slider>("volume-slider");
slider.RegisterValueChangedCallback(evt => { float val = evt.newValue; });
```

### Cleanup

Callbacks registered on elements inside screen UXML templates are destroyed on `_contentArea.Clear()` — no manual unregistration needed.

Tab bar buttons persist across navigations — bound once in `BindTabBar()`.

---

## React Native → Unity Mapping

| React Native | Unity UI Toolkit |
|---|---|
| `<View>` | `<ui:VisualElement>` in UXML |
| `<View style={{flexDirection: 'row'}}>` | `class="row"` in UXML (USS: `flex-direction: row`) |
| `<Text>Hello</Text>` | `<ui:Label text="Hello" class="text-body" />` |
| `<Text style={{fontWeight: 'bold'}}>` | `class="text-bold"` |
| `<TouchableOpacity onPress={fn}>` | `<ui:Button>` + `clicked +=` in C# |
| `<TextInput>` | `<ui:TextField>` |
| `<ScrollView>` | `<ui:ScrollView>` |
| `<FlatList data={items}>` | `<ui:VisualElement name="list" />` + C# template loop |
| `StyleSheet.create({})` | `.uss` file with class selectors |
| `style={{ color: 'red' }}` | `class="my-class"` → USS `.my-class { color: red; }` |
| Dynamic inline style | C# `element.style.backgroundColor = ...` (rare) |
| `Colors.primary` | USS `var(--color-primary)` |
| `useState` | C# field + manual UI update |
| `useEffect` | `Start()`, `OnEnable()`, or rebuild on Navigate |
| `setTimeout` | `StartCoroutine()` or `schedule.Execute().StartingIn()` |
| `setInterval` | `schedule.Execute().Every()` |
| `AsyncStorage` | `PlayerPrefs` + `JsonUtility` |
| `navigation.navigate()` | `Navigate(AppScreen.Xxx)` |
| Tab navigator | Tab bar UXML component, show/hide with USS classes |
| Stack navigator | `Navigate()` with clear-and-rebuild |
| `Animated.Value` | USS `transition-*` or coroutine |
| `SafeAreaView` | Compute `_topInset`/`_bottomInset`, apply to content/tab bar |
| `:hover` / `:active` (web) | USS `:hover` / `:active` pseudo-classes |

---

## Common Recipes

### Scrollbar Hiding

Hide scrollbars on every `<ui:ScrollView>` using UXML attributes. USS class selectors like `.unity-scroller--vertical { display: none }` do not work — Unity's built-in theme has higher specificity and overrides them.

```xml
<ui:ScrollView class="screen-scroll"
    vertical-scroller-visibility="Hidden"
    horizontal-scroller-visibility="Hidden">
    <!-- content still scrollable via touch/drag -->
</ui:ScrollView>
```

Add both attributes to every ScrollView in every UXML file. This is per-element — there is no global setting.

### Modal Overlay (Bottom Sheet)

Create modal overlays as programmatic elements with USS classes — not inline C# styles. The modal has a semi-transparent backdrop and a white panel that slides up from the bottom.

```css
/* ── Modal styles (add to Components.uss) ── */
.modal-backdrop {
    position: absolute;
    top: 0;
    left: 0;
    right: 0;
    bottom: 0;
    background-color: rgba(0, 0, 0, 0.4);
}

.modal-panel {
    position: absolute;
    left: 0;
    right: 0;
    bottom: 0;
    background-color: var(--color-white);
    border-top-left-radius: 24px;
    border-top-right-radius: 24px;
    padding: 24px;
    max-height: 80%;
}

.modal-handle {
    width: 40px;
    height: 4px;
    background-color: var(--color-border);
    border-radius: 2px;
    align-self: center;
    margin-bottom: 16px;
}

.modal-title {
    font-size: var(--font-size-xl);
    -unity-font-style: bold;
    margin-bottom: 16px;
}

.picker-btn {
    width: 48px;
    height: 48px;
    border-radius: 24px;
    margin-right: 8px;
    align-items: center;
    justify-content: center;
    border-width: 2px;
    border-color: transparent;
}

.picker-btn.selected {
    border-color: var(--color-text);
}
```

Show/hide via `AddToClassList("hidden")` / `RemoveFromClassList("hidden")`.

### Horizontal ScrollView (like horizontal FlatList)

```xml
<ui:ScrollView mode="Horizontal" class="h-scroll"
    vertical-scroller-visibility="Hidden"
    horizontal-scroller-visibility="Hidden">
    <ui:VisualElement name="h-scroll-content" class="row" />
</ui:ScrollView>
```

```csharp
var row = screen.Q("h-scroll-content");
var barTemplate = Resources.Load<VisualTreeAsset>("UI/Components/BarColumn");
foreach (var dp in dataPoints)
{
    var bar = barTemplate.Instantiate();
    bar.Q("bar-fill").style.height = new Length(dp.Value, LengthUnit.Percent);
    row.Add(bar);
}
```

### Timer Bar Animation (Coroutine + Inline Style)

```csharp
IEnumerator AnimateTimerBar(VisualElement screen, float duration)
{
    var fill = screen.Q("timer-fill");
    float elapsed = 0f;

    while (elapsed < duration)
    {
        elapsed += Time.deltaTime;
        float pct = Mathf.Clamp01(1f - elapsed / duration) * 100f;
        if (fill != null)
        {
            fill.style.width = new Length(pct, LengthUnit.Percent);
            fill.style.backgroundColor = elapsed > duration * 0.6f
                ? new StyleColor(FromHex("#FF5252"))
                : new StyleColor(FromHex("#66BB6A"));
        }
        yield return null;
    }
}
```

### ListView for Long Lists (like FlatList with Virtualization)

For lists with many items (30+), use `ListView` instead of ScrollView + manual loop:

```xml
<ui:ListView name="history-list" fixed-item-height="60" />
```

```csharp
void BindHistoryWithListView(VisualElement screen)
{
    var listView = screen.Q<ListView>("history-list");
    var history = AppData.RecentRecords(50);

    listView.makeItem = () =>
    {
        var row = new VisualElement();
        row.AddToClassList("list-item");
        row.AddToClassList("card");

        var emoji = new Label();
        emoji.name = "item-emoji";
        emoji.AddToClassList("list-item-emoji");

        var info = new VisualElement();
        info.AddToClassList("grow");

        var title = new Label();
        title.name = "item-title";
        title.AddToClassList("text-body");
        title.AddToClassList("text-bold");

        var subtitle = new Label();
        subtitle.name = "item-subtitle";
        subtitle.AddToClassList("text-caption");

        info.Add(title);
        info.Add(subtitle);

        var value = new Label();
        value.name = "item-value";
        value.AddToClassList("list-item-value");

        row.Add(emoji);
        row.Add(info);
        row.Add(value);
        return row;
    };

    listView.bindItem = (element, index) =>
    {
        var record = history[index];
        element.Q<Label>("item-emoji").text = AppConstants.GetTestEmoji(record.TestType);
        element.Q<Label>("item-title").text = AppConstants.GetTestName(record.TestType);
        element.Q<Label>("item-subtitle").text = record.CreatedAt;
        element.Q<Label>("item-value").text = record.Score.ToString();
    };

    listView.itemsSource = history;
    listView.fixedItemHeight = 60;
    listView.selectionType = SelectionType.None;
}
```

ListView is optional. For short lists (<20 items), ScrollView + template instantiation loop (Pattern 6) is simpler.

### Empty State Pattern

```xml
<ui:VisualElement name="empty-state" class="card empty-state hidden">
    <ui:Label text="📭" class="empty-emoji" />
    <ui:Label text="No data yet" class="text-heading text-center" />
    <ui:Label text="Complete a test to see results" class="text-body text-center text-muted" />
</ui:VisualElement>
```

```csharp
if (items.Count == 0)
    screen.Q("empty-state")?.RemoveFromClassList("hidden");
```

---

## Conversion Checklist

Use this to verify a conversion is complete:

- [ ] Every source screen has a `.uxml` template and `BindXxxScreen()` method
- [ ] All colors from the source are in Theme.uss as `--color-*` custom properties (including border variants, tints, state colors)
- [ ] Typography scale defined as `--font-size-*` custom properties in Theme.uss
- [ ] Zero hardcoded hex color values in Components.uss or UXML inline styles — all use `var(--color-*)`
- [ ] All `<ui:ScrollView>` elements have `vertical-scroller-visibility="Hidden"` and `horizontal-scroller-visibility="Hidden"` attributes
- [ ] Circular elements (avatars, dots, badges) use equal `width`/`height` and pixel `border-radius` — no ovals
- [ ] Component templates exist for repeated items (list rows, stat cards, etc.)
- [ ] USS classes used for styling — minimal inline styles
- [ ] GameBootstrap uses `[RuntimeInitializeOnLoadMethod]`
- [ ] GameManager loads USS to root, loads UXML via Resources.Load
- [ ] Navigate() clears content area, instantiates new UXML, binds data/events
- [ ] PanelSettings: 390×844, match=0, HideAndDontSave, LateUpdate recovery
- [ ] ThemeStyleSheet loaded from Resources and assigned to PanelSettings
- [ ] Font loaded from Resources/Fonts/ and applied to root
- [ ] Every content screen wrapped in `<ui:ScrollView>` with hidden scroller visibility attributes
- [ ] Safe area insets applied to content/tab bar, not root
- [ ] StopAllCoroutines() called on every Navigate()
- [ ] Screen enum named `AppScreen` (not `Screen`)
- [ ] No font sizes below 12px
- [ ] All touch targets at least 44×44px
- [ ] Tab bar min-height at least 65px
- [ ] Primary buttons at least 50px tall
- [ ] `flex-direction: row` set explicitly for horizontal layouts
- [ ] Margins on children instead of gap
- [ ] Short labels in grids use `white-space: nowrap` + `overflow: hidden` + `text-overflow: ellipsis`
- [ ] Grid width math: `(items × width%) + (items × 2 × margin%) ≤ 100%`
- [ ] ClickEvent used for tap handling
- [ ] Show/hide game phases via AddToClassList/RemoveFromClassList
- [ ] Score calculations match source exactly
- [ ] Tab bar shows on tab screens, hides on stack screens
- [ ] All [Serializable] data classes use public fields (not properties)

---

## Do's and Don'ts

### Do

1. Use UXML for layout, USS for styling, C# only for logic
2. Use 390×844 reference resolution — RN values map directly
3. Load global USS on `_root` — cascades to all UXML templates
4. Use `AddToClassList` / `RemoveFromClassList` for state changes
5. Use `EnableInClassList("active", condition)` for conditional toggling
6. Compute safe area insets in `SetupUI()` — apply to content and tab bar
7. Use component templates for repeated items
8. Set `HideFlags.HideAndDontSave` on PanelSettings + `LateUpdate()` recovery
9. Call `StopAllCoroutines()` in Navigate()
10. Null-check UI elements in coroutines — user may navigate away
11. Use `ClickEvent` for tap handling
12. Copy `LiberationSans.ttf` to Resources/Fonts/ and load in SetupUI()
13. Load `UnityDefaultRuntimeTheme.tss` from Resources and assign to `_panelSettings.themeStyleSheet` in SetupUI()
14. Use `white-space: nowrap` on short labels in grid cards
14. Use 1-frame delay for entry transitions: `schedule.Execute(...).StartingIn(50)`
15. Verify grid math for flex-wrap layouts
16. Use equal `width` and `height` + pixel `border-radius` for circular elements (avatars, dots, badges)
17. Hide all scrollbars via UXML attributes on every `<ui:ScrollView>` (see Scrollbar Hiding recipe)  — mobile apps use touch scrolling, not scrollbars

### Don't

1. Don't build UI programmatically in C# — use UXML templates
2. Don't use inline C# styles for static design — put in USS
3. Don't use Canvas / uGUI — use UIDocument / UI Toolkit only
4. Don't use `[SerializeField]` or Inspector assignments — load from Resources
5. Don't put USS style references in UXML — global USS is loaded on root from C#
6. Don't use `gap` — use margins on children
7. Don't use `z-index` — reorder elements or use `BringToFront()`
8. Don't use `em`, `rem`, `vh`, `vw` units — only `px` and `%`
9. Don't use `display: grid` or `display: block` — only `flex` and `none`
10. Don't use 1080×1920 reference resolution — use 390×844
11. Don't add safe area padding to the root
12. Don't forget `[Serializable]` on data classes — JsonUtility silently returns empty objects
13. Don't use `:first-child`, `:last-child`, `:nth-child` in USS — not supported
14. Don't skip loading the Theme Style Sheet (`UnityDefaultRuntimeTheme.tss`) — causes "No Theme Style Sheet set to PanelSettings" warning and broken rendering
15. Don't skip the font copy step — Label text will be invisible
16. Don't use `white-space: normal` on short labels in grids — they'll wrap badly
17. Don't use unequal `width`/`height` for elements that should be circular — you'll get ovals
18. Don't use USS to hide scrollbars — use UXML visibility attributes instead (see Scrollbar Hiding recipe)

---

## References

- [Unity UI Toolkit Manual](https://docs.unity3d.com/Manual/UIElements.html)
- [UXML Format Reference](https://docs.unity3d.com/Manual/UIE-UXML.html)
- [USS Properties Reference](https://docs.unity3d.com/Manual/UIE-USS-Properties-Reference.html)
- [USS Selectors](https://docs.unity3d.com/Manual/UIE-USS-Selectors.html)
- [USS Transitions](https://docs.unity3d.com/Manual/UIE-Transitions.html)
- [Layout Engine (Flexbox)](https://docs.unity3d.com/Manual/UIE-LayoutEngine.html)
- [Runtime UI Guide](https://docs.unity3d.com/Manual/UIE-HowTo-CreateRuntimeUI.html)
- [PanelSettings](https://docs.unity3d.com/ScriptReference/UIElements.PanelSettings.html)
- [Event Handling](https://docs.unity3d.com/Manual/UIE-Events-Handling.html)
- [VisualElement API](https://docs.unity3d.com/ScriptReference/UIElements.VisualElement.html)
- [VisualTreeAsset API](https://docs.unity3d.com/ScriptReference/UIElements.VisualTreeAsset.html)
