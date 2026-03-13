namespace AudioAnalyzer.Console;

/// <summary>Provides console dimension helpers. Injected for testability and consistency with ADR-0040.</summary>
internal interface IConsoleDimensions
{
    /// <summary>Gets the current console width in columns, or 80 if unavailable.</summary>
    int GetConsoleWidth();
}
