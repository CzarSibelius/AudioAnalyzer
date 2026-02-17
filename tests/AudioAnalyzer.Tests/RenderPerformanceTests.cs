using System.Diagnostics;
using AudioAnalyzer.Application;
using Xunit;
using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Console;
using AudioAnalyzer.Domain;
using AudioAnalyzer.Infrastructure;
using AudioAnalyzer.Infrastructure.NowPlaying;
using Microsoft.Extensions.DependencyInjection;

namespace AudioAnalyzer.Tests;

public sealed class RenderPerformanceTests
{
    private const int RenderThresholdMs = 10;
    private const int WarmUpRenderCount = 2;

    [Fact]
    public void RenderSingleFrameCompletesWithinThreshold()
    {
        using var temp = new TempDirectory();
        EnsureTestFiles(temp);

        var presetRepo = new FilePresetRepository(temp.PresetsPath);
        var paletteRepo = new FilePaletteRepository(temp.PalettesPath);
        var settingsRepo = new FileSettingsRepository(presetRepo, Path.Combine(temp.RootPath, "appsettings.json"));
        var settings = settingsRepo.LoadAppSettings();
        var vs = settingsRepo.LoadVisualizerSettings();

        var options = new ServiceConfigurationOptions
        {
            DisplayDimensions = new FixedDisplayDimensions(80, 24),
            NowPlayingProvider = new NullNowPlayingProvider(),
            PaletteRepository = paletteRepo
        };

        using var provider = ServiceConfiguration.Build(settingsRepo, presetRepo, settings, vs, options);
        var renderer = provider.GetRequiredService<IVisualizationRenderer>();
        var snapshot = CreateTestSnapshot(80, 24);

        var originalOut = System.Console.Out;
        try
        {
            System.Console.SetOut(new StringWriter());
            for (int i = 0; i < WarmUpRenderCount; i++)
            {
                renderer.Render(snapshot);
            }

            var sw = Stopwatch.StartNew();
            renderer.Render(snapshot);
            sw.Stop();

            Assert.True(
                sw.ElapsedMilliseconds < RenderThresholdMs,
                $"Single render took {sw.ElapsedMilliseconds}ms; threshold is {RenderThresholdMs}ms (20 FPS = 50ms per frame).");
        }
        finally
        {
            System.Console.SetOut(originalOut);
        }
    }

    private static void EnsureTestFiles(TempDirectory temp)
    {
        Directory.CreateDirectory(temp.PresetsPath);
        Directory.CreateDirectory(temp.PalettesPath);

        File.WriteAllText(
            Path.Combine(temp.PalettesPath, "default.json"),
            """{"Name":"Default","Colors":["Magenta","Yellow","Green","Cyan","Blue"]}""");

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
        File.WriteAllText(Path.Combine(temp.PresetsPath, "preset-1.json"), presetJson);
    }

    private static AnalysisSnapshot CreateTestSnapshot(int width, int height)
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
}
