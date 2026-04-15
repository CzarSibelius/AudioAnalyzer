using System.Threading;
using AudioAnalyzer.Application;
using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Application.Fft;
using AudioAnalyzer.Application.VolumeAnalysis;
using Xunit;

namespace AudioAnalyzer.Tests.Application;

public sealed class AnalysisEngineTests
{
    private sealed class StubBeatTiming : IBeatTimingSource
    {
        public double CurrentBpm => 120;
        public int BeatCount => 0;
        public bool BeatFlashActive => false;
        public double BeatSensitivity { get; set; } = 1.0;

        public void OnAudioFrame(double avgEnergy)
        {
        }

        public void OnVisualTick()
        {
        }
    }

    /// <summary>
    /// <see cref="AnalysisEngine.ProcessAudio"/> may run on the capture thread while the UI thread calls
    /// <see cref="AnalysisEngine.GetSnapshot"/>. Both paths take the same lock for the full method body, so very large
    /// iteration counts mostly serialize and burn minutes without adding coverage; this uses enough interleaving to
    /// guard exceptions and consistent array sizing without pathological runtime.
    /// </summary>
    [Fact]
    public async Task ProcessAudio_and_GetSnapshot_concurrent_stress_remain_consistent()
    {
        var engine = new AnalysisEngine(new StubBeatTiming(), new VolumeAnalyzer(), new FftBandProcessor());
        engine.SetNumBands(24);

        var format = new AudioFormat { SampleRate = 48_000, BitsPerSample = 16, Channels = 1 };
        var buffer = new byte[16_384];
        const int iterations = 80;

        Task audio = Task.Run(() =>
        {
            for (int i = 0; i < iterations; i++)
            {
                engine.ProcessAudio(buffer, buffer.Length, format);
            }
        });

        Task ui = Task.Run(() =>
        {
            for (int i = 0; i < iterations; i++)
            {
                AudioAnalysisSnapshot s = engine.GetSnapshot();
                Assert.Equal(s.NumBands, s.SmoothedMagnitudes.Length);
                Assert.Equal(s.NumBands, s.PeakHold.Length);
                Assert.Equal(s.WaveformSize, s.Waveform.Length);
            }
        });

        await Task.WhenAll(audio, ui);
    }

    [Fact]
    public void GetSnapshot_returns_cloned_arrays_each_call()
    {
        var engine = new AnalysisEngine(new StubBeatTiming(), new VolumeAnalyzer(), new FftBandProcessor());
        engine.SetNumBands(8);
        var format = new AudioFormat { SampleRate = 48_000, BitsPerSample = 16, Channels = 1 };
        var buffer = new byte[16_384];
        engine.ProcessAudio(buffer, buffer.Length, format);

        AudioAnalysisSnapshot a = engine.GetSnapshot();
        AudioAnalysisSnapshot b = engine.GetSnapshot();

        Assert.NotSame(a.SmoothedMagnitudes, b.SmoothedMagnitudes);
        Assert.NotSame(a.PeakHold, b.PeakHold);
        Assert.NotSame(a.Waveform, b.Waveform);
    }

    /// <summary>
    /// Overview is rebuilt on the same ~50 ms gate as the scope display buffer; after enough wall time and audio,
    /// snapshots expose 8192 buckets without cloning the full history ring (see ADR-0077).
    /// </summary>
    [Fact]
    public void GetSnapshot_after_display_tick_includes_decimated_overview_arrays()
    {
        var engine = new AnalysisEngine(new StubBeatTiming(), new VolumeAnalyzer(), new FftBandProcessor());
        engine.SetNumBands(8);
        engine.ApplyMaxHistorySeconds(10, 48_000);
        var format = new AudioFormat { SampleRate = 48_000, BitsPerSample = 16, Channels = 1 };
        var buffer = new byte[4096];

        Thread.Sleep(100);
        engine.ProcessAudio(buffer, buffer.Length, format);

        AudioAnalysisSnapshot s = engine.GetSnapshot();
        Assert.Equal(512, s.WaveformSize);
        Assert.Equal(512, s.Waveform.Length);
        Assert.Equal(8192, s.WaveformOverviewLength);
        Assert.True(s.WaveformOverviewSpanSeconds > 0);
        Assert.Equal(8192, s.WaveformOverviewMin.Length);
        Assert.Equal(8192, s.WaveformOverviewMax.Length);

        AudioAnalysisSnapshot t = engine.GetSnapshot();
        Assert.NotSame(s.WaveformOverviewMin, t.WaveformOverviewMin);
        Assert.NotSame(s.Waveform, t.Waveform);
    }
}
