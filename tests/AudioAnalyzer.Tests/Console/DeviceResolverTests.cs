using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Console;
using AudioAnalyzer.Domain;
using Xunit;

namespace AudioAnalyzer.Tests.Console;

public sealed class DeviceResolverTests
{
    [Fact]
    public void TryResolveFromSettings_LoopbackEmptyDeviceName_PrefersWindowsNullIdWhenPresent()
    {
        var devices = new List<AudioDeviceEntry>
        {
            new() { Name = "System Audio (Loopback)", Id = null },
            new() { Name = "Desktop", Id = CrossPlatformAudioDeviceIds.MacOsDesktopVirtualRouting },
        };
        var settings = new AppSettings { InputMode = "loopback", DeviceName = null };

        (string? deviceId, string name) = DeviceResolver.TryResolveFromSettings(devices, settings);

        Assert.Null(deviceId);
        Assert.Equal("System Audio (Loopback)", name);
    }

    [Fact]
    public void TryResolveFromSettings_LoopbackEmptyDeviceName_OnMacOsHost_PrefersDemoOverDesktopShortcuts()
    {
        if (!OperatingSystem.IsMacOS())
        {
            return;
        }

        var devices = new List<AudioDeviceEntry>
        {
            new() { Name = "Demo", Id = DemoAudioDevice.Prefix + "120" },
            new() { Name = "Desktop row", Id = CrossPlatformAudioDeviceIds.MacOsDesktopVirtualRouting },
            new() { Name = "SCK row", Id = CrossPlatformAudioDeviceIds.MacOsScreenCaptureKitSystemAudio },
        };
        var settings = new AppSettings { InputMode = "loopback", DeviceName = null };

        (string? deviceId, string name) = DeviceResolver.TryResolveFromSettings(devices, settings);

        Assert.Equal(DemoAudioDevice.Prefix + "120", deviceId);
        Assert.Equal("Demo", name);
    }

    [Fact]
    public void TryResolveFromSettings_LoopbackEmptyDeviceName_OnNonMacHost_UsesMacDesktopWhenNoNullRow()
    {
        if (OperatingSystem.IsMacOS())
        {
            return;
        }

        var devices = new List<AudioDeviceEntry>
        {
            new() { Name = "Demo", Id = DemoAudioDevice.Prefix + "120" },
            new() { Name = "Desktop row", Id = CrossPlatformAudioDeviceIds.MacOsDesktopVirtualRouting },
        };
        var settings = new AppSettings { InputMode = "loopback", DeviceName = null };

        (string? deviceId, string name) = DeviceResolver.TryResolveFromSettings(devices, settings);

        Assert.Equal(CrossPlatformAudioDeviceIds.MacOsDesktopVirtualRouting, deviceId);
        Assert.Equal("Desktop row", name);
    }

    [Fact]
    public void TryResolveFromSettings_LoopbackEmptyDeviceName_ExplicitDesktopShortcutStillResolves()
    {
        var devices = new List<AudioDeviceEntry>
        {
            new() { Name = "Demo", Id = DemoAudioDevice.Prefix + "120" },
            new() { Name = "SCK row", Id = CrossPlatformAudioDeviceIds.MacOsScreenCaptureKitSystemAudio },
        };
        var settings = new AppSettings
        {
            InputMode = "device",
            DeviceName = CrossPlatformAudioDeviceIds.MacOsScreenCaptureKitSystemAudio,
        };

        (string? deviceId, string name) = DeviceResolver.TryResolveFromSettings(devices, settings);

        Assert.Equal(CrossPlatformAudioDeviceIds.MacOsScreenCaptureKitSystemAudio, deviceId);
        Assert.Equal("SCK row", name);
    }
}
