using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;
using AudioAnalyzer.Visualizers;

namespace AudioAnalyzer.Console;

/// <summary>Handles key input for the settings overlay: layer list, settings list, renaming, setting edit, preset create.</summary>
internal sealed class SettingsModalKeyHandler : ISettingsModalKeyHandler
{
    private const int NavKeyRepeatMs = 120;
    private readonly IPaletteRepository _paletteRepo;

    public SettingsModalKeyHandler(IPaletteRepository paletteRepo)
    {
        _paletteRepo = paletteRepo ?? throw new ArgumentNullException(nameof(paletteRepo));
    }

    /// <inheritdoc />
    public bool Handle(ConsoleKeyInfo key, SettingsModalKeyContext context)
    {
        var state = context.State;
        var sortedLayers = context.SortedLayers;
        var vs = context.VisualizerSettings;
        var textLayers = context.TextLayers;

        static bool IsNavKey(ConsoleKey k) =>
            k is ConsoleKey.UpArrow or ConsoleKey.DownArrow or ConsoleKey.LeftArrow or ConsoleKey.RightArrow;
        if (IsNavKey(key.Key))
        {
            var now = Environment.TickCount64;
            if (state.LastNavKey == key.Key && (now - state.LastNavTime) < NavKeyRepeatMs)
            {
                return false;
            }
            state.LastNavKey = key.Key;
            state.LastNavTime = now;
        }
        else
        {
            state.LastNavKey = null;
        }

        var selectedLayer = sortedLayers.Count > 0 && state.SelectedLayerIndex < sortedLayers.Count ? sortedLayers[state.SelectedLayerIndex] : null;
        var settingsRows = selectedLayer != null ? GetSettingsRows(selectedLayer) : [];

        if (state.Focus == SettingsModalFocus.Renaming)
        {
            if (key.Key == ConsoleKey.Escape) { state.Renaming = false; state.Focus = SettingsModalFocus.LayerList; return false; }
            if (key.Key == ConsoleKey.Enter)
            {
                var activeId = vs.ActivePresetId;
                if (!string.IsNullOrWhiteSpace(activeId) && !string.IsNullOrWhiteSpace(state.RenameBuffer))
                {
                    var preset = context.PresetRepository.GetById(activeId);
                    if (preset != null)
                    {
                        preset.Name = state.RenameBuffer.Trim();
                        preset.Config = (vs.TextLayers ?? new TextLayersVisualizerSettings()).DeepCopy();
                        context.PresetRepository.Save(activeId, preset);
                        var p = vs.Presets?.FirstOrDefault(x => string.Equals(x.Id, activeId, StringComparison.OrdinalIgnoreCase));
                        if (p != null) { p.Name = state.RenameBuffer.Trim(); }
                    }
                    context.SaveSettings();
                }
                state.Renaming = false;
                state.Focus = SettingsModalFocus.LayerList;
                return false;
            }
            if (key.Key == ConsoleKey.Backspace && state.RenameBuffer.Length > 0) { state.RenameBuffer = state.RenameBuffer[..^1]; return false; }
            if (key.KeyChar is >= ' ' and <= '~' && key.KeyChar >= ' ') { state.RenameBuffer += key.KeyChar; return false; }
        }

        if (state.Focus == SettingsModalFocus.EditingSetting && selectedLayer != null && state.SelectedSettingIndex < settingsRows.Count)
        {
            var row = settingsRows[state.SelectedSettingIndex];
            if (key.Key == ConsoleKey.Escape) { state.Focus = SettingsModalFocus.SettingsList; return false; }
            if (row.EditMode == SettingEditMode.TextEdit &&
                (key.Key == ConsoleKey.Enter || key.Key == ConsoleKey.UpArrow || key.Key == ConsoleKey.DownArrow))
            {
                ApplySettingEdit(selectedLayer, row.Id, state.EditingBuffer);
                context.SaveSettings();
                state.Focus = SettingsModalFocus.SettingsList;
                return false;
            }
            if (row.EditMode == SettingEditMode.TextEdit && key.Key == ConsoleKey.Backspace) { if (state.EditingBuffer.Length > 0) { state.EditingBuffer = state.EditingBuffer[..^1]; } return false; }
            if (row.EditMode == SettingEditMode.TextEdit && key.KeyChar is >= ' ' and <= '~') { state.EditingBuffer += key.KeyChar; return false; }

            if (row.EditMode == SettingEditMode.Cycle && (key.Key == ConsoleKey.LeftArrow || key.Key == ConsoleKey.RightArrow || key.Key == ConsoleKey.Enter))
            {
                CycleSetting(selectedLayer, row.Id, key.Key == ConsoleKey.RightArrow || key.Key == ConsoleKey.Enter);
                context.SaveSettings();
                state.Focus = SettingsModalFocus.SettingsList;
                return false;
            }
            return false;
        }

        if (state.Focus == SettingsModalFocus.SettingsList)
        {
            if (key.Key == ConsoleKey.LeftArrow || key.Key == ConsoleKey.Escape) { state.Focus = SettingsModalFocus.LayerList; return false; }
            if (key.Key == ConsoleKey.UpArrow) { state.SelectedSettingIndex = Math.Max(0, state.SelectedSettingIndex - 1); return false; }
            if (key.Key == ConsoleKey.DownArrow) { state.SelectedSettingIndex = Math.Min(settingsRows.Count - 1, state.SelectedSettingIndex + 1); return false; }
            if (selectedLayer != null && state.SelectedSettingIndex < settingsRows.Count)
            {
                var row = settingsRows[state.SelectedSettingIndex];
                bool cycleForward = key.Key == ConsoleKey.Enter || key.Key == ConsoleKey.Add || key.Key == ConsoleKey.OemPlus;
                bool cycleBackward = key.Key == ConsoleKey.Subtract || key.Key == ConsoleKey.OemMinus;
                if (row.EditMode == SettingEditMode.Cycle && (cycleForward || cycleBackward))
                {
                    CycleSetting(selectedLayer, row.Id, cycleForward);
                    context.SaveSettings();
                    return false;
                }
                if (key.Key == ConsoleKey.Enter && row.EditMode == SettingEditMode.TextEdit)
                {
                    state.EditingBuffer = row.Id == "Snippets"
                        ? (selectedLayer.TextSnippets is { Count: > 0 } ? string.Join(", ", selectedLayer.TextSnippets) : "")
                        : row.Id == "ImagePath"
                            ? ((selectedLayer.GetCustom<AsciiImageSettings>() ?? new AsciiImageSettings()).ImageFolderPath ?? "")
                            : row.DisplayValue;
                    state.Focus = SettingsModalFocus.EditingSetting;
                    return false;
                }
            }
        }

        if (state.Focus == SettingsModalFocus.LayerList)
        {
            if (key.Key == ConsoleKey.Escape) { return true; }
            if (key.Key == ConsoleKey.Enter && selectedLayer != null)
            {
                state.Focus = SettingsModalFocus.SettingsList;
                state.SelectedSettingIndex = 0;
                return false;
            }
            if (key.Key == ConsoleKey.UpArrow) { state.SelectedLayerIndex = sortedLayers.Count > 0 ? (state.SelectedLayerIndex - 1 + sortedLayers.Count) % sortedLayers.Count : 0; return false; }
            if (key.Key == ConsoleKey.DownArrow) { state.SelectedLayerIndex = sortedLayers.Count > 0 ? (state.SelectedLayerIndex + 1) % sortedLayers.Count : 0; return false; }
            if (key.Key == ConsoleKey.Spacebar && selectedLayer != null) { selectedLayer.Enabled = !selectedLayer.Enabled; context.SaveSettings(); return false; }
            if (key.Key == ConsoleKey.LeftArrow)
            {
                if (selectedLayer != null) { selectedLayer.LayerType = TextLayerSettings.CycleTypeBackward(selectedLayer); context.SaveSettings(); }
                return false;
            }
            if (key.Key == ConsoleKey.RightArrow)
            {
                if (selectedLayer != null) { selectedLayer.LayerType = TextLayerSettings.CycleTypeForward(selectedLayer); context.SaveSettings(); }
                return false;
            }
            if (key.Key == ConsoleKey.R)
            {
                if (vs.Presets is { Count: > 0 })
                {
                    var p = vs.Presets.FirstOrDefault(x => string.Equals(x.Id, vs.ActivePresetId, StringComparison.OrdinalIgnoreCase)) ?? vs.Presets[0];
                    state.RenameBuffer = p.Name?.Trim() ?? "";
                    state.Renaming = true;
                    state.Focus = SettingsModalFocus.Renaming;
                }
                return false;
            }
            if (key.Key == ConsoleKey.Backspace && state.Renaming) { if (state.RenameBuffer.Length > 0) { state.RenameBuffer = state.RenameBuffer[..^1]; } return false; }
            if (key.Key == ConsoleKey.N)
            {
                var newPreset = new Preset { Name = $"Preset {vs.Presets?.Count + 1 ?? 1}", Config = textLayers.DeepCopy() };
                var createdId = context.PresetRepository.Create(newPreset);
                vs.ActivePresetId = createdId;
                vs.Presets = context.PresetRepository.GetAll().Select(p => new Preset { Id = p.Id, Name = p.Name, Config = new TextLayersVisualizerSettings() }).ToList();
                context.TextLayers = vs.TextLayers ?? new TextLayersVisualizerSettings();
                context.SortedLayers = context.TextLayers.Layers?.OrderBy(l => l.ZOrder).ToList() ?? [];
                context.SaveSettings();
                return false;
            }
            int digit = DigitFromKey(key.Key);
            if (digit != 0)
            {
                int layerIdx = digit - 1;
                if (layerIdx < sortedLayers.Count)
                {
                    if (key.Modifiers.HasFlag(ConsoleModifiers.Shift)) { sortedLayers[layerIdx].Enabled = !sortedLayers[layerIdx].Enabled; context.SaveSettings(); }
                    else { state.SelectedLayerIndex = layerIdx; }
                }
                return false;
            }
        }

        return false;
    }

    private IReadOnlyList<(string Id, string Label, string DisplayValue, SettingEditMode EditMode)> GetSettingsRows(TextLayerSettings layer)
    {
        var descriptors = SettingDescriptor.BuildAll(layer, _paletteRepo);
        return descriptors.Select(d => (d.Id, d.Label, d.GetDisplayValue(layer), d.EditMode)).ToList();
    }

    private void ApplySettingEdit(TextLayerSettings layer, string settingId, string value)
    {
        var descriptors = SettingDescriptor.BuildAll(layer, _paletteRepo);
        var d = descriptors.FirstOrDefault(x => x.Id == settingId);
        d?.ApplyEdit(layer, value);
    }

    private void CycleSetting(TextLayerSettings layer, string id, bool forward)
    {
        var descriptors = SettingDescriptor.BuildAll(layer, _paletteRepo);
        var d = descriptors.FirstOrDefault(x => x.Id == id);
        d?.Cycle(layer, forward);
    }

    private static int DigitFromKey(ConsoleKey key)
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
}
