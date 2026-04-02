using System.IO.Abstractions;
using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;
using AudioAnalyzer.Visualizers;
using Xunit;

namespace AudioAnalyzer.Tests.Visualizers.TextLayers;

/// <summary>Tests for <see cref="LayerToolbarContextualRows"/> and shared <see cref="FileBasedLayerAssetPaths"/> ordering.</summary>
public sealed class LayerToolbarContextualRowsTests
{
    private static readonly IFileSystem s_realFs = new FileSystem();

    [Fact]
    public void Oscilloscope_uses_default_gain_when_no_custom()
    {
        var layer = new TextLayerSettings { LayerType = TextLayerType.Oscilloscope };
        IReadOnlyList<LayerToolbarContextualRow> rows = LayerToolbarContextualRows.Resolve(layer, 0, s_realFs);
        Assert.Single(rows);
        Assert.Equal("Gain", rows[0].Label);
        Assert.Equal("2.5", rows[0].Value);
    }

    [Fact]
    public void Oscilloscope_respects_custom_gain()
    {
        var layer = new TextLayerSettings { LayerType = TextLayerType.Oscilloscope };
        layer.SetCustom(new OscilloscopeSettings { Gain = 4.5 });
        IReadOnlyList<LayerToolbarContextualRow> rows = LayerToolbarContextualRows.Resolve(layer, 0, s_realFs);
        Assert.Single(rows);
        Assert.Equal("Gain", rows[0].Label);
        Assert.Equal("4.5", rows[0].Value);
    }

    [Fact]
    public void Marquee_returns_empty()
    {
        var layer = new TextLayerSettings { LayerType = TextLayerType.Marquee };
        IReadOnlyList<LayerToolbarContextualRow> rows = LayerToolbarContextualRows.Resolve(layer, 0, s_realFs);
        Assert.Empty(rows);
    }

    [Fact]
    public void AsciiImage_empty_folder_shows_placeholder()
    {
        string dir = Path.Combine(Path.GetTempPath(), "aa-toolbar-img-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var layer = new TextLayerSettings { LayerType = TextLayerType.AsciiImage };
            layer.SetCustom(new AsciiImageSettings { ImageFolderPath = dir });
            IReadOnlyList<LayerToolbarContextualRow> rows = LayerToolbarContextualRows.Resolve(layer, 0, s_realFs);
            Assert.Single(rows);
            Assert.Equal("Image", rows[0].Label);
            Assert.Equal("No images", rows[0].Value);
        }
        finally
        {
            TryDeleteTestDir(dir);
        }
    }

    [Fact]
    public void AsciiImage_selected_file_name_controls_toolbar_first_when_unset()
    {
        string dir = Path.Combine(Path.GetTempPath(), "aa-toolbar-img2-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            File.WriteAllText(Path.Combine(dir, "z.png"), "x");
            File.WriteAllText(Path.Combine(dir, "a.png"), "x");
            File.WriteAllText(Path.Combine(dir, "m.png"), "x");

            var layer = new TextLayerSettings { LayerType = TextLayerType.AsciiImage };
            layer.SetCustom(new AsciiImageSettings { ImageFolderPath = dir });

            Assert.Equal("a.png", LayerToolbarContextualRows.Resolve(layer, 0, s_realFs)[0].Value);
            Assert.Equal("a.png", LayerToolbarContextualRows.Resolve(layer, 99, s_realFs)[0].Value);
        }
        finally
        {
            TryDeleteTestDir(dir);
        }
    }

    [Fact]
    public void AsciiImage_selected_file_name_overrides_snippet_index()
    {
        string dir = Path.Combine(Path.GetTempPath(), "aa-toolbar-img3-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            File.WriteAllText(Path.Combine(dir, "a.png"), "x");
            File.WriteAllText(Path.Combine(dir, "m.png"), "x");

            var layer = new TextLayerSettings { LayerType = TextLayerType.AsciiImage };
            layer.SetCustom(new AsciiImageSettings { ImageFolderPath = dir, SelectedImageFileName = "m.png" });

            Assert.Equal("m.png", LayerToolbarContextualRows.Resolve(layer, 0, s_realFs)[0].Value);
        }
        finally
        {
            TryDeleteTestDir(dir);
        }
    }

    [Fact]
    public void AsciiModel_selected_file_name_controls_toolbar()
    {
        string dir = Path.Combine(Path.GetTempPath(), "aa-toolbar-obj-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            File.WriteAllText(Path.Combine(dir, "b.obj"), "x");
            File.WriteAllText(Path.Combine(dir, "a.obj"), "x");

            var layer = new TextLayerSettings { LayerType = TextLayerType.AsciiModel };
            layer.SetCustom(new AsciiModelSettings { ModelFolderPath = dir });

            Assert.Equal("a.obj", LayerToolbarContextualRows.Resolve(layer, 0, s_realFs)[0].Value);
            layer.SetCustom(new AsciiModelSettings { ModelFolderPath = dir, SelectedModelFileName = "b.obj" });
            Assert.Equal("b.obj", LayerToolbarContextualRows.Resolve(layer, 0, s_realFs)[0].Value);
        }
        finally
        {
            TryDeleteTestDir(dir);
        }
    }

    [Fact]
    public void FileBasedLayerAssetPaths_matches_layer_enumeration_order()
    {
        string dir = Path.Combine(Path.GetTempPath(), "aa-asset-paths-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            File.WriteAllText(Path.Combine(dir, "2.png"), "x");
            File.WriteAllText(Path.Combine(dir, "1.png"), "x");
            var images = FileBasedLayerAssetPaths.GetSortedImagePaths(dir, null, s_realFs);
            Assert.Equal(2, images.Count);
            Assert.EndsWith("1.png", images[0], StringComparison.OrdinalIgnoreCase);
            Assert.EndsWith("2.png", images[1], StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            TryDeleteTestDir(dir);
        }
    }

    private static void TryDeleteTestDir(string dir)
    {
        if (!Directory.Exists(dir))
        {
            return;
        }

        try
        {
            Directory.Delete(dir, recursive: true);
        }
        catch (IOException)
        {
            // Best-effort cleanup of temp test directory.
        }
    }
}
