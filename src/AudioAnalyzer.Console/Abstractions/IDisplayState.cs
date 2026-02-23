namespace AudioAnalyzer.Console;

/// <summary>
/// Application display state (e.g. full-screen mode). Injected where needed for reading or updating.
/// </summary>
internal interface IDisplayState
{
    /// <summary>When true, the visualizer uses the full console; header and toolbar are hidden.</summary>
    bool FullScreen { get; set; }

    /// <summary>Raised when a display state property changes (e.g. so the orchestrator can update layout).</summary>
    event EventHandler? Changed;
}
