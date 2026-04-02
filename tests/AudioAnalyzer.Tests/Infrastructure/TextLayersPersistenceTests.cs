using System.IO.Abstractions.TestingHelpers;
using AudioAnalyzer.Domain;
using AudioAnalyzer.Infrastructure;
using AudioAnalyzer.Tests.TestSupport;
using AudioAnalyzer.Visualizers;
using Xunit;

namespace AudioAnalyzer.Tests.Infrastructure;

/// <summary>ADR-0070: presets keep actual layer count; load caps at <see cref="TextLayersLimits.MaxLayerCount"/> only.</summary>
public sealed class TextLayersPersistenceTests
{
    [Fact]
    public void LoadVisualizerSettings_DoesNotPadEmptyPresetToMaxLayerCount()
    {
        var presetJson = """
            {
              "Name": "Empty",
              "Config": { "Layers": [] }
            }
            """;
        var appSettingsJson = """
            {
              "VisualizerSettings": {
                "ActivePresetId": "preset-1"
              }
            }
            """;
        var fs = TestHelpers.CreateMockFileSystemWithPreset(presetJson, new Dictionary<string, MockFileData>(StringComparer.OrdinalIgnoreCase)
        {
            [TestHelpers.SettingsPath] = new MockFileData(appSettingsJson)
        });
        var presetRepo = new FilePresetRepository(fs, TestHelpers.PresetsPath);
        var repo = new FileSettingsRepository(fs, presetRepo, new DefaultTextLayersSettingsFactory(), TestHelpers.SettingsPath);

        VisualizerSettings vs = repo.LoadVisualizerSettings();

        Assert.NotNull(vs.TextLayers?.Layers);
        Assert.Empty(vs.TextLayers.Layers);
    }

    [Fact]
    public void LoadVisualizerSettings_CapsLayersAboveMax()
    {
        var layerObjects = Enumerable.Range(0, 12).Select(i =>
            $"{{ \"LayerType\": \"StaticText\", \"Enabled\": true, \"ZOrder\": {i}, \"TextSnippets\": [\"x\"], \"Custom\": {{ \"BeatReaction\": \"None\" }} }}");
        var layersJson = string.Join(",", layerObjects);
        var presetJson = "{\n  \"Name\": \"Many\",\n  \"Config\": { \"Layers\": [" + layersJson + "] }\n}";
        var appSettingsJson = """
            {
              "VisualizerSettings": {
                "ActivePresetId": "preset-1"
              }
            }
            """;
        var fs = TestHelpers.CreateMockFileSystemWithPreset(presetJson, new Dictionary<string, MockFileData>(StringComparer.OrdinalIgnoreCase)
        {
            [TestHelpers.SettingsPath] = new MockFileData(appSettingsJson)
        });
        var presetRepo = new FilePresetRepository(fs, TestHelpers.PresetsPath);
        var repo = new FileSettingsRepository(fs, presetRepo, new DefaultTextLayersSettingsFactory(), TestHelpers.SettingsPath);

        VisualizerSettings vs = repo.LoadVisualizerSettings();

        Assert.Equal(TextLayersLimits.MaxLayerCount, vs.TextLayers?.Layers?.Count ?? 0);
    }
}
