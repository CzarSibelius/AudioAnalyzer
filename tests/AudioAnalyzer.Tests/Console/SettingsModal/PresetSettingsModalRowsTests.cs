using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Console;
using AudioAnalyzer.Domain;
using Xunit;

namespace AudioAnalyzer.Tests.Console.SettingsModal;

/// <summary>Tests for <see cref="PresetSettingsModalRows"/>.</summary>
public sealed class PresetSettingsModalRowsTests
{
    private sealed class EmptyPaletteRepository : IPaletteRepository
    {
        public IReadOnlyList<PaletteInfo> GetAll() => [];

        public PaletteDefinition? GetById(string id) => null;
    }

    [Fact]
    public void Build_ReturnsNameAndDefaultPaletteRows()
    {
        var vs = new VisualizerSettings
        {
            ActivePresetId = "a1",
            Presets = [new Preset { Id = "a1", Name = "Test Preset" }],
            TextLayers = new TextLayersVisualizerSettings()
        };
        var textLayers = vs.TextLayers ?? new TextLayersVisualizerSettings();
        var rows = PresetSettingsModalRows.Build(vs, textLayers, new EmptyPaletteRepository());

        Assert.Equal(2, rows.Count);
        Assert.Equal(PresetSettingsModalRows.PresetNameId, rows[0].Id);
        Assert.Equal(SettingEditMode.TextEdit, rows[0].EditMode);
        Assert.Equal("Test Preset", rows[0].DisplayValue);
        Assert.Equal(PresetSettingsModalRows.DefaultPaletteId, rows[1].Id);
        Assert.Equal(SettingEditMode.PalettePicker, rows[1].EditMode);
    }
}
