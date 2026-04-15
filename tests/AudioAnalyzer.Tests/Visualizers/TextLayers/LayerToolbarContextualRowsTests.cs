using System.IO.Abstractions.TestingHelpers;
using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;
using AudioAnalyzer.Visualizers;
using Xunit;

namespace AudioAnalyzer.Tests.Visualizers.TextLayers;

/// <summary>Tests for <see cref="LayerToolbarContextualRows"/> and shared <see cref="FileBasedLayerAssetPaths"/> ordering.</summary>
public sealed class LayerToolbarContextualRowsTests
{
    private static readonly MockFileSystem s_emptyFs = new();

    [Fact]
    public void Oscilloscope_uses_default_gain_when_no_custom()
    {
        var layer = new TextLayerSettings { LayerType = TextLayerType.Oscilloscope };
        IReadOnlyList<LayerToolbarContextualRow> rows = LayerToolbarContextualRows.Resolve(layer, 0, s_emptyFs);
        Assert.Single(rows);
        Assert.Equal("Gain", rows[0].Label);
        Assert.Equal("2.5", rows[0].Value);
    }

    [Fact]
    public void Oscilloscope_respects_custom_gain()
    {
        var layer = new TextLayerSettings { LayerType = TextLayerType.Oscilloscope };
        layer.SetCustom(new OscilloscopeSettings { Gain = 4.5 });
        IReadOnlyList<LayerToolbarContextualRow> rows = LayerToolbarContextualRows.Resolve(layer, 0, s_emptyFs);
        Assert.Single(rows);
        Assert.Equal("Gain", rows[0].Label);
        Assert.Equal("4.5", rows[0].Value);
    }

    [Fact]
    public void WaveformStrip_uses_default_gain_when_no_custom()
    {
        var layer = new TextLayerSettings { LayerType = TextLayerType.WaveformStrip };
        IReadOnlyList<LayerToolbarContextualRow> rows = LayerToolbarContextualRows.Resolve(layer, 0, s_emptyFs);
        Assert.Single(rows);
        Assert.Equal("Gain", rows[0].Label);
        Assert.Equal("2.5", rows[0].Value);
    }

    [Fact]
    public void WaveformStrip_respects_custom_gain()
    {
        var layer = new TextLayerSettings { LayerType = TextLayerType.WaveformStrip };
        layer.SetCustom(new WaveformStripSettings { Gain = 6.0 });
        IReadOnlyList<LayerToolbarContextualRow> rows = LayerToolbarContextualRows.Resolve(layer, 0, s_emptyFs);
        Assert.Single(rows);
        Assert.Equal("Gain", rows[0].Label);
        Assert.Equal("6.0", rows[0].Value);
    }

    [Fact]
    public void Marquee_returns_empty()
    {
        var layer = new TextLayerSettings { LayerType = TextLayerType.Marquee };
        IReadOnlyList<LayerToolbarContextualRow> rows = LayerToolbarContextualRows.Resolve(layer, 0, s_emptyFs);
        Assert.Empty(rows);
    }

    [Fact]
    public void AsciiImage_empty_folder_shows_placeholder()
    {
        const string dir = @"W:\toolbar-empty-img";
        var fs = new MockFileSystem();
        fs.Directory.CreateDirectory(dir);

        var layer = new TextLayerSettings { LayerType = TextLayerType.AsciiImage };
        layer.SetCustom(new AsciiImageSettings { ImageFolderPath = dir });
        IReadOnlyList<LayerToolbarContextualRow> rows = LayerToolbarContextualRows.Resolve(layer, 0, fs);
        Assert.Single(rows);
        Assert.Equal("Image", rows[0].Label);
        Assert.Equal("No images", rows[0].Value);
    }

    [Fact]
    public void AsciiImage_selected_file_name_controls_toolbar_first_when_unset()
    {
        const string dir = @"W:\toolbar-img2";
        var fs = new MockFileSystem(new Dictionary<string, MockFileData>(StringComparer.OrdinalIgnoreCase)
        {
            [Path.Combine(dir, "z.png")] = new MockFileData("x"),
            [Path.Combine(dir, "a.png")] = new MockFileData("x"),
            [Path.Combine(dir, "m.png")] = new MockFileData("x")
        });

        var layer = new TextLayerSettings { LayerType = TextLayerType.AsciiImage };
        layer.SetCustom(new AsciiImageSettings { ImageFolderPath = dir });

        Assert.Equal("a.png", LayerToolbarContextualRows.Resolve(layer, 0, fs)[0].Value);
        Assert.Equal("a.png", LayerToolbarContextualRows.Resolve(layer, 99, fs)[0].Value);
    }

    [Fact]
    public void AsciiImage_selected_file_name_overrides_snippet_index()
    {
        const string dir = @"W:\toolbar-img3";
        var fs = new MockFileSystem(new Dictionary<string, MockFileData>(StringComparer.OrdinalIgnoreCase)
        {
            [Path.Combine(dir, "a.png")] = new MockFileData("x"),
            [Path.Combine(dir, "m.png")] = new MockFileData("x")
        });

        var layer = new TextLayerSettings { LayerType = TextLayerType.AsciiImage };
        layer.SetCustom(new AsciiImageSettings { ImageFolderPath = dir, SelectedImageFileName = "m.png" });

        Assert.Equal("m.png", LayerToolbarContextualRows.Resolve(layer, 0, fs)[0].Value);
    }

    [Fact]
    public void AsciiModel_selected_file_name_controls_toolbar()
    {
        const string dir = @"W:\toolbar-obj";
        var fs = new MockFileSystem(new Dictionary<string, MockFileData>(StringComparer.OrdinalIgnoreCase)
        {
            [Path.Combine(dir, "b.obj")] = new MockFileData("x"),
            [Path.Combine(dir, "a.obj")] = new MockFileData("x")
        });

        var layer = new TextLayerSettings { LayerType = TextLayerType.AsciiModel };
        layer.SetCustom(new AsciiModelSettings { ModelFolderPath = dir });

        Assert.Equal("a.obj", LayerToolbarContextualRows.Resolve(layer, 0, fs)[0].Value);
        layer.SetCustom(new AsciiModelSettings { ModelFolderPath = dir, SelectedModelFileName = "b.obj" });
        Assert.Equal("b.obj", LayerToolbarContextualRows.Resolve(layer, 0, fs)[0].Value);
    }

    [Fact]
    public void FileBasedLayerAssetPaths_matches_layer_enumeration_order()
    {
        const string dir = @"W:\asset-paths-order";
        var fs = new MockFileSystem(new Dictionary<string, MockFileData>(StringComparer.OrdinalIgnoreCase)
        {
            [Path.Combine(dir, "2.png")] = new MockFileData("x"),
            [Path.Combine(dir, "1.png")] = new MockFileData("x")
        });

        var images = FileBasedLayerAssetPaths.GetSortedImagePaths(dir, null, fs);
        Assert.Equal(2, images.Count);
        Assert.EndsWith("1.png", images[0], StringComparison.OrdinalIgnoreCase);
        Assert.EndsWith("2.png", images[1], StringComparison.OrdinalIgnoreCase);
    }
}
