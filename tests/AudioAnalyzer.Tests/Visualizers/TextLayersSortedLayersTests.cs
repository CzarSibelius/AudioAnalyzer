using AudioAnalyzer.Domain;
using AudioAnalyzer.Visualizers;
using Xunit;

namespace AudioAnalyzer.Tests.Visualizers;

public sealed class TextLayersSortedLayersTests
{
    [Fact]
    public void BuildSortedByZOrderCopy_orders_by_ZOrder()
    {
        var settings = new TextLayersVisualizerSettings
        {
            Layers =
            [
                new TextLayerSettings { ZOrder = 10, LayerType = TextLayerType.Marquee },
                new TextLayerSettings { ZOrder = 1, LayerType = TextLayerType.Fill }
            ]
        };

        var sorted = TextLayersSortedLayers.BuildSortedByZOrderCopy(settings);
        Assert.NotNull(sorted);
        Assert.Equal(2, sorted.Count);
        Assert.Equal(TextLayerType.Fill, sorted[0].LayerType);
        Assert.Equal(TextLayerType.Marquee, sorted[1].LayerType);
    }

    [Fact]
    public void BuildSortedByZOrderCopy_returns_null_when_no_layers()
    {
        Assert.Null(TextLayersSortedLayers.BuildSortedByZOrderCopy(null));
        Assert.Null(TextLayersSortedLayers.BuildSortedByZOrderCopy(new TextLayersVisualizerSettings()));
    }
}
