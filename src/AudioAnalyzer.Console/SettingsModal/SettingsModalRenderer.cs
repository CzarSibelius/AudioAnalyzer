using AudioAnalyzer.Application;
using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Application.Display;
using AudioAnalyzer.Domain;
using AudioAnalyzer.Visualizers;

namespace AudioAnalyzer.Console;

/// <summary>Renders the settings overlay modal: frame, preset title, hint line, layer list, and settings panel.</summary>
internal sealed class SettingsModalRenderer : ISettingsModalRenderer
{
    private const int OverlayRowCount = 18;
    private const int LeftColWidth = 28;
    private static readonly int SettingsVisibleRows = OverlayRowCount - 5;

    private readonly VisualizerSettings _visualizerSettings;
    private readonly UiSettings _uiSettings;
    private readonly IPaletteRepository _paletteRepo;
    private readonly IScrollingTextViewport _hintViewport;

    public SettingsModalRenderer(
        VisualizerSettings visualizerSettings,
        UiSettings uiSettings,
        IPaletteRepository paletteRepo,
        IScrollingTextViewportFactory viewportFactory)
    {
        _visualizerSettings = visualizerSettings ?? throw new ArgumentNullException(nameof(visualizerSettings));
        _uiSettings = uiSettings ?? throw new ArgumentNullException(nameof(uiSettings));
        _paletteRepo = paletteRepo ?? throw new ArgumentNullException(nameof(paletteRepo));
        _hintViewport = (viewportFactory ?? throw new ArgumentNullException(nameof(viewportFactory))).CreateViewport();
    }

    /// <inheritdoc />
    public void Draw(SettingsModalState state, IReadOnlyList<TextLayerSettings> sortedLayers, int width)
    {
        for (int r = 0; r < OverlayRowCount; r++)
        {
            try
            {
                System.Console.SetCursorPosition(0, r);
            }
            catch (Exception ex) { _ = ex; /* Console position failed */ }
        }

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
            var activePreset = _visualizerSettings.Presets?.FirstOrDefault(p =>
                string.Equals(p.Id, _visualizerSettings.ActivePresetId, StringComparison.OrdinalIgnoreCase))
                ?? _visualizerSettings.Presets?.FirstOrDefault();
            string presetName = activePreset?.Name?.Trim() ?? "Preset 1";
            string title = state.Renaming
                ? $" New preset name (Enter confirm, Esc cancel): {state.RenameBuffer}_ "
                : $" Preset: {presetName} (R rename) ";
            string titleTruncated = StaticTextViewport.TruncateWithEllipsis(new PlainText(title), width - 2);
            int pad = Math.Max(0, (width - titleTruncated.Length - 2) / 2);
            System.Console.SetCursorPosition(0, 0);
            System.Console.Write(StaticTextViewport.TruncateToWidth(new PlainText("╔" + new string('═', width - 2) + "╗"), width).PadRight(width));
            System.Console.SetCursorPosition(0, 1);
            System.Console.Write(StaticTextViewport.TruncateToWidth(new PlainText("║" + new string(' ', pad) + titleTruncated + new string(' ', width - pad - titleTruncated.Length - 2) + "║"), width).PadRight(width));
            System.Console.SetCursorPosition(0, 2);
            System.Console.Write(StaticTextViewport.TruncateToWidth(new PlainText("╚" + new string('═', width - 2) + "╝"), width).PadRight(width));
            System.Console.SetCursorPosition(0, 3);
            string hint = GetHintText(state);
            string hintLine = hint.Length > width
                ? _hintViewport.Render(new PlainText(hint), width, scrollSpeed)
                : hint.PadRight(width);
            System.Console.Write(hintLine);
            System.Console.SetCursorPosition(0, 4);
            System.Console.Write(StaticTextViewport.TruncateToWidth(new PlainText("  ─" + new string('─', LeftColWidth - 2) + "┬" + new string('─', rightColWidth) + "─"), width).PadRight(width));

            var selectedLayer = sortedLayers.Count > 0 && state.SelectedLayerIndex < sortedLayers.Count
                ? sortedLayers[state.SelectedLayerIndex]
                : null;

            for (int r = 5; r < OverlayRowCount; r++)
            {
                System.Console.SetCursorPosition(LeftColWidth + 1, r);
                System.Console.Write(new string(' ', rightColWidth));
            }

            for (int i = 0; i < sortedLayers.Count && i < 9; i++)
            {
                int row = 5 + i;
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
                    string lineText = $"{row.Label}: {(showBuffer ? state.EditingBuffer + "_" : row.DisplayValue)}";
                    string line = StaticTextViewport.TruncateWithEllipsis(new PlainText(lineText), rightColWidth);
                    string linePadded = line.PadRight(rightColWidth);
                    string lineToWrite = (i == state.SelectedSettingIndex && (state.Focus == SettingsModalFocus.SettingsList || state.Focus == SettingsModalFocus.EditingSetting))
                        ? AnsiConsole.BackgroundCode(selBg) + AnsiConsole.ColorCode(selFg) + linePadded + AnsiConsole.ResetCode
                        : linePadded;
                    System.Console.SetCursorPosition(LeftColWidth + 1, 5 + vi);
                    System.Console.Write(lineToWrite);
                }
            }

            for (int row = 5 + sortedLayers.Count; row < OverlayRowCount; row++)
            {
                System.Console.SetCursorPosition(0, row);
                System.Console.Write(new string(' ', LeftColWidth + 1));
            }
        }
        catch (Exception ex) { _ = ex; /* Draw settings modal failed */ }
    }

    /// <inheritdoc />
    public void DrawHintLine(SettingsModalState state, int width)
    {
        if (width < 40) { return; }
        string hint = GetHintText(state);
        double scrollSpeed = _uiSettings?.DefaultScrollingSpeed ?? 0.25;
        string hintLine = hint.Length > width
            ? _hintViewport.Render(new PlainText(hint), width, scrollSpeed)
            : hint.PadRight(width);
        try
        {
            System.Console.SetCursorPosition(0, 3);
            System.Console.Write(hintLine);
        }
        catch (Exception ex) { _ = ex; /* Console write failed in hint line */ }
    }

    private static string GetHintText(SettingsModalState state)
    {
        return state.Renaming ? "  Type new name, Enter to save, Esc to cancel"
            : state.Focus == SettingsModalFocus.EditingSetting ? "  Type value, Enter or \u2191\u2193 confirm, Esc cancel"
            : state.Focus == SettingsModalFocus.SettingsList ? "  \u2191\u2193 select, Enter or +/- cycle, Enter edit strings, \u2190 or Esc back"
            : "  1-9 select, \u2190\u2192 type, Ctrl+\u2191\u2193 move, Enter settings, Shift+1-9 toggle, R rename, N preset, Esc close";
    }

    private List<(string Id, string Label, string DisplayValue, SettingEditMode EditMode)> GetSettingsRows(TextLayerSettings? layer)
    {
        if (layer == null) { return []; }
        var descriptors = SettingDescriptor.BuildAll(layer, _paletteRepo);
        return descriptors.Select(d => (d.Id, d.Label, d.GetDisplayValue(layer), d.EditMode)).ToList();
    }
}
