using System.Threading;
using AudioAnalyzer.Application;
using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Application.Fft;
using AudioAnalyzer.Application.VolumeAnalysis;
using Xunit;

namespace AudioAnalyzer.Tests.Application;

/// <summary>Guards overview rebuild partition size (Θ(partition) work per gate) and skip behavior for <see cref="IWaveformOverviewRebuildPolicy"/>.</summary>
public sealed class AnalysisEngineOverviewRebuildTests
{
    private sealed class StubBeatTiming : IBeatTimingSource
    {
        public double CurrentBpm => 120;
        public int BeatCount => 0;
        public bool BeatFlashActive => false;
        public double BeatSensitivity { get; set; } = 1.0;

        public void OnAudioFrame(double avgEnergy) => _ = avgEnergy;

        public void OnVisualTick()
        {
        }
    }

    private sealed class MutableOverviewPolicy : IWaveformOverviewRebuildPolicy
    {
        public WaveformOverviewRebuildDecision Decision { get; set; } = WaveformOverviewRebuildDecision.FullRing();

        public WaveformOverviewRebuildDecision GetDecision(int validMonoSampleCount, int sampleRateHz) => Decision;
    }

    [Fact]
    public void Trailing_window_policy_limits_partition_mono_sample_count()
    {
        var policy = new MutableOverviewPolicy { Decision = WaveformOverviewRebuildDecision.TrailingWindow(4_000) };
        var engine = new AnalysisEngine(new StubBeatTiming(), new VolumeAnalyzer(), new FftBandProcessor(), policy);
        engine.SetNumBands(8);
        engine.ApplyMaxHistorySeconds(30, 8_000);
        var format = new AudioFormat { SampleRate = 8_000, BitsPerSample = 16, Channels = 1 };
        var buffer = new byte[32_768];
        for (int i = 0; i < 400; i++)
        {
            engine.ProcessAudio(buffer, buffer.Length, format);
        }

        Thread.Sleep(100);
        engine.ProcessAudio(buffer, buffer.Length, format);

        Assert.Equal(4_000, engine.LastOverviewRebuildPartitionMonoSampleCount);
        AudioAnalysisSnapshot s = engine.GetSnapshot();
        Assert.Equal(4_000, s.WaveformOverviewBuiltValidSampleCount);
        Assert.Equal(4_000, s.WaveformOverviewValidSampleCount);
        Assert.Equal(8192, s.WaveformOverviewLength);
    }

    [Fact]
    public void Skip_policy_clears_overview_snapshot()
    {
        var policy = new MutableOverviewPolicy { Decision = WaveformOverviewRebuildDecision.Skip() };
        var engine = new AnalysisEngine(new StubBeatTiming(), new VolumeAnalyzer(), new FftBandProcessor(), policy);
        engine.SetNumBands(8);
        engine.ApplyMaxHistorySeconds(5, 48_000);
        var format = new AudioFormat { SampleRate = 48_000, BitsPerSample = 16, Channels = 1 };
        var buffer = new byte[4096];
        Thread.Sleep(100);
        engine.ProcessAudio(buffer, buffer.Length, format);

        AudioAnalysisSnapshot s = engine.GetSnapshot();
        Assert.Equal(0, s.WaveformOverviewLength);
        Assert.Equal(0, engine.LastOverviewRebuildPartitionMonoSampleCount);
    }

    [Fact]
    public void Null_policy_rebuilds_full_valid_mono_window()
    {
        var engine = new AnalysisEngine(new StubBeatTiming(), new VolumeAnalyzer(), new FftBandProcessor());
        engine.SetNumBands(8);
        engine.ApplyMaxHistorySeconds(20, 8_000);
        var format = new AudioFormat { SampleRate = 8_000, BitsPerSample = 16, Channels = 1 };
        var buffer = new byte[16_384];
        for (int i = 0; i < 200; i++)
        {
            engine.ProcessAudio(buffer, buffer.Length, format);
        }

        Thread.Sleep(100);
        engine.ProcessAudio(buffer, buffer.Length, format);

        AudioAnalysisSnapshot s = engine.GetSnapshot();
        Assert.Equal(s.WaveformOverviewBuiltValidSampleCount, engine.LastOverviewRebuildPartitionMonoSampleCount);
        Assert.True(engine.LastOverviewRebuildPartitionMonoSampleCount >= 2);
    }
}
