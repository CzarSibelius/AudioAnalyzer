using AudioAnalyzer.Application.Abstractions;
using Xunit;

namespace AudioAnalyzer.Tests;

/// <summary>Tests for TextHelpers.Hackerize transformation.</summary>
public sealed class TextHelpersTests
{
    [Theory]
    [InlineData("Preset", "pReset")]
    [InlineData("Show", "sHow")]
    [InlineData("ascii_image", "aScii_image")]
    public void Hackerize_Basic_TransformsFirstAndSecondLetter(string input, string expected)
    {
        Assert.Equal(expected, TextHelpers.Hackerize(input));
    }

    [Theory]
    [InlineData("Preset 1", "pReset_1")]
    [InlineData("A B", "a_B")]
    [InlineData("My Cool Preset", "mY_Cool_Preset")]
    public void Hackerize_Whitespace_ReplacesWithUnderscores(string input, string expected)
    {
        Assert.Equal(expected, TextHelpers.Hackerize(input));
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("A", "a")]
    [InlineData("AB", "aB")]
    [InlineData("123", "123")]
    public void Hackerize_EdgeCases_HandlesCorrectly(string input, string expected)
    {
        Assert.Equal(expected, TextHelpers.Hackerize(input));
    }

    [Fact]
    public void Hackerize_Null_ReturnsEmptyString()
    {
        Assert.Equal("", TextHelpers.Hackerize(null));
    }
}
