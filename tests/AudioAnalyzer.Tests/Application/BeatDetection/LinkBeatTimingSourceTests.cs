using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Application.BeatDetection;
using Xunit;

namespace AudioAnalyzer.Tests.Application.BeatDetection;

public sealed class LinkBeatTimingSourceTests
{
    private sealed class SteppingLink : ILinkSession
    {
        public bool IsAvailable => true;
        public bool IsEnabled => true;
        private double _beat;

        public void SetEnabled(bool enabled) => _ = enabled;

        public void Capture(out double tempoBpm, out int numPeers, out double beat, double quantum)
        {
            _ = quantum;
            tempoBpm = 120;
            numPeers = 2;
            _beat += 0.6;
            beat = _beat;
        }

        public void Dispose()
        {
        }
    }

    [Fact]
    public void OnVisualTick_increments_beat_count_when_whole_beat_advances()
    {
        var link = new SteppingLink();
        var s = new LinkBeatTimingSource(link);
        s.ResetBeatTracking();

        int start = s.BeatCount;
        for (int i = 0; i < 30; i++)
        {
            s.OnVisualTick();
        }

        Assert.True(s.BeatCount > start);
        Assert.True(s.CurrentBpm >= 1.0);
    }

    [Fact]
    public void When_unavailable_CurrentBpm_is_zero()
    {
        var link = new NullLinkSession();
        var s = new LinkBeatTimingSource(link);
        s.OnVisualTick();
        Assert.Equal(0, s.CurrentBpm);
    }

    private sealed class NullLinkSession : ILinkSession
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
