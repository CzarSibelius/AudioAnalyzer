using System.IO.Abstractions.TestingHelpers;
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
        const string dir = @"V:\adv-img";
        var fs = new MockFileSystem(new Dictionary<string, MockFileData>(StringComparer.OrdinalIgnoreCase)
        {
            [Path.Combine(dir, "a.png")] = new MockFileData("x"),
            [Path.Combine(dir, "b.png")] = new MockFileData("x")
        });

        var layer = new TextLayerSettings { LayerType = TextLayerType.AsciiImage };
        layer.SetCustom(new AsciiImageSettings { ImageFolderPath = dir });

        Assert.True(FileBasedLayerAssetPaths.TryAdvanceDirectoryAssetSelection(layer, null, fs));
        var s = layer.GetCustom<AsciiImageSettings>();
        Assert.NotNull(s);
        Assert.Equal("b.png", s.SelectedImageFileName);

        Assert.True(FileBasedLayerAssetPaths.TryAdvanceDirectoryAssetSelection(layer, null, fs));
        s = layer.GetCustom<AsciiImageSettings>();
        Assert.Equal("a.png", s!.SelectedImageFileName);
    }

    [Fact]
    public void TryAdvanceDirectoryAssetSelection_ascii_model_updates_custom()
    {
        const string dir = @"V:\adv-obj";
        var fs = new MockFileSystem(new Dictionary<string, MockFileData>(StringComparer.OrdinalIgnoreCase)
        {
            [Path.Combine(dir, "m1.obj")] = new MockFileData("x"),
            [Path.Combine(dir, "m2.obj")] = new MockFileData("x")
        });

        var layer = new TextLayerSettings { LayerType = TextLayerType.AsciiModel };
        layer.SetCustom(new AsciiModelSettings { ModelFolderPath = dir });

        Assert.True(FileBasedLayerAssetPaths.TryAdvanceDirectoryAssetSelection(layer, null, fs));
        var s = layer.GetCustom<AsciiModelSettings>();
        Assert.NotNull(s);
        Assert.Equal("m2.obj", s.SelectedModelFileName);
    }

    [Fact]
    public void GetSortedObjPaths_mock_file_system_enumerates_virtual_folder()
    {
        const string folder = "M:/objs";
        string fullPath = Path.Combine(folder, "z.obj");
        var fs = new MockFileSystem(new Dictionary<string, MockFileData>(StringComparer.OrdinalIgnoreCase)
        {
            [fullPath] = new MockFileData("x")
        });

        var list = FileBasedLayerAssetPaths.GetSortedObjPaths(folder, null, fs);
        Assert.Single(list);
        Assert.EndsWith("z.obj", list[0], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TryAdvanceDirectoryAssetSelection_wrong_type_returns_false()
    {
        var layer = new TextLayerSettings { LayerType = TextLayerType.Marquee };
        var fs = new MockFileSystem();
        Assert.False(FileBasedLayerAssetPaths.TryAdvanceDirectoryAssetSelection(layer, null, fs));
    }
}
