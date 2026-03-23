using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Application.Display;
using AudioAnalyzer.Application.Palette;
using AudioAnalyzer.Domain;
using AudioAnalyzer.Visualizers;

namespace AudioAnalyzer.Console;

/// <summary>Renders the settings overlay modal: frame, preset title, hint line, layer list, and settings panel. Hint line is rendered via <see cref="IUiComponentRenderer{T}"/> using <see cref="HorizontalRowComponent"/>.</summary>
internal sealed class SettingsModalRenderer : ISettingsModalRenderer
{
    private const int OverlayRowCount = 16;
    private const int HintRow = 1;
    private const int SeparatorRow = 2;
    private const int FirstLayerRow = 3;
    private const int LeftColWidth = 28;
    private static readonly int SettingsVisibleRows = OverlayRowCount - FirstLayerRow;

    private readonly VisualizerSettings _visualizerSettings;
    private readonly UiSettings _uiSettings;
    private readonly IPaletteRepository _paletteRepo;
    private readonly IUiComponentRenderer<IUiComponent> _componentRenderer;
    private readonly ITitleBarNavigationContext _navigation;
    private readonly ITitleBarBreadcrumbFormatter _breadcrumbFormatter;
    private readonly HorizontalRowComponent _hintRow;
    private readonly CompositeComponent _hintRoot;

    /// <summary>Last palette color phase drawn; used to skip redundant palette cell repaints.</summary>
    private int _lastIdlePalettePhase = int.MinValue;

    /// <summary>Matches <see cref="PaletteSwatchFormatter.ComputeToolbarPhaseOffset"/> tick path; used to detect picker animation frame changes.</summary>
    private const long PaletteAnimationTickBucketMs = 200;

    private int _lastPickerIdleBeatCount = -1;
    private long _lastPickerIdleTickBucket = -1;

    private const string SyncOutputBegin = "\x1b[?2026h";
    private const string SyncOutputEnd = "\x1b[?2026l";

    public SettingsModalRenderer(
        VisualizerSettings visualizerSettings,
        UiSettings uiSettings,
        IPaletteRepository paletteRepo,
        IUiComponentRenderer<IUiComponent> componentRenderer,
        ITitleBarNavigationContext navigation,
        ITitleBarBreadcrumbFormatter breadcrumbFormatter)
    {
        _visualizerSettings = visualizerSettings ?? throw new ArgumentNullException(nameof(visualizerSettings));
        _uiSettings = uiSettings ?? throw new ArgumentNullException(nameof(uiSettings));
        _paletteRepo = paletteRepo ?? throw new ArgumentNullException(nameof(paletteRepo));
        _componentRenderer = componentRenderer ?? throw new ArgumentNullException(nameof(componentRenderer));
        _navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
        _breadcrumbFormatter = breadcrumbFormatter ?? throw new ArgumentNullException(nameof(breadcrumbFormatter));
        _hintRow = new HorizontalRowComponent();
        _hintRoot = new CompositeComponent(_ => [_hintRow]);
    }

