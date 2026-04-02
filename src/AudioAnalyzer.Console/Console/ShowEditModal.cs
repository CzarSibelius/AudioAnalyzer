using AudioAnalyzer.Application;
using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Application.Display;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Console;

/// <summary>Show edit overlay modal. Edit Show entries: add/remove/reorder presets, set duration. Per ADR-0031. Key handling via IKeyHandler per ADR-0047.</summary>
internal sealed class ShowEditModal : IShowEditModal
{
    private const int BreadcrumbRowCount = 1;
    private const int HintRow = 1;
    private const int SeparatorRow = 2;
    private const int FirstListRow = 3;
    private const int OverlayRowCount = 14;
    private const int LeftColWidth = 32;

    private readonly IVisualizationOrchestrator _orchestrator;
    private readonly VisualizerSettings _visualizerSettings;
    private readonly IShowRepository _showRepo;
    private readonly IPresetRepository _presetRepo;
    private readonly IKeyHandler<ShowEditModalKeyContext> _keyHandler;
    private readonly UiSettings _uiSettings;
    private readonly IUiThemeResolver _uiThemeResolver;
    private readonly IConsoleDimensions _consoleDimensions;
    private readonly ITitleBarNavigationContext _navigation;
    private readonly ITitleBarBreadcrumbFormatter _breadcrumbFormatter;

    public ShowEditModal(
        IVisualizationOrchestrator orchestrator,
        VisualizerSettings visualizerSettings,
        IShowRepository showRepo,
        IPresetRepository presetRepo,
        IKeyHandler<ShowEditModalKeyContext> keyHandler,
        UiSettings uiSettings,
        IUiThemeResolver uiThemeResolver,
        IConsoleDimensions consoleDimensions,
        ITitleBarNavigationContext navigation,
        ITitleBarBreadcrumbFormatter breadcrumbFormatter)
    {
        _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
        _visualizerSettings = visualizerSettings ?? throw new ArgumentNullException(nameof(visualizerSettings));
        _showRepo = showRepo ?? throw new ArgumentNullException(nameof(showRepo));
        _presetRepo = presetRepo ?? throw new ArgumentNullException(nameof(presetRepo));
        _keyHandler = keyHandler ?? throw new ArgumentNullException(nameof(keyHandler));
        _uiSettings = uiSettings ?? throw new ArgumentNullException(nameof(uiSettings));
        _uiThemeResolver = uiThemeResolver ?? throw new ArgumentNullException(nameof(uiThemeResolver));
        _consoleDimensions = consoleDimensions ?? throw new ArgumentNullException(nameof(consoleDimensions));
        _navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
        _breadcrumbFormatter = breadcrumbFormatter ?? throw new ArgumentNullException(nameof(breadcrumbFormatter));
    }

