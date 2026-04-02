using AudioAnalyzer.Application.Display;
using Xunit;

namespace AudioAnalyzer.Tests.Application.Display;

public sealed class MainRenderFpsMeterTests
{
    [Fact]
    public void ComputeSmoothedFpsFromIntervals_Empty_ReturnsZero()
    {
        Assert.Equal(0, MainRenderFpsMeter.ComputeSmoothedFpsFromIntervals(ReadOnlySpan<double>.Empty));
    }

    [Fact]
    public void ComputeSmoothedFpsFromIntervals_ConstantInterval_MatchesInverse()
    {
        ReadOnlySpan<double> intervals = [0.1, 0.1, 0.1];
        Assert.Equal(10.0, MainRenderFpsMeter.ComputeSmoothedFpsFromIntervals(intervals), precision: 5);
    }

    [Fact]
    public void ClearIntervals_ClearsRollingBuffer()
    {
        var meter = new MainRenderFpsMeter(4);
        meter.RecordFrameCompleted();
        System.Threading.Thread.Sleep(40);
        meter.RecordFrameCompleted();
        Assert.True(meter.HasIntervalSample);
        meter.ClearIntervals();
        Assert.False(meter.HasIntervalSample);
        Assert.Equal(0, meter.GetSmoothedFps());
    }

    [Fact]
    public void MainRenderFpsMeter_InvalidWindowSize_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new MainRenderFpsMeter(0));
    }
}
