using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Application.BeatDetection;
using Xunit;

namespace AudioAnalyzer.Tests.Application.BeatDetection;

public sealed class AudioDerivedBeatTimingSourceTests
{
    private sealed class FakeDetector : IBeatDetector
    {
        public int ProcessCalls;
        public int DecayCalls;

        public double BeatSensitivity { get; set; } = 1.0;
        public double CurrentBpm => 100;
        public bool BeatFlashActive => false;
        public int BeatCount => 7;

        public void ProcessFrame(double energy)
        {
            ProcessCalls++;
            _ = energy;
        }

        public void DecayFlashFrame() => DecayCalls++;
    }

    [Fact]
    public void OnAudioFrame_and_OnVisualTick_delegate_to_detector()
    {
        var d = new FakeDetector();
        var s = new AudioDerivedBeatTimingSource(d);

        s.OnAudioFrame(0.5);
        s.OnVisualTick();

        Assert.Equal(1, d.ProcessCalls);
        Assert.Equal(1, d.DecayCalls);
        Assert.Equal(100, s.CurrentBpm);
        Assert.Equal(7, s.BeatCount);
    }
}