    /// <inheritdoc />
    public void Show(object consoleLock, Action saveVisualizerSettings)
    {
        var allShows = _showRepo.GetAll();
        var allPresets = _presetRepo.GetAll();

        var currentShowId = _visualizerSettings.ActiveShowId;
        if (string.IsNullOrWhiteSpace(currentShowId) && allShows.Count > 0)
        {
            currentShowId = allShows[0].Id;
            _visualizerSettings.ActiveShowId = currentShowId;
            var show = _showRepo.GetById(currentShowId);
            _visualizerSettings.ActiveShowName = show?.Name?.Trim();
        }

        var context = new ShowEditModalKeyContext
        {
            CurrentShowId = currentShowId,
            Renaming = false,
            RenameBuffer = "",
            SelectedIndex = 0,
            EditingDuration = false,
            DurationBuffer = "",
            AllShows = allShows,
            AllPresets = allPresets,
            SaveVisualizerSettings = saveVisualizerSettings,
            ShowRepo = _showRepo,
            PresetRepo = _presetRepo,
            VisualizerSettings = _visualizerSettings
        };

        int width = _consoleDimensions.GetConsoleWidth();
        int rightColWidth = Math.Max(10, width - LeftColWidth - 1);

        var palette = _uiThemeResolver.GetEffectiveUiPalette();
        var (selBg, selFg) = MenuSelectionAffordance.GetSelectionColors(palette);

        void DrawContent()
        {
            if (width < 40)
            {
                return;
            }

            try
            {
                TitleBarBreadcrumbRow.Write(0, width, _breadcrumbFormatter);

                var show = string.IsNullOrWhiteSpace(context.CurrentShowId) ? null : _showRepo.GetById(context.CurrentShowId);
                string hint = context.Renaming
                    ? "  Type new name, Enter save, Esc cancel"
                    : context.EditingDuration
                        ? "  Type duration value, Enter confirm, Esc cancel"
                        : "  \u2191\u2193 select, A add D delete, P preset, Enter duration, U unit, Esc close";
                System.Console.SetCursorPosition(0, HintRow);
                System.Console.Write(StaticTextViewport.TruncateWithEllipsis(new PlainText(hint), width).PadRight(width));
                System.Console.SetCursorPosition(0, SeparatorRow);
                System.Console.Write(StaticTextViewport.TruncateToWidth(new PlainText("  ─" + new string('─', LeftColWidth - 2) + "┬" + new string('─', rightColWidth) + "─"), width).PadRight(width));

                var entries = show?.Entries ?? new List<ShowEntry>();
                for (int i = 0; i < Math.Min(10, entries.Count + 2); i++)
                {
                    int row = FirstListRow + i;
                    if (row >= OverlayRowCount)
                    {
                        break;
                    }

                    System.Console.SetCursorPosition(0, row);
                    if (i < entries.Count)
                    {
                        var entry = entries[i];
                        var preset = _presetRepo.GetById(entry.PresetId);
                        var presetName = preset?.Name?.Trim() ?? entry.PresetId;
                        var dur = entry.Duration ?? new DurationConfig();
                        var durStr = dur.Unit == DurationUnit.Beats ? $"{dur.Value:F0} beats" : $"{dur.Value:F0}s";
                        string prefix = MenuSelectionAffordance.GetPrefix(i == context.SelectedIndex);
                        string line = $"{prefix}{i + 1}. {StaticTextViewport.TruncateWithEllipsis(new PlainText(presetName), LeftColWidth - 10)}  {durStr}";
                        line = StaticTextViewport.TruncateWithEllipsis(new PlainText(line), LeftColWidth);
                        line = AnsiConsole.PadToDisplayWidth(line, LeftColWidth);
                        string lineToWrite = MenuSelectionAffordance.ApplyRowHighlight(i == context.SelectedIndex, line, selBg, selFg);
                        System.Console.Write(lineToWrite);
                        System.Console.Write(" ");
                        if (i == context.SelectedIndex && !context.EditingDuration)
                        {
                            var cfg = entry.Duration ?? new DurationConfig();
                            string rightLine = $"Unit: {cfg.Unit}  Value: {cfg.Value} (Enter to edit)";
                            System.Console.Write(StaticTextViewport.TruncateWithEllipsis(new PlainText(rightLine), rightColWidth).PadRight(rightColWidth));
                        }
                        else if (i == context.SelectedIndex && context.EditingDuration)
                        {
                            string rightLine = $"Value: {context.DurationBuffer}_";
                            System.Console.Write(StaticTextViewport.TruncateWithEllipsis(new PlainText(rightLine), rightColWidth).PadRight(rightColWidth));
                        }
                        else
                        {
                            System.Console.Write(new string(' ', rightColWidth));
                        }
                    }
                    else
                    {
                        System.Console.Write(new string(' ', LeftColWidth + 1 + rightColWidth));
                    }
                }
            }
            catch (Exception ex) { _ = ex; /* Draw Show edit modal failed */ }
        }

        bool HandleKey(ConsoleKeyInfo key) => _keyHandler.Handle(key, context);

        ModalSystem.RunOverlayModal(
            OverlayRowCount,
            _consoleDimensions.GetConsoleWidth(),
            DrawContent,
            HandleKey,
            consoleLock,
            onClose: () =>
            {
                _navigation.View = TitleBarViewKind.Main;
                _orchestrator.SetOverlayActive(false);
            },
            onEnter: () =>
            {
                _navigation.View = TitleBarViewKind.ShowEditModal;
                _orchestrator.SetOverlayActive(true, OverlayRowCount);
            });
    }
}
