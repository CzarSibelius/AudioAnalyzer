namespace AudioAnalyzer.Console;

/// <summary>Handles main loop keys not consumed by the visualizer renderer.</summary>
internal interface IMainLoopKeyHandler
{
    /// <summary>Handles the key if it is a main loop command. Returns true if handled.</summary>
    /// <param name="key">The key that was pressed.</param>
    /// <param name="ctx">Context with state and operations.</param>
    /// <returns>True if the key was handled; false if unknown.</returns>
    bool TryHandle(ConsoleKeyInfo key, MainLoopKeyContext ctx);
}
