using AudioAnalyzer.Application;
using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;
using AudioAnalyzer.Visualizers;

namespace AudioAnalyzer.Console;

/// <summary>TextLayers settings overlay modal (S key). Layer list, settings panel, preset rename and create per ADR-0023.</summary>
internal static class SettingsModal
{
    private const int OverlayRowCount = 18;

    /// <summary>Shows the settings overlay modal. Blocks until user closes with ESC.</summary>
    public static void Show(AnalysisEngine analysisEngine, VisualizerSettings visualizerSettings, IPresetRepository presetRepository, IPaletteRepository paletteRepo, object consoleLock, Action saveSettings)
    {
        var textLayers = visualizerSettings.TextLayers ?? new TextLayersVisualizerSettings();
        var layers = textLayers.Layers ?? new List<TextLayerSettings>();
        var sortedLayers = layers.OrderBy(l => l.ZOrder).ToList();
        if (sortedLayers.Count == 0)
        {
            sortedLayers = new List<TextLayerSettings>();
        }

        int selectedIndex = 0;
        bool renaming = false;
        string renameBuffer = "";
        SettingsModalFocus focus = SettingsModalFocus.LayerList;
        int selectedSettingIndex = 0;
        string editingBuffer = "";
        const int LeftColWidth = 28;
        int width = ConsoleHeader.GetConsoleWidth();
        int rightColWidth = Math.Max(10, width - LeftColWidth - 3);
        ScrollingTextViewportState settingsHintScrollState = default;
        string? settingsHintLastText = null;
        ConsoleKey? lastNavKey = null;
        long lastNavTime = 0;
        const int NavKeyRepeatMs = 120;

        IReadOnlyList<(string Id, string Label, string DisplayValue, SettingEditMode EditMode)> GetSettingsRows(TextLayerSettings? layer)
        {
            if (layer == null) { return []; }
            var descriptors = SettingDescriptor.BuildAll(layer, paletteRepo);
            return descriptors.Select(d => (d.Id, d.Label, d.GetDisplayValue(layer), d.EditMode)).ToList();
        }

        void DrawHintLineOnly()
        {
            if (width < 40) { return; }
            string hint = renaming ? "  Type new name, Enter to save, Esc to cancel"
                : focus == SettingsModalFocus.EditingSetting ? "  Type value, Enter or \u2191\u2193 confirm, Esc cancel"
                : focus == SettingsModalFocus.SettingsList ? "  \u2191\u2193 select, Enter or +/- cycle, Enter edit strings, \u2190 or Esc back"
                : "  1-9 select, \u2190\u2192 type, Enter settings, Shift+1-9 toggle, R rename, N preset, Esc close";
            if (hint != settingsHintLastText)
            {
                settingsHintScrollState.Reset();
                settingsHintLastText = hint;
            }
            string hintLine = hint.Length > width
                ? ScrollingTextViewport.Render(hint, width, ref settingsHintScrollState, 0.25)
                : hint.PadRight(width);
            try
            {
                System.Console.SetCursorPosition(0, 3);
                System.Console.Write(hintLine);
            }
            catch (Exception ex) { _ = ex; /* Console write failed in hint line */ }
        }

        void DrawSettingsContent()
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

            try
            {
                var activePreset = visualizerSettings.Presets?.FirstOrDefault(p =>
                    string.Equals(p.Id, visualizerSettings.ActivePresetId, StringComparison.OrdinalIgnoreCase))
                    ?? visualizerSettings.Presets?.FirstOrDefault();
                string presetName = activePreset?.Name?.Trim() ?? "Preset 1";
                string title = renaming
                    ? $" New preset name (Enter confirm, Esc cancel): {renameBuffer}_ "
                    : $" Preset: {presetName} (R rename) ";
                string titleTruncated = VisualizerViewport.TruncateWithEllipsis(title, width - 2);
                int pad = Math.Max(0, (width - titleTruncated.Length - 2) / 2);
                System.Console.SetCursorPosition(0, 0);
                System.Console.Write(VisualizerViewport.TruncateToWidth("╔" + new string('═', width - 2) + "╗", width).PadRight(width));
                System.Console.SetCursorPosition(0, 1);
                System.Console.Write(VisualizerViewport.TruncateToWidth("║" + new string(' ', pad) + titleTruncated + new string(' ', width - pad - titleTruncated.Length - 2) + "║", width).PadRight(width));
                System.Console.SetCursorPosition(0, 2);
                System.Console.Write(VisualizerViewport.TruncateToWidth("╚" + new string('═', width - 2) + "╝", width).PadRight(width));
                System.Console.SetCursorPosition(0, 3);
                string hint = renaming ? "  Type new name, Enter to save, Esc to cancel"
                    : focus == SettingsModalFocus.EditingSetting ? "  Type value, Enter or \u2191\u2193 confirm, Esc cancel"
                    : focus == SettingsModalFocus.SettingsList ? "  \u2191\u2193 select, Enter or +/- cycle, Enter edit strings, \u2190 or Esc back"
                    : "  1-9 select, \u2190\u2192 type, Enter settings, Shift+1-9 toggle, R rename, N preset, Esc close";
                if (hint != settingsHintLastText)
                {
                    settingsHintScrollState.Reset();
                    settingsHintLastText = hint;
                }
                string hintLine = hint.Length > width
                    ? ScrollingTextViewport.Render(hint, width, ref settingsHintScrollState, 0.25)
                    : hint.PadRight(width);
                System.Console.Write(hintLine);
                System.Console.SetCursorPosition(0, 4);
                System.Console.Write(VisualizerViewport.TruncateToWidth("  ─" + new string('─', LeftColWidth - 2) + "┬" + new string('─', rightColWidth) + "─", width).PadRight(width));

                var selectedLayer = sortedLayers.Count > 0 && selectedIndex < sortedLayers.Count
                    ? sortedLayers[selectedIndex]
                    : null;

                for (int r = 5; r < OverlayRowCount; r++)
                {
                    System.Console.SetCursorPosition(LeftColWidth + 3, r);
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
                    string prefix = i == selectedIndex ? " ► " : "   ";
                    string enabledMark = layer.Enabled ? "●" : "○";
                    string leftLine = $"{prefix}{enabledMark} {i + 1}. {layer.LayerType}";
                    leftLine = VisualizerViewport.TruncateWithEllipsis(leftLine, LeftColWidth).PadRight(LeftColWidth);

                    System.Console.SetCursorPosition(0, row);
                    if (i == selectedIndex && focus == SettingsModalFocus.LayerList && !renaming)
                    {
                        System.Console.BackgroundColor = ConsoleColor.DarkBlue;
                        System.Console.ForegroundColor = ConsoleColor.White;
                    }
                    System.Console.Write(leftLine);
                    System.Console.ResetColor();
                    System.Console.Write(" │ ");
                    System.Console.Write(new string(' ', rightColWidth));
                }

                if (selectedLayer != null)
                {
                    var settingsRows = GetSettingsRows(selectedLayer);
                    for (int i = 0; i < settingsRows.Count && (5 + i) < OverlayRowCount; i++)
                    {
                        var row = settingsRows[i];
                        bool showBuffer = focus == SettingsModalFocus.EditingSetting && i == selectedSettingIndex && row.EditMode == SettingEditMode.TextEdit;
                        string lineText = $"{row.Label}: {(showBuffer ? editingBuffer + "_" : row.DisplayValue)}";
                        string line = VisualizerViewport.TruncateWithEllipsis(lineText, rightColWidth);
                        System.Console.SetCursorPosition(LeftColWidth + 3, 5 + i);
                        if (i == selectedSettingIndex && (focus == SettingsModalFocus.SettingsList || focus == SettingsModalFocus.EditingSetting))
                        {
                            System.Console.BackgroundColor = ConsoleColor.DarkBlue;
                            System.Console.ForegroundColor = ConsoleColor.White;
                        }
                        System.Console.Write(line.PadRight(rightColWidth));
                        System.Console.ResetColor();
                    }
                }

                for (int row = 5 + sortedLayers.Count; row < OverlayRowCount; row++)
                {
                    System.Console.SetCursorPosition(0, row);
                    System.Console.Write(new string(' ', width));
                }
            }
            catch (Exception ex) { _ = ex; /* Draw settings modal failed */ }
        }

