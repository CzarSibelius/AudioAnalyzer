using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Application.Display;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Console;

/// <summary>Modal to pick <see cref="UiSettings.UiThemeId"/> from <c>themes/</c>, (Custom), or author a new theme.</summary>
internal sealed class UiThemeSelectionModal : IUiThemeSelectionModal
{
    /// <summary>Matches settings overlay idle palette updates: batched console output to reduce tear.</summary>
    private const string SyncOutputBegin = "\x1b[?2026h";
    private const string SyncOutputEnd = "\x1b[?2026l";

    private static readonly string[] s_slotLabels =
    [
        "UI Normal",
        "UI Highlighted",
        "UI Dimmed",
        "UI Label",
        "UI Background",
        "Title AppName",
        "Title Mode",
        "Title Preset",
        "Title Layer",
        "Title Separator",
        "Title Frame"
    ];

    private readonly IPaletteRepository _paletteRepo;
    private readonly IUiThemeRepository _themeRepo;
    private readonly IKeyHandler<UiThemeSelectionKeyContext> _keyHandler;
    private readonly UiSettings _uiSettings;
    private readonly IUiThemeResolver _uiThemeResolver;
    private readonly IConsoleDimensions _consoleDimensions;
    private readonly ITitleBarNavigationContext _navigation;
    private readonly ITitleBarBreadcrumbFormatter _breadcrumbFormatter;

    /// <summary>Initializes a new instance of the <see cref="UiThemeSelectionModal"/> class.</summary>
    public UiThemeSelectionModal(
        IPaletteRepository paletteRepo,
        IUiThemeRepository themeRepo,
        IKeyHandler<UiThemeSelectionKeyContext> keyHandler,
        UiSettings uiSettings,
        IUiThemeResolver uiThemeResolver,
        IConsoleDimensions consoleDimensions,
        ITitleBarNavigationContext navigation,
        ITitleBarBreadcrumbFormatter breadcrumbFormatter)
    {
        _paletteRepo = paletteRepo ?? throw new ArgumentNullException(nameof(paletteRepo));
        _themeRepo = themeRepo ?? throw new ArgumentNullException(nameof(themeRepo));
        _keyHandler = keyHandler ?? throw new ArgumentNullException(nameof(keyHandler));
        _uiSettings = uiSettings ?? throw new ArgumentNullException(nameof(uiSettings));
        _uiThemeResolver = uiThemeResolver ?? throw new ArgumentNullException(nameof(uiThemeResolver));
        _consoleDimensions = consoleDimensions ?? throw new ArgumentNullException(nameof(consoleDimensions));
        _navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
        _breadcrumbFormatter = breadcrumbFormatter ?? throw new ArgumentNullException(nameof(breadcrumbFormatter));
    }