    /// <inheritdoc />
    public void Draw(SettingsModalState state, IReadOnlyList<TextLayerSettings> sortedLayers, int width, AnalysisSnapshot analysisSnapshot)
    {
        if (width < 40)
        {
            return;
        }

        int rightColWidth = Math.Max(10, width - LeftColWidth - 1);
        var palette = (_uiSettings ?? new UiSettings()).Palette ?? new UiPalette();
        var selBg = palette.Background ?? PaletteColor.FromConsoleColor(ConsoleColor.DarkBlue);
        var selFg = palette.Highlighted;
        double scrollSpeed = _uiSettings?.DefaultScrollingSpeed ?? 0.25;

        try
        {
            _navigation.View = TitleBarViewKind.PresetSettingsModal;
            _navigation.PresetSettingsPalettePickerActive = state.Focus == SettingsModalFocus.PickingPalette;
            var selectedLayer = sortedLayers.Count > 0 && state.SelectedLayerIndex < sortedLayers.Count
                ? sortedLayers[state.SelectedLayerIndex]
                : null;
            if (selectedLayer != null)
            {
                _navigation.PresetSettingsLayerOneBased = state.SelectedLayerIndex + 1;
                _navigation.PresetSettingsLayerTypeRaw = selectedLayer.LayerType.ToString();
            }
            else
            {
                _navigation.PresetSettingsLayerOneBased = null;
                _navigation.PresetSettingsLayerTypeRaw = null;
            }

            _navigation.PresetSettingsFocusedSettingId = TryGetFocusedSettingId(state, selectedLayer);

            TitleBarBreadcrumbRow.Write(0, width, _breadcrumbFormatter);

            RenderHintRow(state, width, palette, scrollSpeed, HintRow);
            System.Console.SetCursorPosition(0, SeparatorRow);
            System.Console.Write(StaticTextViewport.TruncateToWidth(new PlainText("  ─" + new string('─', LeftColWidth - 2) + "┬" + new string('─', rightColWidth) + "─"), width).PadRight(width));

            for (int r = FirstLayerRow; r < OverlayRowCount; r++)
            {
                System.Console.SetCursorPosition(LeftColWidth + 1, r);
                System.Console.Write(new string(' ', rightColWidth));
            }

            for (int i = 0; i < sortedLayers.Count && i < TextLayersLimits.MaxLayerCount; i++)
            {
                int row = FirstLayerRow + i;
                if (row >= OverlayRowCount)
                {
                    break;
                }

                var layer = sortedLayers[i];
                string prefix = i == state.SelectedLayerIndex ? " ► " : "   ";
                string enabledMark = layer.Enabled ? "●" : "○";
                string leftLine = $"{prefix}{enabledMark} {i + 1}. {layer.LayerType}";
                leftLine = StaticTextViewport.TruncateWithEllipsis(new PlainText(leftLine), LeftColWidth).PadRight(LeftColWidth);

                System.Console.SetCursorPosition(0, row);
                string leftLineToWrite = (i == state.SelectedLayerIndex && state.Focus == SettingsModalFocus.LayerList && !state.Renaming)
                    ? AnsiConsole.BackgroundCode(selBg) + AnsiConsole.ColorCode(selFg) + leftLine + AnsiConsole.ResetCode
                    : leftLine;
                System.Console.Write(leftLineToWrite);
                System.Console.Write(" ");
                System.Console.Write(new string(' ', rightColWidth));
            }

            if (selectedLayer != null)
            {
                if (state.Focus == SettingsModalFocus.PickingPalette)
                {
                    SettingsSurfacesPaletteDrawing.DrawPicker(
                        _paletteRepo,
                        state,
                        LeftColWidth,
                        FirstLayerRow,
                        SettingsVisibleRows,
                        rightColWidth,
                        selBg,
                        selFg,
                        analysisSnapshot);
                }
                else
                {
                    var settingsRows = GetSettingsRows(selectedLayer);
                    int settingsScrollOffset = settingsRows.Count <= SettingsVisibleRows
                        ? 0
                        : Math.Clamp(state.SelectedSettingIndex - (SettingsVisibleRows - 1), 0, settingsRows.Count - SettingsVisibleRows);
                    for (int vi = 0; vi < SettingsVisibleRows; vi++)
                    {
                        int i = settingsScrollOffset + vi;
                        if (i >= settingsRows.Count) { break; }
                        var row = settingsRows[i];
                        bool showBuffer = state.Focus == SettingsModalFocus.EditingSetting && i == state.SelectedSettingIndex && row.EditMode == SettingEditMode.TextEdit;
                        bool selected = i == state.SelectedSettingIndex && (state.Focus == SettingsModalFocus.SettingsList || state.Focus == SettingsModalFocus.EditingSetting);
                        System.Console.SetCursorPosition(LeftColWidth + 1, FirstLayerRow + vi);
                        if (row.Id == "Palette" && !showBuffer)
                        {
                            System.Console.Write(SettingsSurfacesPaletteDrawing.FormatPaletteSettingRow(
                                _paletteRepo,
                                _visualizerSettings,
                                selectedLayer,
                                rightColWidth,
                                selected,
                                selBg,
                                selFg,
                                analysisSnapshot));
                            continue;
                        }

                        string labelWithColon = string.IsNullOrEmpty(row.Label) ? "" : row.Label + ":";
                        string lineText = $"{labelWithColon}{(showBuffer ? state.EditingBuffer + "_" : row.DisplayValue)}";
                        string line = StaticTextViewport.TruncateWithEllipsis(new PlainText(lineText), rightColWidth);
                        string linePadded = line.PadRight(rightColWidth);
                        string lineToWrite = selected
                            ? AnsiConsole.BackgroundCode(selBg) + AnsiConsole.ColorCode(selFg) + linePadded + AnsiConsole.ResetCode
                            : linePadded;
                        System.Console.Write(lineToWrite);
                    }
                }
            }

            for (int row = FirstLayerRow + sortedLayers.Count; row < OverlayRowCount; row++)
            {
                System.Console.SetCursorPosition(0, row);
                System.Console.Write(new string(' ', LeftColWidth + 1));
            }

            if (selectedLayer != null && state.Focus != SettingsModalFocus.PickingPalette)
            {
                _lastIdlePalettePhase = GetPalettePhaseForLayer(selectedLayer, analysisSnapshot);
            }
            else if (selectedLayer == null)
            {
                _lastIdlePalettePhase = int.MinValue;
            }

            if (state.Focus == SettingsModalFocus.PickingPalette && selectedLayer != null)
            {
                SyncPickerIdleTracking(analysisSnapshot);
            }
            else
            {
                _lastPickerIdleBeatCount = -1;
                _lastPickerIdleTickBucket = -1;
            }
        }
        catch (Exception ex) { _ = ex; /* Draw settings modal failed */ }
    }

