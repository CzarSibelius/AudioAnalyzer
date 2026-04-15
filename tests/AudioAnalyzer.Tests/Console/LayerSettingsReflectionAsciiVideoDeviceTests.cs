using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Application.Palette;
using AudioAnalyzer.Domain;
using AudioAnalyzer.Visualizers;
using Xunit;

namespace AudioAnalyzer.Tests.ConsoleSettingsReflection;

/// <summary>Webcam device row shows platform display names when <see cref="IAsciiVideoDeviceCatalog"/> returns entries.</summary>
public sealed class LayerSettingsReflectionAsciiVideoDeviceTests
{
    private sealed class StubPaletteRepo : IPaletteRepository
    {
        public IReadOnlyList<PaletteInfo> GetAll() => [];

        public PaletteDefinition? GetById(string id) => null;
    }

    private sealed class StubCharsetRepo : ICharsetRepository
    {
        public IReadOnlyList<CharsetInfo> GetAll() => [];

        public CharsetDefinition? GetById(string id) => null;

        public void Save(string id, CharsetDefinition definition)
        {
        }

        public string Create(CharsetDefinition definition) => "charset-1";
    }

    private sealed class StubDeviceCatalog : IAsciiVideoDeviceCatalog
    {
        private readonly IReadOnlyList<AsciiVideoDeviceEntry> _entries;

        public StubDeviceCatalog(IReadOnlyList<AsciiVideoDeviceEntry> entries) => _entries = entries;

        public IReadOnlyList<AsciiVideoDeviceEntry> GetDevices() => _entries;
    }

    [Fact]
    public void WebcamDeviceIndex_DisplayValue_IncludesDisplayName_WhenCatalogHasDevices()
    {
        var layer = new TextLayerSettings
        {
            LayerType = TextLayerType.AsciiVideo,
            Enabled = true,
            ZOrder = 0
        };
        layer.SetCustom(new AsciiVideoSettings { WebcamDeviceIndex = 0 });

        var catalog = new StubDeviceCatalog(
        [
            new AsciiVideoDeviceEntry(0, "Integrated Camera"),
            new AsciiVideoDeviceEntry(1, "USB HD Webcam")
        ]);

        IReadOnlyList<AudioAnalyzer.Console.SettingDescriptor> descriptors = AudioAnalyzer.Console.SettingDescriptor.BuildAll(layer, new StubPaletteRepo(), catalog, new StubCharsetRepo());
        AudioAnalyzer.Console.SettingDescriptor? row = descriptors.FirstOrDefault(d => d.Id == "WebcamDeviceIndex");
        Assert.NotNull(row);
        string display = row!.GetDisplayValue(layer);
        Assert.Contains("0", display, StringComparison.Ordinal);
        Assert.Contains("Integrated Camera", display, StringComparison.Ordinal);
        Assert.Contains('·', display);
    }

    [Fact]
    public void WebcamDeviceIndex_DisplayValue_FallsBackToIndex_WhenCatalogEmpty()
    {
        var layer = new TextLayerSettings
        {
            LayerType = TextLayerType.AsciiVideo,
            Enabled = true,
            ZOrder = 0
        };
        layer.SetCustom(new AsciiVideoSettings { WebcamDeviceIndex = 3 });

        var catalog = new StubDeviceCatalog([]);
        IReadOnlyList<AudioAnalyzer.Console.SettingDescriptor> descriptors = AudioAnalyzer.Console.SettingDescriptor.BuildAll(layer, new StubPaletteRepo(), catalog, new StubCharsetRepo());
        AudioAnalyzer.Console.SettingDescriptor row = descriptors.First(d => d.Id == "WebcamDeviceIndex");
        Assert.Equal("3", row.GetDisplayValue(layer));
    }
}