        void ApplySettingEdit(TextLayerSettings layer, string settingId, string value)
        {
            var descriptors = SettingDescriptor.BuildAll(layer, paletteRepo);
            var d = descriptors.FirstOrDefault(x => x.Id == settingId);
            d?.ApplyEdit(layer, value);
        }

        void CycleSetting(TextLayerSettings layer, string id, bool forward)
        {
            var descriptors = SettingDescriptor.BuildAll(layer, paletteRepo);
            var d = descriptors.FirstOrDefault(x => x.Id == id);
            d?.Cycle(layer, forward);
        }

        static int DigitFromKey(ConsoleKey key)
        {
            return key switch
            {
                ConsoleKey.D1 or ConsoleKey.NumPad1 => 1,
                ConsoleKey.D2 or ConsoleKey.NumPad2 => 2,
                ConsoleKey.D3 or ConsoleKey.NumPad3 => 3,
                ConsoleKey.D4 or ConsoleKey.NumPad4 => 4,
                ConsoleKey.D5 or ConsoleKey.NumPad5 => 5,
                ConsoleKey.D6 or ConsoleKey.NumPad6 => 6,
                ConsoleKey.D7 or ConsoleKey.NumPad7 => 7,
                ConsoleKey.D8 or ConsoleKey.NumPad8 => 8,
                ConsoleKey.D9 or ConsoleKey.NumPad9 => 9,
                _ => 0
            };
        }

