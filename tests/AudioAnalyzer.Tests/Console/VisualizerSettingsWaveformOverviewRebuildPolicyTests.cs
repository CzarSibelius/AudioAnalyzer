using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Console;
using AudioAnalyzer.Domain;
using AudioAnalyzer.Visualizers;
using Xunit;

namespace AudioAnalyzer.Tests.Console;

public sealed class VisualizerSettingsWaveformOverviewRebuildPolicyTests
{
    [Fact]
    public void No_strip_layers_returns_skip()
    {
        var vz = new VisualizerSettings
        {
            TextLayers = new TextLayersVisualizerSettings
            {
                Layers =
                [
                    new TextLayerSettings { LayerType = TextLayerType.Marquee, Enabled = true }
                ]
            }
        };
        var policy = new VisualizerSettingsWaveformOverviewRebuildPolicy(vz);
        WaveformOverviewRebuildDecision d = policy.GetDecision(100_000, 48_000);
        Assert.Equal(WaveformOverviewRebuildMode.Skip, d.Mode);
    }

    [Fact]
    public void Strip_default_settings_returns_trailing_window_clamped_to_valid_count()
    {
        var strip = new TextLayerSettings { LayerType = TextLayerType.WaveformStrip, Enabled = true };
        var vz = new VisualizerSettings
        {
            TextLayers = new TextLayersVisualizerSettings { Layers = [strip] }
        };
        var policy = new VisualizerSettingsWaveformOverviewRebuildPolicy(vz);
        WaveformOverviewRebuildDecision d = policy.GetDecision(50_000, 48_000);
        Assert.Equal(WaveformOverviewRebuildMode.TrailingWindow, d.Mode);
        Assert.Equal(50_000, d.TrailingMonoSamples);
    }

    [Fact]
    public void Fixed_visible_strip_returns_trailing_window_clamped_to_valid_count()
    {
        var strip = new TextLayerSettings { LayerType = TextLayerType.WaveformStrip, Enabled = true };
        strip.SetCustom(new WaveformStripSettings { FixedVisibleSeconds = 2.0 });
        var vz = new VisualizerSettings
        {
            TextLayers = new TextLayersVisualizerSettings { Layers = [strip] }
        };
        var policy = new VisualizerSettingsWaveformOverviewRebuildPolicy(vz);
        WaveformOverviewRebuildDecision d = policy.GetDecision(10_000, 8_000);
        Assert.Equal(WaveformOverviewRebuildMode.TrailingWindow, d.Mode);
        Assert.Equal(10_000, d.TrailingMonoSamples);
    }

    [Fact]
    public void Multiple_fixed_strips_use_largest_requested_window()
    {
        var a = new TextLayerSettings { LayerType = TextLayerType.WaveformStrip, Enabled = true };
        a.SetCustom(new WaveformStripSettings { FixedVisibleSeconds = 1.0 });
        var b = new TextLayerSettings { LayerType = TextLayerType.WaveformStrip, Enabled = true };
        b.SetCustom(new WaveformStripSettings { FixedVisibleSeconds = 5.0 });
        var vz = new VisualizerSettings
        {
            TextLayers = new TextLayersVisualizerSettings { Layers = [a, b] }
        };
        var policy = new VisualizerSettingsWaveformOverviewRebuildPolicy(vz);
        WaveformOverviewRebuildDecision d = policy.GetDecision(500_000, 48_000);
        Assert.Equal(WaveformOverviewRebuildMode.TrailingWindow, d.Mode);
        Assert.Equal(240_000, d.TrailingMonoSamples);
    }
}
