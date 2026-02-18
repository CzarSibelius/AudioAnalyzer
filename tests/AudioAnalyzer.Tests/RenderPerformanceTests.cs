using System.Diagnostics;
using AudioAnalyzer.Application;
using AudioAnalyzer.Application.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AudioAnalyzer.Tests;

public sealed class RenderPerformanceTests
{
    private const int RenderThresholdMs = 10;
    private const int WarmUpRenderCount = 2;

    [Fact]
    public void RenderSingleFrameCompletesWithinThreshold()
    {
        var fileSystem = TestHelpers.CreateMockFileSystem();
        using var provider = TestHelpers.BuildTestServiceProvider(fileSystem);
        var renderer = provider.GetRequiredService<IVisualizationRenderer>();
        var snapshot = TestHelpers.CreateTestSnapshot(80, 24);

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

    [Fact]
    public void RenderMultipleFramesCompletesWithoutException()
    {
        var fileSystem = TestHelpers.CreateMockFileSystem();
        using var provider = TestHelpers.BuildTestServiceProvider(fileSystem);
        var renderer = provider.GetRequiredService<IVisualizationRenderer>();
        var snapshot = TestHelpers.CreateTestSnapshot(80, 24);

        var originalOut = System.Console.Out;
        try
        {
            System.Console.SetOut(new StringWriter());
            for (int i = 0; i < 5; i++)
            {
                renderer.Render(snapshot);
            }
        }
        finally
        {
            System.Console.SetOut(originalOut);
        }
    }
}
