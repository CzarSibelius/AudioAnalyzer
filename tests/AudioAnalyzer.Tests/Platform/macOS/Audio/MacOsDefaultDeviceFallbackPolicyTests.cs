using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;
using AudioAnalyzer.Platform.macOS.Audio;
using Xunit;

namespace AudioAnalyzer.Tests.Platform.macOS.Audio;

public sealed class MacOsDefaultDeviceFallbackPolicyTests
{
    [Fact]
    public void ResolveLoopbackFallback_PrefersDemoWhenPresent()
    {
        var policy = new MacOsDefaultDeviceFallbackPolicy();
        var devices = new List<AudioDeviceEntry>
        {
            new() { Name = "Mic", Id = "macos-input:uid-mic" },
            new() { Name = "Demo", Id = DemoAudioDevice.Prefix + "120" },
        };

        (string? deviceId, string name) = policy.ResolveLoopbackFallback(devices);

        Assert.Equal(DemoAudioDevice.Prefix + "120", deviceId);
        Assert.Equal("Demo", name);
    }

    [Fact]
    public void ResolveLoopbackFallback_NoDemo_ReturnsFirstDevice()
    {
        var policy = new MacOsDefaultDeviceFallbackPolicy();
        var devices = new List<AudioDeviceEntry>
        {
            new() { Name = "Mic", Id = "macos-input:uid-mic" },
            new() { Name = "Other", Id = "macos-input:uid-other" },
        };

        (string? deviceId, string name) = policy.ResolveLoopbackFallback(devices);

        Assert.Equal("macos-input:uid-mic", deviceId);
        Assert.Equal("Mic", name);
    }

    [Fact]
    public void ResolveLoopbackFallback_Empty_ReturnsEmpty()
    {
        var policy = new MacOsDefaultDeviceFallbackPolicy();

        (string? deviceId, string name) = policy.ResolveLoopbackFallback([]);

        Assert.Null(deviceId);
        Assert.Equal("", name);
    }
}
