using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Console;

/// <summary>Resolves and applies color palettes for palette-aware visualizers.</summary>
internal static class PaletteResolver
{
    /// <summary>Resolves the palette for the given mode from settings and applies it to the renderer.</summary>
    public static void ResolveAndSetForMode(
        VisualizationMode mode,
        VisualizerSettings visSettings,
        IPaletteRepository repo,
        IVisualizationRenderer visRenderer)
    {
        IReadOnlyList<PaletteColor>? palette;
        string? displayName;
        string? paletteId = null;

        switch (mode)
        {
            case VisualizationMode.Geiss:
                paletteId = visSettings.Geiss?.PaletteId;
                break;
            case VisualizationMode.UnknownPleasures:
                paletteId = visSettings.UnknownPleasures?.PaletteId;
                if (string.IsNullOrWhiteSpace(paletteId))
                {
                    var legacyPalette = ColorPaletteParser.Parse(visSettings.UnknownPleasures?.Palette);
                    if (legacyPalette != null && legacyPalette.Count > 0)
                    {
                        visRenderer.SetPaletteForMode(mode, legacyPalette, "Custom");
                        return;
                    }
                }
                break;
            case VisualizationMode.TextLayers:
                return;
            default:
                return;
        }

        if (!string.IsNullOrWhiteSpace(paletteId))
        {
            var def = repo.GetById(paletteId);
            if (def != null && (palette = ColorPaletteParser.Parse(def)) != null && palette.Count > 0)
            {
                displayName = def.Name?.Trim();
                visRenderer.SetPaletteForMode(mode, palette, string.IsNullOrEmpty(displayName) ? paletteId : displayName);
                return;
            }
        }

        palette = ColorPaletteParser.DefaultPalette;
        displayName = "Default";
        visRenderer.SetPaletteForMode(mode, palette, displayName);
    }
}
