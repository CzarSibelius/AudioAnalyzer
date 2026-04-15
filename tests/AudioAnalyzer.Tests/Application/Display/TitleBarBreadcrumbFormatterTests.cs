using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Application.Display;
using AudioAnalyzer.Domain;
using Xunit;

namespace AudioAnalyzer.Tests.Application.Display;

/// <summary>Tests for <see cref="TitleBarBreadcrumbFormatter"/> (ADR-0060).</summary>
public sealed class TitleBarBreadcrumbFormatterTests
{
    private sealed class TestNav : ITitleBarNavigationContext
    {
        public TitleBarViewKind View { get; set; }
        public bool PresetSettingsPalettePickerActive { get; set; }
        public bool PresetSettingsCharsetPickerActive { get; set; }
        public int? PresetSettingsLayerOneBased { get; set; }
        public string? PresetSettingsLayerTypeRaw { get; set; }
        public string? PresetSettingsFocusedSettingId { get; set; }
    }

    private sealed class StubVisualizer : IVisualizer
    {
        public string? LayerName { get; init; }
        public int Z { get; init; } = -1;

        public bool SupportsPaletteCycling => false;

        public void Render(VisualizationFrameContext frame, VisualizerViewport viewport) { }

        public string? GetActiveLayerDisplayName() => LayerName;

        public int GetActiveLayerZIndex() => Z;
    }

    private static VisualizerSettings CreateVisualizerSettings(ApplicationMode mode = ApplicationMode.PresetEditor)
    {
        return new VisualizerSettings
        {
            ApplicationMode = mode,
            ActivePresetId = "p1",
            Presets =
            [
                new Preset { Id = "p1", Name = "My Preset" }
            ]
        };
    }

    private static UiSettings CreateUiSettings() => new() { TitleBarAppName = "aUdioNLZR" };

    private sealed class TestUiThemeResolver : IUiThemeResolver
    {
        private readonly UiSettings _ui;

        public TestUiThemeResolver(UiSettings ui) => _ui = ui;

        public UiPalette GetEffectiveUiPalette() => _ui.Palette ?? new UiPalette();

        public TitleBarPalette GetEffectiveTitleBarPalette() => _ui.TitleBarPalette ?? new TitleBarPalette();
    }

    private static TitleBarBreadcrumbFormatter CreateFormatter(VisualizerSettings vs, IVisualizer viz, ITitleBarNavigationContext nav)
    {
        var ui = CreateUiSettings();
        return new TitleBarBreadcrumbFormatter(ui, new TestUiThemeResolver(ui), vs, viz, nav);
    }

    [Fact]
    public void MainView_IncludesLayerSegment_WhenZIndexKnown()
    {
        var nav = new TestNav { View = TitleBarViewKind.Main };
        var vs = CreateVisualizerSettings();
        var f = CreateFormatter(
            vs,
            new StubVisualizer { LayerName = "ascii_image", Z = 0 },
            nav);

        string line = f.BuildAnsiLine();
        Assert.Contains("aUdioNLZR", line);
        Assert.Contains("pReset", line);
        Assert.Contains("mY_Preset", line);
        Assert.Contains("[1]:", line);
        Assert.Contains("aScii_image", line);
    }

    [Fact]
    public void PresetSettingsModal_OmitsSettingsSegment_EndsAtPresetWhenNoLayer()
    {
        var nav = new TestNav { View = TitleBarViewKind.PresetSettingsModal, PresetSettingsPalettePickerActive = false };
        var f = CreateFormatter(
            CreateVisualizerSettings(),
            new StubVisualizer(),
            nav);

        string line = StripAnsi(f.BuildAnsiLine());
        Assert.Contains("aUdioNLZR", line);
        Assert.Contains("mY_Preset", line);
        Assert.DoesNotContain("sEttings", line);
    }

    [Fact]
    public void PresetSettingsModal_WithFocusedLayer_AppendsLayerSegment()
    {
        var nav = new TestNav
        {
            View = TitleBarViewKind.PresetSettingsModal,
            PresetSettingsPalettePickerActive = false,
            PresetSettingsLayerOneBased = 2,
            PresetSettingsLayerTypeRaw = "Fill"
        };
        var f = CreateFormatter(
            CreateVisualizerSettings(),
            new StubVisualizer(),
            nav);

        string line = StripAnsi(f.BuildAnsiLine());
        Assert.Contains("[2]:", line);
        Assert.Contains("fIll", line);
    }

