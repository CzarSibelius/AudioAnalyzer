using System.Globalization;

namespace AudioAnalyzer.Application.Abstractions;

/// <summary>
/// Renders text within a fixed-width viewport. When text exceeds the width, it scrolls slowly back and forth (ping-pong).
/// Uses grapheme clusters so emoji are never split during scrolling.
/// </summary>
public static class ScrollingTextViewport
{
    /// <summary>
    /// Advances scroll state and returns the visible slice of text, padded to <paramref name="width"/>.
    /// When text fits, returns it left-aligned. When text overflows, returns a sliding window that bounces at the ends.
    /// </summary>
    /// <param name="text">The full text to display (plain or ANSI-styled).</param>
    /// <param name="width">Viewport width (number of characters visible).</param>
    /// <param name="state">Scroll state; updated in place.</param>
    /// <param name="speedPerFrame">Characters to advance per frame. Use ~0.2–0.3 for a slow scroll.</param>
    /// <returns>The visible substring padded to width with spaces.</returns>
    public static string Render<T>(T text, int width, ref ScrollingTextViewportState state, double speedPerFrame)
        where T : IDisplayText
    {
        if (width <= 0)
        {
            return "";
        }

        if (string.IsNullOrEmpty(text.Value))
        {
            return new string(' ', width);
        }

        int visibleLength = text.GetVisibleLength();
        if (visibleLength <= width)
        {
            state.Reset();
            return text.PadToWidth(width);
        }

        int maxOffset = visibleLength - width;

        state.Offset += speedPerFrame * state.Direction;

        if (state.Offset >= maxOffset)
        {
            state.Offset = maxOffset;
            state.Direction = -1;
        }
        else if (state.Offset <= 0)
        {
            state.Offset = 0;
            state.Direction = 1;
        }

        int start = (int)Math.Clamp(Math.Floor(state.Offset), 0, maxOffset);
        return text.GetVisibleSubstring(start, width);
    }

    /// <summary>
    /// Renders a static label followed by scrollable content. The label is always visible; the text scrolls in the remaining width when it overflows.
    /// </summary>
    /// <param name="label">Static prefix (e.g. "Device: ") always shown at the start.</param>
    /// <param name="text">Dynamic content; scrolls within the remaining width when it overflows (plain or ANSI-styled).</param>
    /// <param name="totalWidth">Total cell width. The scrolling region is totalWidth minus the visible length of the label.</param>
    /// <param name="state">Scroll state; updated in place.</param>
    /// <param name="speedPerFrame">Characters to advance per frame.</param>
    /// <returns>Label + scroll output, padded to totalWidth.</returns>
    public static string RenderWithLabel<T>(string label, T text, int totalWidth, ref ScrollingTextViewportState state, double speedPerFrame)
        where T : IDisplayText
    {
        if (totalWidth <= 0)
        {
            return "";
        }

        var effectiveLabel = label ?? "";
        int labelVisible = string.IsNullOrEmpty(effectiveLabel)
            ? 0
            : new StringInfo(effectiveLabel).LengthInTextElements;
        int scrollWidth = Math.Max(0, totalWidth - labelVisible);
        string scrollPart = string.IsNullOrEmpty(text.Value)
            ? new string(' ', scrollWidth)
            : Render(text, scrollWidth, ref state, speedPerFrame);
        string result = effectiveLabel + scrollPart;
        int visibleLen = AnsiConsole.GetVisibleLength(result);
        return visibleLen > totalWidth
            ? AnsiConsole.GetVisibleSubstring(result, 0, totalWidth)
            : AnsiConsole.PadToVisibleWidth(result, totalWidth);
    }
}
