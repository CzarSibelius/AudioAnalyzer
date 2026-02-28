using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Application.Abstractions;

/// <summary>
/// Describes one key binding for discovery (e.g. dynamic help). Per ADR-0048.
/// </summary>
/// <param name="Key">Display string for the key and modifiers (e.g. "Tab", "Ctrl+Shift+E").</param>
/// <param name="Description">Short description for help or other consumers.</param>
/// <param name="Section">Optional section/category for grouping (e.g. "Main", "Preset settings modal").</param>
/// <param name="ApplicableMode">When set, binding is shown in help only for this mode; when null, shown in both. Per ADR-0049.</param>
public sealed record KeyBinding(string Key, string Description, string? Section = null, ApplicationMode? ApplicableMode = null);