        bool HandleSettingsKey(ConsoleKeyInfo key)
        {
            // Rate-limit arrow keys to avoid jumpy navigation from OS key repeat
            static bool IsNavKey(ConsoleKey k) =>
                k is ConsoleKey.UpArrow or ConsoleKey.DownArrow or ConsoleKey.LeftArrow or ConsoleKey.RightArrow;
            if (IsNavKey(key.Key))
            {
                var now = Environment.TickCount64;
                if (lastNavKey == key.Key && (now - lastNavTime) < NavKeyRepeatMs)
                {
                    return false;
                }
                lastNavKey = key.Key;
                lastNavTime = now;
            }
            else
            {
                lastNavKey = null;
            }

            var selectedLayer = sortedLayers.Count > 0 && selectedIndex < sortedLayers.Count ? sortedLayers[selectedIndex] : null;
            var settingsRows = selectedLayer != null ? GetSettingsRows(selectedLayer) : [];

            if (focus == SettingsModalFocus.Renaming)
            {
                if (key.Key == ConsoleKey.Escape) { renaming = false; focus = SettingsModalFocus.LayerList; return false; }
                if (key.Key == ConsoleKey.Enter)
                {
                    var activeId = visualizerSettings.ActivePresetId;
                    if (!string.IsNullOrWhiteSpace(activeId) && !string.IsNullOrWhiteSpace(renameBuffer))
                    {
                        var preset = presetRepository.GetById(activeId);
                        if (preset != null)
                        {
                            preset.Name = renameBuffer.Trim();
                            preset.Config = (visualizerSettings.TextLayers ?? new TextLayersVisualizerSettings()).DeepCopy();
                            presetRepository.Save(activeId, preset);
                            var p = visualizerSettings.Presets?.FirstOrDefault(x => string.Equals(x.Id, activeId, StringComparison.OrdinalIgnoreCase));
                            if (p != null) { p.Name = renameBuffer.Trim(); }
                        }
                        saveSettings();
                    }
                    renaming = false;
                    focus = SettingsModalFocus.LayerList;
                    return false;
                }
                if (key.Key == ConsoleKey.Backspace && renameBuffer.Length > 0) { renameBuffer = renameBuffer[..^1]; return false; }
                if (key.KeyChar is >= ' ' and <= '~' && key.KeyChar >= ' ') { renameBuffer += key.KeyChar; return false; }
            }

            if (focus == SettingsModalFocus.EditingSetting && selectedLayer != null && selectedSettingIndex < settingsRows.Count)
            {
                var row = settingsRows[selectedSettingIndex];
                if (key.Key == ConsoleKey.Escape) { focus = SettingsModalFocus.SettingsList; return false; }
                if (row.EditMode == SettingEditMode.TextEdit &&
                    (key.Key == ConsoleKey.Enter || key.Key == ConsoleKey.UpArrow || key.Key == ConsoleKey.DownArrow))
                {
                    ApplySettingEdit(selectedLayer, row.Id, editingBuffer);
                    saveSettings();
                    focus = SettingsModalFocus.SettingsList;
                    return false;
                }
                if (row.EditMode == SettingEditMode.TextEdit && key.Key == ConsoleKey.Backspace) { if (editingBuffer.Length > 0) { editingBuffer = editingBuffer[..^1]; } return false; }
                if (row.EditMode == SettingEditMode.TextEdit && key.KeyChar is >= ' ' and <= '~') { editingBuffer += key.KeyChar; return false; }

                if (row.EditMode == SettingEditMode.Cycle && (key.Key == ConsoleKey.LeftArrow || key.Key == ConsoleKey.RightArrow || key.Key == ConsoleKey.Enter))
                {
                    CycleSetting(selectedLayer, row.Id, key.Key == ConsoleKey.RightArrow || key.Key == ConsoleKey.Enter);
                    saveSettings();
                    focus = SettingsModalFocus.SettingsList;
                    return false;
                }
                return false;
            }

            if (focus == SettingsModalFocus.SettingsList)
            {
                if (key.Key == ConsoleKey.LeftArrow || key.Key == ConsoleKey.Escape) { focus = SettingsModalFocus.LayerList; return false; }
                if (key.Key == ConsoleKey.UpArrow) { selectedSettingIndex = Math.Max(0, selectedSettingIndex - 1); return false; }
                if (key.Key == ConsoleKey.DownArrow) { selectedSettingIndex = Math.Min(settingsRows.Count - 1, selectedSettingIndex + 1); return false; }
                if (selectedLayer != null && selectedSettingIndex < settingsRows.Count)
                {
                    var row = settingsRows[selectedSettingIndex];
                    bool cycleForward = key.Key == ConsoleKey.Enter || key.Key == ConsoleKey.Add || key.Key == ConsoleKey.OemPlus;
                    bool cycleBackward = key.Key == ConsoleKey.Subtract || key.Key == ConsoleKey.OemMinus;
                    if (row.EditMode == SettingEditMode.Cycle && (cycleForward || cycleBackward))
                    {
                        CycleSetting(selectedLayer, row.Id, cycleForward);
                        saveSettings();
                        return false;
                    }
                    if (key.Key == ConsoleKey.Enter && row.EditMode == SettingEditMode.TextEdit)
                    {
                        editingBuffer = row.Id == "Snippets"
                            ? (selectedLayer.TextSnippets is { Count: > 0 } ? string.Join(", ", selectedLayer.TextSnippets) : "")
                            : row.Id == "ImagePath"
                                ? ((selectedLayer.GetCustom<AsciiImageSettings>() ?? new AsciiImageSettings()).ImageFolderPath ?? "")
                                : row.DisplayValue;
                        focus = SettingsModalFocus.EditingSetting;
                        return false;
                    }
                }
            }

            if (focus == SettingsModalFocus.LayerList)
            {
                if (key.Key == ConsoleKey.Escape) { return true; }
                if (key.Key == ConsoleKey.Enter && selectedLayer != null)
                {
                    focus = SettingsModalFocus.SettingsList;
                    selectedSettingIndex = 0;
                    return false;
                }
                if (key.Key == ConsoleKey.UpArrow) { selectedIndex = sortedLayers.Count > 0 ? (selectedIndex - 1 + sortedLayers.Count) % sortedLayers.Count : 0; return false; }
                if (key.Key == ConsoleKey.DownArrow) { selectedIndex = sortedLayers.Count > 0 ? (selectedIndex + 1) % sortedLayers.Count : 0; return false; }
                if (key.Key == ConsoleKey.Spacebar && selectedLayer != null) { selectedLayer.Enabled = !selectedLayer.Enabled; saveSettings(); return false; }
                if (key.Key == ConsoleKey.LeftArrow)
                {
                    if (selectedLayer != null) { selectedLayer.LayerType = TextLayerSettings.CycleTypeBackward(selectedLayer); saveSettings(); }
                    return false;
                }
                if (key.Key == ConsoleKey.RightArrow)
                {
                    if (selectedLayer != null) { selectedLayer.LayerType = TextLayerSettings.CycleTypeForward(selectedLayer); saveSettings(); }
                    return false;
                }
                if (key.Key == ConsoleKey.R)
                {
                    if (visualizerSettings.Presets is { Count: > 0 })
                    {
                        var p = visualizerSettings.Presets.FirstOrDefault(x => string.Equals(x.Id, visualizerSettings.ActivePresetId, StringComparison.OrdinalIgnoreCase)) ?? visualizerSettings.Presets[0];
                        renameBuffer = p.Name?.Trim() ?? "";
                        renaming = true;
                        focus = SettingsModalFocus.Renaming;
                    }
                    return false;
                }
                if (key.Key == ConsoleKey.Backspace && renaming) { if (renameBuffer.Length > 0) { renameBuffer = renameBuffer[..^1]; } return false; }
                if (key.Key == ConsoleKey.N)
                {
                    var newPreset = new Preset { Name = $"Preset {visualizerSettings.Presets?.Count + 1 ?? 1}", Config = textLayers.DeepCopy() };
                    var createdId = presetRepository.Create(newPreset);
                    visualizerSettings.ActivePresetId = createdId;
                    visualizerSettings.Presets = presetRepository.GetAll().Select(p => new Preset { Id = p.Id, Name = p.Name, Config = new TextLayersVisualizerSettings() }).ToList();
                    textLayers = visualizerSettings.TextLayers ?? new TextLayersVisualizerSettings();
                    sortedLayers = textLayers.Layers?.OrderBy(l => l.ZOrder).ToList() ?? [];
                    saveSettings();
                    return false;
                }
                int digit = DigitFromKey(key.Key);
                if (digit != 0)
                {
                    int layerIdx = digit - 1;
                    if (layerIdx < sortedLayers.Count)
                    {
                        if (key.Modifiers.HasFlag(ConsoleModifiers.Shift)) { sortedLayers[layerIdx].Enabled = !sortedLayers[layerIdx].Enabled; saveSettings(); }
                        else { selectedIndex = layerIdx; }
                    }
                    return false;
                }
            }

            return false;
        }

        ModalSystem.RunOverlayModal(
            OverlayRowCount,
            DrawSettingsContent,
            HandleSettingsKey,
            consoleLock,
            onClose: () => analysisEngine.SetOverlayActive(false),
            onEnter: () => analysisEngine.SetOverlayActive(true, OverlayRowCount),
            onScrollTick: DrawHintLineOnly);
    }
}
