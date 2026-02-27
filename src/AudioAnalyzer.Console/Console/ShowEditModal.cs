using AudioAnalyzer.Application;
using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Application.Display;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Console;

/// <summary>Show edit overlay modal. Edit Show entries: add/remove/reorder presets, set duration. Per ADR-0031. Key handling via IKeyHandler per ADR-0047.</summary>
internal sealed class ShowEditModal : IShowEditModal
{
    private const int OverlayRowCount = 16;
    private const int LeftColWidth = 32;

    private readonly IVisualizationOrchestrator _orchestrator;
    private readonly VisualizerSettings _visualizerSettings;
    private readonly IShowRepository _showRepo;
    private readonly IPresetRepository _presetRepo;
    private readonly IKeyHandler<ShowEditModalKeyContext> _keyHandler;
    private readonly UiSettings _uiSettings;

    public ShowEditModal(
        IVisualizationOrchestrator orchestrator,
        VisualizerSettings visualizerSettings,
        IShowRepository showRepo,
        IPresetRepository presetRepo,
        IKeyHandler<ShowEditModalKeyContext> keyHandler,
        UiSettings uiSettings)
    {
        _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
        _visualizerSettings = visualizerSettings ?? throw new ArgumentNullException(nameof(visualizerSettings));
        _showRepo = showRepo ?? throw new ArgumentNullException(nameof(showRepo));
        _presetRepo = presetRepo ?? throw new ArgumentNullException(nameof(presetRepo));
        _keyHandler = keyHandler ?? throw new ArgumentNullException(nameof(keyHandler));
        _uiSettings = uiSettings ?? throw new ArgumentNullException(nameof(uiSettings));
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

        int width = ConsoleHeader.GetConsoleWidth();
        int rightColWidth = Math.Max(10, width - LeftColWidth - 1);

        var palette = (_uiSettings ?? new UiSettings()).Palette ?? new UiPalette();
        var selBg = palette.Background ?? PaletteColor.FromConsoleColor(ConsoleColor.DarkBlue);
        var selFg = palette.Highlighted;

        void DrawContent()
        {
            if (width < 40)
            {
                return;
            }

            try
            {
                var show = string.IsNullOrWhiteSpace(context.CurrentShowId) ? null : _showRepo.GetById(context.CurrentShowId);
                var showName = show?.Name?.Trim() ?? "Show 1";
                var title = context.Renaming
                    ? $" New show name (Enter confirm, Esc cancel): {context.RenameBuffer}_ "
                    : $" Show: {showName} (R rename, N new) ";
                var titleTruncated = StaticTextViewport.TruncateWithEllipsis(new PlainText(title), width - 2);
                int pad = Math.Max(0, (width - titleTruncated.Length - 2) / 2);
                System.Console.SetCursorPosition(0, 0);
                System.Console.Write(StaticTextViewport.TruncateToWidth(new PlainText("╔" + new string('═', width - 2) + "╗"), width).PadRight(width));
                System.Console.SetCursorPosition(0, 1);
                System.Console.Write(StaticTextViewport.TruncateToWidth(new PlainText("║" + new string(' ', pad) + titleTruncated + new string(' ', width - pad - titleTruncated.Length - 2) + "║"), width).PadRight(width));
                System.Console.SetCursorPosition(0, 2);
                System.Console.Write(StaticTextViewport.TruncateToWidth(new PlainText("╚" + new string('═', width - 2) + "╝"), width).PadRight(width));

                string hint = context.Renaming
                    ? "  Type new name, Enter save, Esc cancel"
                    : context.EditingDuration
                        ? "  Type duration value, Enter confirm, Esc cancel"
                        : "  \u2191\u2193 select, A add D delete, P preset, Enter duration, U unit, Esc close";
                System.Console.SetCursorPosition(0, 3);
                System.Console.Write(StaticTextViewport.TruncateWithEllipsis(new PlainText(hint), width).PadRight(width));
                System.Console.SetCursorPosition(0, 4);
                System.Console.Write(StaticTextViewport.TruncateToWidth(new PlainText("  ─" + new string('─', LeftColWidth - 2) + "┬" + new string('─', rightColWidth) + "─"), width).PadRight(width));

                var entries = show?.Entries ?? new List<ShowEntry>();
                for (int i = 0; i < Math.Min(10, entries.Count + 2); i++)
                {
                    int row = 5 + i;
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
                        string prefix = i == context.SelectedIndex ? " ► " : "   ";
                        string line = $"{prefix}{i + 1}. {StaticTextViewport.TruncateWithEllipsis(new PlainText(presetName), LeftColWidth - 10)}  {durStr}";
                        line = StaticTextViewport.TruncateWithEllipsis(new PlainText(line), LeftColWidth).PadRight(LeftColWidth);
                        string lineToWrite = i == context.SelectedIndex
                            ? AnsiConsole.BackgroundCode(selBg) + AnsiConsole.ColorCode(selFg) + line + AnsiConsole.ResetCode
                            : line;
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
            DrawContent,
            HandleKey,
            consoleLock,
            onClose: () => _orchestrator.SetOverlayActive(false),
            onEnter: () => _orchestrator.SetOverlayActive(true, OverlayRowCount));
    }
}