    /// <inheritdoc />
    public void DrawIdleOverlayTick(SettingsModalState state, IReadOnlyList<TextLayerSettings> sortedLayers, int width, AnalysisSnapshot analysisSnapshot)
    {
        if (width < 40)
        {
            return;
        }

        double scrollSpeed = _uiSettings?.DefaultScrollingSpeed ?? 0.25;
        var palette = _uiSettings?.Palette ?? new UiPalette();
        try
        {
            System.Console.Write(SyncOutputBegin);
            RenderHintRow(state, width, palette, scrollSpeed, HintRow);
            TryRedrawPaletteRowForIdleInOpenSyncFrame(state, sortedLayers, width, analysisSnapshot);
            System.Console.Write(SyncOutputEnd);
        }
        catch (Exception ex) { _ = ex; /* Idle overlay tick failed */ }
    }

    /// <summary>Updates palette cell if phase advanced; caller must wrap the whole idle frame in synchronized output.</summary>
    private void TryRedrawPaletteRowForIdleInOpenSyncFrame(SettingsModalState state, IReadOnlyList<TextLayerSettings> sortedLayers, int width, AnalysisSnapshot analysisSnapshot)
    {
        if (width < 40)
        {
            return;
        }

        if (state.Focus == SettingsModalFocus.PickingPalette)
        {
            TryRedrawPalettePickerForIdle(state, sortedLayers, width, analysisSnapshot);
            return;
        }

        var selectedLayer = sortedLayers.Count > 0 && state.SelectedLayerIndex < sortedLayers.Count
            ? sortedLayers[state.SelectedLayerIndex]
            : null;
        if (selectedLayer == null)
        {
            return;
        }

        int phase = GetPalettePhaseForLayer(selectedLayer, analysisSnapshot);
        if (phase == _lastIdlePalettePhase)
        {
            return;
        }

        int rightColWidth = Math.Max(10, width - LeftColWidth - 1);
        var settingsRows = GetSettingsRows(selectedLayer);
        int paletteRowIndex = -1;
        for (int i = 0; i < settingsRows.Count; i++)
        {
            if (settingsRows[i].Id == "Palette")
            {
                paletteRowIndex = i;
                break;
            }
        }

        if (paletteRowIndex < 0)
        {
            _lastIdlePalettePhase = phase;
            return;
        }

        int settingsScrollOffset = settingsRows.Count <= SettingsVisibleRows
            ? 0
            : Math.Clamp(state.SelectedSettingIndex - (SettingsVisibleRows - 1), 0, settingsRows.Count - SettingsVisibleRows);
        int vi = paletteRowIndex - settingsScrollOffset;
        if (vi < 0 || vi >= SettingsVisibleRows)
        {
            _lastIdlePalettePhase = phase;
            return;
        }

        var uiPalette = (_uiSettings ?? new UiSettings()).Palette ?? new UiPalette();
        var selBg = uiPalette.Background ?? PaletteColor.FromConsoleColor(ConsoleColor.DarkBlue);
        var selFg = uiPalette.Highlighted;
        var row = settingsRows[paletteRowIndex];
        bool showBuffer = state.Focus == SettingsModalFocus.EditingSetting && paletteRowIndex == state.SelectedSettingIndex && row.EditMode == SettingEditMode.TextEdit;
        if (row.Id != "Palette" || showBuffer)
        {
            _lastIdlePalettePhase = phase;
            return;
        }

        bool selected = paletteRowIndex == state.SelectedSettingIndex && (state.Focus == SettingsModalFocus.SettingsList || state.Focus == SettingsModalFocus.EditingSetting);
        string line = SettingsSurfacesPaletteDrawing.FormatPaletteSettingRow(
            _paletteRepo,
            _visualizerSettings,
            selectedLayer,
            rightColWidth,
            selected,
            selBg,
            selFg,
            analysisSnapshot);

        try
        {
            System.Console.SetCursorPosition(LeftColWidth + 1, FirstLayerRow + vi);
            System.Console.Write(line);
            _lastIdlePalettePhase = phase;
        }
        catch (Exception ex) { _ = ex; /* Palette idle redraw failed */ }
    }

