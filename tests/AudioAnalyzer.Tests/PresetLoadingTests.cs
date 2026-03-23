using System.IO.Abstractions.TestingHelpers;
using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;
using AudioAnalyzer.Infrastructure;
using AudioAnalyzer.Visualizers;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AudioAnalyzer.Tests;

/// <summary>Verifies preset loading, saving, and round-trip. Ensures preset config integrates with the render pipeline.</summary>
public sealed class PresetLoadingTests
{
    [Fact]
    public void PresetLoadAndRenderSucceeds()
    {
        var fileSystem = TestHelpers.CreateMockFileSystem();

        var presetRepo = new FilePresetRepository(fileSystem, TestHelpers.PresetsPath);
        Preset? preset = presetRepo.GetById("preset-1");
        Assert.NotNull(preset);
        Assert.NotEmpty(preset.Config.Layers);

        using var provider = TestHelpers.BuildTestServiceProvider(fileSystem);
        var renderer = provider.GetRequiredService<IVisualizationRenderer>();
        var snapshot = TestHelpers.CreateTestSnapshot(80, 24);

        var originalOut = System.Console.Out;
        try
        {
            System.Console.SetOut(new StringWriter());
            renderer.Render(snapshot);
        }
        finally
        {
            System.Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void PresetSaveAndLoadRoundTripPreservesConfig()
    {
        var paletteJson = """{"Name":"Default","Colors":["Magenta","Yellow","Green"]}""";
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            [Path.Combine(TestHelpers.PalettesPath, "default.json")] = new MockFileData(paletteJson)
        });

        var presetRepo = new FilePresetRepository(fileSystem, TestHelpers.PresetsPath);
        var marqueeLayer = new TextLayerSettings
        {
            LayerType = TextLayerType.Marquee,
            Enabled = true,
            ZOrder = 0,
            TextSnippets = ["Round-trip test"],
            SpeedMultiplier = 1.2
        };
        marqueeLayer.SetCustom(new MarqueeSettings { BeatReaction = MarqueeBeatReaction.None });

        var config = new TextLayersVisualizerSettings
        {
            PaletteId = "default",
            Layers = [marqueeLayer]
        };

        string id = presetRepo.Create(new Preset { Name = "RoundTrip", Config = config });
        Assert.Equal("preset-1", id);

        Preset? loaded = presetRepo.GetById(id);
        Assert.NotNull(loaded);
        Assert.Equal("RoundTrip", loaded.Name);
        Assert.Single(loaded.Config.Layers);
        Assert.Equal(TextLayerType.Marquee, loaded.Config.Layers[0].LayerType);
        Assert.Equal(["Round-trip test"], loaded.Config.Layers[0].TextSnippets);
        Assert.Equal(1.2, loaded.Config.Layers[0].SpeedMultiplier);
    }

    [Fact]
    public void PresetSaveAndLoadRoundTripPreservesRenderBounds()
    {
        var paletteJson = """{"Name":"Default","Colors":["Magenta","Yellow","Green"]}""";
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            [Path.Combine(TestHelpers.PalettesPath, "default.json")] = new MockFileData(paletteJson)
        });

        var presetRepo = new FilePresetRepository(fileSystem, TestHelpers.PresetsPath);
        var layer = new TextLayerSettings
        {
            LayerType = TextLayerType.Fill,
            Enabled = true,
            ZOrder = 0,
            RenderBounds = new TextLayerRenderBounds { X = 0.1, Y = 0.2, Width = 0.5, Height = 0.3 }
        };

        var config = new TextLayersVisualizerSettings
        {
            PaletteId = "default",
            Layers = [layer]
        };

        string id = presetRepo.Create(new Preset { Name = "BoundsTest", Config = config });
        Preset? loaded = presetRepo.GetById(id);
        Assert.NotNull(loaded);
        var rb = loaded!.Config.Layers[0].RenderBounds;
        Assert.NotNull(rb);
        Assert.Equal(0.1, rb!.X, precision: 10);
        Assert.Equal(0.2, rb.Y, precision: 10);
        Assert.Equal(0.5, rb.Width, precision: 10);
        Assert.Equal(0.3, rb.Height, precision: 10);
    }
}
