using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;
using AudioAnalyzer.Infrastructure;
using Microsoft.Extensions.Logging;

namespace AudioAnalyzer.Platform.macOS.Audio;

/// <summary>
/// Device listing for macOS (pinned <c>net10.0-macos*</c> host per ADR-0086): Demo synthesis, desktop-output shortcuts (virtual routing per ADR-0085;
/// optional ScreenCaptureKit system audio per ADR-0086 / PBI-016), and Core Audio inputs (PBI-013 / ADR-0084). There is no WASAPI-style built-in loopback;
/// desktop visualization uses virtual devices, SCK with Screen Recording consent, or routing.
/// </summary>
public sealed partial class MacOsAudioDeviceInfo : IAudioDeviceInfo
{
    private readonly ILogger<MacOsAudioDeviceInfo> _logger;
    private readonly IMacOsAudioEnumerator _enumerator;
    private readonly IMacOsScreenCaptureKitSystemAudioInputFactory _sckSystemAudioFactory;

    /// <summary>Initializes a new instance of the <see cref="MacOsAudioDeviceInfo"/> class.</summary>
    public MacOsAudioDeviceInfo(
        ILogger<MacOsAudioDeviceInfo> logger,
        IMacOsAudioEnumerator enumerator,
        IMacOsScreenCaptureKitSystemAudioInputFactory sckSystemAudioFactory)
    {
        _logger = logger;
        _enumerator = enumerator;
        _sckSystemAudioFactory = sckSystemAudioFactory ?? throw new ArgumentNullException(nameof(sckSystemAudioFactory));
    }

    /// <inheritdoc />
    public IReadOnlyList<AudioDeviceEntry> GetDevices()
    {
        LogGetDevicesBegin();
        var list = new List<AudioDeviceEntry>
        {
            new() { Name = "Demo Mode (90 BPM)", Id = DemoAudioDevice.Prefix + "90" },
            new() { Name = "Demo Mode (120 BPM)", Id = DemoAudioDevice.Prefix + "120" },
            new() { Name = "Demo Mode (140 BPM)", Id = DemoAudioDevice.Prefix + "140" },
            new()
            {
                Name = "🔊 Desktop / system output (virtual mixer if installed)",
                Id = CrossPlatformAudioDeviceIds.MacOsDesktopVirtualRouting,
            },
            new()
            {
                Name = "🖥️ Desktop / system audio (ScreenCaptureKit — Screen Recording permission)",
                Id = CrossPlatformAudioDeviceIds.MacOsScreenCaptureKitSystemAudio,
            },
        };

        foreach (MacOsPhysicalAudioDevice d in _enumerator.GetPhysicalInputs())
        {
            list.Add(new AudioDeviceEntry { Name = d.DisplayName, Id = MacOsAudioDeviceIds.EncodeInputUid(d.Uid) });
        }

        LogGetDevicesEnd(list.Count);
        return list;
    }

    /// <inheritdoc />
    public IAudioInput CreateCapture(string? deviceId)
    {
        if (string.IsNullOrEmpty(deviceId))
        {
            return new SyntheticAudioInput(120);
        }

        if (deviceId.StartsWith(DemoAudioDevice.Prefix, StringComparison.Ordinal))
        {
            int bpm = 120;
            if (deviceId.Length > DemoAudioDevice.Prefix.Length &&
                int.TryParse(deviceId.AsSpan(DemoAudioDevice.Prefix.Length), out var parsed))
            {
                bpm = Math.Clamp(parsed, 60, 180);
            }

            return new SyntheticAudioInput(bpm);
        }

        if (string.Equals(deviceId, CrossPlatformAudioDeviceIds.MacOsScreenCaptureKitSystemAudio, StringComparison.Ordinal))
        {
            return _sckSystemAudioFactory.Create();
        }

        if (string.Equals(deviceId, CrossPlatformAudioDeviceIds.MacOsDesktopVirtualRouting, StringComparison.Ordinal))
        {
            foreach (MacOsPhysicalAudioDevice d in _enumerator.GetPhysicalInputs())
            {
                if (MacOsDesktopMixSinkHeuristic.LooksLikeDesktopMixSink(d.HardwareName, d.Uid))
                {
                    string encoded = MacOsAudioDeviceIds.EncodeInputUid(d.Uid);
                    if (_enumerator.TryCreateCapture(encoded, out IAudioInput? routed) && routed != null)
                    {
                        LogDesktopVirtualRoutingPicked(d.HardwareName);
                        return routed;
                    }
                }
            }

            LogDesktopVirtualRoutingNoSinkFound();
            return new SyntheticAudioInput(120);
        }

        if (_enumerator.TryCreateCapture(deviceId, out IAudioInput? capture) && capture != null)
        {
            return capture;
        }

        LogUnknownDevice(deviceId);
        return new SyntheticAudioInput(120);
    }
}
