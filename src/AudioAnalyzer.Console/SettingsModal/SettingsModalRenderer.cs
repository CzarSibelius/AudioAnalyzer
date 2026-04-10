using System.Text;
using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Application.Display;
using AudioAnalyzer.Application.Palette;
using AudioAnalyzer.Domain;
using AudioAnalyzer.Visualizers;

namespace AudioAnalyzer.Console;

/// <summary>Renders the settings overlay modal (frame, preset title, hint line, layer list, settings panel). Hint line is rendered via <see cref="IUiComponentRenderer{T}"/> using <see cref="HorizontalRowComponent"/>.</summary>
internal sealed class SettingsModalRenderer : ISettingsModalRenderer
{
    private const int OverlayRowCount = 16;
    private const int HintRow = 1;
    private const int SeparatorRow = 2;
    private const int FirstLayerRow = 3;

    /// <summary>First console row for the right-hand settings and palette-picker column; one below <see cref="FirstLayerRow"/> so detail lines align with layer list rows, not the Preset header.</summary>
    private const int RightColumnContentStartRow = FirstLayerRow + 1;

    private const int LeftColWidth = 28;
    private static readonly int SettingsVisibleRows = OverlayRowCount - RightColumnContentStartRow;

    private readonly VisualizerSettings _visualizerSettings;
    private readonly UiSettings _uiSettings;
    private readonly IUiThemeResolver _uiThemeResolver;
    private readonly IPaletteRepository _paletteRepo;
    private readonly IAsciiVideoDeviceCatalog _asciiVideoDeviceCatalog;
    private readonly IUiComponentRenderer<IUiComponent> _componentRenderer;
    private readonly ITitleBarNavigationContext _navigation;
    private readonly ITitleBarBreadcrumbFormatter _breadcrumbFormatter;
    private readonly HorizontalRowComponent _hintRow;
    private readonly CompositeComponent _hintRoot;

    /// <summary>Last palette color phase drawn for layer Palette row; used to skip redundant palette cell repaints.</summary>
    private int _lastIdlePalettePhase = int.MinValue;

    /// <summary>Last phase for preset default palette row idle animation.</summary>
    private int _lastIdlePresetDefaultPalettePhase = int.MinValue;

    private int _lastPickerIdleBeatCount = -1;
    private long _lastPickerIdleTickBucket = -1;

    private const string SyncOutputBegin = "\x1b[?2026h";
    private const string SyncOutputEnd = "\x1b[?2026l";

    public SettingsModalRenderer(
        VisualizerSettings visualizerSettings,
        UiSettings uiSettings,
        IUiThemeResolver uiThemeResolver,
        IPaletteRepository paletteRepo,
        IAsciiVideoDeviceCatalog asciiVideoDeviceCatalog,
        IUiComponentRenderer<IUiComponent> componentRenderer,
        ITitleBarNavigationContext navigation,
        ITitleBarBreadcrumbFormatter breadcrumbFormatter)
    {
        _visualizerSettings = visualizerSettings ?? throw new ArgumentNullException(nameof(visualizerSettings));
        _uiSettings = uiSettings ?? throw new ArgumentNullException(nameof(uiSettings));
        _uiThemeResolver = uiThemeResolver ?? throw new ArgumentNullException(nameof(uiThemeResolver));
        _paletteRepo = paletteRepo ?? throw new ArgumentNullException(nameof(paletteRepo));
        _asciiVideoDeviceCatalog = asciiVideoDeviceCatalog ?? throw new ArgumentNullException(nameof(asciiVideoDeviceCatalog));
        _componentRenderer = componentRenderer ?? throw new ArgumentNullException(nameof(componentRenderer));
        _navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
        _breadcrumbFormatter = breadcrumbFormatter ?? throw new ArgumentNullException(nameof(breadcrumbFormatter));
        _hintRow = new HorizontalRowComponent();
        _hintRoot = new CompositeComponent(_ => [_hintRow]);
    }

