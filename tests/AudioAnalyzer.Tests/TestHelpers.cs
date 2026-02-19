using System.IO.Abstractions.TestingHelpers;
using AudioAnalyzer.Application;
using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Console;
using AudioAnalyzer.Domain;
using AudioAnalyzer.Infrastructure;
using AudioAnalyzer.Infrastructure.NowPlaying;
using Microsoft.Extensions.DependencyInjection;

namespace AudioAnalyzer.Tests;

/// <summary>Shared test setup for render and preset tests. Uses MockFileSystem for deterministic, isolated tests.</summary>
internal static class TestHelpers
{
    private const string TestRoot = "C:/AudioAnalyzerTest";

    public static string PresetsPath => Path.Combine(TestRoot, "presets");
    public static string PalettesPath => Path.Combine(TestRoot, "palettes");
    public static string ShowsPath => Path.Combine(TestRoot, "shows");
    public static string SettingsPath => Path.Combine(TestRoot, "appsettings.json");

    public static AnalysisSnapshot CreateTestSnapshot(int width, int height)
    {
        int numBands = Math.Min(40, Math.Max(8, width / 2));
        var magnitudes = new double[numBands];
        for (int i = 0; i < numBands; i++)
        {
            magnitudes[i] = 0.3 + 0.4 * Math.Sin(i * 0.2);
        }

        return new AnalysisSnapshot
        {
            DisplayStartRow = 8,
            TerminalWidth = width,
            TerminalHeight = height,
            FullScreenMode = false,
            Volume = 0.5f,
            CurrentBpm = 120,
            BeatSensitivity = 1.3,
            BeatFlashActive = false,
            BeatCount = 0,
            NumBands = numBands,
            SmoothedMagnitudes = magnitudes,
            PeakHold = magnitudes,
            TargetMaxMagnitude = 1.0,
            Waveform = new float[512],
            WaveformPosition = 0,
            WaveformSize = 512,
            LeftChannel = 0.5f,
            RightChannel = 0.5f,
            LeftPeakHold = 0.5f,
            RightPeakHold = 0.5f
        };
    }

    public static MockFileSystem CreateMockFileSystem()
    {
        var paletteJson = """{"Name":"Default","Colors":["Magenta","Yellow","Green","Cyan","Blue"]}""";
        var presetJson = """
            {
              "Name": "Test Preset",
              "Config": {
                "Layers": [
                  { "LayerType": "GeissBackground", "Enabled": true, "ZOrder": 0, "BeatReaction": "Flash", "SpeedMultiplier": 1.0 },
                  { "LayerType": "StaticText", "Enabled": true, "ZOrder": 1, "TextSnippets": ["Test"], "BeatReaction": "None" }
                ]
              }
            }
            """;

        return new MockFileSystem(new Dictionary<string, MockFileData>
        {
            [Path.Combine(PalettesPath, "default.json")] = new MockFileData(paletteJson),
            [Path.Combine(PresetsPath, "preset-1.json")] = new MockFileData(presetJson)
        });
    }

    public static MockFileSystem CreateMockFileSystemWithPreset(string presetJson)
    {
        var paletteJson = """{"Name":"Default","Colors":["Magenta","Yellow","Green","Cyan","Blue"]}""";

        return new MockFileSystem(new Dictionary<string, MockFileData>
        {
            [Path.Combine(PalettesPath, "default.json")] = new MockFileData(paletteJson),
            [Path.Combine(PresetsPath, "preset-1.json")] = new MockFileData(presetJson)
        });
    }

    public static ServiceProvider BuildTestServiceProvider(MockFileSystem fileSystem)
    {
        var presetRepo = new FilePresetRepository(fileSystem, PresetsPath);
        var paletteRepo = new FilePaletteRepository(fileSystem, PalettesPath);
        var settingsRepo = new FileSettingsRepository(fileSystem, presetRepo, SettingsPath);
        AppSettings settings = settingsRepo.LoadAppSettings();
        VisualizerSettings vs = settingsRepo.LoadVisualizerSettings();

        var options = new ServiceConfigurationOptions
        {
            DisplayDimensions = new FixedDisplayDimensions(80, 24),
            NowPlayingProvider = new NullNowPlayingProvider(),
            PaletteRepository = paletteRepo,
            FileSystem = fileSystem,
            ShowsDirectory = ShowsPath
        };

        return ServiceConfiguration.Build(settingsRepo, presetRepo, settings, vs, options);
    }
}
