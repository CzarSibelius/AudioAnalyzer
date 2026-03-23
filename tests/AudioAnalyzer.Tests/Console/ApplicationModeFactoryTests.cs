using AudioAnalyzer.Console;
using AudioAnalyzer.Domain;
using Xunit;

namespace AudioAnalyzer.Tests.Console;

public sealed class ApplicationModeFactoryTests
{
    private static ApplicationModeFactory CreateFactory(VisualizerSettings vs) =>
        new(
            vs,
            new PresetEditorApplicationMode(),
            new ShowPlayApplicationMode(),
            new SettingsApplicationMode());

    [Fact]
    public void GetActiveApplicationMode_PresetEditor_ReturnsPresetMode()
    {
        var vs = new VisualizerSettings { ApplicationMode = ApplicationMode.PresetEditor };
        var mode = CreateFactory(vs).GetActiveApplicationMode();
        Assert.IsType<PresetEditorApplicationMode>(mode);
        Assert.Equal(3, mode.HeaderLineCount);
        Assert.False(mode.UsesGeneralSettingsHubKeyHandling);
    }

    [Fact]
    public void GetActiveApplicationMode_ShowPlay_ReturnsShowMode()
    {
        var vs = new VisualizerSettings { ApplicationMode = ApplicationMode.ShowPlay };
        var mode = CreateFactory(vs).GetActiveApplicationMode();
        Assert.IsType<ShowPlayApplicationMode>(mode);
        Assert.Equal(3, mode.HeaderLineCount);
    }

    [Fact]
    public void GetActiveApplicationMode_Settings_ReturnsSettingsMode()
    {
        var vs = new VisualizerSettings { ApplicationMode = ApplicationMode.Settings };
        var mode = CreateFactory(vs).GetActiveApplicationMode();
        Assert.IsType<SettingsApplicationMode>(mode);
        Assert.Equal(1, mode.HeaderLineCount);
        Assert.True(mode.UsesGeneralSettingsHubKeyHandling);
    }

    [Fact]
    public void ApplicationModeHeaderProvider_DelegatesToFactory()
    {
        var vs = new VisualizerSettings { ApplicationMode = ApplicationMode.PresetEditor };
        var factory = CreateFactory(vs);
        var provider = new ApplicationModeHeaderProvider(factory);
        Assert.Equal(3, provider.HeaderLineCount);
        vs.ApplicationMode = ApplicationMode.Settings;
        Assert.Equal(1, provider.HeaderLineCount);
    }
}
