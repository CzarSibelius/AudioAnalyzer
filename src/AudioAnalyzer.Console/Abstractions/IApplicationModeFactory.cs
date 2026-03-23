namespace AudioAnalyzer.Console;

/// <summary>
/// Resolves the active <see cref="IApplicationMode"/> from persisted <see cref="Domain.ApplicationMode"/>.
/// </summary>
internal interface IApplicationModeFactory
{
    /// <summary>Returns the mode implementation for <see cref="Domain.VisualizerSettings.ApplicationMode"/>.</summary>
    IApplicationMode GetActiveApplicationMode();
}
