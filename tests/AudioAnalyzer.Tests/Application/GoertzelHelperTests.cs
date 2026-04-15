using AudioAnalyzer.Application.Fft;
using Xunit;

namespace AudioAnalyzer.Tests.Application;

public sealed class GoertzelHelperTests
{
    [Fact]
    public void BandPower_is_highest_near_tuned_frequency()
    {
        const double sr = 48_000;
        const int n = 240;
        var buf = new float[n];
        double w = 2 * Math.PI * 1_000.0 / sr;
        for (int i = 0; i < n; i++)
        {
            buf[i] = (float)Math.Sin(w * i);
        }

        float pTarget = GoertzelHelper.BandPower(buf, 1_000.0, sr);
        float pOff = GoertzelHelper.BandPower(buf, 250.0, sr);
        Assert.True(pTarget > pOff * 2f, $"expected dominant bin at 1kHz: target={pTarget} off={pOff}");
    }
}
