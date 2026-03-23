namespace AudioAnalyzer.Visualizers;

/// <summary>Direction of the linear gradient in Fill layer local cell coordinates.</summary>
public enum FillGradientDirection
{
    /// <summary>0 at left column, 1 at right.</summary>
    LeftToRight,

    /// <summary>0 at right column, 1 at left.</summary>
    RightToLeft,

    /// <summary>0 at top row, 1 at bottom.</summary>
    TopToBottom,

    /// <summary>0 at bottom row, 1 at top.</summary>
    BottomToTop,

    /// <summary>0 at top-left, 1 at bottom-right.</summary>
    TopLeftToBottomRight,

    /// <summary>0 at top-right, 1 at bottom-left.</summary>
    TopRightToBottomLeft,

    /// <summary>0 at bottom-left, 1 at top-right.</summary>
    BottomLeftToTopRight,

    /// <summary>0 at bottom-right, 1 at top-left.</summary>
    BottomRightToTopLeft
}
