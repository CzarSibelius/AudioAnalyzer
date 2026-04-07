namespace AudioAnalyzer.Application.Abstractions;

/// <summary>Advances scroll state and computes the visible slice of text for a scrolling viewport (ping-pong). No ANSI.</summary>
public interface IScrollingTextEngine
{
    /// <summary>Advances scroll state and returns the visible slice of text, padded to <paramref name="width"/>.</summary>
    /// <param name="text">The full text to display (plain or ANSI-styled).</param>
    /// <param name="width">Viewport width (number of characters visible).</param>
    /// <param name="state">Scroll state; updated in place.</param>
    /// <param name="speedPerReferenceFrame">Characters to advance per reference frame at 60 Hz (ADR-0072).</param>
    /// <param name="frameDeltaSeconds">Wall-clock seconds since last scroll tick.</param>
    /// <returns>The visible substring padded to width with spaces.</returns>
    string GetVisibleSlice<T>(T text, int width, ref ScrollingTextViewportState state, double speedPerReferenceFrame, double frameDeltaSeconds)
        where T : IDisplayText;
}
