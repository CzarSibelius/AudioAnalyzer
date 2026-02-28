using AudioAnalyzer.Application.Abstractions;

namespace AudioAnalyzer.Console;

/// <summary>One section of help content: title and key bindings. Used by dynamic help per ADR-0049.</summary>
/// <param name="SectionTitle">Display title for the section (e.g. "Keyboard controls").</param>
/// <param name="Bindings">Key bindings in this section.</param>
internal sealed record HelpSection(string SectionTitle, IReadOnlyList<KeyBinding> Bindings);
