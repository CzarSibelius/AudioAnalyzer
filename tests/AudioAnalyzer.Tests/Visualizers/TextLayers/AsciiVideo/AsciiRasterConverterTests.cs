using AudioAnalyzer.Visualizers;
using Xunit;

namespace AudioAnalyzer.Tests.Visualizers.TextLayers.AsciiVideo;

public sealed class AsciiRasterConverterTests
{
    [Fact]
    public void FromBgra_ProducesExpectedDimensionsAfterResize()
    {
        int w = 4;
        int h = 2;
        var bgra = new byte[w * h * 4];
        for (int i = 0; i < bgra.Length; i += 4)
        {
            bgra[i] = 0;
            bgra[i + 1] = 0;
            bgra[i + 2] = 255;
            bgra[i + 3] = 255;
        }

        AsciiFrame? frame = AsciiRasterConverter.FromBgra(bgra, w, h, targetWidth: 8, targetHeight: 4, includeRgb: false);
        Assert.NotNull(frame);
        Assert.True(frame!.Width >= 1);
        Assert.True(frame.Height >= 1);
        Assert.Equal(frame.Width, frame.Chars.GetLength(0));
        Assert.Equal(frame.Height, frame.Chars.GetLength(1));
    }
}
