using AudioAnalyzer.Domain;
using Xunit;

namespace AudioAnalyzer.Tests.Domain;

public sealed class DemoAudioDeviceTests
{
    [Theory]
    [InlineData("demo:120", true, 120)]
    [InlineData("demo:60", true, 60)]
    [InlineData("capture:Mic", false, 120)]
    [InlineData(null, false, 120)]
    public void TryGetBpm_parses_demo_prefix(string? id, bool ok, int expected)
    {
        bool r = DemoAudioDevice.TryGetBpm(id, out int bpm);
        Assert.Equal(ok, r);
        Assert.Equal(expected, bpm);
    }
}
