namespace AudioAnalyzer.Application.Abstractions;

/// <summary>
/// Builds the ANSI title breadcrumb line (ADR-0036, ADR-0060) from UI state, visualizer settings, and <see cref="ITitleBarNavigationContext"/>.
/// </summary>
public interface ITitleBarBreadcrumbFormatter
{
    /// <summary>Returns one preformatted ANSI line for the current navigation context.</summary>
    string BuildAnsiLine();
}
