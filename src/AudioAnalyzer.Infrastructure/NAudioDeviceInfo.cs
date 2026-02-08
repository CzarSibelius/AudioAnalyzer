using AudioAnalyzer.Application.Abstractions;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace AudioAnalyzer.Infrastructure;

public sealed class NAudioDeviceInfo : IAudioDeviceInfo
{
    private const string LoopbackPrefix = "loopback:";
    private const string CapturePrefix = "capture:";

    public IReadOnlyList<AudioDeviceEntry> GetDevices()
    {
        var list = new List<AudioDeviceEntry>();
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
        catch
        {
            if (list.Count == 0)
            {
                list.Add(new AudioDeviceEntry { Name = "System Audio (Loopback)", Id = null });
            }
        }
        return list;
    }

    public IAudioInput CreateCapture(string? deviceId)
    {
        if (string.IsNullOrEmpty(deviceId))
        {
            var loopback = new WasapiLoopbackCapture();
            return new NAudioAudioInput(loopback);
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
                    return new NAudioAudioInput(capture);
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
                    return new NAudioAudioInput(loopback);
                }
            }
        }
        return new NAudioAudioInput(new WasapiLoopbackCapture());
    }
}
