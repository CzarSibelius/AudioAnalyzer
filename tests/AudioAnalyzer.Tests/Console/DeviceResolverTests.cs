using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Console;
using AudioAnalyzer.Domain;
using Xunit;

namespace AudioAnalyzer.Tests.Console;

public sealed class DeviceResolverTests
{
    private sealed class StubFallbackPolicy : IDefaultDeviceFallbackPolicy
    {
        private readonly (string? deviceId, string name) _result;

        public StubFallbackPolicy((string? deviceId, string name) result) => _result = result;

        public bool WasCalled { get; private set; }

        public (string? deviceId, string name) ResolveLoopbackFallback(IReadOnlyList<AudioDeviceEntry> devices)
        {
            WasCalled = true;
            return _result;
        }
    }

    [Fact]
    public void TryResolveFromSettings_LoopbackEmptyDeviceName_PrefersNullIdSystemAudioWhenPresent()
    {
        var devices = new List<AudioDeviceEntry>
        {
            new() { Name = "System Audio (Loopback)", Id = null },
            new() { Name = "Tap row", Id = CrossPlatformAudioDeviceIds.MacOsCoreAudioTapSystemAudio },
        };
        var settings = new AppSettings { InputMode = "loopback", DeviceName = null };
        var policy = new StubFallbackPolicy((null, "unused"));

        (string? deviceId, string name) = DeviceResolver.TryResolveFromSettings(devices, settings, policy);

        Assert.Null(deviceId);
        Assert.Equal("System Audio (Loopback)", name);
        Assert.False(policy.WasCalled);
    }

    [Fact]
    public void TryResolveFromSettings_LoopbackEmptyDeviceName_PrefersCoreAudioTapOverFallback()
    {
        var devices = new List<AudioDeviceEntry>
        {
            new() { Name = "Demo", Id = DemoAudioDevice.Prefix + "120" },
            new() { Name = "Tap row", Id = CrossPlatformAudioDeviceIds.MacOsCoreAudioTapSystemAudio },
        };
        var settings = new AppSettings { InputMode = "loopback", DeviceName = null };
        var policy = new StubFallbackPolicy((DemoAudioDevice.Prefix + "120", "Demo"));

        (string? deviceId, string name) = DeviceResolver.TryResolveFromSettings(devices, settings, policy);

        Assert.Equal(CrossPlatformAudioDeviceIds.MacOsCoreAudioTapSystemAudio, deviceId);
        Assert.Equal("Tap row", name);
        Assert.False(policy.WasCalled);
    }

    [Fact]
    public void TryResolveFromSettings_LoopbackEmptyDeviceName_NoSystemAudioOrTap_DelegatesToFallbackPolicy()
    {
        var devices = new List<AudioDeviceEntry>
        {
            new() { Name = "Mic", Id = "macos-input:uid-mic" },
            new() { Name = "Demo", Id = DemoAudioDevice.Prefix + "120" },
        };
        var settings = new AppSettings { InputMode = "loopback", DeviceName = null };
        var policy = new StubFallbackPolicy((DemoAudioDevice.Prefix + "120", "Demo"));

        (string? deviceId, string name) = DeviceResolver.TryResolveFromSettings(devices, settings, policy);

        Assert.True(policy.WasCalled);
        Assert.Equal(DemoAudioDevice.Prefix + "120", deviceId);
        Assert.Equal("Demo", name);
    }

    [Fact]
    public void TryResolveFromSettings_LoopbackEmptyDeviceName_ExplicitCoreAudioTapStillResolves()
    {
        var devices = new List<AudioDeviceEntry>
        {
            new() { Name = "Demo", Id = DemoAudioDevice.Prefix + "120" },
            new() { Name = "Tap row", Id = CrossPlatformAudioDeviceIds.MacOsCoreAudioTapSystemAudio },
        };
        var settings = new AppSettings
        {
            InputMode = "device",
            DeviceName = CrossPlatformAudioDeviceIds.MacOsCoreAudioTapSystemAudio,
        };
        var policy = new StubFallbackPolicy((null, "unused"));

        (string? deviceId, string name) = DeviceResolver.TryResolveFromSettings(devices, settings, policy);

        Assert.Equal(CrossPlatformAudioDeviceIds.MacOsCoreAudioTapSystemAudio, deviceId);
        Assert.Equal("Tap row", name);
        Assert.False(policy.WasCalled);
    }

    [Fact]
    public void TryResolveFromSettings_EmptyDevices_ReturnsEmpty()
    {
        var settings = new AppSettings { InputMode = "loopback", DeviceName = null };
        var policy = new StubFallbackPolicy((null, "unused"));

        (string? deviceId, string name) = DeviceResolver.TryResolveFromSettings([], settings, policy);

        Assert.Null(deviceId);
        Assert.Equal("", name);
        Assert.False(policy.WasCalled);
    }
}
