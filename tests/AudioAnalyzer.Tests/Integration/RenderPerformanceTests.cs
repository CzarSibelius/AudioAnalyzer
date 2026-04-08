using System.Diagnostics;
using System.IO.Abstractions.TestingHelpers;
using System.Text.Json;
using AudioAnalyzer.Application;
using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Tests.TestSupport;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AudioAnalyzer.Tests.Integration;

public sealed class RenderPerformanceTests
{
    /// <summary>Budget for one full frame consistent with the at-least ~60 FPS bar (ADR-0067).</summary>
    private const int RenderThresholdMs = 17;

    /// <summary>
    /// AsciiModel shape-mode rasterization over the full visualizer viewport is heavier than the default preset;
    /// same order of magnitude as <see cref="RenderThresholdMs"/> but allows modest CI variance.
    /// </summary>
    private const int AsciiModelRenderThresholdMs = 25;

    private const int WarmUpRenderCount = 2;

    private const string MinimalTriangleObj = """
        v -1 -1 0
        v 1 -1 0
        v 0 1 0
        f 1 2 3
        """;

    [Fact]
    public void RenderSingleFrameCompletesWithinThreshold()
    {
        var fileSystem = TestHelpers.CreateMockFileSystem();
        using var provider = TestHelpers.BuildTestServiceProvider(fileSystem);
        var renderer = provider.GetRequiredService<IVisualizationRenderer>();
        var frame = TestHelpers.CreateTestFrame(80, 24);

        var originalOut = System.Console.Out;
        try
        {
            System.Console.SetOut(new StringWriter());
            for (int i = 0; i < WarmUpRenderCount; i++)
            {
                renderer.Render(frame);
            }

            var sw = Stopwatch.StartNew();
            renderer.Render(frame);
            sw.Stop();

            Assert.True(
                sw.ElapsedMilliseconds < RenderThresholdMs,
                $"Single render took {sw.ElapsedMilliseconds}ms; threshold is {RenderThresholdMs}ms (at-least ~60 FPS bar ≈ 16.7ms per frame).");
        }
        finally
        {
            System.Console.SetOut(originalOut);
        }
    }

    /// <summary>
    /// End-to-end main render with AsciiModel loading an OBJ via the same <see cref="System.IO.Abstractions.IFileSystem"/> as presets (mock in tests).
    /// Uses a tiny mesh so time is dominated by per-cell rasterization, not I/O.
    /// </summary>
    [Fact]
    public void AsciiModelSingleFrameCompletesWithinThreshold()
    {
        string modelDir = Path.Combine(TestHelpers.MockFileSystemContentRoot, "models", "sample");
        string modelPath = Path.Combine(modelDir, "triangle.obj");

        var presetDto = new
        {
            Name = "AsciiModel Perf",
            Config = new
            {
                Layers = new[]
                {
                    new
                    {
                        LayerType = "AsciiModel",
                        Enabled = true,
                        ZOrder = 0,
                        TextSnippets = Array.Empty<string>(),
                        SpeedMultiplier = 1.0,
                        Custom = new
                        {
                            ModelFolderPath = modelDir,
                            EnableZoom = false,
                            SelectedModelFileName = "triangle.obj"
                        }
                    }
                }
            }
        };
        string presetJson = JsonSerializer.Serialize(presetDto);

        var extra = new Dictionary<string, MockFileData>(StringComparer.OrdinalIgnoreCase)
        {
            [modelPath] = new MockFileData(MinimalTriangleObj)
        };
        var fileSystem = TestHelpers.CreateMockFileSystemWithPreset(presetJson, extra);

        using var provider = TestHelpers.BuildTestServiceProvider(fileSystem);
        var renderer = provider.GetRequiredService<IVisualizationRenderer>();
        var frame = TestHelpers.CreateTestFrame(80, 24);

        var originalOut = System.Console.Out;
        try
        {
            System.Console.SetOut(new StringWriter());
            for (int i = 0; i < WarmUpRenderCount; i++)
            {
                renderer.Render(frame);
            }

            var sw = Stopwatch.StartNew();
            renderer.Render(frame);
            sw.Stop();

            Assert.True(
                sw.ElapsedMilliseconds < AsciiModelRenderThresholdMs,
                $"AsciiModel single render took {sw.ElapsedMilliseconds}ms; threshold is {AsciiModelRenderThresholdMs}ms.");
        }
        finally
        {
            System.Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void RenderMultipleFramesCompletesWithoutException()
    {
        var fileSystem = TestHelpers.CreateMockFileSystem();
        using var provider = TestHelpers.BuildTestServiceProvider(fileSystem);
        var renderer = provider.GetRequiredService<IVisualizationRenderer>();
        var frame = TestHelpers.CreateTestFrame(80, 24);

        var originalOut = System.Console.Out;
        try
        {
            System.Console.SetOut(new StringWriter());
            for (int i = 0; i < 5; i++)
            {
                renderer.Render(frame);
            }
        }
        finally
        {
            System.Console.SetOut(originalOut);
        }
    }
}
