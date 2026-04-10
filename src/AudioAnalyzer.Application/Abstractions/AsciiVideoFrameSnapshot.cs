namespace AudioAnalyzer.Application.Abstractions;

/// <summary>One decoded video frame as BGRA8888 row-major pixels, suitable for ASCII conversion.</summary>
public sealed class AsciiVideoFrameSnapshot
{
    /// <summary>Initializes a new instance of the <see cref="AsciiVideoFrameSnapshot"/> class.</summary>
    public AsciiVideoFrameSnapshot(int width, int height, long sequence, byte[] bgraPixels)
    {
        Width = width;
        Height = height;
        Sequence = sequence;
        BgraPixels = bgraPixels ?? throw new ArgumentNullException(nameof(bgraPixels));
        if (bgraPixels.Length < width * height * 4)
        {
            throw new ArgumentException("BGRA buffer too small for dimensions.", nameof(bgraPixels));
        }
    }

    /// <summary>Frame width in pixels.</summary>
    public int Width { get; }

    /// <summary>Frame height in pixels.</summary>
    public int Height { get; }

    /// <summary>Monotonic frame id for change detection.</summary>
    public long Sequence { get; }

    /// <summary>Length = <see cref="Width"/> * <see cref="Height"/> * 4, order BGRA per pixel, rows contiguous.</summary>
    public byte[] BgraPixels { get; }
}