    /// <inheritdoc />
    public void Draw(SettingsModalState state, IReadOnlyList<TextLayerSettings> sortedLayers, int width, VisualizationFrameContext frame)
    {
        ArgumentNullException.ThrowIfNull(frame);
        if (width < 40)
        {
            return;
        }

        int rightColWidth = Math.Max(10, width - LeftColWidth - 1);
        var palette = _uiThemeResolver.GetEffectiveUiPalette();
        var (selBg, selFg) = MenuSelectionAffordance.GetSelectionColors(palette);
        double scrollSpeed = _uiSettings.DefaultScrollingSpeed;

        try
        {
            _navigation.View = TitleBarViewKind.PresetSettingsModal;
            _navigation.PresetSettingsPalettePickerActive = state.Focus == SettingsModalFocus.PickingPalette;
            var textLayers = _visualizerSettings.TextLayers ?? new TextLayersVisualizerSettings();
            var selectedLayer = !state.LeftPanelPresetSelected && sortedLayers.Count > 0 && state.SelectedLayerIndex < sortedLayers.Count
                ? sortedLayers[state.SelectedLayerIndex]
                : null;
            if (state.LeftPanelPresetSelected)
            {
                _navigation.PresetSettingsLayerOneBased = null;
                _navigation.PresetSettingsLayerTypeRaw = null;
            }
            else if (selectedLayer != null)
            {
                _navigation.PresetSettingsLayerOneBased = state.SelectedLayerIndex + 1;
                _navigation.PresetSettingsLayerTypeRaw = selectedLayer.LayerType.ToString();
            }
            else
            {
                _navigation.PresetSettingsLayerOneBased = null;
                _navigation.PresetSettingsLayerTypeRaw = null;
            }

            _navigation.PresetSettingsFocusedSettingId = TryGetFocusedSettingId(state, selectedLayer, textLayers);

            TitleBarBreadcrumbRow.Write(0, width, _breadcrumbFormatter);

            RenderHintRow(state, width, palette, scrollSpeed, HintRow, 1.0 / 60.0);
            System.Console.SetCursorPosition(0, SeparatorRow);
            System.Console.Write(StaticTextViewport.TruncateToWidth(new PlainText("  ─" + new string('─', LeftColWidth - 2) + "┬" + new string('─', rightColWidth) + "─"), width).PadRight(width));

            for (int r = FirstLayerRow; r < OverlayRowCount; r++)
            {
                System.Console.SetCursorPosition(LeftColWidth + 1, r);
                System.Console.Write(new string(' ', rightColWidth));
            }

            int presetRow = FirstLayerRow;
            string presetPrefix = MenuSelectionAffordance.GetPrefix(state.LeftPanelPresetSelected);
            string presetLeft = $"{presetPrefix}Preset";
            presetLeft = StaticTextViewport.TruncateWithEllipsis(new PlainText(presetLeft), LeftColWidth).PadRight(LeftColWidth);
            System.Console.SetCursorPosition(0, presetRow);
            string presetLeftWrite = state.LeftPanelPresetSelected && state.Focus == SettingsModalFocus.LayerList && !state.Renaming
                ? MenuSelectionAffordance.ApplyRowHighlight(true, presetLeft, selBg, selFg)
                : presetLeft;
            System.Console.Write(presetLeftWrite);
            System.Console.Write(" ");
            System.Console.Write(new string(' ', rightColWidth));

            int enabledLayerCountForTiming = 0;
            if (_uiSettings.ShowLayerRenderTime)
            {
                foreach (var l in sortedLayers)
                {
                    if (l.Enabled)
                    {
                        enabledLayerCountForTiming++;
                    }
                }
            }

            double perLayerBudgetMs = LayerRenderTimeFormatting.PerLayerBudgetMsFor60Fps(enabledLayerCountForTiming);

            for (int i = 0; i < sortedLayers.Count && i < TextLayersLimits.MaxLayerCount; i++)
            {
                int row = FirstLayerRow + 1 + i;
                if (row >= OverlayRowCount)
                {
                    break;
                }

                var layer = sortedLayers[i];
                string prefix = MenuSelectionAffordance.GetPrefix(!state.LeftPanelPresetSelected && i == state.SelectedLayerIndex);
                string enabledMark = layer.Enabled ? "●" : "○";
                string basePlain = $"{prefix}{enabledMark} {i + 1}. {layer.LayerType}";
                bool rowSelected = !state.LeftPanelPresetSelected && i == state.SelectedLayerIndex && state.Focus == SettingsModalFocus.LayerList && !state.Renaming;
                string leftLine;
                if (_uiSettings.ShowLayerRenderTime)
                {
                    double? ms = null;
                    var times = frame.LayerRenderTimeMs;
                    if (times != null && i < times.Length)
                    {
                        ms = times[i];
                    }

                    string suffixPlain = LayerRenderTimeFormatting.FormatEntrySuffix(ms);
                    int suffixCols = DisplayWidth.GetDisplayWidth(suffixPlain);
                    int baseMax = Math.Max(0, LeftColWidth - suffixCols);
                    string baseTrunc = StaticTextViewport.TruncateWithEllipsis(new PlainText(basePlain), baseMax);
                    if (rowSelected)
                    {
                        leftLine = new PlainText(baseTrunc + suffixPlain).PadToDisplayWidth(LeftColWidth);
                    }
                    else
                    {
                        PaletteColor tierFg = LayerRenderTimeFormatting.GetTierForeground(palette, ms, perLayerBudgetMs);
                        var sb = new StringBuilder();
                        sb.Append(baseTrunc);
                        AnsiConsole.AppendColored(sb, suffixPlain, tierFg);
                        leftLine = AnsiConsole.PadToDisplayWidth(sb.ToString(), LeftColWidth);
                    }
                }
                else
                {
                    leftLine = StaticTextViewport.TruncateWithEllipsis(new PlainText(basePlain), LeftColWidth).PadRight(LeftColWidth);
                }

                System.Console.SetCursorPosition(0, row);
                string leftLineToWrite = rowSelected
                    ? MenuSelectionAffordance.ApplyRowHighlight(true, leftLine, selBg, selFg)
                    : leftLine;
                System.Console.Write(leftLineToWrite);
                System.Console.Write(" ");
                System.Console.Write(new string(' ', rightColWidth));
            }

            bool showPresetPanel = state.LeftPanelPresetSelected;
            bool showLayerPanel = !state.LeftPanelPresetSelected && selectedLayer != null;

            if (state.Focus == SettingsModalFocus.PickingPalette)
            {
                SettingsSurfacesPaletteDrawing.DrawPicker(
                    _paletteRepo,
                    state,
                    LeftColWidth,
                    RightColumnContentStartRow,
                    SettingsVisibleRows,
                    rightColWidth,
                    selBg,
                    selFg,
                    frame.Analysis,
                    includeInheritFirst: !state.PalettePickerForPresetDefault);
            }
            else if (showPresetPanel)
            {
                var presetSettingsRows = PresetSettingsModalRows.Build(_visualizerSettings, textLayers, _paletteRepo);
                for (int vi = 0; vi < SettingsVisibleRows; vi++)
                {
                    if (vi >= presetSettingsRows.Count) { break; }
                    var row = presetSettingsRows[vi];
                    bool showBuffer = state.Focus == SettingsModalFocus.EditingSetting && vi == state.SelectedSettingIndex && row.EditMode == SettingEditMode.TextEdit;
                    bool selected = vi == state.SelectedSettingIndex && (state.Focus == SettingsModalFocus.SettingsList || state.Focus == SettingsModalFocus.EditingSetting);
                    System.Console.SetCursorPosition(LeftColWidth + 1, RightColumnContentStartRow + vi);
                    if (row.Id == PresetSettingsModalRows.DefaultPaletteId && !showBuffer)
                    {
                        System.Console.Write(SettingsSurfacesPaletteDrawing.FormatPresetDefaultPaletteSettingRow(
                            _paletteRepo,
                            textLayers,
                            rightColWidth,
                            selected,
                            selBg,
                            selFg,
                            frame.Analysis));
                        continue;
                    }

                    string aff = MenuSelectionAffordance.GetPrefix(selected);
                    string labelWithColon = string.IsNullOrEmpty(row.Label) ? "" : row.Label + ":";
                    string lineText = $"{aff}{labelWithColon}{(showBuffer ? state.EditingBuffer + "_" : row.DisplayValue)}";
                    string line = StaticTextViewport.TruncateWithEllipsis(new PlainText(lineText), rightColWidth);
                    string linePadded = AnsiConsole.PadToDisplayWidth(line, rightColWidth);
                    System.Console.Write(MenuSelectionAffordance.ApplyRowHighlight(selected, linePadded, selBg, selFg));
                }
            }
            else if (showLayerPanel)
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
                    bool rowSelected = i == state.SelectedSettingIndex && (state.Focus == SettingsModalFocus.SettingsList || state.Focus == SettingsModalFocus.EditingSetting);
                    System.Console.SetCursorPosition(LeftColWidth + 1, RightColumnContentStartRow + vi);
                    if (row.Id == "Palette" && !showBuffer)
                    {
                        System.Console.Write(SettingsSurfacesPaletteDrawing.FormatPaletteSettingRow(
                            _paletteRepo,
                            _visualizerSettings,
                            selectedLayer!,
                            rightColWidth,
                            rowSelected,
                            selBg,
                            selFg,
                            frame.Analysis));
                        continue;
                    }

                    string aff = MenuSelectionAffordance.GetPrefix(rowSelected);
                    string labelWithColon = string.IsNullOrEmpty(row.Label) ? "" : row.Label + ":";
                    string lineText = $"{aff}{labelWithColon}{(showBuffer ? state.EditingBuffer + "_" : row.DisplayValue)}";
                    string line = StaticTextViewport.TruncateWithEllipsis(new PlainText(lineText), rightColWidth);
                    string linePadded = AnsiConsole.PadToDisplayWidth(line, rightColWidth);
                    System.Console.Write(MenuSelectionAffordance.ApplyRowHighlight(rowSelected, linePadded, selBg, selFg));
                }
            }

