using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;
using AudioAnalyzer.Tests.TestSupport;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AudioAnalyzer.Tests.Integration;

/// <summary>Verifies each layer type renders without throwing. Guards against regressions when adding or modifying layers.</summary>
public sealed class LayerRenderingTests
{
    [Theory]
    [MemberData(nameof(GetRenderableLayerTypes))]
    public void LayerRendersWithoutThrowing(TextLayerType layerType)
    {
        var presetJson = BuildSingleLayerPresetJson(layerType);
        var fileSystem = TestHelpers.CreateMockFileSystemWithPreset(presetJson);

        using var provider = TestHelpers.BuildTestServiceProvider(fileSystem);
        var renderer = provider.GetRequiredService<IVisualizationRenderer>();
        var frame = TestHelpers.CreateTestFrame(80, 24);

        var originalOut = System.Console.Out;
        try
        {
            System.Console.SetOut(new StringWriter());
            renderer.Render(frame);
        }
        finally
        {
            System.Console.SetOut(originalOut);
        }
    }

    public static IEnumerable<object[]> GetRenderableLayerTypes()
    {
        foreach (TextLayerType t in Enum.GetValues<TextLayerType>())
        {
            if (t != TextLayerType.None)
            {
                yield return [t];
            }
        }
    }

    private static string BuildSingleLayerPresetJson(TextLayerType layerType)
    {
        var layerJson = $$"""
            {
              "LayerType": "{{layerType}}",
              "Enabled": true,
              "ZOrder": 0,
              "SpeedMultiplier": 1.0
            }
            """;

        return $$"""
            {
              "Name": "Single Layer",
              "Config": {
                "Layers": [ {{layerJson}} ]
              }
            }
            """;
    }
}
