using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Console;

/// <summary>
/// Inputs for building the main content tree (toolbar + hub or visualizer) for the active mode.
/// </summary>
internal sealed class MainContentRenderArgs
{
    /// <summary>Render context for the main stack (width, snapshot, palette display name, etc.).</summary>
    public required RenderContext Context { get; init; }

    /// <summary>Toolbar row; modes set cell data each frame.</summary>
    public required HorizontalRowComponent ToolbarRow { get; init; }

    /// <summary>Fullscreen and layout guard.</summary>
    public required IDisplayState DisplayState { get; init; }

    /// <summary>Current visualizer (TextLayers).</summary>
    public required IVisualizer Visualizer { get; init; }

    /// <summary>App UI settings.</summary>
    public required UiSettings UiSettings { get; init; }

    /// <summary>Resolved UI chrome palette (theme or inline <see cref="UiSettings.Palette"/>).</summary>
    public required UiPalette EffectiveUiPalette { get; init; }

    /// <summary>Palette colors for P-key swatch in toolbar when applicable.</summary>
    public IReadOnlyList<PaletteColor>? PaletteForSwatch { get; init; }

    /// <summary>Palette display name for toolbar.</summary>
    public string? PaletteDisplayName { get; init; }
}
