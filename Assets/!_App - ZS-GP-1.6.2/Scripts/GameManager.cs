using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;

public class GameManager : MonoBehaviour
{
    // ── UI refs ──
    PanelSettings _panelSettings;
    UIDocument _doc;
    VisualElement _root;
    VisualElement _contentArea;
    VisualElement _tabBar;
    VisualElement _modal;

    // ── Runtime font (kept referenced so it is not garbage-collected) ──
    FontAsset _appFontAsset;

    // ── Safe area (reference px) ──
    float _topInset;

    // ── Navigation ──
    AppScreen _currentScreen = AppScreen.Today;

    // ── Vertical scrolls whose content currently fits the viewport (must not scroll) ──
    readonly HashSet<ScrollView> _fittingScrolls = new HashSet<ScrollView>();

    // ── Welcome state ──
    int _welcomePage;

    // ── Plinko board geometry (last built board) ──
    VisualElement _board;
    float _bW, _bH, _ballSize, _bTopInset, _bPegArea, _bBottomCells, _bRowGap;

    const int ROWS = 9;
    const int CELLS = 7;

    // =====================================================================
    //  Lifecycle
    // =====================================================================
    void Start()
    {
        SetupUI();
        Navigate(AppData.Data.onboarded ? AppScreen.Today : AppScreen.Welcome);
    }

    void LateUpdate()
    {
        if (_doc != null && _doc.panelSettings == null && _panelSettings != null)
        {
            _doc.panelSettings = _panelSettings;
            Navigate(_currentScreen);
        }
    }

    // =====================================================================
    //  Setup
    // =====================================================================
    void SetupUI()
    {
        _panelSettings = Resources.Load<PanelSettings>("UI/Panel Settings");

        _doc = gameObject.AddComponent<UIDocument>();
        _doc.panelSettings = _panelSettings;

        // Unity's Screen.safeArea uses a bottom-left origin, so safe.y is the
        // BOTTOM inset; the TOP (notch) inset is Screen.height - safe.yMax.
        // Floor at 44 (≈ iOS notch / Android status-bar height in reference px) so the device
        // notch never overlaps top content even when the platform under-reports safeArea (e.g.
        // the editor Game view returns the full screen). A real, larger reported inset still wins.
        float scale = 390f / Mathf.Max(1, Screen.width);
        var safe = Screen.safeArea;
        float reportedTop = (Screen.height - (safe.y + safe.height)) * scale;
        _topInset = Mathf.Max(reportedTop, 44f);

        _root = _doc.rootVisualElement;
        _root.style.flexDirection = FlexDirection.Column;
        _root.style.width = new Length(100, LengthUnit.Percent);
        _root.style.height = new Length(100, LengthUnit.Percent);

        _root.styleSheets.Add(Resources.Load<StyleSheet>("UI/Styles/Theme"));
        _root.styleSheets.Add(Resources.Load<StyleSheet>("UI/Styles/Components"));
        _root.AddToClassList("screen");

        ApplyRootFont();

        _contentArea = new VisualElement();
        _contentArea.style.flexGrow = 1;
        _contentArea.style.flexShrink = 1;
        _contentArea.style.overflow = Overflow.Hidden;
        _root.Add(_contentArea);
        InstallScrollGuard();

        _tabBar = Resources.Load<VisualTreeAsset>("UI/Components/TabBar").Instantiate();
        _tabBar.style.flexShrink = 0;
        BindTabBar();
        _root.Add(_tabBar);
    }

    // Give the panel a real TextCore font so Labels AND editable TextFields render
    // (an editable TextField NREs at runtime when the resolved font asset is null).
    // Latin glyphs come from the built-in font; emoji fall back to NotoEmoji-Bold.
    static FontAsset TryCreateFont(Font f)
    {
        if (f == null) return null;
        try { return FontAsset.CreateFontAsset(f); }
        catch { return null; }
    }

    void ApplyRootFont()
    {
        var emoji = Resources.Load<FontAsset>("Fonts/NotoEmoji-Bold SDF");

        // Build a Latin TextCore font asset. CreateFontAsset(Font) can return null on some
        // runtimes (this is exactly why the editable TextField NRE'd: the default font stayed
        // null). Try several strategies and use the first that yields a non-null asset.
        FontAsset latin = TryCreateFont(Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));
        if (latin == null) latin = TryCreateFont(Resources.GetBuiltinResource<Font>("Arial.ttf"));
        if (latin == null)
        {
            // OS-font-by-name overload — most reliable on desktop editor/player.
            try { latin = FontAsset.CreateFontAsset("Arial", "Regular"); } catch { latin = null; }
        }
        if (latin == null) latin = TryCreateFont(Font.CreateDynamicFontFromOSFont("Arial", 16));
        if (latin == null)
            Debug.LogWarning("[GameManager] Could not build a Latin FontAsset; " +
                             "falling back to emoji font for TextField default (Latin may render as boxes).");

