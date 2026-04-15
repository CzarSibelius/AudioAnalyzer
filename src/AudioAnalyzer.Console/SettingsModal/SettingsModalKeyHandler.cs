using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;
using AudioAnalyzer.Visualizers;
using KeyHandling = AudioAnalyzer.Console.KeyHandling;

namespace AudioAnalyzer.Console;

/// <summary>Config for settings overlay keys: layer list, settings list, renaming, setting edit, preset create.</summary>
internal sealed class SettingsModalKeyHandlerConfig : IKeyHandlerConfig<SettingsModalKeyContext>
{
    private const int NavKeyRepeatMs = 120;
    private const string Section = "Preset settings modal (S)";
    private readonly IPaletteRepository _paletteRepo;
    private readonly IAsciiVideoDeviceCatalog _asciiVideoDeviceCatalog;

    public SettingsModalKeyHandlerConfig(IPaletteRepository paletteRepo, IAsciiVideoDeviceCatalog asciiVideoDeviceCatalog)
    {
        _paletteRepo = paletteRepo ?? throw new ArgumentNullException(nameof(paletteRepo));
        _asciiVideoDeviceCatalog = asciiVideoDeviceCatalog ?? throw new ArgumentNullException(nameof(asciiVideoDeviceCatalog));
    }

    private static IReadOnlyList<KeyHandling.KeyBindingEntry<SettingsModalKeyContext>> GetLayerListEntries()
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
                    if (state.LeftPanelPresetSelected)
                    {
                        state.Focus = SettingsModalFocus.SettingsList;
                        state.SelectedSettingIndex = 0;
                        return false;
                    }

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
                    if (state.LeftPanelPresetSelected)
                    {
                        return false;
                    }

                    var sortedLayers = context.SortedLayers;
                    if (sortedLayers.Count == 0)
                    {
                        return false;
                    }

                    if (key.Key == ConsoleKey.UpArrow && state.SelectedLayerIndex > 0)
                    {
                        var a = sortedLayers[state.SelectedLayerIndex];
                        var b = sortedLayers[state.SelectedLayerIndex - 1];
                        (a.ZOrder, b.ZOrder) = (b.ZOrder, a.ZOrder);
                        context.SortedLayers = context.TextLayers.Layers?.OrderBy(l => l.ZOrder).ToList() ?? [];
                        state.SelectedLayerIndex--;
                        context.NotifyLayersStructureChanged();
                        return false;
                    }

