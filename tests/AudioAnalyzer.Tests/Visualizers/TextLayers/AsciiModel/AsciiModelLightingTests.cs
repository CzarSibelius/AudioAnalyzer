using System.Numerics;
using AudioAnalyzer.Domain;
using AudioAnalyzer.Visualizers;
using Xunit;

namespace AudioAnalyzer.Tests.Visualizers.TextLayers.AsciiModel;

/// <summary>Tests for <see cref="AsciiModelLighting"/>.</summary>
public sealed class AsciiModelLightingTests
{
    [Fact]
    public void CombineDiffuseAndAmbient_at_zero_diffuse_returns_ambient()
    {
        Assert.Equal(0.2f, AsciiModelLighting.CombineDiffuseAndAmbient(0f, 0.2f), 5);
    }

    [Fact]
    public void CombineDiffuseAndAmbient_at_full_diffuse_is_one()
    {
        Assert.Equal(1f, AsciiModelLighting.CombineDiffuseAndAmbient(1f, 0.2f), 5);
    }

    [Fact]
    public void GetLightDirection_Headlight_is_positive_Z()
    {
        var v = AsciiModelLighting.GetLightDirection(AsciiModelLightingPreset.Headlight, 0, 0);
        Assert.Equal(new Vector3(0f, 0f, 1f), v);
    }
}
