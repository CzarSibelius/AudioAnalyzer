using AudioAnalyzer.Visualizers;
using Xunit;

namespace AudioAnalyzer.Tests.Visualizers.TextLayers.AsciiModel;

/// <summary>Tests for <see cref="AsciiShapeTable"/>.</summary>
public sealed class AsciiShapeTableTests
{
    [Fact]
    public void ApplyGlobalContrast_ExponentOne_LeavesUnchanged()
    {
        var expected = new float[] { 0.3f, 0.6f, 0.2f, 0.1f, 0.5f, 0.4f };
        Span<float> s = stackalloc float[6];
        expected.AsSpan().CopyTo(s);

        AsciiShapeTable.ApplyGlobalContrast(s, 1f);

        Assert.Equal(expected, s.ToArray());
    }

    [Fact]
    public void ApplyGlobalContrast_ExponentAboveOne_ReducesNonMaxComponents()
    {
        Span<float> s = stackalloc float[6];
        s[0] = 0.2f;
        s[1] = 0.8f;
        s[2] = 0.4f;
        s[3] = 0.4f;
        s[4] = 0.4f;
        s[5] = 0.4f;

        AsciiShapeTable.ApplyGlobalContrast(s, 2f);

        Assert.Equal(0.8f, s[1], 5);
        Assert.True(s[0] < 0.2f);
    }

    [Fact]
    public void FindBestCharacter_ExactRowMatch_ReturnsCharsetChar()
    {
        ReadOnlySpan<float> rows = AsciiShapeTable.NormalizedShapeRows;
        const int row = 17;
        Span<float> s = stackalloc float[6];
        for (int k = 0; k < 6; k++)
        {
            s[k] = rows[row * 6 + k];
        }

        char c = AsciiShapeTable.FindBestCharacter(s);

        Assert.Equal(AsciiShapeTable.ShapeCharset[row], c);
    }
}
