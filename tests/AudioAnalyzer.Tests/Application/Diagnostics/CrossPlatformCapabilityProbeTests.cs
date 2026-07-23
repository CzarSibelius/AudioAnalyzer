using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Application.Diagnostics;
using Xunit;

namespace AudioAnalyzer.Tests.Application.Diagnostics;

/// <summary>Tests availability mapping of the cross-platform managed capability probes.</summary>
public sealed class CrossPlatformCapabilityProbeTests
{
    private sealed class FakeAudioDeviceInfo : IAudioDeviceInfo
    {
        private readonly IReadOnlyList<AudioDeviceEntry> _devices;
        private readonly bool _throw;

        public FakeAudioDeviceInfo(IReadOnlyList<AudioDeviceEntry> devices, bool throwOnEnumerate = false)
        {
            _devices = devices;
            _throw = throwOnEnumerate;
        }

        public IReadOnlyList<AudioDeviceEntry> GetDevices() =>
            _throw ? throw new InvalidOperationException("boom") : _devices;

        public IAudioInput CreateCapture(string? deviceId) => throw new NotSupportedException();
    }

    private sealed class FakeLinkSession : ILinkSession
    {
        public FakeLinkSession(bool isAvailable)
        {
            IsAvailable = isAvailable;
        }

        public bool IsAvailable { get; }

        public bool IsEnabled => false;

        public void SetEnabled(bool enabled)
        {
        }

        public void Capture(out double tempoBpm, out int numPeers, out double beat, double quantum)
        {
            tempoBpm = 0;
            numPeers = 0;
            beat = 0;
        }

        public void Dispose()
        {
        }
    }

    [Fact]
    public void AudioCapture_Available_WhenDevicesPresent()
    {
        var probe = new AudioCaptureCapabilityProbe(
            new FakeAudioDeviceInfo([new AudioDeviceEntry { Name = "Speakers", Id = "1" }]));

        var status = Assert.Single(probe.Probe());

        Assert.Equal(FeatureCapabilityIds.AudioCapture, status.Id);
        Assert.Equal(FeatureAvailability.Available, status.Availability);
        Assert.Equal("Speakers", status.Detail);
        Assert.Equal(FeatureCapabilityCategory.Audio, status.Category);
    }

    [Fact]
    public void AudioCapture_Unavailable_WhenNoDevices()
    {
        var probe = new AudioCaptureCapabilityProbe(new FakeAudioDeviceInfo([]));

        var status = Assert.Single(probe.Probe());

        Assert.Equal(FeatureAvailability.Unavailable, status.Availability);
    }

    [Fact]
    public void AudioCapture_Unavailable_WhenEnumerationThrows()
    {
        var probe = new AudioCaptureCapabilityProbe(new FakeAudioDeviceInfo([], throwOnEnumerate: true));

        var status = Assert.Single(probe.Probe());

        Assert.Equal(FeatureAvailability.Unavailable, status.Availability);
    }

    [Theory]
    [InlineData(true, FeatureAvailability.Available)]
    [InlineData(false, FeatureAvailability.Unavailable)]
    public void AbletonLink_MapsFromLinkSessionAvailability(bool linkAvailable, FeatureAvailability expected)
    {
        var probe = new AbletonLinkCapabilityProbe(new FakeLinkSession(linkAvailable));

        var status = Assert.Single(probe.Probe());

        Assert.Equal(FeatureCapabilityIds.AbletonLink, status.Id);
        Assert.Equal(expected, status.Availability);
        Assert.Equal(FeatureCapabilityCategory.Integration, status.Category);
    }
}