    private void SyncPickerIdleTracking(AnalysisSnapshot analysisSnapshot)
    {
        _lastPickerIdleBeatCount = analysisSnapshot.BeatCount;
        _lastPickerIdleTickBucket = Environment.TickCount64 / PaletteAnimationTickBucketMs;
    }

    private void TryRedrawPalettePickerForIdle(SettingsModalState state, IReadOnlyList<TextLayerSettings> sortedLayers, int width, AnalysisSnapshot analysisSnapshot)
    {
        var selectedLayer = sortedLayers.Count > 0 && state.SelectedLayerIndex < sortedLayers.Count
            ? sortedLayers[state.SelectedLayerIndex]
            : null;
        if (selectedLayer == null)
        {
            return;
        }

        if (!PaletteAnimationFrameAdvanced(analysisSnapshot, _lastPickerIdleBeatCount, _lastPickerIdleTickBucket, out int beatCount, out long tickBucket))
        {
            return;
        }

        int rightColWidth = Math.Max(10, width - LeftColWidth - 1);
        var uiPalette = (_uiSettings ?? new UiSettings()).Palette ?? new UiPalette();
        var selBg = uiPalette.Background ?? PaletteColor.FromConsoleColor(ConsoleColor.DarkBlue);
        var selFg = uiPalette.Highlighted;
        try
        {
            SettingsSurfacesPaletteDrawing.DrawPicker(
                _paletteRepo,
                state,
                LeftColWidth,
                FirstLayerRow,
                SettingsVisibleRows,
                rightColWidth,
                selBg,
                selFg,
                analysisSnapshot);
            _lastPickerIdleBeatCount = beatCount;
            _lastPickerIdleTickBucket = tickBucket;
        }
        catch (Exception ex) { _ = ex; /* Palette picker idle redraw failed */ }
    }