            for (int row = FirstLayerRow + 1 + sortedLayers.Count; row < OverlayRowCount; row++)
            {
                System.Console.SetCursorPosition(0, row);
                System.Console.Write(new string(' ', LeftColWidth + 1));
            }

            if (showPresetPanel && state.Focus != SettingsModalFocus.PickingPalette)
            {
                _lastIdlePresetDefaultPalettePhase = GetPalettePhaseForPresetDefault(textLayers, frame);
            }
            else
            {
                _lastIdlePresetDefaultPalettePhase = int.MinValue;
            }

            if (showLayerPanel && state.Focus != SettingsModalFocus.PickingPalette)
            {
                _lastIdlePalettePhase = GetPalettePhaseForLayer(selectedLayer!, frame);
            }
            else if (!showLayerPanel)
            {
                _lastIdlePalettePhase = int.MinValue;
            }

            if (state.Focus == SettingsModalFocus.PickingPalette)
            {
                SyncPickerIdleTracking(frame);
            }
            else
            {
                _lastPickerIdleBeatCount = -1;
                _lastPickerIdleTickBucket = -1;
            }
        }
        catch (Exception ex)
        {
            /* Console unavailable during settings modal draw: swallow to avoid crashing the app loop */
            _ = ex;
        }
    }

    /// <inheritdoc />
    public void DrawIdleOverlayTick(SettingsModalState state, IReadOnlyList<TextLayerSettings> sortedLayers, int width, VisualizationFrameContext frame)
    {
        if (width < 40)
        {
            return;
        }

        double scrollSpeed = _uiSettings.DefaultScrollingSpeed;
        var palette = _uiThemeResolver.GetEffectiveUiPalette();
        try
        {
            System.Console.Write(SyncOutputBegin);
            RenderHintRow(state, width, palette, scrollSpeed, HintRow, 0.05);
            TryRedrawPaletteRowForIdleInOpenSyncFrame(state, sortedLayers, width, frame);
            System.Console.Write(SyncOutputEnd);
        }
        catch (Exception ex)
        {
            /* Console unavailable during settings modal idle tick: swallow to avoid crashing */
            _ = ex;
        }
    }

    /// <summary>Updates palette cell if phase advanced; caller must wrap the whole idle frame in synchronized output.</summary>
    private void TryRedrawPaletteRowForIdleInOpenSyncFrame(SettingsModalState state, IReadOnlyList<TextLayerSettings> sortedLayers, int width, VisualizationFrameContext frame)
    {
        if (width < 40)
        {
            return;
        }

        if (state.Focus == SettingsModalFocus.PickingPalette)
        {
            TryRedrawPalettePickerForIdle(state, width, frame);
            return;
        }

        var textLayers = _visualizerSettings.TextLayers ?? new TextLayersVisualizerSettings();
        if (state.LeftPanelPresetSelected)
        {
            int phase = GetPalettePhaseForPresetDefault(textLayers, frame);
            if (phase == _lastIdlePresetDefaultPalettePhase)
            {
                return;
            }

            int rightColWidth = Math.Max(10, width - LeftColWidth - 1);
            var presetRows = PresetSettingsModalRows.Build(_visualizerSettings, textLayers, _paletteRepo);
            int paletteRowIndex = -1;
            for (int i = 0; i < presetRows.Count; i++)
            {
                if (presetRows[i].Id == PresetSettingsModalRows.DefaultPaletteId)
                {
                    paletteRowIndex = i;
                    break;
                }
            }

            if (paletteRowIndex < 0)
            {
                _lastIdlePresetDefaultPalettePhase = phase;
                return;
            }

            int vi = paletteRowIndex;
            var uiPalette = _uiThemeResolver.GetEffectiveUiPalette();
            var (selBg, selFg) = MenuSelectionAffordance.GetSelectionColors(uiPalette);
            var row = presetRows[paletteRowIndex];
            bool showBuffer = state.Focus == SettingsModalFocus.EditingSetting && paletteRowIndex == state.SelectedSettingIndex && row.EditMode == SettingEditMode.TextEdit;
            if (showBuffer)
            {
                _lastIdlePresetDefaultPalettePhase = phase;
                return;
            }

            bool selected = paletteRowIndex == state.SelectedSettingIndex && (state.Focus == SettingsModalFocus.SettingsList || state.Focus == SettingsModalFocus.EditingSetting);
            string line = SettingsSurfacesPaletteDrawing.FormatPresetDefaultPaletteSettingRow(
                _paletteRepo,
                textLayers,
                rightColWidth,
                selected,
                selBg,
                selFg,
                frame.Analysis);

            try
            {
                System.Console.SetCursorPosition(LeftColWidth + 1, RightColumnContentStartRow + vi);
                System.Console.Write(line);
                _lastIdlePresetDefaultPalettePhase = phase;
            }
            catch (Exception ex)
            {
                /* Console unavailable: swallow */
                _ = ex;
            }

            return;
        }

        var selectedLayer = !state.LeftPanelPresetSelected && sortedLayers.Count > 0 && state.SelectedLayerIndex < sortedLayers.Count
            ? sortedLayers[state.SelectedLayerIndex]
            : null;
        if (selectedLayer == null)
        {
            return;
        }

        int layerPhase = GetPalettePhaseForLayer(selectedLayer, frame);
        if (layerPhase == _lastIdlePalettePhase)
        {
            return;
        }

        int rightColW = Math.Max(10, width - LeftColWidth - 1);
        var settingsRows = GetSettingsRows(selectedLayer);
        int paletteRowIndexLayer = -1;
        for (int i = 0; i < settingsRows.Count; i++)
        {
            if (settingsRows[i].Id == "Palette")
            {
                paletteRowIndexLayer = i;
                break;
            }
        }

        if (paletteRowIndexLayer < 0)
        {
            _lastIdlePalettePhase = layerPhase;
            return;
        }

        int settingsScrollOffset = settingsRows.Count <= SettingsVisibleRows
            ? 0
            : Math.Clamp(state.SelectedSettingIndex - (SettingsVisibleRows - 1), 0, settingsRows.Count - SettingsVisibleRows);
        int viLayer = paletteRowIndexLayer - settingsScrollOffset;
        if (viLayer < 0 || viLayer >= SettingsVisibleRows)
        {
            _lastIdlePalettePhase = layerPhase;
            return;
        }

        var uiPal = _uiThemeResolver.GetEffectiveUiPalette();
        var (selBgL, selFgL) = MenuSelectionAffordance.GetSelectionColors(uiPal);
        var rowL = settingsRows[paletteRowIndexLayer];
        bool showBufferL = state.Focus == SettingsModalFocus.EditingSetting && paletteRowIndexLayer == state.SelectedSettingIndex && rowL.EditMode == SettingEditMode.TextEdit;
        if (rowL.Id != "Palette" || showBufferL)
        {
            _lastIdlePalettePhase = layerPhase;
            return;
        }

        bool selectedL = paletteRowIndexLayer == state.SelectedSettingIndex && (state.Focus == SettingsModalFocus.SettingsList || state.Focus == SettingsModalFocus.EditingSetting);
        string lineL = SettingsSurfacesPaletteDrawing.FormatPaletteSettingRow(
            _paletteRepo,
            _visualizerSettings,
            selectedLayer,
            rightColW,
            selectedL,
            selBgL,
            selFgL,
            frame.Analysis);

        try
        {
            System.Console.SetCursorPosition(LeftColWidth + 1, RightColumnContentStartRow + viLayer);
            System.Console.Write(lineL);
            _lastIdlePalettePhase = layerPhase;
        }
        catch (Exception ex)
        {
            /* Console unavailable: swallow */
            _ = ex;
        }
    }

    private void SyncPickerIdleTracking(VisualizationFrameContext frame)
    {
        _lastPickerIdleBeatCount = frame.Analysis.BeatCount;
        _lastPickerIdleTickBucket = PaletteSwatchFormatter.GetPaletteAnimationTickBucket();
    }

    private void TryRedrawPalettePickerForIdle(SettingsModalState state, int width, VisualizationFrameContext frame)
    {
        if (!PaletteSwatchFormatter.PaletteAnimationFrameAdvanced(frame.Analysis, _lastPickerIdleBeatCount, _lastPickerIdleTickBucket, out int beatCount, out long tickBucket))
        {
            return;
        }

        int rightColWidth = Math.Max(10, width - LeftColWidth - 1);
        var uiPalette = _uiThemeResolver.GetEffectiveUiPalette();
        var (selBg, selFg) = MenuSelectionAffordance.GetSelectionColors(uiPalette);
        try
        {
            SettingsSurfacesPaletteDrawing.DrawPicker(
                _paletteRepo,
                state,
                LeftColWidth,
                RightColumnContentStartRow,
                SettingsVisibleRows,
                rightColWidth,
                selBg,
                selFg,
                frame.Analysis,
                includeInheritFirst: !state.PalettePickerForPresetDefault);
            _lastPickerIdleBeatCount = beatCount;
            _lastPickerIdleTickBucket = tickBucket;
        }
        catch (Exception ex)
        {
            /* Console unavailable: swallow */
            _ = ex;
        }
    }

    private int GetPalettePhaseForLayer(TextLayerSettings layer, VisualizationFrameContext frame)
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
        return PaletteSwatchFormatter.ComputeToolbarPhaseOffset(frame.Analysis, colors?.Count ?? 0);
    }

    private int GetPalettePhaseForPresetDefault(TextLayersVisualizerSettings textLayers, VisualizationFrameContext frame)
    {
        string effectiveId = textLayers.PaletteId ?? "";
        if (string.IsNullOrWhiteSpace(effectiveId))
        {
            effectiveId = "default";
        }

        var def = _paletteRepo.GetById(effectiveId);
        var colors = ColorPaletteParser.Parse(def);
        return PaletteSwatchFormatter.ComputeToolbarPhaseOffset(frame.Analysis, colors?.Count ?? 0);
    }

    private void RenderHintRow(SettingsModalState state, int width, UiPalette palette, double scrollSpeed, int startRow, double frameDeltaSeconds)
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
            ScrollSpeed = scrollSpeed,
            FrameDeltaSeconds = frameDeltaSeconds > 0 ? frameDeltaSeconds : 1.0 / 60.0
        };
        _componentRenderer.Render(_hintRoot, context);
    }

    private static string GetHintText(SettingsModalState state)
    {
        return state.Renaming ? "  Type new name, Enter to save, Esc to cancel"
            : state.Focus == SettingsModalFocus.EditingSetting ? "  Type value, Enter or \u2191\u2193 confirm, Esc cancel"
            : state.Focus == SettingsModalFocus.PickingPalette ? "  \u2191\u2193 or +/- preview, Enter save, Esc discard"
            : state.Focus == SettingsModalFocus.SettingsList ? "  \u2191\u2193 select, Enter or +/- cycle (Enter = palette list on Palette), Enter edit strings, \u2190 or Esc back"
            : "  1-9 layer, Ins add, Del remove, \u2191\u2192 Preset or layers, \u2190\u2192 type, Ctrl+\u2191\u2193 move, Enter settings, Shift+1-9 toggle, R rename, N preset, Esc close";
    }

    private List<(string Id, string Label, string DisplayValue, SettingEditMode EditMode)> GetSettingsRows(TextLayerSettings? layer)
    {
        if (layer == null) { return []; }
        var descriptors = SettingDescriptor.BuildAll(layer, _paletteRepo, _asciiVideoDeviceCatalog);
        return descriptors.Select(d => (d.Id, d.Label, d.GetDisplayValue(layer), d.EditMode)).ToList();
    }

    private string? TryGetFocusedSettingId(SettingsModalState state, TextLayerSettings? selectedLayer, TextLayersVisualizerSettings textLayers)
    {
        if (state.Focus != SettingsModalFocus.SettingsList
            && state.Focus != SettingsModalFocus.EditingSetting
            && state.Focus != SettingsModalFocus.PickingPalette)
        {
            return null;
        }

        if (state.LeftPanelPresetSelected)
        {
            var rows = PresetSettingsModalRows.Build(_visualizerSettings, textLayers, _paletteRepo);
            if (state.SelectedSettingIndex < 0 || state.SelectedSettingIndex >= rows.Count)
            {
                return null;
            }

            return rows[state.SelectedSettingIndex].Id;
        }

        if (selectedLayer == null)
        {
            return null;
        }

        var layerRows = GetSettingsRows(selectedLayer);
        if (state.SelectedSettingIndex < 0 || state.SelectedSettingIndex >= layerRows.Count)
        {
            return null;
        }

        return layerRows[state.SelectedSettingIndex].Id;
    }

}
