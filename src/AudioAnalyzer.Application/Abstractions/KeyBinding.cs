namespace AudioAnalyzer.Application.Abstractions;

/// <summary>
/// Describes one key binding for discovery (e.g. dynamic help). Per ADR-0048.
/// </summary>
/// <param name="Key">Display string for the key and modifiers (e.g. "Tab", "Ctrl+Shift+E").</param>
/// <param name="Description">Short description for help or other consumers.</param>
/// <param name="Section">Optional section/category for grouping (e.g. "Main", "Preset settings modal").</param>
public sealed record KeyBinding(string Key, string Description, string? Section = null);
