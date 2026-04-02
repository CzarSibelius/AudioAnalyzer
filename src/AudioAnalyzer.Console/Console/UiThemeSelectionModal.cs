using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Application.Display;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Console;

/// <summary>Modal to pick <see cref="UiSettings.UiThemePaletteId"/> from layer palette files or (Custom).</summary>
internal sealed class UiThemeSelectionModal : IUiThemeSelectionModal
{
    /// <summary>Matches settings overlay idle palette updates: batched console output to reduce tear.</summary>
    private const string SyncOutputBegin = "\x1b[?2026h";
    private const string SyncOutputEnd = "\x1b[?2026l";

    private readonly IPaletteRepository _paletteRepo;
    private readonly IKeyHandler<UiThemeSelectionKeyContext> _keyHandler;
    private readonly UiSettings _uiSettings;
    private readonly IUiThemeResolver _uiThemeResolver;
    private readonly IConsoleDimensions _consoleDimensions;
    private readonly ITitleBarNavigationContext _navigation;
    private readonly ITitleBarBreadcrumbFormatter _breadcrumbFormatter;

    public UiThemeSelectionModal(
        IPaletteRepository paletteRepo,
        IKeyHandler<UiThemeSelectionKeyContext> keyHandler,
        UiSettings uiSettings,
        IUiThemeResolver uiThemeResolver,
        IConsoleDimensions consoleDimensions,
        ITitleBarNavigationContext navigation,
        ITitleBarBreadcrumbFormatter breadcrumbFormatter)
    {
        _paletteRepo = paletteRepo ?? throw new ArgumentNullException(nameof(paletteRepo));
        _keyHandler = keyHandler ?? throw new ArgumentNullException(nameof(keyHandler));
        _uiSettings = uiSettings ?? throw new ArgumentNullException(nameof(uiSettings));
        _uiThemeResolver = uiThemeResolver ?? throw new ArgumentNullException(nameof(uiThemeResolver));
        _consoleDimensions = consoleDimensions ?? throw new ArgumentNullException(nameof(consoleDimensions));
        _navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
        _breadcrumbFormatter = breadcrumbFormatter ?? throw new ArgumentNullException(nameof(breadcrumbFormatter));
    }

    /// <inheritdoc />
    public void Show(Action<bool> setModalOpen, Func<AnalysisSnapshot> getSnapshot, Action saveSettings)
    {
        ArgumentNullException.ThrowIfNull(setModalOpen);
        ArgumentNullException.ThrowIfNull(getSnapshot);
        ArgumentNullException.ThrowIfNull(saveSettings);

        IReadOnlyList<PaletteInfo> palettes = _paletteRepo.GetAll();
        int totalCount = 1 + palettes.Count;
        if (totalCount <= 0)
        {
            return;
        }

        int selectedIndex = 0;
        string? currentId = _uiSettings.UiThemePaletteId;
        if (!string.IsNullOrWhiteSpace(currentId))
        {
            for (int i = 0; i < palettes.Count; i++)
            {
                if (string.Equals(palettes[i].Id, currentId.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    selectedIndex = i + 1;
                    break;
                }
            }
        }

        var context = new UiThemeSelectionKeyContext
        {
            Palettes = palettes,
            SelectedIndex = selectedIndex,
            UiSettings = _uiSettings,
            SaveSettings = saveSettings
        };

        var palette = _uiThemeResolver.GetEffectiveUiPalette();
        var (selBg, selFg) = MenuSelectionAffordance.GetSelectionColors(palette);
        var currentColor = palette.Highlighted;

        int lastPaletteAnimBeatCount = -1;
        long lastPaletteAnimTickBucket = -1;

        void PaintThemePaletteList(AnalysisSnapshot snapshot)
        {
            int width = _consoleDimensions.GetConsoleWidth();
            int maxVisible = Math.Max(1, Math.Min(18, System.Console.WindowHeight - 5));
            int visibleRows = Math.Min(maxVisible, totalCount);
            int scroll = SettingsSurfacesListDrawing.ComputeListScrollOffset(context.SelectedIndex, totalCount, visibleRows);

            SettingsSurfacesListDrawing.DrawUiThemePaletteList(
                3,
                width,
                _paletteRepo,
                palettes,
                context.SelectedIndex,
                scroll,
                visibleRows,
                _uiSettings.UiThemePaletteId,
                selBg,
                selFg,
                currentColor,
                snapshot);
        }

        void SyncPaletteListTrackingFrom(AnalysisSnapshot snapshot)
        {
            lastPaletteAnimBeatCount = snapshot.BeatCount;
            lastPaletteAnimTickBucket = PaletteSwatchFormatter.GetPaletteAnimationTickBucket();
        }

        void DrawContent()
        {
            int width = _consoleDimensions.GetConsoleWidth();
            AnalysisSnapshot snapshot = getSnapshot();
            TitleBarBreadcrumbRow.Write(0, width, _breadcrumbFormatter);

            System.Console.SetCursorPosition(0, 1);
            System.Console.WriteLine("  Use ↑/↓ to select, ENTER to confirm, ESC to cancel");
            System.Console.WriteLine();

            PaintThemePaletteList(snapshot);
            SyncPaletteListTrackingFrom(snapshot);
        }

        void OnIdleTick()
        {
            AnalysisSnapshot snapshot = getSnapshot();
            if (!PaletteSwatchFormatter.PaletteAnimationFrameAdvanced(
                    snapshot,
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
                PaintThemePaletteList(snapshot);
                System.Console.Write(SyncOutputEnd);
                SyncPaletteListTrackingFrom(snapshot);
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