    private static bool PaletteAnimationFrameAdvanced(
        AnalysisSnapshot snapshot,
        int lastBeatCount,
        long lastTickBucket,
        out int beatCount,
        out long tickBucket)
    {
        beatCount = snapshot.BeatCount;
        tickBucket = Environment.TickCount64 / PaletteAnimationTickBucketMs;
        if (snapshot.CurrentBpm >= 1.0)
        {
            return beatCount != lastBeatCount;
        }

        return tickBucket != lastTickBucket;
    }

    private int GetPalettePhaseForLayer(TextLayerSettings layer, AnalysisSnapshot analysisSnapshot)
    {
        string effectiveId = string.IsNullOrWhiteSpace(layer.PaletteId)
            ? (_visualizerSettings.TextLayers?.PaletteId ?? "")
            : layer.PaletteId!;
        if (string.IsNullOrWhiteSpace(effectiveId))
        {
            effectiveId = "default";
        }

        var def = _paletteRepo.GetById(effectiveId);
        var colors = ColorPaletteParser.Parse(def);
        return PaletteSwatchFormatter.ComputeToolbarPhaseOffset(analysisSnapshot, colors?.Count ?? 0);
    }

    private void RenderHintRow(SettingsModalState state, int width, UiPalette palette, double scrollSpeed, int startRow)
    {
        string hint = GetHintText(state);
        var hintDescriptor = new LabeledValueDescriptor("", () => new PlainText(hint));
        _hintRow.SetRowData([hintDescriptor], [width]);
        var context = new RenderContext
        {
            Width = width,
            StartRow = startRow,
            MaxLines = 1,
            Palette = palette,
            ScrollSpeed = scrollSpeed
        };
        _componentRenderer.Render(_hintRoot, context);
    }

    private static string GetHintText(SettingsModalState state)
    {
        return state.Renaming ? "  Type new name, Enter to save, Esc to cancel"
            : state.Focus == SettingsModalFocus.EditingSetting ? "  Type value, Enter or \u2191\u2193 confirm, Esc cancel"
            : state.Focus == SettingsModalFocus.PickingPalette ? "  \u2191\u2193 or +/- preview, Enter save, Esc discard"
            : state.Focus == SettingsModalFocus.SettingsList ? "  \u2191\u2193 select, Enter or +/- cycle (Enter = palette list on Palette), Enter edit strings, \u2190 or Esc back"
            : "  1-9 select, \u2190\u2192 type, Ctrl+\u2191\u2193 move, Enter settings, Shift+1-9 toggle, R rename, N preset, Esc close";
    }

    private List<(string Id, string Label, string DisplayValue, SettingEditMode EditMode)> GetSettingsRows(TextLayerSettings? layer)
    {
        if (layer == null) { return []; }
        var descriptors = SettingDescriptor.BuildAll(layer, _paletteRepo);
        return descriptors.Select(d => (d.Id, d.Label, d.GetDisplayValue(layer), d.EditMode)).ToList();
    }

    private string? TryGetFocusedSettingId(SettingsModalState state, TextLayerSettings? selectedLayer)
    {
        if (selectedLayer == null)
        {
            return null;
        }

        if (state.Focus != SettingsModalFocus.SettingsList
            && state.Focus != SettingsModalFocus.EditingSetting
            && state.Focus != SettingsModalFocus.PickingPalette)
        {
            return null;
        }

        var rows = GetSettingsRows(selectedLayer);
        if (state.SelectedSettingIndex < 0 || state.SelectedSettingIndex >= rows.Count)
        {
            return null;
        }

        return rows[state.SelectedSettingIndex].Id;
    }

}
