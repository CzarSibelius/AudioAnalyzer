using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;
using AudioAnalyzer.Infrastructure;
using Microsoft.Extensions.Logging;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace AudioAnalyzer.Platform.Windows.Audio;

/// <summary>Enumerates WASAPI capture and loopback devices and builds NAudio captures on Windows.</summary>
public sealed partial class WindowsAudioDeviceInfo : IAudioDeviceInfo
{
    private readonly ILogger<WindowsAudioDeviceInfo> _logger;
    private const string LoopbackPrefix = "loopback:";
    private const string CapturePrefix = "capture:";

    /// <summary>Initializes a new instance of the <see cref="WindowsAudioDeviceInfo"/> class.</summary>
    /// <param name="logger">Logger for enumeration failures.</param>
    public WindowsAudioDeviceInfo(ILogger<WindowsAudioDeviceInfo> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public IReadOnlyList<AudioDeviceEntry> GetDevices()
    {
        var list = new List<AudioDeviceEntry>();

        list.Add(new AudioDeviceEntry { Name = "Demo Mode (90 BPM)", Id = DemoAudioDevice.Prefix + "90" });
        list.Add(new AudioDeviceEntry { Name = "Demo Mode (120 BPM)", Id = DemoAudioDevice.Prefix + "120" });
        list.Add(new AudioDeviceEntry { Name = "Demo Mode (140 BPM)", Id = DemoAudioDevice.Prefix + "140" });

        try
        {
            var enumerator = new MMDeviceEnumerator();
            list.Add(new AudioDeviceEntry { Name = "System Audio (Loopback)", Id = null });

            var captureDevices = enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active);
            foreach (var device in captureDevices)
            {
                list.Add(new AudioDeviceEntry { Name = "🎤 " + device.FriendlyName, Id = CapturePrefix + device.FriendlyName });
            }

            var renderDevices = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
            foreach (var device in renderDevices)
            {
                list.Add(new AudioDeviceEntry { Name = "🔊 " + device.FriendlyName + " (Loopback)", Id = LoopbackPrefix + device.FriendlyName });
            }
        }
        catch (Exception ex)
        {
            LogWasapiEnumerationFailed(ex);
        }

        return list;
    }

    /// <inheritdoc />
    public IAudioInput CreateCapture(string? deviceId)
    {
        if (string.IsNullOrEmpty(deviceId))
        {
            var loopback = new WasapiLoopbackCapture();
            return new WindowsWaveInAudioInput(loopback);
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

        var enumerator = new MMDeviceEnumerator();
        if (deviceId.StartsWith(CapturePrefix, StringComparison.Ordinal))
        {
            var name = deviceId.Substring(CapturePrefix.Length);
            var captureDevices = enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active);
            foreach (var d in captureDevices)
            {
                if (d.FriendlyName == name)
                {
                    var capture = new WasapiCapture(d);
                    return new WindowsWaveInAudioInput(capture);
                }
            }
        }
        else if (deviceId.StartsWith(LoopbackPrefix, StringComparison.Ordinal))
        {
            var name = deviceId.Substring(LoopbackPrefix.Length);
            var renderDevices = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
            foreach (var d in renderDevices)
            {
                if (d.FriendlyName == name)
                {
                    var loopback = new WasapiLoopbackCapture(d);
                    return new WindowsWaveInAudioInput(loopback);
                }
            }
        }

        return new WindowsWaveInAudioInput(new WasapiLoopbackCapture());
    }
}
