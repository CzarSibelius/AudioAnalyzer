using AudioAnalyzer.Domain;
using AudioAnalyzer.Visualizers;
using Xunit;

namespace AudioAnalyzer.Tests.Visualizers.TextLayers;

/// <summary>Tests for <see cref="FileBasedLayerAssetPaths"/> selection helpers.</summary>
public sealed class FileBasedLayerAssetPathsTests
{
    [Fact]
    public void ResolveIndexByFileName_empty_list_returns_zero()
    {
        Assert.Equal(0, FileBasedLayerAssetPaths.ResolveIndexByFileName([], "a.png"));
    }

    [Fact]
    public void ResolveIndexByFileName_null_or_whitespace_returns_zero()
    {
        var paths = new[] { @"C:\x\a.png", @"C:\x\b.png" };
        Assert.Equal(0, FileBasedLayerAssetPaths.ResolveIndexByFileName(paths, null));
        Assert.Equal(0, FileBasedLayerAssetPaths.ResolveIndexByFileName(paths, "   "));
    }

    [Fact]
    public void ResolveIndexByFileName_matches_case_insensitive()
    {
        var paths = new[] { @"D:\f\Lower.png" };
        Assert.Equal(0, FileBasedLayerAssetPaths.ResolveIndexByFileName(paths, "LOWER.PNG"));
    }

    [Fact]
    public void ResolveIndexByFileName_first_match_when_duplicates_after_sort()
    {
        var paths = new[] { @"C:\a\z.png", @"C:\b\z.png" };
        Assert.Equal(0, FileBasedLayerAssetPaths.ResolveIndexByFileName(paths, "z.png"));
    }

    [Fact]
    public void ResolveIndexByFileName_unknown_returns_zero()
    {
        var paths = new[] { @"C:\x\only.png" };
        Assert.Equal(0, FileBasedLayerAssetPaths.ResolveIndexByFileName(paths, "missing.png"));
    }

    [Fact]
    public void NextFileNameAfter_wraps_and_uses_current()
    {
        var paths = new[] { @"C:\x\a.png", @"C:\x\b.png", @"C:\x\c.png" };
        Assert.Equal("b.png", FileBasedLayerAssetPaths.NextFileNameAfter(paths, "a.png"));
        Assert.Equal("c.png", FileBasedLayerAssetPaths.NextFileNameAfter(paths, "b.png"));
        Assert.Equal("a.png", FileBasedLayerAssetPaths.NextFileNameAfter(paths, "c.png"));
        Assert.Equal("b.png", FileBasedLayerAssetPaths.NextFileNameAfter(paths, null));
    }

    [Fact]
    public void NextFileNameAfter_empty_returns_null()
    {
        Assert.Null(FileBasedLayerAssetPaths.NextFileNameAfter([], null));
    }

    [Fact]
    public void TryAdvanceDirectoryAssetSelection_ascii_image_updates_custom()
    {
        string dir = Path.Combine(Path.GetTempPath(), "aa-adv-img-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            File.WriteAllText(Path.Combine(dir, "a.png"), "x");
            File.WriteAllText(Path.Combine(dir, "b.png"), "x");

            var layer = new TextLayerSettings { LayerType = TextLayerType.AsciiImage };
            layer.SetCustom(new AsciiImageSettings { ImageFolderPath = dir });

            Assert.True(FileBasedLayerAssetPaths.TryAdvanceDirectoryAssetSelection(layer));
            var s = layer.GetCustom<AsciiImageSettings>();
            Assert.NotNull(s);
            Assert.Equal("b.png", s.SelectedImageFileName);

            Assert.True(FileBasedLayerAssetPaths.TryAdvanceDirectoryAssetSelection(layer));
            s = layer.GetCustom<AsciiImageSettings>();
            Assert.Equal("a.png", s!.SelectedImageFileName);
        }
        finally
        {
            TryDeleteTestDir(dir);
        }
    }

    [Fact]
    public void TryAdvanceDirectoryAssetSelection_ascii_model_updates_custom()
    {
        string dir = Path.Combine(Path.GetTempPath(), "aa-adv-obj-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            File.WriteAllText(Path.Combine(dir, "m1.obj"), "x");
            File.WriteAllText(Path.Combine(dir, "m2.obj"), "x");

            var layer = new TextLayerSettings { LayerType = TextLayerType.AsciiModel };
            layer.SetCustom(new AsciiModelSettings { ModelFolderPath = dir });

            Assert.True(FileBasedLayerAssetPaths.TryAdvanceDirectoryAssetSelection(layer));
            var s = layer.GetCustom<AsciiModelSettings>();
            Assert.NotNull(s);
            Assert.Equal("m2.obj", s.SelectedModelFileName);
        }
        finally
        {
            TryDeleteTestDir(dir);
        }
    }

    [Fact]
    public void TryAdvanceDirectoryAssetSelection_wrong_type_returns_false()
    {
        var layer = new TextLayerSettings { LayerType = TextLayerType.Marquee };
        Assert.False(FileBasedLayerAssetPaths.TryAdvanceDirectoryAssetSelection(layer));
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
            // Best-effort cleanup.
        }
    }
}
