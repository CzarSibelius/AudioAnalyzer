using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;
using AudioAnalyzer.Platform.Windows.Audio;
using Xunit;

namespace AudioAnalyzer.Tests.Platform.Windows.Audio;

public sealed class WindowsDefaultDeviceFallbackPolicyTests
{
    [Fact]
    public void ResolveLoopbackFallback_ReturnsFirstDevice()
    {
        var policy = new WindowsDefaultDeviceFallbackPolicy();
        var devices = new List<AudioDeviceEntry>
        {
            new() { Name = "Demo", Id = DemoAudioDevice.Prefix + "90" },
            new() { Name = "Speakers (Loopback)", Id = "loopback:Speakers" },
        };

        (string? deviceId, string name) = policy.ResolveLoopbackFallback(devices);

        Assert.Equal(DemoAudioDevice.Prefix + "90", deviceId);
        Assert.Equal("Demo", name);
    }

    [Fact]
    public void ResolveLoopbackFallback_Empty_ReturnsEmpty()
    {
        var policy = new WindowsDefaultDeviceFallbackPolicy();

        (string? deviceId, string name) = policy.ResolveLoopbackFallback([]);

        Assert.Null(deviceId);
        Assert.Equal("", name);
    }
}
