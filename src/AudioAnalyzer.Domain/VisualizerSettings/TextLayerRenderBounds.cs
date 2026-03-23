namespace AudioAnalyzer.Domain;

/// <summary>
/// Normalized axis-aligned rectangle (0–1 relative to the text-layer viewport width and height).
/// Used to restrict where a layer draws; cells outside the rectangle keep the composite from lower layers.
/// </summary>
public sealed class TextLayerRenderBounds
{
    /// <summary>Left edge, 0 = first column.</summary>
    public double X { get; set; }

    /// <summary>Top edge, 0 = first row.</summary>
    public double Y { get; set; }

    /// <summary>Width as a fraction of viewport width (0–1).</summary>
    public double Width { get; set; } = 1.0;

    /// <summary>Height as a fraction of viewport height (0–1).</summary>
    public double Height { get; set; } = 1.0;

    /// <summary>Converts normalized bounds to integer cell coordinates. When <paramref name="bounds"/> is null, returns the full viewport.</summary>
    public static (int Left, int Top, int Width, int Height) ToPixelRect(TextLayerRenderBounds? bounds, int viewportWidth, int viewportHeight)
    {
        if (viewportWidth < 1 || viewportHeight < 1)
        {
            return (0, 0, 0, 0);
        }

        if (bounds is null)
        {
            return (0, 0, viewportWidth, viewportHeight);
        }

        double x = Math.Clamp(bounds.X, 0.0, 1.0);
        double y = Math.Clamp(bounds.Y, 0.0, 1.0);
        double w = Math.Clamp(bounds.Width, 0.0, 1.0);
        double h = Math.Clamp(bounds.Height, 0.0, 1.0);

        // Keep rectangle inside 0..1
        if (x + w > 1.0) { w = 1.0 - x; }
        if (y + h > 1.0) { h = 1.0 - y; }
        if (w <= 0 || h <= 0)
        {
            return (0, 0, 1, 1);
        }

        int left = (int)(x * viewportWidth);
        int top = (int)(y * viewportHeight);
        int right = (int)Math.Ceiling((x + w) * viewportWidth);
        int bottom = (int)Math.Ceiling((y + h) * viewportHeight);

        left = Math.Clamp(left, 0, viewportWidth - 1);
        top = Math.Clamp(top, 0, viewportHeight - 1);
        right = Math.Clamp(right, left + 1, viewportWidth);
        bottom = Math.Clamp(bottom, top + 1, viewportHeight);

        int pixelW = right - left;
        int pixelH = bottom - top;
        pixelW = Math.Max(1, pixelW);
        pixelH = Math.Max(1, pixelH);
        if (left + pixelW > viewportWidth) { left = viewportWidth - pixelW; }
        if (top + pixelH > viewportHeight) { top = viewportHeight - pixelH; }
        left = Math.Max(0, left);
        top = Math.Max(0, top);

        return (left, top, pixelW, pixelH);
    }

    /// <summary>Creates a deep copy.</summary>
    public TextLayerRenderBounds DeepCopy()
    {
        return new TextLayerRenderBounds
        {
            X = X,
            Y = Y,
            Width = Width,
            Height = Height
        };
    }
}
