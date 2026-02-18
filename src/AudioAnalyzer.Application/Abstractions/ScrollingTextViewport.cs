namespace AudioAnalyzer.Application.Abstractions;

/// <summary>
/// Renders text within a fixed-width viewport. When text exceeds the width, it scrolls slowly back and forth (ping-pong).
/// </summary>
public static class ScrollingTextViewport
{
    /// <summary>
    /// Advances scroll state and returns the visible slice of text, padded to <paramref name="width"/>.
    /// When text fits, returns it left-aligned. When text overflows, returns a sliding window that bounces at the ends.
    /// </summary>
    /// <param name="text">The full text to display.</param>
    /// <param name="width">Viewport width (number of characters visible).</param>
    /// <param name="state">Scroll state; updated in place.</param>
    /// <param name="speedPerFrame">Characters to advance per frame. Use ~0.2–0.3 for a slow scroll.</param>
    /// <returns>The visible substring padded to width with spaces.</returns>
    public static string Render(string text, int width, ref ScrollingTextViewportState state, double speedPerFrame)
    {
        if (width <= 0)
        {
            return "";
        }

        if (string.IsNullOrEmpty(text))
        {
            return new string(' ', width);
        }

        if (text.Length <= width)
        {
            state.Reset();
            return text.PadRight(width);
        }

        int maxOffset = text.Length - width;

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
        string visible = text.Substring(start, width);
        return visible.PadRight(width);
    }

    /// <summary>
    /// Like <see cref="Render"/> but supports text with embedded ANSI escape sequences. Slices by visible character
    /// positions so colors are preserved and escape sequences are never cut in half.
    /// </summary>
    public static string RenderWithAnsi(string text, int width, ref ScrollingTextViewportState state, double speedPerFrame)
    {
        if (width <= 0)
        {
            return "";
        }

        if (string.IsNullOrEmpty(text))
        {
            return new string(' ', width);
        }

        int visibleLength = AnsiConsole.GetVisibleLength(text);
        if (visibleLength <= width)
        {
            state.Reset();
            return AnsiConsole.PadToVisibleWidth(text, width);
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
        return AnsiConsole.GetVisibleSubstring(text, start, width);
    }

    /// <summary>
    /// Renders a static label followed by scrollable content. The label is always visible; the text scrolls in the remaining width when it overflows.
    /// </summary>
    /// <param name="label">Static prefix (e.g. "Device: ") always shown at the start.</param>
    /// <param name="text">Dynamic content; scrolls within the remaining width when it overflows.</param>
    /// <param name="totalWidth">Total cell width. The scrolling region is totalWidth - label.Length characters.</param>
    /// <param name="state">Scroll state; updated in place.</param>
    /// <param name="speedPerFrame">Characters to advance per frame.</param>
    /// <returns>Label + scroll output, padded to totalWidth.</returns>
    public static string RenderWithLabel(string label, string text, int totalWidth, ref ScrollingTextViewportState state, double speedPerFrame)
    {
        if (totalWidth <= 0)
        {
            return "";
        }

        var effectiveLabel = label ?? "";
        int scrollWidth = Math.Max(0, totalWidth - effectiveLabel.Length);
        string scrollPart = Render(text ?? "", scrollWidth, ref state, speedPerFrame);
        string result = effectiveLabel + scrollPart;
        return result.Length >= totalWidth ? result[..totalWidth] : result.PadRight(totalWidth);
    }

    /// <summary>
    /// Like <see cref="RenderWithLabel"/> but the text may contain ANSI escape sequences (e.g. for styling).
    /// </summary>
    public static string RenderWithLabelWithAnsi(string label, string text, int totalWidth, ref ScrollingTextViewportState state, double speedPerFrame)
    {
        if (totalWidth <= 0)
        {
            return "";
        }

        var effectiveLabel = label ?? "";
        int scrollWidth = Math.Max(0, totalWidth - effectiveLabel.Length);
        string scrollPart = string.IsNullOrEmpty(text)
            ? new string(' ', scrollWidth)
            : RenderWithAnsi(text, scrollWidth, ref state, speedPerFrame);
        string result = effectiveLabel + scrollPart;
        int visibleLen = AnsiConsole.GetVisibleLength(result);
        return visibleLen > totalWidth
            ? AnsiConsole.GetVisibleSubstring(result, 0, totalWidth)
            : AnsiConsole.PadToVisibleWidth(result, totalWidth);
    }
}