    [Fact]
    public void PresetSettingsModal_WithFocusedSetting_AppendsHackerizedSettingId()
    {
        var nav = new TestNav
        {
            View = TitleBarViewKind.PresetSettingsModal,
            PresetSettingsPalettePickerActive = false,
            PresetSettingsLayerOneBased = 2,
            PresetSettingsLayerTypeRaw = "Fill",
            PresetSettingsFocusedSettingId = "Speed"
        };
        var f = CreateFormatter(
            CreateVisualizerSettings(),
            new StubVisualizer(),
            nav);

        string line = StripAnsi(f.BuildAnsiLine());
        Assert.Contains("[2]:", line);
        Assert.Contains("fIll", line);
        Assert.Contains("sPeed", line);
        Assert.DoesNotContain("sEttings", line);
    }

    [Fact]
    public void PresetSettingsModal_PresetRow_FocusedSetting_AppendsWithoutLayerSegment()
    {
        var nav = new TestNav
        {
            View = TitleBarViewKind.PresetSettingsModal,
            PresetSettingsPalettePickerActive = false,
            PresetSettingsLayerOneBased = null,
            PresetSettingsLayerTypeRaw = null,
            PresetSettingsFocusedSettingId = "DefaultPalette"
        };
        var f = CreateFormatter(
            CreateVisualizerSettings(),
            new StubVisualizer(),
            nav);

        string line = StripAnsi(f.BuildAnsiLine());
        Assert.Contains("mY_Preset", line);
        Assert.Contains("dEfaultPalette", line);
        Assert.DoesNotContain("[1]:", line);
    }

    [Fact]
    public void PresetSettingsModal_PresetRow_PalettePicker_AppendsEditorAfterSettingSegment()
    {
        var nav = new TestNav
        {
            View = TitleBarViewKind.PresetSettingsModal,
            PresetSettingsPalettePickerActive = true,
            PresetSettingsLayerOneBased = null,
            PresetSettingsLayerTypeRaw = null,
            PresetSettingsFocusedSettingId = "DefaultPalette"
        };
        var f = CreateFormatter(
            CreateVisualizerSettings(),
            new StubVisualizer(),
            nav);

        string line = StripAnsi(f.BuildAnsiLine());
        Assert.Contains("dEfaultPalette", line);
        Assert.Contains("eDitor", line);
    }

    [Fact]
    public void PresetSettingsModal_WithPalettePicker_AppendsEditorAfterSettingSegment()
    {
        var nav = new TestNav
        {
            View = TitleBarViewKind.PresetSettingsModal,
            PresetSettingsPalettePickerActive = true,
            PresetSettingsLayerOneBased = 1,
            PresetSettingsLayerTypeRaw = "Oscilloscope",
            PresetSettingsFocusedSettingId = "Palette"
        };
        var f = CreateFormatter(
            CreateVisualizerSettings(),
            new StubVisualizer(),
            nav);

        string line = StripAnsi(f.BuildAnsiLine());
        Assert.Contains("pAlette", line);
        Assert.Contains("eDitor", line);
        Assert.DoesNotContain("sEttings", line);
    }

    [Fact]
    public void DeviceAudioInput_UsesAppSettingsTrack()
    {
        var nav = new TestNav { View = TitleBarViewKind.DeviceAudioInputModal };
        var f = CreateFormatter(
            CreateVisualizerSettings(),
            new StubVisualizer(),
            nav);

        string line = StripAnsi(f.BuildAnsiLine());
        Assert.Contains("aUdioNLZR", line);
        Assert.Contains("sEttings", line);
        Assert.Contains("aUdioinput", line);
    }

    [Fact]
    public void MainView_SettingsMode_ShowsHubHome()
    {
        var nav = new TestNav { View = TitleBarViewKind.Main };
        var f = CreateFormatter(
            CreateVisualizerSettings(ApplicationMode.Settings),
            new StubVisualizer(),
            nav);

        string line = StripAnsi(f.BuildAnsiLine());
        Assert.Contains("aUdioNLZR", line);
        Assert.Contains("sEttings", line);
        Assert.DoesNotContain("pReset", line);
    }

    private static string StripAnsi(string s)
    {
        var sb = new System.Text.StringBuilder(s.Length);
        for (int i = 0; i < s.Length; i++)
        {
            if (s[i] == '\x1b' && i + 1 < s.Length && s[i + 1] == '[')
            {
                int j = i + 2;
                while (j < s.Length && s[j] != 'm')
                {
                    j++;
                }
                i = j < s.Length ? j : i;
                continue;
            }
            sb.Append(s[i]);
        }
        return sb.ToString();
    }
}