        // Choose the panel-wide default. Prefer the Latin font; fall back to the emoji font so the
        // default is NEVER null (a null default font asset makes editable TextFields NRE on show).
        FontAsset def = latin != null ? latin : emoji;
        if (def == null) return; // nothing usable — leave the theme default in place

        // Make sure both Latin glyphs and emoji are reachable from the default font.
        if (latin != null && emoji != null)
            latin.fallbackFontAssetTable = new List<FontAsset> { emoji };

        _appFontAsset = def;

        // Labels inherit this font definition from the root.
        _root.style.unityFontDefinition = new StyleFontDefinition(FontDefinition.FromSDFFont(def));

        // CRITICAL: editable TextFields (e.g. the "New Task" input on the Add-Task sheet) resolve
        // their font from PanelSettings.textSettings.defaultFontAsset — NOT from the inherited
        // unityFontDefinition above. The Panel Text Settings asset ships with a null default font
        // asset, so the TextField's internal text input throws a NullReferenceException the moment
        // the modal is shown. Assign our font asset (and emoji fallback) as the panel default.
        var ts = _panelSettings != null ? _panelSettings.textSettings : null;
        if (ts != null)
        {
            ts.defaultFontAsset = def;
            if (emoji != null)
            {
                var fb = ts.fallbackFontAssets;
                if (fb == null) fb = new List<FontAsset>();
                if (!fb.Contains(emoji)) fb.Add(emoji);
                ts.fallbackFontAssets = fb;
            }
        }
    }

    // =====================================================================
    //  Navigation
    // =====================================================================
    void Navigate(AppScreen screen)
    {
        StopAllCoroutines();
        CloseModals();
        _currentScreen = screen;
        _fittingScrolls.Clear();
        _contentArea.Clear();

        var template = Resources.Load<VisualTreeAsset>($"UI/Screens/{screen}Screen");
        var el = template.Instantiate();
        el.style.flexGrow = 1;
        _contentArea.Add(el);

        // Disable phantom scrolling up-front: a screen only scrolls when its
        // content genuinely overflows the viewport (re-evaluated on each layout).
        ClampScreenScroll(el);

        var content = el.Q("screen-content");
        if (content != null) content.style.paddingTop = _topInset + 8f;

        bool showTabs = screen == AppScreen.Today || screen == AppScreen.Tomorrow
                     || screen == AppScreen.Stats || screen == AppScreen.Settings;
        _tabBar.style.display = showTabs ? DisplayStyle.Flex : DisplayStyle.None;

        switch (screen)
        {
            case AppScreen.Welcome: BindWelcomeScreen(el); break;
            case AppScreen.Today: BindTodayScreen(el); break;
            case AppScreen.Tomorrow: BindTomorrowScreen(el); break;
            case AppScreen.Stats: BindStatsScreen(el); break;
            case AppScreen.Settings: BindSettingsScreen(el); break;
            case AppScreen.Drop: BindDropScreen(el); break;
            case AppScreen.FullSprint: BindFullSprintScreen(el); break;
        }

        if (showTabs) UpdateTabHighlight(screen);
    }

    void BindTabBar()
    {
        _tabBar.Q<Button>("tab-today").clicked += () => Navigate(AppScreen.Today);
        _tabBar.Q<Button>("tab-tomorrow").clicked += () => Navigate(AppScreen.Tomorrow);
        _tabBar.Q<Button>("tab-stats").clicked += () => Navigate(AppScreen.Stats);
        _tabBar.Q<Button>("tab-settings").clicked += () => Navigate(AppScreen.Settings);
    }

    void UpdateTabHighlight(AppScreen screen)
    {
        var map = new Dictionary<string, AppScreen>
        {
            { "tab-today", AppScreen.Today },
            { "tab-tomorrow", AppScreen.Tomorrow },
            { "tab-stats", AppScreen.Stats },
            { "tab-settings", AppScreen.Settings },
        };
        foreach (var kv in map)
            _tabBar.Q<Button>(kv.Key).EnableInClassList("active", kv.Value == screen);
    }

    // =====================================================================
    //  Scroll guard — a screen only scrolls when its content overflows
    // =====================================================================
    void ClampScreenScroll(VisualElement screen)
    {
        foreach (var sv in screen.Query<ScrollView>().ToList())
        {
            sv.touchScrollBehavior = ScrollView.TouchScrollBehavior.Clamped;
            // Horizontal rows are meant to scroll sideways — leave them alone.
            if (sv.mode == ScrollViewMode.Horizontal) continue;
            ConfigureVerticalAutoScroll(sv);
        }
    }

    void ConfigureVerticalAutoScroll(ScrollView sv)
    {
        void Apply()
        {
            float content = sv.contentContainer.layout.height;
            float viewport = sv.contentViewport.layout.height;
            bool fits = content <= viewport + 1f;
            if (fits)
            {
                _fittingScrolls.Add(sv);
                if (sv.scrollOffset.y != 0f)
                    sv.scrollOffset = new Vector2(sv.scrollOffset.x, 0f);
            }
            else _fittingScrolls.Remove(sv);
        }

        sv.contentContainer.RegisterCallback<GeometryChangedEvent>(_ => Apply());
        sv.contentViewport.RegisterCallback<GeometryChangedEvent>(_ => Apply());

        // Direct counter-measure: whatever moves the scroll offset (a drag that
        // started on a button/field, momentum, scroll-to-focus, etc.), if the
        // content fits we snap it back to the top synchronously in the same
        // dispatch — so no movement is ever rendered (no flicker).
        if (sv.verticalScroller != null)
            sv.verticalScroller.valueChanged += _ =>
            {
                if (_fittingScrolls.Contains(sv) && sv.scrollOffset.y != 0f)
                    sv.scrollOffset = new Vector2(sv.scrollOffset.x, 0f);
            };
    }

    // Installed once on the persistent _contentArea. Because it sits above every
    // screen, its trickle-down handler runs before any ScrollView's own scroll
    // logic AND before interactive children that capture the pointer — so a drag
    // started on a button / text field is blocked too. Only scroll gestures whose
    // nearest ScrollView ancestor is a "fitting" vertical scroll are swallowed;
    // horizontal rows and genuinely overflowing screens are left alone.
    void InstallScrollGuard()
    {
        _contentArea.RegisterCallback<PointerMoveEvent>(e =>
        {
            if (ScrollGuardBlocks(e.target as VisualElement))
            { e.StopPropagation(); e.StopImmediatePropagation(); }
        }, TrickleDown.TrickleDown);

        _contentArea.RegisterCallback<WheelEvent>(e =>
        {
            if (ScrollGuardBlocks(e.target as VisualElement))
            { e.StopPropagation(); e.StopImmediatePropagation(); }
        }, TrickleDown.TrickleDown);
    }

    bool ScrollGuardBlocks(VisualElement target)
    {
        for (var e = target; e != null && e != _contentArea; e = e.parent)
            if (e is ScrollView sv)
                return _fittingScrolls.Contains(sv); // nearest scroll decides
        return false;
    }

    // =====================================================================
    //  Today
    // =====================================================================
    void BindTodayScreen(VisualElement el)
    {
        var data = AppData.Data;
        bool evening = !AppConstants.IsMorning();
        int done = data.tasks.Count(t => t.done);
        int total = data.tasks.Count;
        var fmt = AppConstants.GetFormat(data.dayFormat);

        if (fmt != null && AppConstants.ComputeCleared(fmt.id, total, done))
            AppData.RecordDayClear(fmt.id);

        el.Q<Label>("today-kicker").text = (evening ? "Evening" : "Morning") + " session";
        el.Q<Label>("today-title").text = fmt != null ? fmt.title : "Today";

        string requiredText = fmt != null
            ? (fmt.required == -1 ? $"{done}/{total} (all)" : $"{done}/{fmt.required}")
            : $"{done}/{total}";
        el.Q<Label>("today-progress").text = requiredText;

        var formatSlot = el.Q("today-format-slot");
        var boardSlot = el.Q("today-board-slot");
        formatSlot.Clear();
        boardSlot.Clear();
        if (fmt != null)
        {
            boardSlot.style.display = DisplayStyle.None;
            formatSlot.style.display = DisplayStyle.Flex;
            BuildFormatBadge(formatSlot, fmt);
        }
        else
        {
            formatSlot.style.display = DisplayStyle.None;
            boardSlot.style.display = DisplayStyle.Flex;
            float bw = 350f;
            BuildPlinkoBoard(boardSlot, bw, bw * 0.7f, -1, false);
        }

        var cta = el.Q<Button>("today-cta");
        var ctaIcon = el.Q<Label>("today-cta-icon");
        var ctaText = el.Q<Label>("today-cta-text");
        cta.RemoveFromClassList("cta-disabled");
        cta.RemoveFromClassList("cta-sprint");
        cta.SetEnabled(true);

        if (fmt != null && fmt.id == "full-sprint")
        {
            cta.AddToClassList("cta-sprint");
            ctaIcon.text = "🔥";
            ctaText.text = "Full Sprint";
            cta.clicked += () => Navigate(AppScreen.FullSprint);
        }
        else if (fmt != null)
        {
            cta.AddToClassList("cta-disabled");
            ctaIcon.text = "✔";
            ctaText.text = "Day Format Set";
            cta.SetEnabled(false);
        }
        else
        {
            ctaIcon.text = "🚀";
            ctaText.text = "Drop for Today";
            cta.clicked += () => Navigate(AppScreen.Drop);
        }

        var add = el.Q<Button>("today-add");
        add.SetEnabled(total < 10);
        add.clicked += () => ShowAddTask(false);

        el.Q<Label>("today-section").text = $"Tasks ({total}/10)";

        var list = el.Q("today-list");
        list.Clear();
        foreach (var t in data.tasks) AddTaskCard(list, t, evening);
        el.Q("today-empty").style.display = total == 0 ? DisplayStyle.Flex : DisplayStyle.None;
    }

    // =====================================================================
    //  Tomorrow
    // =====================================================================
    void BindTomorrowScreen(VisualElement el)
    {
        var data = AppData.Data;
        var boardSlot = el.Q("tomorrow-board-slot");
        boardSlot.Clear();
        BuildPlinkoBoard(boardSlot, 350f, 350f * 0.7f, -1, false);

        el.Q<Button>("tomorrow-add").clicked += () => ShowAddTask(true);
        el.Q<Label>("tomorrow-section").text = $"Staged Tasks ({data.tomorrowTasks.Count})";

        var list = el.Q("tomorrow-list");
        list.Clear();
        foreach (var t in data.tomorrowTasks) AddStagedRow(list, t);
        el.Q("tomorrow-empty").style.display =
            data.tomorrowTasks.Count == 0 ? DisplayStyle.Flex : DisplayStyle.None;
    }

    // =====================================================================
    //  Stats
    // =====================================================================
    void BindStatsScreen(VisualElement el)
    {
        var data = AppData.Data;
        el.Q<Label>("stat-day-clear").text = data.badges.dayClearCount.ToString();
        el.Q<Label>("stat-streak").text = data.streak.ToString();
        el.Q<Label>("stat-sprints").text = data.badges.fullSprintWins.ToString();

        SetBadge(el, "badge-day-clear", data.badges.dayClearCount > 0);
        SetBadge(el, "badge-on-fire", data.badges.onFire);
        SetBadge(el, "badge-week-champion", data.badges.weekChampion);

        var hist = el.Q("stats-history");
        hist.Clear();
        foreach (var h in data.history.Take(30)) AddHistoryRow(hist, h);
        el.Q("stats-history-empty").style.display =
            data.history.Count == 0 ? DisplayStyle.Flex : DisplayStyle.None;
    }

    void SetBadge(VisualElement el, string name, bool unlocked)
    {
        var tile = el.Q(name);
        tile.EnableInClassList("badge-locked", !unlocked);
        var lockLabel = el.Q<Label>(name + "-lock");
        if (lockLabel != null)
            lockLabel.style.display = unlocked ? DisplayStyle.None : DisplayStyle.Flex;
    }

    void AddHistoryRow(VisualElement list, DayRecord h)
    {
        var inst = Resources.Load<VisualTreeAsset>("UI/Components/HistoryRow").Instantiate();
        var fmt = AppConstants.GetFormat(h.formatId);
        inst.Q("history-dot").style.backgroundColor =
            AppConstants.FromHex(fmt != null ? fmt.colorHex : AppConstants.STEADY);
        inst.Q<Label>("history-title").text = fmt != null ? fmt.title : h.formatId;
        inst.Q<Label>("history-date").text = h.date;
        inst.Q<Label>("history-tasks").text = $"{h.doneTasks}/{h.totalTasks}";
        var st = inst.Q<Label>("history-status");
        if (h.cleared)
        {
            st.text = "✔";
            st.style.color = AppConstants.FromHex(AppConstants.QUICK);
        }
        else
        {
            st.text = "○";
            st.style.color = AppConstants.FromHex(AppConstants.COMPLETED);
        }
        list.Add(inst);
    }

    // =====================================================================
    //  Settings
    // =====================================================================
    void BindSettingsScreen(VisualElement el)
    {
        var s = AppData.Data.settings;
        el.Q<Label>("setting-morning-value").text = s.morningDrop;
        el.Q<Label>("setting-sprint-value").text = s.sprintDeadline;
        el.Q<Label>("setting-evening-value").text = s.eveningReminder;

        el.Q<Button>("setting-morning").clicked += () => ShowTimePicker("morningDrop");
        el.Q<Button>("setting-sprint").clicked += () => ShowTimePicker("sprintDeadline");
        el.Q<Button>("setting-evening").clicked += () => ShowTimePicker("eveningReminder");

        var easy = el.Q<Button>("setting-risk-easy");
        var hard = el.Q<Button>("setting-risk-hardcore");
        UpdateRiskChips(easy, hard, s.riskLevel);
        easy.clicked += () => { AppData.UpdateSetting("riskLevel", "easy"); UpdateRiskChips(easy, hard, "easy"); };
        hard.clicked += () => { AppData.UpdateSetting("riskLevel", "hardcore"); UpdateRiskChips(easy, hard, "hardcore"); };

        el.Q<Button>("setting-reset").clicked += ShowConfirmReset;
    }

    void UpdateRiskChips(Button easy, Button hard, string risk)
    {
        easy.RemoveFromClassList("risk-chip-easy-active");
        easy.RemoveFromClassList("risk-chip-active");
        hard.RemoveFromClassList("risk-chip-hardcore-active");
        hard.RemoveFromClassList("risk-chip-active");
        if (risk == "easy")
        {
            easy.AddToClassList("risk-chip-easy-active");
            easy.AddToClassList("risk-chip-active");
        }
        else
        {
            hard.AddToClassList("risk-chip-hardcore-active");
            hard.AddToClassList("risk-chip-active");
        }
    }

    // =====================================================================
    //  Drop
    // =====================================================================
    void BindDropScreen(VisualElement el)
    {
        var s = AppData.Data.settings;
        float bw = 358f;        // 390 ref width - 32 horizontal padding
        float bh = bw * 1.2f;
        BuildPlinkoBoard(el.Q("drop-board-slot"), bw, bh, -1, false);

        ShowDropStage(el, "ready");

        var easy = el.Q<Button>("drop-risk-easy");
        var hard = el.Q<Button>("drop-risk-hardcore");
        UpdateRiskChips(easy, hard, s.riskLevel);
        easy.clicked += () => { AppData.UpdateSetting("riskLevel", "easy"); UpdateRiskChips(easy, hard, "easy"); };
        hard.clicked += () => { AppData.UpdateSetting("riskLevel", "hardcore"); UpdateRiskChips(easy, hard, "hardcore"); };

        el.Q<Button>("drop-close").clicked += () => Navigate(AppScreen.Today);
        el.Q<Button>("drop-ball").clicked += () => StartCoroutine(DoDrop(el));
    }

    void ShowDropStage(VisualElement el, string stage)
    {
        el.Q("drop-ready").style.display = stage == "ready" ? DisplayStyle.Flex : DisplayStyle.None;
        el.Q("drop-dropping").style.display = stage == "dropping" ? DisplayStyle.Flex : DisplayStyle.None;
        el.Q("drop-landed").style.display = stage == "landed" ? DisplayStyle.Flex : DisplayStyle.None;
    }

    IEnumerator DoDrop(VisualElement el)
    {
        var s = AppData.Data.settings;
        int cell = AppConstants.PickTargetCell(s.riskLevel);
        ShowDropStage(el, "dropping");

        float width = _bW, height = _bH, ballSize = _ballSize;
        float topInset = _bTopInset, pegArea = _bPegArea, bottomCells = _bBottomCells, rowGap = _bRowGap;
        float cellWidth = width / CELLS;

        float startX = width / 2f - ballSize / 2f;
        float startY = topInset - ballSize;
        float endX = cell * cellWidth + (cellWidth - ballSize) / 2f;
        float endY = topInset + pegArea + bottomCells - ballSize - 8f;

        var xs = new List<float>();
        var ys = new List<float>();
        for (int r = 0; r <= ROWS; r++)
        {
            float t = (r + 1f) / (ROWS + 1f);
            float lx = startX + (endX - startX) * t;
            float amp = (1f - t) * cellWidth * 0.6f;
            float jit = (UnityEngine.Random.value - 0.5f) * 2f * amp;
            xs.Add(lx + jit);
            ys.Add(topInset + (r + 1) * rowGap - ballSize / 2f);
        }
        xs.Add(endX);
        ys.Add(endY);

        var ball = new VisualElement();
        ball.AddToClassList("plinko-ball");
        ball.style.width = ballSize;
        ball.style.height = ballSize;
        SetBorderRadius(ball, ballSize / 2f);
        var clock = new Label("🕐");
        clock.AddToClassList("plinko-ball-icon");
        ball.Add(clock);
        ball.style.left = startX;
        ball.style.top = startY;
        if (_board != null) _board.Add(ball);

        float prevX = startX, prevY = startY;
        for (int i = 0; i < xs.Count; i++)
        {
            float tx = xs[i], ty = ys[i];
            float dur = 0.2f;
            float t0 = 0f;
            while (t0 < dur)
            {
                t0 += Time.deltaTime;
                float k = Mathf.Clamp01(t0 / dur);
                ball.style.left = Mathf.Lerp(prevX, tx, k);
                ball.style.top = Mathf.Lerp(prevY, ty, k);
                yield return null;
            }
            prevX = tx;
            prevY = ty;
        }

        var fmt = AppConstants.FormatForCell(cell);
        AppData.SetDayFormat(fmt.id);
        bool isSprint = fmt.id == "full-sprint";

        BuildPlinkoBoard(el.Q("drop-board-slot"), width, height, cell, isSprint);

        var slot = el.Q("drop-format-slot");
        slot.Clear();
        BuildFormatBadge(slot, fmt);

        var cont = el.Q<Button>("drop-continue");
        var ci = el.Q<Label>("drop-continue-icon");
        var ct = el.Q<Label>("drop-continue-text");
        if (isSprint)
        {
            cont.style.backgroundColor = AppConstants.FromHex(AppConstants.SPRINT);
            ci.style.color = AppConstants.FromHex(AppConstants.WHITE);
            ct.style.color = AppConstants.FromHex(AppConstants.WHITE);
            ci.text = "🔥";
            ct.text = "Start Full Sprint";
        }
        else
        {
            cont.style.backgroundColor = AppConstants.FromHex(AppConstants.ACCENT);
            ci.style.color = AppConstants.FromHex(AppConstants.TEXT_INVERSE);
            ct.style.color = AppConstants.FromHex(AppConstants.TEXT_INVERSE);
            ci.text = "✔";
            ct.text = "Let's go";
        }
        cont.clicked += () => Navigate(isSprint ? AppScreen.FullSprint : AppScreen.Today);

        ShowDropStage(el, "landed");
    }

    // =====================================================================
    //  Full Sprint
    // =====================================================================
    void BindFullSprintScreen(VisualElement el)
    {
        var data = AppData.Data;
        var s = data.settings;
        int done = data.tasks.Count(t => t.done);
        int total = data.tasks.Count;
        bool cleared = data.dayFormat == "full-sprint"
                       && AppConstants.ComputeCleared("full-sprint", total, done);
        if (cleared) AppData.RecordDayClear("full-sprint");

        el.Q<Button>("sprint-close").clicked += () => Navigate(AppScreen.Today);

        float pct = total == 0 ? 0f : Mathf.Min(100f, (float)done / total * 100f);
        el.Q("sprint-progress-fill").style.width = new Length(pct, LengthUnit.Percent);
        el.Q<Label>("sprint-progress-text").text = $"{done}/{total} tasks complete";

        el.Q("sprint-cleared").style.display = cleared ? DisplayStyle.Flex : DisplayStyle.None;

        var list = el.Q("sprint-list");
        list.Clear();
        foreach (var t in data.tasks) AddTaskCard(list, t, true);
        el.Q("sprint-empty").style.display = total == 0 ? DisplayStyle.Flex : DisplayStyle.None;

        var label = el.Q<Label>("sprint-timer-label");
        var timer = el.Q<Label>("sprint-timer");
        StartCoroutine(SprintCountdown(label, timer, s.sprintDeadline));
    }

    IEnumerator SprintCountdown(Label label, Label timer, string deadline)
    {
        while (true)
        {
            int secs = AppConstants.SecondsUntilHHmm(deadline);
            bool expired = secs <= 0;
            if (label != null)
                label.text = expired ? "Sprint window closed" : $"Finish all by {deadline}";
            if (timer != null)
            {
                timer.text = AppConstants.FmtCountdown(secs);
                timer.style.color = AppConstants.FromHex(expired ? AppConstants.TEXT_SECONDARY : "#FCA5A5");
            }
            yield return new WaitForSeconds(1f);
        }
    }

    // =====================================================================
    //  Welcome (onboarding)
    // =====================================================================
    static readonly string[] WTitle = { "Add Your Tasks", "Drop the Ball", "Hit Full Sprint" };
    static readonly string[] WBody =
    {
        "Drop in up to 10 things you want to get done today.",
        "The ball picks your day format: Steady, Quick Win, Deep Work, or Full Sprint.",
        "Close every task before 6 PM and earn the Day Clear badge.",
    };
    static readonly string[] WColor = { "#3B82F6", "#10B981", "#EF4444" };
    static readonly int[] WTarget = { -1, 3, 0 };

    void BindWelcomeScreen(VisualElement el)
    {
        _welcomePage = 0;
        RenderWelcomeSlide(el);
        el.Q<Button>("welcome-next").clicked += () =>
        {
            if (_welcomePage < 2)
            {
                _welcomePage++;
                RenderWelcomeSlide(el);
            }
            else
            {
                AppData.MarkOnboarded();
                Navigate(AppScreen.Today);
            }
        };
    }

    void RenderWelcomeSlide(VisualElement el)
    {
        int p = _welcomePage;
        var boardSlot = el.Q("welcome-board-slot");
        boardSlot.Clear();
        float bw = 310f;
        BuildPlinkoBoard(boardSlot, bw, bw * 1.1f, WTarget[p], p == 2, WTarget[p] >= 0);

        var badge = el.Q("welcome-badge");
        badge.style.backgroundColor = AppConstants.FromHex(WColor[p]);
        el.Q<Label>("welcome-badge-text").text = $"Step {p + 1} of 3";
        el.Q<Label>("welcome-title").text = WTitle[p];
        el.Q<Label>("welcome-body").text = WBody[p];

        for (int i = 0; i < 3; i++)
            el.Q("welcome-dot-" + i).EnableInClassList("dot-active", i == p);

        el.Q<Label>("welcome-next-text").text = p == 2 ? "Plan My Day" : "Next";
    }

    // =====================================================================
    //  Reusable builders
    // =====================================================================
    void AddTaskCard(VisualElement list, TaskItem t, bool evening)
    {
        var card = Resources.Load<VisualTreeAsset>("UI/Components/TaskCard").Instantiate();
        var root = card.Q("task-card");
        Color prio = AppConstants.PriorityColor(t.priority);
        Color border = t.done ? AppConstants.FromHex(AppConstants.COMPLETED) : prio;

        root.style.backgroundColor = AppConstants.FromHex(evening ? AppConstants.TASK_EVENING : AppConstants.TASK_MORNING);
        root.style.borderLeftColor = border;

        var cb = card.Q<Button>("task-checkbox");
        SetBorderColor(cb, border);
        cb.style.backgroundColor = t.done ? border : Color.clear;
        card.Q<Label>("task-check-icon").style.display = t.done ? DisplayStyle.Flex : DisplayStyle.None;

        var title = card.Q<Label>("task-title");
        title.text = t.title;
        title.EnableInClassList("task-title-done", t.done);

        card.Q("task-tag").style.backgroundColor = AppConstants.WithAlpha(prio, 0.2f);
        var tagText = card.Q<Label>("task-tag-text");
        tagText.text = AppConstants.PriorityLabel(t.priority);
        tagText.style.color = prio;

        string id = t.id;
        cb.clicked += () => { AppData.ToggleTask(id); Navigate(_currentScreen); };
        card.Q<Button>("task-delete").clicked += () => { AppData.RemoveTask(id); Navigate(_currentScreen); };

        list.Add(card);
    }

    void AddStagedRow(VisualElement list, TaskItem t)
    {
        var inst = Resources.Load<VisualTreeAsset>("UI/Components/StagedRow").Instantiate();
        inst.Q<Label>("staged-title").text = t.title;
        string id = t.id;
        inst.Q<Button>("staged-delete").clicked += () => { AppData.RemoveTomorrowTask(id); Navigate(_currentScreen); };
        list.Add(inst);
    }

    void BuildFormatBadge(VisualElement slot, DayFormat fmt)
    {
        var inst = Resources.Load<VisualTreeAsset>("UI/Components/DayFormatBadge").Instantiate();
        Color c = AppConstants.FromHex(fmt.colorHex);
        var card = inst.Q("format-card");
        SetBorderColor(card, c);
        card.style.backgroundColor = AppConstants.WithAlpha(c, 0.13f);
        inst.Q("format-icon-wrap").style.backgroundColor = c;
        inst.Q<Label>("format-icon").text = fmt.emoji;
        var title = inst.Q<Label>("format-title");
        title.text = fmt.title;
        title.style.color = c;
        inst.Q<Label>("format-desc").text = fmt.description;
        slot.Add(inst);
    }

    void BuildPlinkoBoard(VisualElement slot, float width, float height, int targetCell, bool redAlarm, bool staticBall = false)
    {
        slot.Clear();

        float ballSize = Mathf.Max(22f, Mathf.Round(width * 0.07f));
        float pegSize = Mathf.Max(8f, Mathf.Round(width * 0.022f));
        float topInset = 24f;
        float bottomCellsHeight = Mathf.Max(64f, height * 0.13f);
        float pegArea = height - topInset - bottomCellsHeight - 12f;
        float rowGap = pegArea / (ROWS + 1);

        var board = new VisualElement();
        board.AddToClassList("plinko-board");
        board.style.width = width;
        board.style.height = height;
        board.style.backgroundColor = AppConstants.FromHex(redAlarm ? AppConstants.BG_BOARD_RED : AppConstants.BG_BOARD);

        for (int r = 0; r < ROWS; r++)
        {
            int count = r + 3;
            float totalSpan = (count - 1) * rowGap * 1.05f;
            float startX = (width - totalSpan) / 2f;
            float y = topInset + (r + 1) * rowGap;
            for (int c = 0; c < count; c++)
            {
                float x = startX + c * rowGap * 1.05f;
                var peg = new VisualElement();
                peg.AddToClassList("plinko-peg");
                peg.style.width = pegSize;
                peg.style.height = pegSize;
                SetBorderRadius(peg, pegSize / 2f);
                peg.style.left = x - pegSize / 2f;
                peg.style.top = y - pegSize / 2f;
                board.Add(peg);
            }
        }

        var cellsRow = new VisualElement();
        cellsRow.style.position = Position.Absolute;
        cellsRow.style.left = 0;
        cellsRow.style.right = 0;
        cellsRow.style.bottom = 0;
        cellsRow.style.height = bottomCellsHeight;
        cellsRow.style.flexDirection = FlexDirection.Row;
        for (int idx = 0; idx < CELLS; idx++)
        {
            var f = AppConstants.Formats[AppConstants.CELL_FORMATS[idx]];
            bool isTarget = targetCell == idx;
            var cell = new VisualElement();
            cell.AddToClassList("plinko-cell");
            cell.style.flexGrow = 1;
            cell.style.flexBasis = 0;
            cell.style.marginLeft = 2;
            cell.style.marginRight = 2;
            cell.style.height = new Length(100, LengthUnit.Percent);
            cell.style.backgroundColor = AppConstants.FromHex(f.colorHex);
            cell.style.opacity = isTarget ? 1f : 0.75f;
            if (isTarget)
            {
                cell.style.borderTopWidth = 3;
                cell.style.borderTopColor = AppConstants.FromHex(AppConstants.WHITE);
            }
            cellsRow.Add(cell);
        }
        board.Add(cellsRow);

        // Static "waiting" ball at top-center (welcome slides — mirrors Source PlinkoBoard
        // with animateDrop=false: the ball rests above the pegs at the top center).
        if (staticBall)
        {
            var sball = new VisualElement();
            sball.AddToClassList("plinko-ball");
            sball.style.width = ballSize;
            sball.style.height = ballSize;
            SetBorderRadius(sball, ballSize / 2f);
            var sicon = new Label("\U0001F550");
            sicon.AddToClassList("plinko-ball-icon");
            sball.Add(sicon);
            sball.style.left = width / 2f - ballSize / 2f;
            sball.style.top = topInset - ballSize;
            board.Add(sball);
        }

        slot.Add(board);

        _board = board;
        _bW = width; _bH = height; _ballSize = ballSize;
        _bTopInset = topInset; _bPegArea = pegArea; _bBottomCells = bottomCellsHeight; _bRowGap = rowGap;
    }

    // =====================================================================
    //  Modals
    // =====================================================================
    void CloseModals()
    {
        if (_modal != null)
        {
            _modal.RemoveFromHierarchy();
            _modal = null;
        }
    }

    VisualElement MountModal(string component)
    {
        CloseModals();
        var vta = Resources.Load<VisualTreeAsset>("UI/Components/" + component);
        if (vta == null)
        {
            Debug.LogError($"[GameManager] Modal UXML not found: UI/Components/{component}");
            return new VisualElement();
        }
        _modal = vta.Instantiate();
        _modal.style.position = Position.Absolute;
        _modal.style.left = 0;
        _modal.style.right = 0;
        _modal.style.top = 0;
        _modal.style.bottom = 0;
        _root.Add(_modal);
        return _modal;
    }

    void ShowAddTask(bool tomorrow)
    {
        var m = MountModal("AddTaskSheet");
        string prio = "med";

        var low = m.Q<Button>("addtask-prio-low");
        var med = m.Q<Button>("addtask-prio-med");
        var high = m.Q<Button>("addtask-prio-high");
        void Refresh()
        {
            StylePriorityChip(low, "low", prio == "low");
            StylePriorityChip(med, "med", prio == "med");
            StylePriorityChip(high, "high", prio == "high");
        }
        low.clicked += () => { prio = "low"; Refresh(); };
        med.clicked += () => { prio = "med"; Refresh(); };
        high.clicked += () => { prio = "high"; Refresh(); };
        Refresh();

        var input = m.Q<TextField>("addtask-input");
        m.Q<Button>("addtask-cancel").clicked += CloseModals;
        m.Q<Button>("addtask-submit").clicked += () =>
        {
            string txt = (input.value ?? "").Trim();
            if (string.IsNullOrEmpty(txt)) return;
            if (tomorrow) AppData.AddTomorrowTask(txt, prio);
            else AppData.AddTask(txt, prio);
            CloseModals();
            Navigate(_currentScreen);
        };

        var backdrop = m.Q("addtask-backdrop");
        backdrop.RegisterCallback<ClickEvent>(e => { if (e.target == backdrop) CloseModals(); });
    }

    void StylePriorityChip(Button chip, string prio, bool active)
    {
        Color c = AppConstants.PriorityColor(prio);
        SetBorderColor(chip, c);
        chip.style.backgroundColor = active ? c : Color.clear;
        var lbl = chip.Q<Label>();
        if (lbl != null)
            lbl.style.color = active ? AppConstants.FromHex(AppConstants.TEXT_INVERSE) : c;
    }

    void ShowTimePicker(string key)
    {
        var m = MountModal("TimePicker");
        var list = m.Q("timepicker-list");
        foreach (var time in AppConstants.TIMES)
        {
            string tv = time;
            var b = new Button();
            b.AddToClassList("time-row");
            var l = new Label(time);
            l.AddToClassList("time-text");
            b.Add(l);
            b.clicked += () => { AppData.UpdateSetting(key, tv); CloseModals(); Navigate(_currentScreen); };
            list.Add(b);
        }
        var backdrop = m.Q("timepicker-backdrop");
        backdrop.RegisterCallback<ClickEvent>(e => { if (e.target == backdrop) CloseModals(); });
    }

    void ShowConfirmReset()
    {
        var m = MountModal("ConfirmReset");
        m.Q<Button>("confirm-cancel").clicked += CloseModals;
        m.Q<Button>("confirm-reset").clicked += () =>
        {
            AppData.ResetAll();
            CloseModals();
            Navigate(AppScreen.Today);
        };
        var backdrop = m.Q("confirm-backdrop");
        backdrop.RegisterCallback<ClickEvent>(e => { if (e.target == backdrop) CloseModals(); });
    }

    // =====================================================================
    //  Style helpers (IStyle has no shorthand border properties)
    // =====================================================================
    void SetBorderColor(VisualElement e, Color c)
    {
        e.style.borderTopColor = c;
        e.style.borderRightColor = c;
        e.style.borderBottomColor = c;
        e.style.borderLeftColor = c;
    }

    void SetBorderRadius(VisualElement e, float r)
    {
        e.style.borderTopLeftRadius = r;
        e.style.borderTopRightRadius = r;
        e.style.borderBottomLeftRadius = r;
        e.style.borderBottomRightRadius = r;
    }
}
