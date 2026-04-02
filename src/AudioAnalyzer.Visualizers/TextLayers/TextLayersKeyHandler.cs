using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Visualizers;

/// <summary>Config for TextLayers visualizer keys: 1–<see cref="TextLayersLimits.MaxLayerCount"/> select/toggle, P palette, [ ] gain, I next image/model, Left/Right cycle type.</summary>
public sealed class TextLayersKeyHandlerConfig : IKeyHandlerConfig<TextLayersKeyContext>
{
    private const string Section = "Layered text";

    private sealed record BindingEntry(
        Func<ConsoleKeyInfo, bool> Matches,
        Func<ConsoleKeyInfo, TextLayersKeyContext, bool> Action,
        string Key,
        string Description,
        string? Section)
    {
        public KeyBinding ToKeyBinding() => new(Key, Description, Section);
    }

    private static IReadOnlyList<BindingEntry> GetEntries()
    {
        return
        [
            new BindingEntry(
                Matches: k => k.Key == ConsoleKey.P,
                Action: (_, context) =>
                {
                    var sortedLayers = context.SortedLayers;
                    var config = context.Settings;
                    int idx = Math.Clamp(context.PaletteCycleLayerIndex, 0, sortedLayers.Count - 1);
                    var paletteLayer = sortedLayers[idx];
                    var currentId = paletteLayer.PaletteId ?? config?.PaletteId ?? "";
                    var all = context.PaletteRepo.GetAll();
                    if (all.Count == 0)
                    {
                        return true;
                    }
                    int nextIndex = 0;
                    for (int i = 0; i < all.Count; i++)
                    {
                        if (string.Equals(all[i].Id, currentId, StringComparison.OrdinalIgnoreCase))
                        {
                            nextIndex = (i + 1) % all.Count;
                            break;
                        }
                    }
                    var next = all[nextIndex];
                    paletteLayer.PaletteId = next.Id;
                    return true;
                },
                Key: "P",
                Description: "Cycle color palette",
                Section),
            new BindingEntry(
                Matches: k => k.Key is ConsoleKey.Oem4 or ConsoleKey.Oem6,
                Action: (key, context) =>
                {
                    var sortedLayers = context.SortedLayers;
                    int layerIndex = Math.Clamp(context.PaletteCycleLayerIndex, 0, sortedLayers.Count - 1);
                    var layer = sortedLayers[layerIndex];
                    if (layer.LayerType != TextLayerType.Oscilloscope)
                    {
                        return false;
                    }
                    var osc = layer.GetCustom<OscilloscopeSettings>() ?? new OscilloscopeSettings();
                    double delta = key.Key is ConsoleKey.Oem6 ? 0.5 : -0.5;
                    osc.Gain = Math.Clamp(osc.Gain + delta, 1.0, 10.0);
                    layer.SetCustom(osc);
                    return true;
                },
                Key: "[ / ]",
                Description: "Adjust oscilloscope gain (when Oscilloscope layer selected)",
                Section),
            new BindingEntry(
                Matches: k => k.Key == ConsoleKey.I,
                Action: (_, context) =>
                {
                    var sortedLayers = context.SortedLayers;
                    bool anyAdvanced = false;
                    for (int i = 0; i < sortedLayers.Count; i++)
                    {
                        if (FileBasedLayerAssetPaths.TryAdvanceDirectoryAssetSelection(sortedLayers[i], context.UiSettings, context.FileSystem))
                        {
                            anyAdvanced = true;
                        }
                    }
                    return anyAdvanced;
                },
                Key: "I",
                Description: "Next image/model (AsciiImage / AsciiModel)",
                Section),
            new BindingEntry(
                Matches: k => k.Key is ConsoleKey.LeftArrow or ConsoleKey.RightArrow,
                Action: (key, context) =>
                {
                    var sortedLayers = context.SortedLayers;
                    int layerIndex = Math.Clamp(context.PaletteCycleLayerIndex, 0, sortedLayers.Count - 1);
                    var layer = sortedLayers[layerIndex];
                    var previousType = layer.LayerType;
                    layer.LayerType = key.Key is ConsoleKey.LeftArrow
                        ? TextLayerSettings.CycleTypeBackward(layer)
                        : TextLayerSettings.CycleTypeForward(layer);
                    context.ClearLayerState(layerIndex, previousType);
                    return true;
                },
                Key: "←/→",
                Description: "Change layer type",
                Section),
            new BindingEntry(
                Matches: k => DigitFromKey(k.Key) != 0 && k.Modifiers.HasFlag(ConsoleModifiers.Shift),
                Action: (key, context) =>
                {
                    int digit = DigitFromKey(key.Key);
                    var sortedLayers = context.SortedLayers;
                    var config = context.Settings;
                    if (digit == 0 || config?.Layers is not { Count: > 0 })
                    {
                        return false;
                    }
                    int layerIdx = digit - 1;
                    if (layerIdx >= sortedLayers.Count)
                    {
                        return false;
                    }
                    context.PaletteCycleLayerIndex = layerIdx;
                    var l = sortedLayers[layerIdx];
                    l.Enabled = !l.Enabled;
                    return true;
                },
                Key: "Shift+1-9",
                Description: "Toggle layer enabled/disabled by slot",
                Section),
            new BindingEntry(
                Matches: k => DigitFromKey(k.Key) != 0 && !k.Modifiers.HasFlag(ConsoleModifiers.Shift),
                Action: (key, context) =>
                {
                    int digit = DigitFromKey(key.Key);
                    var sortedLayers = context.SortedLayers;
                    var config = context.Settings;
                    if (digit == 0 || config?.Layers is not { Count: > 0 })
                    {
                        return false;
                    }
                    int layerIdx = digit - 1;
                    if (layerIdx >= sortedLayers.Count)
                    {
                        return false;
                    }
                    context.PaletteCycleLayerIndex = layerIdx;
                    return true;
                },
                Key: "1-9",
                Description: "Select layer",
                Section),
        ];
    }

    private static readonly Lazy<IReadOnlyList<BindingEntry>> s_entries = new(GetEntries);

    /// <inheritdoc />
    public IReadOnlyList<KeyBinding> GetBindings() =>
        s_entries.Value.Select(e => e.ToKeyBinding()).ToList();

    /// <inheritdoc />
    public bool Handle(ConsoleKeyInfo key, TextLayersKeyContext context)
    {
        var sortedLayers = context.SortedLayers;
        if (sortedLayers is not { Count: > 0 })
        {
            return false;
        }

        foreach (var entry in s_entries.Value)
        {
            if (entry.Matches(key))
            {
                return entry.Action(key, context);
            }
        }
        return false;
    }

    private static int DigitFromKey(ConsoleKey key) =>
        key switch
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
