using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Console;
using AudioAnalyzer.Domain;
using AudioAnalyzer.Visualizers;
using Xunit;

namespace AudioAnalyzer.Tests.Console;

/// <summary>Tests for <see cref="PresetEditorCanvasLayerStackService"/> (PBI-005 canvas Insert/Delete).</summary>
public sealed class PresetEditorCanvasLayerStackServiceTests
{
    private readonly PresetEditorCanvasLayerStackService _sut = new(
        new DefaultTextLayersSettingsFactory(),
        new TextLayerStateStore());

    [Fact]
    public void TryInsertLayer_AddsLayerAndSelectsLastSortedSlot()
    {
        var vs = new VisualizerSettings { TextLayers = new TextLayersVisualizerSettings() };
        var vis = new RecordingVisualizer { ActiveIndex = 0 };

        Assert.True(_sut.TryInsertLayer(vs, vis));

        var layers = vs.TextLayers!.Layers!;
        Assert.Single(layers);
        Assert.Equal(0, vis.ActiveIndex);
        Assert.True(vis.StructureChangedCount >= 1);
    }

    [Fact]
    public void TryInsertLayer_AtMax_ReturnsFalse()
    {
        var vs = new VisualizerSettings { TextLayers = new TextLayersVisualizerSettings() };
        var layers = vs.TextLayers!.Layers ??= new List<TextLayerSettings>();
        var factory = new DefaultTextLayersSettingsFactory();
        for (int i = 0; i < TextLayersLimits.MaxLayerCount; i++)
        {
            layers.Add(factory.CreatePaddingMarqueeLayer(i, i + 1));
        }

        var vis = new RecordingVisualizer();

        Assert.False(_sut.TryInsertLayer(vs, vis));
    }

    [Fact]
    public void TryDeleteActiveLayer_RemovesAtActiveIndex()
    {
        var factory = new DefaultTextLayersSettingsFactory();
        var vs = new VisualizerSettings
        {
            TextLayers = new TextLayersVisualizerSettings
            {
                Layers =
                [
                    factory.CreatePaddingMarqueeLayer(0, 1),
                    factory.CreatePaddingMarqueeLayer(1, 2)
                ]
            }
        };
        var vis = new RecordingVisualizer { ActiveIndex = 0 };

        Assert.True(_sut.TryDeleteActiveLayer(vs, vis));

        Assert.Single(vs.TextLayers!.Layers!);
        Assert.True(vis.StructureChangedCount >= 1);
    }

    [Fact]
    public void TryDeleteActiveLayer_NoLayers_ReturnsFalse()
    {
        var vs = new VisualizerSettings { TextLayers = new TextLayersVisualizerSettings { Layers = [] } };
        var vis = new RecordingVisualizer();

        Assert.False(_sut.TryDeleteActiveLayer(vs, vis));
    }

    private sealed class RecordingVisualizer : IVisualizer
    {
        public int ActiveIndex { get; set; } = -1;
        public int StructureChangedCount { get; private set; }

        public bool SupportsPaletteCycling => false;

        public void Render(VisualizationFrameContext frame, VisualizerViewport viewport)
        {
        }

        public int GetActiveLayerZIndex() => ActiveIndex;

        public void OnTextLayersStructureChanged() => StructureChangedCount++;

        public void SetActiveSortedLayerIndex(int sortedZOrderSlotIndex) =>
            ActiveIndex = sortedZOrderSlotIndex;
    }
}
