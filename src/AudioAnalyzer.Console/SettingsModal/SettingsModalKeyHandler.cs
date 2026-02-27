using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;
using AudioAnalyzer.Visualizers;
using KeyHandling = AudioAnalyzer.Console.KeyHandling;

namespace AudioAnalyzer.Console;

/// <summary>Handles key input for the settings overlay: layer list, settings list, renaming, setting edit, preset create.</summary>
internal sealed class SettingsModalKeyHandler : IKeyHandler<SettingsModalKeyContext>
{
    private const int NavKeyRepeatMs = 120;
    private const string Section = "Preset settings modal (S)";
    private readonly IPaletteRepository _paletteRepo;

    public SettingsModalKeyHandler(IPaletteRepository paletteRepo)
    {
        _paletteRepo = paletteRepo ?? throw new ArgumentNullException(nameof(paletteRepo));
    }

    private IReadOnlyList<KeyHandling.KeyBindingEntry<SettingsModalKeyContext>> GetLayerListEntries()
    {
        return
        [
            new KeyHandling.KeyBindingEntry<SettingsModalKeyContext>(
                Matches: k => k.Key == ConsoleKey.Escape,
                Action: (_, context) => true,
                Key: "Escape",
                Description: "Close modal",
                Section),
            new KeyHandling.KeyBindingEntry<SettingsModalKeyContext>(
                Matches: k => k.Key == ConsoleKey.Enter,
                Action: (_, context) =>
                {
                    var state = context.State;
                    var sortedLayers = context.SortedLayers;
                    var selectedLayer = sortedLayers.Count > 0 && state.SelectedLayerIndex < sortedLayers.Count ? sortedLayers[state.SelectedLayerIndex] : null;
                    if (selectedLayer != null)
                    {
                        state.Focus = SettingsModalFocus.SettingsList;
                        state.SelectedSettingIndex = 0;
                    }
                    return false;
                },
                Key: "Enter",
                Description: "Move to settings panel or cycle selected setting",
                Section),
            new KeyHandling.KeyBindingEntry<SettingsModalKeyContext>(
                Matches: k => k.Modifiers.HasFlag(ConsoleModifiers.Control) && (k.Key == ConsoleKey.UpArrow || k.Key == ConsoleKey.DownArrow),
                Action: (key, context) =>
                {
                    var state = context.State;
                    var sortedLayers = context.SortedLayers;
                    if (sortedLayers.Count == 0) return false;
                    if (key.Key == ConsoleKey.UpArrow && state.SelectedLayerIndex > 0)
                    {
                        var a = sortedLayers[state.SelectedLayerIndex];
                        var b = sortedLayers[state.SelectedLayerIndex - 1];
                        (a.ZOrder, b.ZOrder) = (b.ZOrder, a.ZOrder);
                        context.SortedLayers = context.TextLayers.Layers?.OrderBy(l => l.ZOrder).ToList() ?? [];
                        state.SelectedLayerIndex--;
                        context.SaveSettings();
                        return false;
                    }
                    if (key.Key == ConsoleKey.DownArrow && state.SelectedLayerIndex < sortedLayers.Count - 1)
                    {
                        var a = sortedLayers[state.SelectedLayerIndex];
                        var b = sortedLayers[state.SelectedLayerIndex + 1];
                        (a.ZOrder, b.ZOrder) = (b.ZOrder, a.ZOrder);
                        context.SortedLayers = context.TextLayers.Layers?.OrderBy(l => l.ZOrder).ToList() ?? [];
                        state.SelectedLayerIndex++;
                        context.SaveSettings();
                        return false;
                    }
                    return false;
                },
                Key: "Ctrl+↑/↓",
                Description: "Reorder layer",
                Section),
            new KeyHandling.KeyBindingEntry<SettingsModalKeyContext>(
                Matches: k => k.Key == ConsoleKey.UpArrow || k.Key == ConsoleKey.DownArrow,
                Action: (key, context) =>
                {
                    var state = context.State;
                    var sortedLayers = context.SortedLayers;
                    if (key.Key == ConsoleKey.UpArrow)
                        state.SelectedLayerIndex = sortedLayers.Count > 0 ? (state.SelectedLayerIndex - 1 + sortedLayers.Count) % sortedLayers.Count : 0;
                    else
                        state.SelectedLayerIndex = sortedLayers.Count > 0 ? (state.SelectedLayerIndex + 1) % sortedLayers.Count : 0;
                    return false;
                },
                Key: "↑/↓",
                Description: "Select layer or setting",
                Section),
            new KeyHandling.KeyBindingEntry<SettingsModalKeyContext>(
                Matches: k => k.Key == ConsoleKey.Spacebar,
                Action: (_, context) =>
                {
                    var sortedLayers = context.SortedLayers;
                    var state = context.State;
                    var selectedLayer = sortedLayers.Count > 0 && state.SelectedLayerIndex < sortedLayers.Count ? sortedLayers[state.SelectedLayerIndex] : null;
                    if (selectedLayer != null) { selectedLayer.Enabled = !selectedLayer.Enabled; context.SaveSettings(); }
                    return false;
                },
                Key: "Space",
                Description: "Toggle layer enabled/disabled",
                Section),
            new KeyHandling.KeyBindingEntry<SettingsModalKeyContext>(
                Matches: k => k.Key == ConsoleKey.LeftArrow || k.Key == ConsoleKey.RightArrow,
                Action: (key, context) =>
                {
                    var sortedLayers = context.SortedLayers;
                    var state = context.State;
                    var selectedLayer = sortedLayers.Count > 0 && state.SelectedLayerIndex < sortedLayers.Count ? sortedLayers[state.SelectedLayerIndex] : null;
                    if (selectedLayer != null)
                    {
                        selectedLayer.LayerType = key.Key == ConsoleKey.LeftArrow
                            ? TextLayerSettings.CycleTypeBackward(selectedLayer)
                            : TextLayerSettings.CycleTypeForward(selectedLayer);
                        context.SaveSettings();
                    }
                    return false;
                },
                Key: "←/→",
                Description: "Change layer type (left panel)",
                Section),
            new KeyHandling.KeyBindingEntry<SettingsModalKeyContext>(
                Matches: k => k.Key == ConsoleKey.R,
                Action: (_, context) =>
                {
                    var vs = context.VisualizerSettings;
                    var state = context.State;
                    if (vs.Presets is { Count: > 0 })
                    {
                        var p = vs.Presets.FirstOrDefault(x => string.Equals(x.Id, vs.ActivePresetId, StringComparison.OrdinalIgnoreCase)) ?? vs.Presets[0];
                        state.RenameBuffer = p.Name?.Trim() ?? "";
                        state.Renaming = true;
                        state.Focus = SettingsModalFocus.Renaming;
                    }
                    return false;
                },
                Key: "R",
                Description: "Rename preset",
                Section),
            new KeyHandling.KeyBindingEntry<SettingsModalKeyContext>(
                Matches: k => k.Key == ConsoleKey.N,
                Action: (_, context) =>
                {
                    var vs = context.VisualizerSettings;
                    var textLayers = context.TextLayers;
                    var newPreset = new Preset { Name = $"Preset {vs.Presets?.Count + 1 ?? 1}", Config = textLayers.DeepCopy() };
                    var createdId = context.PresetRepository.Create(newPreset);
                    vs.ActivePresetId = createdId;
                    vs.Presets = context.PresetRepository.GetAll().Select(p => new Preset { Id = p.Id, Name = p.Name, Config = new TextLayersVisualizerSettings() }).ToList();
                    context.TextLayers = vs.TextLayers ?? new TextLayersVisualizerSettings();
                    context.SortedLayers = context.TextLayers.Layers?.OrderBy(l => l.ZOrder).ToList() ?? [];
                    context.SaveSettings();
                    return false;
                },
                Key: "N",
                Description: "New preset (duplicate of current)",
                Section),
            new KeyHandling.KeyBindingEntry<SettingsModalKeyContext>(
                Matches: k => DigitFromKey(k.Key) != 0 && !k.Modifiers.HasFlag(ConsoleModifiers.Shift),
                Action: (key, context) =>
                {
                    var state = context.State;
                    var sortedLayers = context.SortedLayers;
                    int layerIdx = DigitFromKey(key.Key) - 1;
                    if (layerIdx < sortedLayers.Count)
                        state.SelectedLayerIndex = layerIdx;
                    return false;
                },
                Key: "1-9",
                Description: "Select layer",
                Section),
            new KeyHandling.KeyBindingEntry<SettingsModalKeyContext>(
                Matches: k => DigitFromKey(k.Key) != 0 && k.Modifiers.HasFlag(ConsoleModifiers.Shift),
                Action: (key, context) =>
                {
                    var sortedLayers = context.SortedLayers;
                    int layerIdx = DigitFromKey(key.Key) - 1;
                    if (layerIdx < sortedLayers.Count)
                    {
                        sortedLayers[layerIdx].Enabled = !sortedLayers[layerIdx].Enabled;
                        context.SaveSettings();
                    }
                    return false;
                },
                Key: "Shift+1-9",
                Description: "Toggle layer enabled/disabled by slot",
                Section),
        ];
    }

    /// <inheritdoc />
    public IReadOnlyList<KeyBinding> GetBindings()
    {
        var layerList = GetLayerListEntries().Select(e => e.ToKeyBinding()).ToList();
        return layerList
            .Concat([new KeyBinding("+/-", "Cycle selected setting (when cycleable)", Section)])
            .Concat([new KeyBinding("←/Escape", "Back to layer list from settings panel", Section)])
            .ToList();
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
            foreach (var entry in GetLayerListEntries())
            {
                if (entry.Matches(key))
                {
                    return entry.Action(key, context);
                }
            }
        }

        return false;
    }

    private List<(string Id, string Label, string DisplayValue, SettingEditMode EditMode)> GetSettingsRows(TextLayerSettings layer)
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
