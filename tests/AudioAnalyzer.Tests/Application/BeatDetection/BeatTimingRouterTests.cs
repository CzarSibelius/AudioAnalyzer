using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Application.BeatDetection;
using AudioAnalyzer.Domain;
using Xunit;

namespace AudioAnalyzer.Tests.Application.BeatDetection;

public sealed class BeatTimingRouterTests
{
    [Fact]
    public void ApplyFromSettings_switches_active_source()
    {
        var audio = new AudioDerivedBeatTimingSource(new BeatDetector());
        var demo = new DemoBeatTimingSource();
        var link = new LinkBeatTimingSource(new LinkBeatTimingTestsNullLink());
        var router = new BeatTimingRouter(audio, demo, link);

        router.ApplyFromSettings(BpmSource.DemoDevice, "demo:90");
        Assert.Equal(BpmSource.DemoDevice, router.ActiveBpmSource);
        Assert.Equal(90, router.CurrentBpm);

        router.ApplyFromSettings(BpmSource.AudioAnalysis, null);
        Assert.Equal(BpmSource.AudioAnalysis, router.ActiveBpmSource);
    }

    [Fact]
    public void NotifyAudioCaptureStopped_clears_audio_derived_bpm_when_active_source_is_audio()
    {
        var t = new DateTime(2024, 7, 1, 8, 0, 0, DateTimeKind.Utc);
        var detector = new BeatDetector(() => t);
        var audio = new AudioDerivedBeatTimingSource(detector);
        var demo = new DemoBeatTimingSource();
        var link = new LinkBeatTimingSource(new LinkBeatTimingTestsNullLink());
        var router = new BeatTimingRouter(audio, demo, link);
        router.ApplyFromSettings(BpmSource.AudioAnalysis, null);

        for (int i = 0; i < 12; i++)
        {
            router.OnAudioFrame(0.12);
        }

        t += TimeSpan.FromMilliseconds(300);
        router.OnAudioFrame(0.55);
        t += TimeSpan.FromMilliseconds(500);
        router.OnAudioFrame(0.55);
        t += TimeSpan.FromMilliseconds(500);
        router.OnAudioFrame(0.55);
        Assert.True(router.CurrentBpm > 0);

        router.NotifyAudioCaptureStopped();

        Assert.Equal(0, router.CurrentBpm);
        Assert.Equal(0, router.BeatCount);
    }

    [Fact]
    public void NotifyAudioCaptureStopped_does_not_clear_demo_tempo()
    {
        var detector = new BeatDetector();
        var audio = new AudioDerivedBeatTimingSource(detector);
        var demo = new DemoBeatTimingSource();
        var link = new LinkBeatTimingSource(new LinkBeatTimingTestsNullLink());
        var router = new BeatTimingRouter(audio, demo, link);
        router.ApplyFromSettings(BpmSource.DemoDevice, "demo:100");

        Assert.Equal(100, router.CurrentBpm);
        router.NotifyAudioCaptureStopped();
        Assert.Equal(100, router.CurrentBpm);
    }

    private sealed class LinkBeatTimingTestsNullLink : ILinkSession
    {
        public bool IsAvailable => false;
        public bool IsEnabled => false;
        public void SetEnabled(bool enabled) => _ = enabled;
        public void Capture(out double tempoBpm, out int numPeers, out double beat, double quantum)
        {
            tempoBpm = 0;
            numPeers = 0;
            beat = 0;
            _ = quantum;
        }

        public void Dispose()
        {
        }
    }
}
