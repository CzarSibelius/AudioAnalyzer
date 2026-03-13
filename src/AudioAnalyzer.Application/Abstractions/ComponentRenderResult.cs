namespace AudioAnalyzer.Application.Abstractions;

/// <summary>
/// Return value from a leaf component renderer. Carries lines consumed and optional line content
/// for the dispatcher to write (when non-null); when null, the renderer already wrote (e.g. visualizer area).
/// </summary>
public readonly struct ComponentRenderResult
{
    /// <summary>Number of lines consumed (e.g. 1 for title bar or labeled row).</summary>
    public int LinesConsumed { get; }

    /// <summary>
    /// Line content to write at context.StartRow. When non-null, the dispatcher writes these lines;
    /// when null, the renderer already wrote and the dispatcher only advances by <see cref="LinesConsumed"/>.
    /// </summary>
    public IReadOnlyList<string>? LineContents { get; }

    /// <summary>Creates a result with lines to write.</summary>
    public ComponentRenderResult(int linesConsumed, IReadOnlyList<string>? lineContents)
    {
        LinesConsumed = linesConsumed;
        LineContents = lineContents;
    }

    /// <summary>Single line: dispatcher writes the line at StartRow.</summary>
    public static ComponentRenderResult Line(int linesConsumed, string line) =>
        new(linesConsumed, line != null ? [line] : null);

    /// <summary>Multiple lines: dispatcher writes each line at StartRow + index.</summary>
    public static ComponentRenderResult Line(int linesConsumed, IReadOnlyList<string> lines) =>
        new(linesConsumed, lines);

    /// <summary>Renderer already wrote; dispatcher only advances StartRow by linesConsumed.</summary>
    public static ComponentRenderResult Written(int linesConsumed) =>
        new(linesConsumed, null);
}
