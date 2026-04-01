using AudioAnalyzer.Application.BeatDetection;
using Xunit;

namespace AudioAnalyzer.Tests.Application.BeatDetection;

public sealed class DemoBeatTimingSourceTests
{
    [Theory]
    [InlineData("demo:90", 90)]
    [InlineData("demo:140", 140)]
    [InlineData("loopback:x", 120)]
    public void ConfigureFromDeviceId_sets_bpm(string deviceId, int expectedBpm)
    {
        var s = new DemoBeatTimingSource();
        s.ConfigureFromDeviceId(deviceId);
        Assert.Equal(expectedBpm, s.CurrentBpm);
    }
}