    /// <inheritdoc />
    public void Show(Action<bool> setModalOpen, Func<AudioAnalysisSnapshot> getAnalysis, Action saveSettings)
    {
        ArgumentNullException.ThrowIfNull(setModalOpen);
        ArgumentNullException.ThrowIfNull(getAnalysis);
        ArgumentNullException.ThrowIfNull(saveSettings);

        IReadOnlyList<ThemeInfo> themes = _themeRepo.GetAll();
        int themeListSelectedIndex = 0;
        string? currentId = _uiSettings.UiThemeId;
        if (!string.IsNullOrWhiteSpace(currentId))
        {
            for (int i = 0; i < themes.Count; i++)
            {
                if (string.Equals(themes[i].Id, currentId.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    themeListSelectedIndex = i + 1;
                    break;
                }
            }
        }

        var context = new UiThemeSelectionKeyContext
        {
            Themes = themes,
            ThemeListSelectedIndex = themeListSelectedIndex,
            UiSettings = _uiSettings,
            SaveSettings = saveSettings,
            ThemeRepo = _themeRepo,
            PaletteRepo = _paletteRepo
        };

        var palette = _uiThemeResolver.GetEffectiveUiPalette();
        var (selBg, selFg) = MenuSelectionAffordance.GetSelectionColors(palette);
        var currentColor = palette.Highlighted;

        int lastPaletteAnimBeatCount = -1;
        long lastPaletteAnimTickBucket = -1;

        void PaintThemeList(AudioAnalysisSnapshot analysis)
        {
            int width = _consoleDimensions.GetConsoleWidth();
            int totalCount = 1 + context.Themes.Count;
            int maxVisible = Math.Max(1, Math.Min(18, System.Console.WindowHeight - 5));
            int visibleRows = Math.Min(maxVisible, totalCount);
            int scroll = SettingsSurfacesListDrawing.ComputeListScrollOffset(
                context.ThemeListSelectedIndex,
                totalCount,
                visibleRows);

            SettingsSurfacesListDrawing.DrawUiThemeFileList(
                3,
                width,
                _paletteRepo,
                context.Themes,
                context.ThemeListSelectedIndex,
                scroll,
                visibleRows,
                _uiSettings.UiThemeId,
                selBg,
                selFg,
                currentColor,
                analysis);
        }

        void PaintPalettePick(AudioAnalysisSnapshot analysis)
        {
            int width = _consoleDimensions.GetConsoleWidth();
            var palettes = context.NewPalettes ?? [];
            int totalCount = palettes.Count;
            if (totalCount <= 0)
            {
                return;
            }

            int maxVisible = Math.Max(1, Math.Min(18, System.Console.WindowHeight - 5));
            int visibleRows = Math.Min(maxVisible, totalCount);
            int scroll = SettingsSurfacesListDrawing.ComputeListScrollOffset(
                context.NewPaletteSelectedIndex,
                totalCount,
                visibleRows);

            SettingsSurfacesListDrawing.DrawPaletteOnlyList(
                3,
                width,
                _paletteRepo,
                palettes,
                context.NewPaletteSelectedIndex,
                scroll,
                visibleRows,
                selBg,
                selFg,
                analysis);
        }

        void PaintSlotEditor()
        {
            int width = _consoleDimensions.GetConsoleWidth();
            if (context.SlotEditPaletteColors is not { Count: > 0 } || context.SlotEditIndices is not { Length: 11 })
            {
                return;
            }

            int totalRows = s_slotLabels.Length + 1;
            int maxVisible = Math.Max(1, Math.Min(18, System.Console.WindowHeight - 5));
            int visibleRows = Math.Min(maxVisible, totalRows);
            int scroll = SettingsSurfacesListDrawing.ComputeListScrollOffset(
                context.SlotEditSelectedRow,
                totalRows,
                visibleRows);

            SettingsSurfacesListDrawing.DrawUiThemeSlotAuthoringList(
                3,
                width,
                s_slotLabels,
                context.SlotEditPaletteColors,
                context.SlotEditIndices,
                context.SlotEditSelectedRow,
                scroll,
                visibleRows,
                selBg,
                selFg);
        }

        void SyncPaletteListTrackingFrom(AudioAnalysisSnapshot analysis)
        {
            lastPaletteAnimBeatCount = analysis.BeatCount;
            lastPaletteAnimTickBucket = PaletteSwatchFormatter.GetPaletteAnimationTickBucket();
        }

        void DrawContent()
        {
            int width = _consoleDimensions.GetConsoleWidth();
            AudioAnalysisSnapshot analysis = getAnalysis();
            TitleBarBreadcrumbRow.Write(0, width, _breadcrumbFormatter);

            System.Console.SetCursorPosition(0, 1);
            switch (context.Phase)
            {
                case UiThemeAuthoringPhase.PickTheme:
                    System.Console.WriteLine("  ↑/↓ select  Enter apply  Esc cancel  N new from palette");
                    break;
                case UiThemeAuthoringPhase.NewPickPalette:
                    System.Console.WriteLine("  Pick source palette  Enter confirm  Esc back");
                    break;
                case UiThemeAuthoringPhase.NewEditSlots:
                    System.Console.WriteLine("  ↑/↓ slot  ←/→ index  Enter on Save row  Esc back");
                    break;
            }

            System.Console.WriteLine();

            switch (context.Phase)
            {
                case UiThemeAuthoringPhase.PickTheme:
                    PaintThemeList(analysis);
                    SyncPaletteListTrackingFrom(analysis);
                    break;
                case UiThemeAuthoringPhase.NewPickPalette:
                    PaintPalettePick(analysis);
                    SyncPaletteListTrackingFrom(analysis);
                    break;
                case UiThemeAuthoringPhase.NewEditSlots:
                    PaintSlotEditor();
                    break;
            }
        }

        void OnIdleTick()
        {
            if (context.Phase == UiThemeAuthoringPhase.NewEditSlots)
            {
                return;
            }

            AudioAnalysisSnapshot analysis = getAnalysis();
            if (!PaletteSwatchFormatter.PaletteAnimationFrameAdvanced(
                    analysis,
                    lastPaletteAnimBeatCount,
                    lastPaletteAnimTickBucket,
                    out _,
                    out _))
            {
                return;
            }

            try
            {
                System.Console.Write(SyncOutputBegin);
                if (context.Phase == UiThemeAuthoringPhase.PickTheme)
                {
                    PaintThemeList(analysis);
                }
                else if (context.Phase == UiThemeAuthoringPhase.NewPickPalette)
                {
                    PaintPalettePick(analysis);
                }

                System.Console.Write(SyncOutputEnd);
                SyncPaletteListTrackingFrom(analysis);
            }
            catch (Exception ex)
            {
                _ = ex; /* Console redraw unavailable */
            }
        }

        bool HandleKey(ConsoleKeyInfo key) => _keyHandler.Handle(key, context);

        ModalSystem.RunModal(
            DrawContent,
            HandleKey,
            onClose: () =>
            {
                setModalOpen(false);
                _navigation.View = TitleBarViewKind.Main;
            },
            onEnter: () =>
            {
                setModalOpen(true);
                _navigation.View = TitleBarViewKind.SettingsHub;
            },
            onIdleTick: OnIdleTick);
    }
}
