using AudioAnalyzer.Domain;
using AudioAnalyzer.Visualizers;
using Xunit;

namespace AudioAnalyzer.Tests.Visualizers.TextLayers;

/// <summary>Tests for <see cref="LayerAssetFolder"/> path resolution.</summary>
public sealed class LayerAssetFolderTests
{
    [Fact]
    public void ResolveGlobalBase_null_ui_settings_uses_app_base_directory()
    {
        string expected = Path.GetFullPath(AppContext.BaseDirectory);
        Assert.Equal(expected, LayerAssetFolder.ResolveGlobalBase(null));
    }

    [Fact]
    public void ResolveGlobalBase_empty_default_uses_app_base_directory()
    {
        var ui = new UiSettings { DefaultAssetFolderPath = "   " };
        string expected = Path.GetFullPath(AppContext.BaseDirectory);
        Assert.Equal(expected, LayerAssetFolder.ResolveGlobalBase(ui));
    }

    [Fact]
    public void ResolveGlobalBase_custom_path_is_full_path()
    {
        string temp = Path.Combine(Path.GetTempPath(), "layer-asset-base-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        try
        {
            var ui = new UiSettings { DefaultAssetFolderPath = temp };
            Assert.Equal(Path.GetFullPath(temp), LayerAssetFolder.ResolveGlobalBase(ui));
        }
        finally
        {
            TryDeleteDir(temp);
        }
    }

    [Fact]
    public void ResolveEffectiveFolder_empty_layer_uses_global_base()
    {
        var ui = new UiSettings();
        string global = LayerAssetFolder.ResolveGlobalBase(ui);
        Assert.Equal(global, LayerAssetFolder.ResolveEffectiveFolder(null, ui));
        Assert.Equal(global, LayerAssetFolder.ResolveEffectiveFolder("", ui));
        Assert.Equal(global, LayerAssetFolder.ResolveEffectiveFolder("  ", ui));
    }

    [Fact]
    public void ResolveEffectiveFolder_relative_layer_combines_with_global_base()
    {
        string temp = Path.Combine(Path.GetTempPath(), "layer-asset-rel-" + Guid.NewGuid().ToString("N"));
        string sub = Path.Combine(temp, "assets");
        Directory.CreateDirectory(sub);
        try
        {
            var ui = new UiSettings { DefaultAssetFolderPath = temp };
            string expected = Path.GetFullPath(Path.Combine(temp, "assets"));
            Assert.Equal(expected, LayerAssetFolder.ResolveEffectiveFolder("assets", ui));
        }
        finally
        {
            TryDeleteDir(temp);
        }
    }

    [Fact]
    public void ResolveEffectiveFolder_rooted_layer_ignores_global_base()
    {
        string temp = Path.Combine(Path.GetTempPath(), "layer-asset-rooted-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        try
        {
            var ui = new UiSettings { DefaultAssetFolderPath = Path.GetTempPath() };
            Assert.Equal(Path.GetFullPath(temp), LayerAssetFolder.ResolveEffectiveFolder(temp, ui));
        }
        finally
        {
            TryDeleteDir(temp);
        }
    }

    private static void TryDeleteDir(string dir)
    {
        try
        {
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, recursive: true);
            }
        }
        catch (IOException)
        {
            // Best-effort cleanup.
        }
    }
}
