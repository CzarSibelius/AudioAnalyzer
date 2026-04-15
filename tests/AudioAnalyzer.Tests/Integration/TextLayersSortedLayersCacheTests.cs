using System.Reflection;
using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;
using AudioAnalyzer.Tests.TestSupport;
using AudioAnalyzer.Visualizers;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AudioAnalyzer.Tests.Integration;

/// <summary>Ensures sorted-layer list caching is invalidated when <see cref="IVisualizer.OnTextLayersStructureChanged"/> runs.</summary>
public sealed class TextLayersSortedLayersCacheTests
{
    [Fact]
    public void OnTextLayersStructureChanged_ClearsSortedLayersSnapshotCache()
    {
        var fileSystem = TestHelpers.CreateMockFileSystem();
        using var provider = TestHelpers.BuildTestServiceProvider(fileSystem);
        var visualizer = provider.GetRequiredService<IVisualizer>();
        var renderer = provider.GetRequiredService<IVisualizationRenderer>();
        var frame = TestHelpers.CreateTestFrame(80, 24);

        var originalOut = System.Console.Out;
        try
        {
            System.Console.SetOut(new StringWriter());
            renderer.Render(frame);
            renderer.Render(frame);
        }
        finally
        {
            System.Console.SetOut(originalOut);
        }

        FieldInfo? cacheField = visualizer.GetType().GetField(
            "_sortedLayersSnapshotCache",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(cacheField);
        Assert.NotNull(cacheField.GetValue(visualizer));

        visualizer.OnTextLayersStructureChanged();

        Assert.Null(cacheField.GetValue(visualizer));
    }

    [Fact]
    public void OnTextLayersStructureChanged_AfterZOrderSwap_PermutesLayerStatesAndNextRenderUsesNewOrder()
    {
        var fileSystem = TestHelpers.CreateMockFileSystem();
        using var provider = TestHelpers.BuildTestServiceProvider(fileSystem);
        var visualizer = provider.GetRequiredService<IVisualizer>();
        var vs = provider.GetRequiredService<VisualizerSettings>();
        var renderer = provider.GetRequiredService<IVisualizationRenderer>();
        var frame = TestHelpers.CreateTestFrame(80, 24);

        var geiss = vs.TextLayers!.Layers!.First(l => l.LayerType == TextLayerType.GeissBackground);
        var st = vs.TextLayers.Layers.First(l => l.LayerType == TextLayerType.StaticText);
        Assert.True(geiss.ZOrder < st.ZOrder);

        FieldInfo layerStatesField = visualizer.GetType().GetField(
            "_layerStates",
            BindingFlags.Instance | BindingFlags.NonPublic)!;
        FieldInfo cacheField = visualizer.GetType().GetField(
            "_sortedLayersSnapshotCache",
            BindingFlags.Instance | BindingFlags.NonPublic)!;

        var originalOut = System.Console.Out;
        try
        {
            System.Console.SetOut(new StringWriter());
            renderer.Render(frame);
            renderer.Render(frame);
        }
        finally
        {
            System.Console.SetOut(originalOut);
        }

        Assert.NotNull(cacheField.GetValue(visualizer));
        var layerStates = (List<(double Offset, int SnippetIndex)>)layerStatesField.GetValue(visualizer)!;
        while (layerStates.Count < 2)
        {
            layerStates.Add((0, 0));
        }

        layerStates[0] = (10, 20);
        layerStates[1] = (30, 40);

        (geiss.ZOrder, st.ZOrder) = (st.ZOrder, geiss.ZOrder);
        visualizer.OnTextLayersStructureChanged();

        Assert.Null(cacheField.GetValue(visualizer));
        Assert.Equal((30, 40), layerStates[0]);
        Assert.Equal((10, 20), layerStates[1]);

        try
        {
            System.Console.SetOut(new StringWriter());
            renderer.Render(frame);
        }
        finally
        {
            System.Console.SetOut(originalOut);
        }

        Assert.NotNull(cacheField.GetValue(visualizer));

        var store = provider.GetRequiredService<ITextLayerStateStore>();
        store.ClearAllSlots();
        store.EnsureCapacity(2);
        ITextLayerStateStore<BeatCirclesState> typed = (TextLayerStateStore)store;
        typed.GetState(0).LastBeatCount = 111;
        typed.GetState(1).LastBeatCount = 222;

        (geiss.ZOrder, st.ZOrder) = (st.ZOrder, geiss.ZOrder);
        visualizer.OnTextLayersStructureChanged();

        Assert.Equal(222, typed.GetState(0).LastBeatCount);
        Assert.Equal(111, typed.GetState(1).LastBeatCount);

        try
        {
            System.Console.SetOut(new StringWriter());
            renderer.Render(frame);
        }
        finally
        {
            System.Console.SetOut(originalOut);
        }

        var cacheAfter = (List<TextLayerSettings>?)cacheField.GetValue(visualizer);
        Assert.NotNull(cacheAfter);
        Assert.Equal(TextLayerType.GeissBackground, cacheAfter[0].LayerType);
        Assert.Equal(TextLayerType.StaticText, cacheAfter[1].LayerType);
    }
}
