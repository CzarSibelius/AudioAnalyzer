using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;
using AudioAnalyzer.Infrastructure;
using AudioAnalyzer.Platform.macOS.Audio.CoreAudioTap;
using Microsoft.Extensions.Logging;

namespace AudioAnalyzer.Platform.macOS.Audio;

/// <summary>
/// Device listing for macOS (pinned <c>net10.0-macos*</c> host per ADR-0086): Demo synthesis, Core Audio process-tap system audio
/// (macOS 14.2+, System Audio Recording consent per ADR-0087), and Core Audio inputs (PBI-013 / ADR-0084). There is no WASAPI-style
/// built-in loopback; system/desktop "what you hear" capture uses the Core Audio tap (see ADR-0088).
/// </summary>
public sealed partial class MacOsAudioDeviceInfo : IAudioDeviceInfo
{
    private readonly ILogger<MacOsAudioDeviceInfo> _logger;
    private readonly IMacOsAudioEnumerator _enumerator;
    private readonly IMacOsCoreAudioTapSystemAudioInputFactory _tapSystemAudioFactory;

    /// <summary>Initializes a new instance of the <see cref="MacOsAudioDeviceInfo"/> class.</summary>
    public MacOsAudioDeviceInfo(
        ILogger<MacOsAudioDeviceInfo> logger,
        IMacOsAudioEnumerator enumerator,
        IMacOsCoreAudioTapSystemAudioInputFactory tapSystemAudioFactory)
    {
        _logger = logger;
        _enumerator = enumerator;
        _tapSystemAudioFactory = tapSystemAudioFactory ?? throw new ArgumentNullException(nameof(tapSystemAudioFactory));
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
        };

        if (MacOsCoreAudioTapAvailability.IsOperatingSystemSupported)
        {
            string tapLabel = MacOsCoreAudioTapAvailability.IsCaptureReady
                ? "🔉 Desktop / system audio (Core Audio tap — System Audio Recording permission)"
                : "🔉 Desktop / system audio (Core Audio tap — build native/audio-tap-shim, see docs)";
            list.Add(new AudioDeviceEntry
            {
                Name = tapLabel,
                Id = CrossPlatformAudioDeviceIds.MacOsCoreAudioTapSystemAudio,
            });

            if (!MacOsCoreAudioTapAvailability.IsCaptureReady)
            {
                LogCoreAudioTapShimNotLoaded();
            }
        }

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

        if (string.Equals(deviceId, CrossPlatformAudioDeviceIds.MacOsCoreAudioTapSystemAudio, StringComparison.Ordinal))
        {
            return _tapSystemAudioFactory.Create();
        }

        if (_enumerator.TryCreateCapture(deviceId, out IAudioInput? capture) && capture != null)
        {
            return capture;
        }

        LogUnknownDevice(deviceId);
        return new SyntheticAudioInput(120);
    }
}
