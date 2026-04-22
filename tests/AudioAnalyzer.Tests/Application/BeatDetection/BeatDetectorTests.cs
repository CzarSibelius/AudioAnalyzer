using AudioAnalyzer.Application.BeatDetection;
using Xunit;

namespace AudioAnalyzer.Tests.Application.BeatDetection;

public sealed class BeatDetectorTests
{
    private static void WarmupHistory(BeatDetector d, double energy = 0.12)
    {
        for (int i = 0; i < 12; i++)
        {
            d.ProcessFrame(energy);
        }
    }

    private static void AcceptBeatAtInterval(BeatDetector d, ref DateTime t, TimeSpan step)
    {
        t += step;
        d.ProcessFrame(0.55);
    }

    [Fact]
    public void CurrentBpm_resets_to_zero_after_stale_window_without_accepted_beats()
    {
        var t = new DateTime(2024, 6, 1, 12, 0, 0, DateTimeKind.Utc);
        var d = new BeatDetector(() => t);
        WarmupHistory(d);

        AcceptBeatAtInterval(d, ref t, TimeSpan.FromMilliseconds(300));
        AcceptBeatAtInterval(d, ref t, TimeSpan.FromMilliseconds(500));
        AcceptBeatAtInterval(d, ref t, TimeSpan.FromMilliseconds(500));
        Assert.True(d.CurrentBpm > 0);

        t += TimeSpan.FromSeconds(BeatDetector.StaleBeatWindowSeconds + 0.5);
        d.ProcessFrame(0.12);

        Assert.Equal(0, d.CurrentBpm);
        Assert.Equal(0, d.BeatCount);
    }

    [Fact]
    public void ResetAudioDerivedBeatTiming_clears_bpm_count_and_flash()
    {
        var t = new DateTime(2024, 6, 1, 12, 0, 0, DateTimeKind.Utc);
        var d = new BeatDetector(() => t);
        WarmupHistory(d);
        AcceptBeatAtInterval(d, ref t, TimeSpan.FromMilliseconds(300));
        AcceptBeatAtInterval(d, ref t, TimeSpan.FromMilliseconds(500));
        Assert.True(d.CurrentBpm > 0);
        Assert.True(d.BeatCount > 0);

        d.ResetAudioDerivedBeatTiming();

        Assert.Equal(0, d.CurrentBpm);
        Assert.Equal(0, d.BeatCount);
        Assert.False(d.BeatFlashActive);
    }
}
