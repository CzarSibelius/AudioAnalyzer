using AudioAnalyzer.Application.Abstractions;

namespace AudioAnalyzer.Console.KeyHandling;

/// <summary>
/// Internal binding entry used by key handlers to drive both GetBindings() and Handle() from a single source of truth.
/// Matcher and action are used in Handle(); Key, Description, and Section are used for GetBindings() and help.
/// </summary>
/// <param name="Matches">Returns true when the given key matches this binding. Optional context parameter for context-aware matching.</param>
/// <param name="Action">Invoked when the key matches (key, context); return value is the Handle() result (true = handled).</param>
/// <param name="Key">Display string for the key (e.g. "Tab", "Ctrl+Shift+E").</param>
/// <param name="Description">Short description for help.</param>
/// <param name="Section">Optional section for grouping.</param>
internal sealed record KeyBindingEntry<TContext>(
    Func<ConsoleKeyInfo, bool> Matches,
    Func<ConsoleKeyInfo, TContext, bool> Action,
    string Key,
    string Description,
    string? Section = null)
{
    /// <summary>Converts this entry to the public KeyBinding DTO for discovery/help.</summary>
    public KeyBinding ToKeyBinding() => new(Key, Description, Section);
}
