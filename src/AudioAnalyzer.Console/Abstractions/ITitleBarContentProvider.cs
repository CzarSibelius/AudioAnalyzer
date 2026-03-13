using AudioAnalyzer.Application.Abstractions;

namespace AudioAnalyzer.Console;

/// <summary>Provides the title bar line content (app/mode/preset/layer breadcrumb) as preformatted display text.</summary>
internal interface ITitleBarContentProvider
{
    /// <summary>Returns the current title bar content (ANSI-colored breadcrumb). Used by a labeled row viewport with <see cref="Viewport.PreformattedAnsi"/>.</summary>
    IDisplayText GetTitleBarContent();
}
