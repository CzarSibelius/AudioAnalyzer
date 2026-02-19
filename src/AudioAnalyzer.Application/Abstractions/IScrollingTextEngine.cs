namespace AudioAnalyzer.Application.Abstractions;

/// <summary>Advances scroll state and computes the visible slice of text for a scrolling viewport (ping-pong). No ANSI.</summary>
public interface IScrollingTextEngine
{
    /// <summary>Advances scroll state and returns the visible slice of text, padded to <paramref name="width"/>.</summary>
    /// <param name="text">The full text to display (plain or ANSI-styled).</param>
    /// <param name="width">Viewport width (number of characters visible).</param>
    /// <param name="state">Scroll state; updated in place.</param>
    /// <param name="speedPerFrame">Characters to advance per frame.</param>
    /// <returns>The visible substring padded to width with spaces.</returns>
    string GetVisibleSlice<T>(T text, int width, ref ScrollingTextViewportState state, double speedPerFrame)
        where T : IDisplayText;
}
