namespace AudioAnalyzer.Console;

/// <summary>Handles key input for the settings overlay modal. Updates state and context; returns true to close the modal.</summary>
internal interface ISettingsModalKeyHandler
{
    /// <summary>Handles one key. Mutates context.State and context.SortedLayers/TextLayers/VisualizerSettings as needed.</summary>
    /// <param name="key">The key pressed.</param>
    /// <param name="context">Current state and mutable data. May be updated.</param>
    /// <returns>True to close the modal (e.g. Escape in layer list).</returns>
    bool Handle(ConsoleKeyInfo key, SettingsModalKeyContext context);
}
