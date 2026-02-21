using System.Text;
using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Visualizers;

/// <summary>Builds the TextLayers toolbar suffix: layer digits 1–9, hints, oscilloscope gain, palette name.</summary>
public sealed class TextLayersToolbarBuilder : ITextLayersToolbarBuilder
{
    /// <inheritdoc />
    public string? BuildSuffix(TextLayersToolbarContext context)
    {
        var sortedLayers = context.SortedLayers;
        if (sortedLayers is not { Count: > 0 })
        {
            var emptyPalette = context.UiSettings.Palette ?? new UiPalette();
            return AnsiConsole.ColorCode(emptyPalette.Label) + "Layers:" + AnsiConsole.ResetCode + AnsiConsole.ColorCode(emptyPalette.Dimmed) + "(config in settings, S: settings)" + AnsiConsole.ResetCode;
        }

        var config = context.Settings;
        int idx = Math.Clamp(context.PaletteCycleLayerIndex, 0, sortedLayers.Count - 1);
        var layer = sortedLayers[idx];
        var paletteId = layer.PaletteId ?? config?.PaletteId ?? "default";
        var paletteDef = context.PaletteRepo.GetById(paletteId);
        var paletteName = paletteDef?.Name?.Trim() ?? paletteId;
        if (string.IsNullOrWhiteSpace(paletteName))
        {
            paletteName = "Default";
        }

        var palette = context.UiSettings.Palette ?? new UiPalette();
        var sb = new StringBuilder();
        AnsiConsole.AppendColored(sb, "Layers:", palette.Label);
        for (int i = 0; i < 9; i++)
        {
            char digit = (char)('1' + i);
            if (i >= sortedLayers.Count)
            {
                AnsiConsole.AppendColored(sb, digit, palette.Dimmed);
            }
            else
            {
                var l = sortedLayers[i];
                if (!l.Enabled)
                {
                    AnsiConsole.AppendColored(sb, digit, palette.Dimmed);
                }
                else if (i == idx)
                {
                    AnsiConsole.AppendColored(sb, digit, palette.Highlighted);
                }
                else
                {
                    AnsiConsole.AppendColored(sb, digit, palette.Normal);
                }
            }
        }
        sb.Append(" (1-9 select, \u2190\u2192 type, Shift+1-9 toggle");
        var hasAscii = sortedLayers.Any(l => l.LayerType == TextLayerType.AsciiImage);
        if (hasAscii)
        {
            sb.Append(", I: next image");
        }
        sb.Append(", S: settings)");
        if (layer.LayerType == TextLayerType.Oscilloscope)
        {
            var osc = layer.GetCustom<OscilloscopeSettings>() ?? new OscilloscopeSettings();
            sb.Append(" | Gain:");
            sb.Append(osc.Gain.ToString("F1", System.Globalization.CultureInfo.InvariantCulture));
            sb.Append(" ([ ])");
        }
        sb.Append(" | Palette(L");
        sb.Append(idx + 1);
        sb.Append("):");
        sb.Append(paletteName);
        sb.Append(" (P)");
        return sb.ToString();
    }
}
