using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Console;

/// <summary>Provides help content assembled from key handler bindings, ordered by active view. Per ADR-0049.</summary>
internal interface IHelpContentProvider
{
    /// <summary>Returns help sections ordered for the given application mode (current view first).</summary>
    /// <param name="currentMode">Active view; used to prioritize section order.</param>
    /// <returns>Ordered list of sections with bindings; only sections that have bindings are included.</returns>
    IReadOnlyList<HelpSection> GetSections(ApplicationMode currentMode);
}
