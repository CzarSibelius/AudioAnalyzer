using System.Globalization;
using AudioAnalyzer.Domain;

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
    /// Formats a label with optional hotkey. When hotkey is provided, returns "Label (K): "; otherwise "Label: ".
    /// Use for labeled viewports that reference features with keyboard shortcuts. Per ADR-0034.
    /// </summary>
    /// <param name="label">Base label (e.g. "Preset", "Device") without colon.</param>
    /// <param name="hotkey">Optional hotkey to show in parentheses (e.g. "V", "D", "Tab").</param>
    /// <returns>Formatted label string.</returns>
    public static string FormatLabel(string label, string? hotkey)
    {
        var baseLabel = label ?? "";
        if (string.IsNullOrWhiteSpace(hotkey))
        {
            return string.IsNullOrEmpty(baseLabel) ? "" : baseLabel + ": ";
        }
        return string.IsNullOrEmpty(baseLabel) ? "" : baseLabel + " (" + hotkey + "): ";
    }

    /// <summary>
    /// Renders a static label followed by scrollable content. The label is always visible; the text scrolls in the remaining width when it overflows.
    /// When palette colors are provided, wraps label and scroll output with ANSI codes. Per ADR-0033.
    /// </summary>
    /// <param name="label">Static prefix (e.g. "Device: ") always shown at the start. When <paramref name="hotkey"/> is provided, use base label (e.g. "Device").</param>
    /// <param name="text">Dynamic content; scrolls within the remaining width when it overflows (plain or ANSI-styled).</param>
    /// <param name="totalWidth">Total cell width. The scrolling region is totalWidth minus the visible length of the label.</param>
    /// <param name="state">Scroll state; updated in place.</param>
    /// <param name="speedPerFrame">Characters to advance per frame.</param>
    /// <param name="labelColor">Optional color for the label. When null, label is plain.</param>
    /// <param name="textColor">Optional color for the dynamic text. When null, preserves any ANSI in the text.</param>
    /// <param name="hotkey">Optional hotkey to show in the label (e.g. "V", "D"). When provided, label is formatted via <see cref="FormatLabel"/>.</param>
    /// <returns>Label + scroll output, padded to totalWidth.</returns>
    public static string RenderWithLabel<T>(string label, T text, int totalWidth, ref ScrollingTextViewportState state, double speedPerFrame, PaletteColor? labelColor = null, PaletteColor? textColor = null, string? hotkey = null)
        where T : IDisplayText
    {
        if (totalWidth <= 0)
        {
            return "";
        }

        var effectiveLabel = !string.IsNullOrEmpty(hotkey) ? FormatLabel(label ?? "", hotkey) : (label ?? "");
        int labelVisible = string.IsNullOrEmpty(effectiveLabel)
            ? 0
            : new StringInfo(effectiveLabel).LengthInTextElements;
        int scrollWidth = Math.Max(0, totalWidth - labelVisible);
        string scrollPart = string.IsNullOrEmpty(text.Value)
            ? new string(' ', scrollWidth)
            : Render(text, scrollWidth, ref state, speedPerFrame);

        string labelSegment = labelColor.HasValue
            ? AnsiConsole.ColorCode(labelColor.Value) + effectiveLabel + AnsiConsole.ResetCode
            : effectiveLabel;
        string scrollSegment = textColor.HasValue
            ? AnsiConsole.ColorCode(textColor.Value) + scrollPart + AnsiConsole.ResetCode
            : scrollPart;

        string result = labelSegment + scrollSegment;
        int visibleLen = AnsiConsole.GetVisibleLength(result);
        return visibleLen > totalWidth
            ? AnsiConsole.GetVisibleSubstring(result, 0, totalWidth)
            : AnsiConsole.PadToVisibleWidth(result, totalWidth);
    }
}
