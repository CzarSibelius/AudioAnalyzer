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
