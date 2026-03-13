using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Application.Abstractions;

/// <summary>
/// Renders a single row of viewports into one line: for each viewport, invokes the value getter,
/// applies scrolling/truncation and label formatting, then concatenates cells to the given total width.
/// Scroll state is held per slot inside the renderer.
/// </summary>
public interface ILabeledRowRenderer
{
    /// <summary>
    /// Renders a row of viewports into one line. Each viewport is allocated the corresponding width from <paramref name="widths"/>.
    /// </summary>
    /// <param name="viewports">Viewports (label + value getter) in order.</param>
    /// <param name="widths">Width in columns for each cell. Must match <paramref name="viewports"/>.Count.</param>
    /// <param name="totalWidth">Total line width; output is padded or truncated to this.</param>
    /// <param name="palette">UI palette for label and text colors.</param>
    /// <param name="scrollSpeed">Scrolling speed (characters per frame) for overflowing content.</param>
    /// <param name="startSlotIndex">Starting index for scroll state slots so different rows use distinct state. Default 0.</param>
    /// <returns>The rendered line (ANSI-colored, display-width padded/truncated to <paramref name="totalWidth"/>).</returns>
    string RenderRow(
        IReadOnlyList<Viewport> viewports,
        IReadOnlyList<int> widths,
        int totalWidth,
        UiPalette palette,
        double scrollSpeed,
        int startSlotIndex = 0);
}
