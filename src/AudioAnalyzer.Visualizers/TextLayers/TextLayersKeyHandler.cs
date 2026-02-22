using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Visualizers;

/// <summary>Handles key input for the TextLayers visualizer: 1–9 select/toggle, P palette, [ ] gain, I next image, Left/Right cycle type.</summary>
public sealed class TextLayersKeyHandler : IKeyHandler<TextLayersKeyContext>
{
    /// <inheritdoc />
    public bool Handle(ConsoleKeyInfo key, TextLayersKeyContext context)
    {
        var sortedLayers = context.SortedLayers;
        if (sortedLayers is not { Count: > 0 })
        {
            return false;
        }

        var config = context.Settings;

        if (key.Key is ConsoleKey.P)
        {
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
        }

        if (key.Key is ConsoleKey.Oem4 or ConsoleKey.Oem6)
        {
            int layerIndex = Math.Clamp(context.PaletteCycleLayerIndex, 0, sortedLayers.Count - 1);
            var layer = sortedLayers[layerIndex];
            if (layer.LayerType == TextLayerType.Oscilloscope)
            {
                var osc = layer.GetCustom<OscilloscopeSettings>() ?? new OscilloscopeSettings();
                double delta = key.Key is ConsoleKey.Oem6 ? 0.5 : -0.5;
                osc.Gain = Math.Clamp(osc.Gain + delta, 1.0, 10.0);
                layer.SetCustom(osc);
                return true;
            }
        }

        if (key.Key is ConsoleKey.I)
        {
            bool anyAdvanced = false;
            for (int i = 0; i < sortedLayers.Count; i++)
            {
                if (sortedLayers[i].LayerType != TextLayerType.AsciiImage)
                {
                    continue;
                }
                context.AdvanceSnippetIndex(i);
                anyAdvanced = true;
            }
            return anyAdvanced;
        }

        if (key.Key is ConsoleKey.LeftArrow or ConsoleKey.RightArrow)
        {
            int layerIndex = Math.Clamp(context.PaletteCycleLayerIndex, 0, sortedLayers.Count - 1);
            var layer = sortedLayers[layerIndex];
            var previousType = layer.LayerType;
            layer.LayerType = key.Key is ConsoleKey.LeftArrow
                ? TextLayerSettings.CycleTypeBackward(layer)
                : TextLayerSettings.CycleTypeForward(layer);
            context.ClearLayerState(layerIndex, previousType);
            return true;
        }

        int digit = key.Key switch
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

        if (key.Modifiers.HasFlag(ConsoleModifiers.Shift))
        {
            var l = sortedLayers[layerIdx];
            l.Enabled = !l.Enabled;
            return true;
        }

        return true;
    }
}
