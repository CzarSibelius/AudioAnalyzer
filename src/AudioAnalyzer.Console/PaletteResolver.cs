using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Console;

/// <summary>Resolves and applies color palettes for palette-aware visualizers.</summary>
internal static class PaletteResolver
{
    /// <summary>Resolves the palette for the given mode from settings and applies it to the renderer. TextLayers resolves palettes per-layer at render time.</summary>
    public static void ResolveAndSetForMode(
        VisualizationMode mode,
        VisualizerSettings visSettings,
        IPaletteRepository repo,
        IVisualizationRenderer visRenderer)
    {
        // All palette-aware modes (TextLayers) resolve palettes internally. No pre-resolve needed.
    }
}
