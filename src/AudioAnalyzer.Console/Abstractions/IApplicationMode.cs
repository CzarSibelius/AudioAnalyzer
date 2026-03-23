using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Console;

/// <summary>
/// Runtime behavior and layout for a top-level <see cref="ApplicationMode"/> (preset editor, show play, or general settings).
/// </summary>
internal interface IApplicationMode
{
    /// <summary>Persisted mode key.</summary>
    ApplicationMode Key { get; }

    /// <summary>Human-readable name for help and UI.</summary>
    string DisplayName { get; }

    /// <summary>Number of header rows (title bar only = 1; full analysis header = 3).</summary>
    int HeaderLineCount { get; }

    /// <summary>Whether fullscreen layout may hide the toolbar (not used in Settings).</summary>
    bool AllowsVisualizerFullscreen { get; }

    /// <summary>When true, the shell routes keys to the general settings hub handler before the main loop.</summary>
    bool UsesGeneralSettingsHubKeyHandling { get; }

    /// <summary>Visualizer layer keys; returns false in Settings when the hub consumes keys separately.</summary>
    bool TryHandleVisualizerKeys(ConsoleKeyInfo key, ITextLayerBoundsEditSession bounds, IVisualizer visualizer);

    /// <summary>Toolbar row plus main block (hub or visualizer area).</summary>
    IReadOnlyList<IUiComponent> GetMainComponents(MainContentRenderArgs args);
}
