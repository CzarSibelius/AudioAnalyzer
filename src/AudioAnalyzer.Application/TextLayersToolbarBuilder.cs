using System.Text;
using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Application.Display;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Application;

/// <summary>Builds the TextLayers toolbar suffix: layer digits 1–<see cref="TextLayersLimits.MaxLayerCount"/>, oscilloscope gain (when applicable), palette name.</summary>
public sealed class TextLayersToolbarBuilder : ITextLayersToolbarBuilder
{
    private readonly IScrollingTextViewport _labelViewport;

    /// <summary>Creates a new toolbar builder with a viewport used for consistent label formatting.</summary>
    public TextLayersToolbarBuilder(IScrollingTextViewport labelViewport)
    {
        _labelViewport = labelViewport ?? throw new ArgumentNullException(nameof(labelViewport));
    }

    /// <inheritdoc />
    public string? BuildSuffix(TextLayersToolbarContext context)
    {
        var sortedLayers = context.SortedLayers;
        if (sortedLayers is not { Count: > 0 })
        {
            var emptyPalette = context.UiSettings.Palette ?? new UiPalette();
            string layersLabel = _labelViewport.FormatLabel("Layers", null);
            return AnsiConsole.ColorCode(emptyPalette.Label) + layersLabel + AnsiConsole.ResetCode + AnsiConsole.ColorCode(emptyPalette.Dimmed) + "(config in settings)" + AnsiConsole.ResetCode;
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
        AnsiConsole.AppendColored(sb, _labelViewport.FormatLabel("Layers", null), palette.Label);
        for (int i = 0; i < TextLayersLimits.MaxLayerCount; i++)
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
                else
                {
                    AnsiConsole.AppendColored(sb, digit, palette.Normal);
                }
            }
        }
        if (layer.LayerType == TextLayerType.Oscilloscope)
        {
            double gain = context.OscilloscopeGain ?? 2.5;
            sb.Append(" | ");
            sb.Append(_labelViewport.FormatLabel("Gain", null));
            sb.Append(gain.ToString("F1", System.Globalization.CultureInfo.InvariantCulture));
        }
        sb.Append(" | ");
        sb.Append(_labelViewport.FormatLabel("Palette(L" + (idx + 1) + ")", null));
        sb.Append(paletteName);
        return sb.ToString();
    }

    /// <inheritdoc />
    public IReadOnlyList<Viewport> BuildViewports(TextLayersToolbarContext context)
    {
        var sortedLayers = context.SortedLayers;
        var palette = context.UiSettings.Palette ?? new UiPalette();
        var list = new List<Viewport>();

        if (sortedLayers is not { Count: > 0 })
        {
            list.Add(new Viewport("Layers", () => new PlainText("(config in settings)"), labelColor: palette.Label, textColor: palette.Dimmed));
            return list;
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

        var layersSb = new StringBuilder();
        AnsiConsole.AppendColored(layersSb, _labelViewport.FormatLabel("Layers", null), palette.Label);
        for (int i = 0; i < TextLayersLimits.MaxLayerCount; i++)
        {
            char digit = (char)('1' + i);
            if (i >= sortedLayers.Count)
            {
                AnsiConsole.AppendColored(layersSb, digit, palette.Dimmed);
            }
            else
            {
                var l = sortedLayers[i];
                if (!l.Enabled)
                {
                    AnsiConsole.AppendColored(layersSb, digit, palette.Dimmed);
                }
                else
                {
                    AnsiConsole.AppendColored(layersSb, digit, palette.Normal);
                }
            }
        }
        string layersAnsi = layersSb.ToString();
        list.Add(new Viewport("Layers", () => new AnsiText(layersAnsi), preformattedAnsi: true));

        if (layer.LayerType == TextLayerType.Oscilloscope)
        {
            double gain = context.OscilloscopeGain ?? 2.5;
            string gainStr = gain.ToString("F1", System.Globalization.CultureInfo.InvariantCulture);
            list.Add(new Viewport("Gain", () => new PlainText(gainStr), labelColor: palette.Label, textColor: palette.Normal));
        }

        string paletteLabel = "Palette(L" + (idx + 1) + ")";
        list.Add(new Viewport(paletteLabel, () => new PlainText(paletteName), labelColor: palette.Label, textColor: palette.Normal));
        return list;
    }
}
