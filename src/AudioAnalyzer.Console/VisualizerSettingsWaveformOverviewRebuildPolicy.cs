using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;
using AudioAnalyzer.Visualizers;

namespace AudioAnalyzer.Console;

/// <summary>
/// Skips overview aggregation when no WaveformStrip layer is enabled; otherwise partitions a trailing mono window whose
/// sample count is the largest <see cref="WaveformStripSettings.FixedVisibleSeconds"/> among enabled strips (clamped to valid mono count).
/// </summary>
public sealed class VisualizerSettingsWaveformOverviewRebuildPolicy : IWaveformOverviewRebuildPolicy
{
    private readonly VisualizerSettings _visualizerSettings;

    /// <summary>Constructs the policy.</summary>
    public VisualizerSettingsWaveformOverviewRebuildPolicy(VisualizerSettings visualizerSettings)
    {
        _visualizerSettings = visualizerSettings ?? throw new ArgumentNullException(nameof(visualizerSettings));
    }

    /// <inheritdoc />
    public WaveformOverviewRebuildDecision GetDecision(int validMonoSampleCount, int sampleRateHz)
    {
        if (validMonoSampleCount < 2 || sampleRateHz <= 0)
        {
            return WaveformOverviewRebuildDecision.Skip();
        }

        var textLayers = _visualizerSettings.TextLayers?.Layers;
        if (textLayers is not { Count: > 0 })
        {
            return WaveformOverviewRebuildDecision.Skip();
        }

        bool anyStrip = false;
        int maxWindowSamples = 0;
        foreach (TextLayerSettings layer in textLayers)
        {
            if (!layer.Enabled || layer.LayerType != TextLayerType.WaveformStrip)
            {
                continue;
            }

            anyStrip = true;
            WaveformStripSettings? strip = layer.GetCustom<WaveformStripSettings>();
            double seconds = Math.Clamp(strip?.FixedVisibleSeconds ?? 15.0, 1.0, 120.0);
            long win = (long)Math.Round(seconds * sampleRateHz);
            int windowSamples = (int)Math.Clamp(win, 2, validMonoSampleCount);
            maxWindowSamples = Math.Max(maxWindowSamples, windowSamples);
        }

        if (!anyStrip)
        {
            return WaveformOverviewRebuildDecision.Skip();
        }

        return WaveformOverviewRebuildDecision.TrailingWindow(maxWindowSamples);
    }
}
