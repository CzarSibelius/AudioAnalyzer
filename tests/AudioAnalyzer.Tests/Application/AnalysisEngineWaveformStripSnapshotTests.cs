using System.Threading;
using AudioAnalyzer.Application;
using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Application.Fft;
using AudioAnalyzer.Application.VolumeAnalysis;
using Xunit;

namespace AudioAnalyzer.Tests.Application;

public sealed class AnalysisEngineWaveformStripSnapshotTests
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
    public void GetSnapshot_includes_stereo_overview_goertzel_and_timeline_fields_after_display_tick()
    {
        var engine = new AnalysisEngine(new MutableBeatTiming(), new VolumeAnalyzer(), new FftBandProcessor());
        engine.SetNumBands(8);
        engine.ApplyMaxHistorySeconds(10, 48_000);
        var format = new AudioFormat { SampleRate = 48_000, BitsPerSample = 16, Channels = 2 };
        var buffer = new byte[4096];
        Thread.Sleep(100);
        engine.ProcessAudio(buffer, buffer.Length, format);

        var s = engine.GetSnapshot();
        Assert.Equal(8192, s.WaveformOverviewLength);
        Assert.Equal(8192, s.WaveformOverviewMinRight.Length);
        Assert.Equal(8192, s.WaveformOverviewBandLowGoertzel.Length);
        Assert.True(s.WaveformOverviewValidSampleCount > 100);
        Assert.Equal(8192, s.WaveformOverviewMin.Length);
        Assert.True(s.WaveformOverviewNewestMonoSampleIndex >= s.WaveformOverviewOldestMonoSampleIndex);
        Assert.True(s.WaveformOverviewBuiltValidSampleCount > 100);
        Assert.True(s.WaveformOverviewBuiltNewestMonoSampleIndex > 0);
        Assert.True(s.WaveformOverviewBuiltOldestMonoSampleIndex >= 1);
        Assert.True(s.WaveformOverviewBuiltNewestMonoSampleIndex >= s.WaveformOverviewBuiltOldestMonoSampleIndex);
    }

    [Fact]
    public void GetSnapshot_records_beat_mark_when_beat_count_advances()
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
        var s = engine.GetSnapshot();
        Assert.Equal(1, s.WaveformBeatMarkLength);
        Assert.Single(s.WaveformBeatMarkMonoSampleIndex);
        Assert.Single(s.WaveformBeatMarkBeatOrdinal);
        Assert.Equal(1, s.WaveformBeatMarkBeatOrdinal[0]);
        Assert.True(s.WaveformBeatMarkMonoSampleIndex[0] > 0);
    }
}