                    if (key.Key == ConsoleKey.DownArrow && state.SelectedLayerIndex < sortedLayers.Count - 1)
                    {
                        var a = sortedLayers[state.SelectedLayerIndex];
                        var b = sortedLayers[state.SelectedLayerIndex + 1];
                        (a.ZOrder, b.ZOrder) = (b.ZOrder, a.ZOrder);
                        context.SortedLayers = context.TextLayers.Layers?.OrderBy(l => l.ZOrder).ToList() ?? [];
                        state.SelectedLayerIndex++;
                        context.NotifyLayersStructureChanged();
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
                    int n = sortedLayers.Count;
                    if (key.Key == ConsoleKey.UpArrow)
                    {
                        if (n == 0)
                        {
                            state.LeftPanelPresetSelected = true;
                            return false;
                        }

                        if (state.LeftPanelPresetSelected)
                        {
                            state.LeftPanelPresetSelected = false;
                            state.SelectedLayerIndex = n - 1;
                        }
                        else if (state.SelectedLayerIndex <= 0)
                        {
                            state.LeftPanelPresetSelected = true;
                        }
                        else
                        {
                            state.SelectedLayerIndex--;
                        }
                    }
                    else
                    {
                        if (n == 0)
                        {
                            state.LeftPanelPresetSelected = true;
                            return false;
                        }

                        if (state.LeftPanelPresetSelected)
                        {
                            state.LeftPanelPresetSelected = false;
                            state.SelectedLayerIndex = 0;
                        }
                        else if (state.SelectedLayerIndex >= n - 1)
                        {
                            state.LeftPanelPresetSelected = true;
                        }
                        else
                        {
                            state.SelectedLayerIndex++;
                        }
                    }

                    return false;
                },
                Key: "↑/↓",
                Description: "Select Preset row or layer",
                Section),
            new KeyHandling.KeyBindingEntry<SettingsModalKeyContext>(
                Matches: k => k.Key == ConsoleKey.Spacebar,
                Action: (_, context) =>
                {
                    var sortedLayers = context.SortedLayers;
                    var state = context.State;
                    if (state.LeftPanelPresetSelected)
                    {
                        return false;
                    }

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
                    if (state.LeftPanelPresetSelected)
                    {
                        return false;
                    }

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
                    {
                        state.LeftPanelPresetSelected = false;
                        state.SelectedLayerIndex = layerIdx;
                    }
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
            new KeyHandling.KeyBindingEntry<SettingsModalKeyContext>(
                Matches: k => k.Key == ConsoleKey.Insert,
                Action: (_, context) =>
                {
                    var textLayers = context.TextLayers;
                    var layers = textLayers.Layers ??= new List<TextLayerSettings>();
                    if (layers.Count >= TextLayersLimits.MaxLayerCount)
                    {
                        return false;
                    }

                    int maxZ = layers.Count > 0 ? layers.Max(l => l.ZOrder) : -1;
                    int displayNum = layers.Count + 1;
                    var newLayer = context.DefaultTextLayersFactory.CreatePaddingMarqueeLayer(maxZ + 1, displayNum);
                    layers.Add(newLayer);
                    context.SortedLayers = layers.OrderBy(l => l.ZOrder).ToList();
                    var state = context.State;
                    state.LeftPanelPresetSelected = false;
                    state.SelectedLayerIndex = context.SortedLayers.Count - 1;
                    context.NotifyLayersStructureChanged();
                    return false;
                },
                Key: "Insert",
                Description: "Add text layer (up to max)",
                Section),
            new KeyHandling.KeyBindingEntry<SettingsModalKeyContext>(
                Matches: k => k.Key == ConsoleKey.Delete,
                Action: (_, context) =>
                {
                    var state = context.State;
                    if (state.LeftPanelPresetSelected)
                    {
                        return false;
                    }

                    var sorted = context.SortedLayers;
                    if (sorted.Count == 0)
                    {
                        return false;
                    }

                    int idx = state.SelectedLayerIndex;
                    if (idx < 0 || idx >= sorted.Count)
                    {
                        return false;
                    }

                    var toRemove = sorted[idx];
                    context.TextLayers.Layers?.Remove(toRemove);
                    context.LayerStateStore.RemoveSlotAt(idx);
                    context.SortedLayers = (context.TextLayers.Layers ?? []).OrderBy(l => l.ZOrder).ToList();
                    if (context.SortedLayers.Count == 0)
                    {
                        state.LeftPanelPresetSelected = true;
                        state.SelectedLayerIndex = 0;
                    }
                    else
                    {
                        state.SelectedLayerIndex = Math.Min(idx, context.SortedLayers.Count - 1);
                    }

                    context.NotifyLayersStructureChanged();
                    return false;
                },
                Key: "Delete",
                Description: "Remove selected text layer",
                Section),
        ];
    }

    /// <inheritdoc />
    public IReadOnlyList<KeyBinding> GetBindings()
    {
        var layerList = GetLayerListEntries().Select(e => e.ToKeyBinding()).ToList();
        return layerList
            .Concat([new KeyBinding("+/-", "Cycle selected setting (when cycleable); on Palette row, Enter opens list", Section)])
            .Concat([new KeyBinding("Enter", "Open palette list (Palette row); in list: preview with arrows/+/−, Enter save, Esc discard", Section)])
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

        var selectedLayer = !state.LeftPanelPresetSelected && sortedLayers.Count > 0 && state.SelectedLayerIndex < sortedLayers.Count
            ? sortedLayers[state.SelectedLayerIndex]
            : null;
        var presetRows = PresetSettingsModalRows.Build(vs, textLayers, _paletteRepo);
        var layerSettingsRows = selectedLayer != null ? GetSettingsRows(selectedLayer) : [];
        var settingsRows = state.LeftPanelPresetSelected ? presetRows : layerSettingsRows;

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

        if (state.Focus == SettingsModalFocus.PickingPalette && state.PalettePickerForPresetDefault)
        {
            int count = GetPalettePickerEntryCount(includeInheritFirst: false);
            if (count <= 0)
            {
                state.PalettePickerForPresetDefault = false;
                state.Focus = SettingsModalFocus.SettingsList;
                return false;
            }

            if (key.Key == ConsoleKey.Escape)
            {
                textLayers.PaletteId = state.PalettePickerOriginalPaletteId;
                state.PalettePickerOriginalPaletteId = null;
                state.PalettePickerForPresetDefault = false;
                state.Focus = SettingsModalFocus.SettingsList;
                return false;
            }

            if (key.Key == ConsoleKey.Enter)
            {
                context.SaveSettings();
                state.PalettePickerOriginalPaletteId = null;
                state.PalettePickerForPresetDefault = false;
                state.Focus = SettingsModalFocus.SettingsList;
                return false;
            }

            bool moveUp = key.Key == ConsoleKey.UpArrow || key.Key == ConsoleKey.Subtract || key.Key == ConsoleKey.OemMinus;
            bool moveDown = key.Key == ConsoleKey.DownArrow || key.Key == ConsoleKey.Add || key.Key == ConsoleKey.OemPlus;
            if (moveUp || moveDown)
            {
                int idx = state.PalettePickerSelectedIndex;
                if (moveUp)
                {
                    idx = Math.Max(0, idx - 1);
                }
                else
                {
                    idx = Math.Min(count - 1, idx + 1);
                }

                state.PalettePickerSelectedIndex = idx;
                ApplyPalettePickerSelectionPreset(textLayers, idx);
                return false;
            }

            return false;
        }

        if (state.Focus == SettingsModalFocus.PickingPalette && selectedLayer != null)
        {
            int count = GetPalettePickerEntryCount(includeInheritFirst: true);
            if (key.Key == ConsoleKey.Escape)
            {
                selectedLayer.PaletteId = state.PalettePickerOriginalPaletteId;
                state.PalettePickerOriginalPaletteId = null;
                state.Focus = SettingsModalFocus.SettingsList;
                return false;
            }

            if (key.Key == ConsoleKey.Enter)
            {
                context.SaveSettings();
                state.PalettePickerOriginalPaletteId = null;
                state.Focus = SettingsModalFocus.SettingsList;
                return false;
            }

            bool moveUp = key.Key == ConsoleKey.UpArrow || key.Key == ConsoleKey.Subtract || key.Key == ConsoleKey.OemMinus;
            bool moveDown = key.Key == ConsoleKey.DownArrow || key.Key == ConsoleKey.Add || key.Key == ConsoleKey.OemPlus;
            if (moveUp || moveDown)
            {
                int idx = state.PalettePickerSelectedIndex;
                if (moveUp)
                {
                    idx = Math.Max(0, idx - 1);
                }
                else
                {
                    idx = Math.Min(count - 1, idx + 1);
                }

                state.PalettePickerSelectedIndex = idx;
                ApplyPalettePickerSelection(selectedLayer, idx);
                return false;
            }

            return false;
        }

        if (state.Focus == SettingsModalFocus.EditingSetting && state.LeftPanelPresetSelected && state.SelectedSettingIndex < settingsRows.Count)
        {
            var row = settingsRows[state.SelectedSettingIndex];
            if (key.Key == ConsoleKey.Escape) { state.Focus = SettingsModalFocus.SettingsList; return false; }
            if (row.Id == PresetSettingsModalRows.PresetNameId && row.EditMode == SettingEditMode.TextEdit &&
                (key.Key == ConsoleKey.Enter || key.Key == ConsoleKey.UpArrow || key.Key == ConsoleKey.DownArrow))
            {
                ApplyPresetNameFromBuffer(vs, context.PresetRepository, state.EditingBuffer, context.SaveSettings);
                state.Focus = SettingsModalFocus.SettingsList;
                return false;
            }

            if (row.EditMode == SettingEditMode.TextEdit && key.Key == ConsoleKey.Backspace) { if (state.EditingBuffer.Length > 0) { state.EditingBuffer = state.EditingBuffer[..^1]; } return false; }
            if (row.EditMode == SettingEditMode.TextEdit && key.KeyChar is >= ' ' and <= '~') { state.EditingBuffer += key.KeyChar; return false; }
            return false;
        }

        if (state.Focus == SettingsModalFocus.EditingSetting && selectedLayer != null && state.SelectedSettingIndex < settingsRows.Count)
        {
            var row = settingsRows[state.SelectedSettingIndex];
            if (key.Key == ConsoleKey.Escape) { state.Focus = SettingsModalFocus.SettingsList; return false; }
            if (row.EditMode == SettingEditMode.TextEdit &&
                (key.Key == ConsoleKey.Enter || key.Key == ConsoleKey.UpArrow || key.Key == ConsoleKey.DownArrow))
            {
                ApplySettingEdit(selectedLayer, row.Id, state.EditingBuffer);
                NotifyDrawOrderChanged(context, state, selectedLayer);
                state.Focus = SettingsModalFocus.SettingsList;
                return false;
            }
            if (row.EditMode == SettingEditMode.TextEdit && key.Key == ConsoleKey.Backspace) { if (state.EditingBuffer.Length > 0) { state.EditingBuffer = state.EditingBuffer[..^1]; } return false; }
            if (row.EditMode == SettingEditMode.TextEdit && key.KeyChar is >= ' ' and <= '~') { state.EditingBuffer += key.KeyChar; return false; }

            if (row.EditMode == SettingEditMode.Cycle && (key.Key == ConsoleKey.LeftArrow || key.Key == ConsoleKey.RightArrow || key.Key == ConsoleKey.Enter))
            {
                CycleSetting(selectedLayer, row.Id, key.Key == ConsoleKey.RightArrow || key.Key == ConsoleKey.Enter);
                NotifyDrawOrderChanged(context, state, selectedLayer);
                state.Focus = SettingsModalFocus.SettingsList;
                return false;
            }
            return false;
        }

        if (state.Focus == SettingsModalFocus.SettingsList)
        {
            if (key.Key == ConsoleKey.LeftArrow || key.Key == ConsoleKey.Escape) { state.Focus = SettingsModalFocus.LayerList; return false; }
            if (settingsRows.Count == 0)
            {
                return false;
            }

            if (key.Key == ConsoleKey.UpArrow) { state.SelectedSettingIndex = Math.Max(0, state.SelectedSettingIndex - 1); return false; }
            if (key.Key == ConsoleKey.DownArrow) { state.SelectedSettingIndex = Math.Min(settingsRows.Count - 1, state.SelectedSettingIndex + 1); return false; }
            if (state.SelectedSettingIndex < settingsRows.Count)
            {
                var row = settingsRows[state.SelectedSettingIndex];
                if (!state.LeftPanelPresetSelected && selectedLayer != null && key.Key == ConsoleKey.Enter && row.EditMode == SettingEditMode.BoundVisualEdit)
                {
                    context.RequestVisualBoundsEdit?.Invoke(state.SelectedLayerIndex);
                    return true;
                }

                bool cycleForward = key.Key == ConsoleKey.Enter || key.Key == ConsoleKey.Add || key.Key == ConsoleKey.OemPlus;
                bool cycleBackward = key.Key == ConsoleKey.Subtract || key.Key == ConsoleKey.OemMinus;

                if (state.LeftPanelPresetSelected && row.Id == PresetSettingsModalRows.DefaultPaletteId && row.EditMode == SettingEditMode.PalettePicker)
                {
                    if (key.Key == ConsoleKey.Enter)
                    {
                        state.PalettePickerOriginalPaletteId = textLayers.PaletteId;
                        state.PalettePickerSelectedIndex = ComputeInitialPalettePickerIndexPreset(textLayers);
                        state.PalettePickerForPresetDefault = true;
                        ApplyPalettePickerSelectionPreset(textLayers, state.PalettePickerSelectedIndex);
                        state.Focus = SettingsModalFocus.PickingPalette;
                        return false;
                    }

                    if (cycleForward || cycleBackward)
                    {
                        CyclePresetPalette(textLayers, cycleForward);
                        context.SaveSettings();
                        return false;
                    }
                }
                else if (!state.LeftPanelPresetSelected && selectedLayer != null && row.EditMode == SettingEditMode.PalettePicker)
                {
                    if (key.Key == ConsoleKey.Enter)
                    {
                        state.PalettePickerOriginalPaletteId = selectedLayer.PaletteId;
                        state.PalettePickerSelectedIndex = ComputeInitialPalettePickerIndex(selectedLayer);
                        ApplyPalettePickerSelection(selectedLayer, state.PalettePickerSelectedIndex);
                        state.Focus = SettingsModalFocus.PickingPalette;
                        return false;
                    }

                    if (cycleForward || cycleBackward)
                    {
                        CycleSetting(selectedLayer, row.Id, cycleForward);
                        NotifyDrawOrderChanged(context, state, selectedLayer);
                        return false;
                    }
                }
                else if (!state.LeftPanelPresetSelected && selectedLayer != null && row.EditMode == SettingEditMode.Cycle && (cycleForward || cycleBackward))
                {
                    CycleSetting(selectedLayer, row.Id, cycleForward);
                    NotifyDrawOrderChanged(context, state, selectedLayer);
                    return false;
                }

                if (state.LeftPanelPresetSelected && row.Id == PresetSettingsModalRows.PresetNameId && key.Key == ConsoleKey.Enter && row.EditMode == SettingEditMode.TextEdit)
                {
                    state.EditingBuffer = row.DisplayValue;
                    state.Focus = SettingsModalFocus.EditingSetting;
                    return false;
                }

                if (!state.LeftPanelPresetSelected && selectedLayer != null && key.Key == ConsoleKey.Enter && row.EditMode == SettingEditMode.TextEdit)
                {
                    state.EditingBuffer = row.Id == "Snippets"
                        ? (selectedLayer.TextSnippets is { Count: > 0 } ? string.Join(", ", selectedLayer.TextSnippets) : "")
                        : row.Id == "ImagePath"
                            ? ((selectedLayer.GetCustom<AsciiImageSettings>() ?? new AsciiImageSettings()).ImageFolderPath ?? "")
                        : row.Id == "ModelPath"
                            ? ((selectedLayer.GetCustom<AsciiModelSettings>() ?? new AsciiModelSettings()).ModelFolderPath ?? "")
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

    private static void ApplyPresetNameFromBuffer(VisualizerSettings vs, IPresetRepository presetRepository, string buffer, Action saveSettings)
    {
        var activeId = vs.ActivePresetId;
        if (string.IsNullOrWhiteSpace(activeId) || string.IsNullOrWhiteSpace(buffer))
        {
            return;
        }

        var preset = presetRepository.GetById(activeId);
        if (preset == null)
        {
            return;
        }

        preset.Name = buffer.Trim();
        preset.Config = (vs.TextLayers ?? new TextLayersVisualizerSettings()).DeepCopy();
        presetRepository.Save(activeId, preset);
        var p = vs.Presets?.FirstOrDefault(x => string.Equals(x.Id, activeId, StringComparison.OrdinalIgnoreCase));
        if (p != null)
        {
            p.Name = buffer.Trim();
        }

        saveSettings();
    }

    /// <summary>Invalidates sorted-layer cache, persists, and keeps the layer list selection on the same <see cref="TextLayerSettings"/> instance after ZOrder may have changed.</summary>
    private static void NotifyDrawOrderChanged(SettingsModalKeyContext context, SettingsModalState state, TextLayerSettings? layerToKeepSelected)
    {
        context.NotifyLayersStructureChanged();
        if (layerToKeepSelected == null)
        {
            return;
        }

        int idx = context.SortedLayers.FindIndex(l => ReferenceEquals(l, layerToKeepSelected));
        if (idx >= 0)
        {
            state.SelectedLayerIndex = idx;
        }
    }

    private void CyclePresetPalette(TextLayersVisualizerSettings textLayers, bool forward)
    {
        var all = _paletteRepo.GetAll();
        if (all.Count == 0)
        {
            return;
        }

        string? currentId = textLayers.PaletteId ?? "";
        int index = 0;
        bool found = false;
        for (int i = 0; i < all.Count; i++)
        {
            if (string.Equals(all[i].Id, currentId, StringComparison.OrdinalIgnoreCase))
            {
                index = forward ? (i + 1) % all.Count : (i - 1 + all.Count) % all.Count;
                found = true;
                break;
            }
        }

        if (!found)
        {
            index = forward ? 0 : all.Count - 1;
        }

        textLayers.PaletteId = all[index].Id;
    }

    private List<(string Id, string Label, string DisplayValue, SettingEditMode EditMode)> GetSettingsRows(TextLayerSettings layer)
    {
        var descriptors = SettingDescriptor.BuildAll(layer, _paletteRepo, _asciiVideoDeviceCatalog);
        return descriptors.Select(d => (d.Id, d.Label, d.GetDisplayValue(layer), d.EditMode)).ToList();
    }

    private void ApplySettingEdit(TextLayerSettings layer, string settingId, string value)
    {
        var descriptors = SettingDescriptor.BuildAll(layer, _paletteRepo, _asciiVideoDeviceCatalog);
        var d = descriptors.FirstOrDefault(x => x.Id == settingId);
        d?.ApplyEdit(layer, value);
    }

    private void CycleSetting(TextLayerSettings layer, string id, bool forward)
    {
        var descriptors = SettingDescriptor.BuildAll(layer, _paletteRepo, _asciiVideoDeviceCatalog);
        var d = descriptors.FirstOrDefault(x => x.Id == id);
        d?.Cycle(layer, forward);
    }

    private int GetPalettePickerEntryCount(bool includeInheritFirst) =>
        includeInheritFirst ? 1 + _paletteRepo.GetAll().Count : _paletteRepo.GetAll().Count;

    private int ComputeInitialPalettePickerIndex(TextLayerSettings layer)
    {
        var palettes = _paletteRepo.GetAll();
        if (string.IsNullOrWhiteSpace(layer.PaletteId))
        {
            return 0;
        }

        for (int i = 0; i < palettes.Count; i++)
        {
            if (string.Equals(palettes[i].Id, layer.PaletteId, StringComparison.OrdinalIgnoreCase))
            {
                return i + 1;
            }
        }

        return 0;
    }

    private int ComputeInitialPalettePickerIndexPreset(TextLayersVisualizerSettings textLayers)
    {
        var palettes = _paletteRepo.GetAll();
        if (palettes.Count == 0)
        {
            return 0;
        }

        string? cur = textLayers.PaletteId ?? "";
        if (string.IsNullOrWhiteSpace(cur))
        {
            return 0;
        }

        for (int i = 0; i < palettes.Count; i++)
        {
            if (string.Equals(palettes[i].Id, cur, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return 0;
    }

    private void ApplyPalettePickerSelection(TextLayerSettings layer, int index)
    {
        if (index <= 0)
        {
            layer.PaletteId = null;
            return;
        }

        var palettes = _paletteRepo.GetAll();
        int pi = index - 1;
        if (pi >= 0 && pi < palettes.Count)
        {
            layer.PaletteId = palettes[pi].Id;
        }
    }

    private void ApplyPalettePickerSelectionPreset(TextLayersVisualizerSettings textLayers, int index)
    {
        var palettes = _paletteRepo.GetAll();
        if (index < 0 || index >= palettes.Count)
        {
            return;
        }

        textLayers.PaletteId = palettes[index].Id;
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
