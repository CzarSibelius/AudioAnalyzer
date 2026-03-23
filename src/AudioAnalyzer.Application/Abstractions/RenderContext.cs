using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Application.Abstractions;

/// <summary>
/// Context passed from a container to the component renderer. Supplies layout constraints and optional data
/// (device name for header, snapshot for main content). StartRow is advanced by the container after each component.
/// </summary>
public sealed class RenderContext
{
    /// <summary>Console width in columns.</summary>
    public int Width { get; set; }

    /// <summary>Current row where the next component should be drawn. Container advances this after each component.</summary>
    public int StartRow { get; set; }

    /// <summary>Maximum lines available for this region (e.g. visualizer area).</summary>
    public int MaxLines { get; set; }

    /// <summary>UI palette for label and text colors.</summary>
    public UiPalette Palette { get; set; } = new();

    /// <summary>Scrolling speed (characters per frame) for labeled rows.</summary>
    public double ScrollSpeed { get; set; }

    /// <summary>Optional current audio input device display name (header viewports, General Settings hub).</summary>
    public string? DeviceName { get; set; }

    /// <summary>Optional analysis snapshot for main content (toolbar, visualizer).</summary>
    public AnalysisSnapshot? Snapshot { get; set; }

    /// <summary>Optional palette display name for toolbar palette cell.</summary>
    public string? PaletteDisplayName { get; set; }

    /// <summary>When true, the component renderer should invalidate any write caches (e.g. header line cache) before rendering.</summary>
    public bool InvalidateWriteCache { get; set; }
}
