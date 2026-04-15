using System.Threading;
using AudioAnalyzer.Application;
using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Application.Fft;
using AudioAnalyzer.Application.VolumeAnalysis;
using Xunit;

namespace AudioAnalyzer.Tests.Application;

public sealed class AnalysisEngineRetainedHistoryResetTests
{
    private sealed class MutableBeatTiming : IBeatTimingSource
    {
        public double CurrentBpm { get; set; } = 120;
        public int BeatCount { get; set; }
        public bool BeatFlashActive => false;
        public double BeatSensitivity { get; set; } = 1.0;

        public void OnAudioFrame(double avgEnergy) => _ = avgEnergy;

        public void OnVisualTick()
        {
        }
    }

    [Fact]
    public void ResetRetainedWaveformHistory_clears_overview_and_beat_marks()
    {
        var beats = new MutableBeatTiming();
        var engine = new AnalysisEngine(beats, new VolumeAnalyzer(), new FftBandProcessor());
        engine.SetNumBands(8);
        engine.ApplyMaxHistorySeconds(5, 48_000);
        var format = new AudioFormat { SampleRate = 48_000, BitsPerSample = 16, Channels = 1 };
        var buffer = new byte[4096];
        Thread.Sleep(100);
        engine.ProcessAudio(buffer, buffer.Length, format);

        beats.BeatCount = 1;
        var before = engine.GetSnapshot();
        Assert.True(before.WaveformOverviewLength > 0);
        Assert.Equal(1, before.WaveformBeatMarkLength);

        ((IWaveformRetainedHistoryReset)engine).ResetRetainedWaveformHistory();

        var after = engine.GetSnapshot();
        Assert.Equal(0, after.WaveformOverviewLength);
        Assert.Equal(0, after.WaveformBeatMarkLength);
    }
}
