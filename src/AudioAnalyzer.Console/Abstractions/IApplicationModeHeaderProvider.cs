namespace AudioAnalyzer.Console;

/// <summary>
/// Supplies the current header row count for layout (orchestrator display start row, header container).
/// </summary>
internal interface IApplicationModeHeaderProvider
{
    /// <summary>Header line count for the active application mode.</summary>
    int HeaderLineCount { get; }
}
