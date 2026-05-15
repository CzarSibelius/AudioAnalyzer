using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;
using AudioAnalyzer.Infrastructure;
using AudioAnalyzer.Platform.macOS.Audio;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AudioAnalyzer.Tests.Platform.macOS.Audio;

public sealed class MacOsAudioDeviceInfoTests
{
    private static MacOsAudioDeviceInfo CreateInfo(FakeMacOsAudioEnumerator fake)
    {
        return new MacOsAudioDeviceInfo(
            NullLogger<MacOsAudioDeviceInfo>.Instance,
            fake,
            new MacOsScreenCaptureKitSystemAudioInputFactory(NullLoggerFactory.Instance));
    }

    [Fact]
    public void GetDevices_IncludesDemoModesAndPhysicalInputs()
    {
        var fake = new FakeMacOsAudioEnumerator();
        fake.PhysicalInputs.Add(new MacOsPhysicalAudioDevice("🎤 Test Mic", "uid-test", "Test Mic"));

        var info = CreateInfo(fake);
        var devices = info.GetDevices();

        Assert.Contains(devices, d => d.Id == DemoAudioDevice.Prefix + "120");
        Assert.Contains(devices, d => d.Id == CrossPlatformAudioDeviceIds.MacOsDesktopVirtualRouting);
        Assert.Contains(devices, d => d.Id == CrossPlatformAudioDeviceIds.MacOsScreenCaptureKitSystemAudio);
        Assert.Contains(devices, d => d.Id == MacOsAudioDeviceIds.EncodeInputUid("uid-test"));
    }

    [Fact]
    public void CreateCapture_DesktopVirtualRouting_PicksFirstHeuristicVirtualMixer()
    {
        var fake = new FakeMacOsAudioEnumerator();
        fake.PhysicalInputs.Add(new MacOsPhysicalAudioDevice("🎤 Built-in", "uid-in", "Built-in Mic"));
        fake.PhysicalInputs.Add(new MacOsPhysicalAudioDevice("🔊 BlackHole 2ch (desktop mix)", "uid-bh", "BlackHole 2ch"));
        using var dummy = new DummyAudioInput();
        fake.Captures[MacOsAudioDeviceIds.EncodeInputUid("uid-bh")] = dummy;

        var info = CreateInfo(fake);
        IAudioInput resolved = info.CreateCapture(CrossPlatformAudioDeviceIds.MacOsDesktopVirtualRouting);

        Assert.Same(dummy, resolved);
    }

    [Fact]
    public void CreateCapture_DesktopVirtualRouting_NoHeuristicMatch_FallsBackToSynthetic()
    {
        var fake = new FakeMacOsAudioEnumerator();
        fake.PhysicalInputs.Add(new MacOsPhysicalAudioDevice("🎤 Only Mic", "uid-in", "Built-in Mic"));

        var info = CreateInfo(fake);
        using var input = info.CreateCapture(CrossPlatformAudioDeviceIds.MacOsDesktopVirtualRouting);

        Assert.IsType<SyntheticAudioInput>(input);
    }

    [Fact]
    public void CreateCapture_NullDeviceId_ReturnsSynthetic120()
    {
        var fake = new FakeMacOsAudioEnumerator();
        var info = CreateInfo(fake);

        using var input = info.CreateCapture(null);
        Assert.IsType<SyntheticAudioInput>(input);
    }

    [Fact]
    public void CreateCapture_EncodedPhysical_DelegatesToEnumerator()
    {
        var fake = new FakeMacOsAudioEnumerator();
        using var dummy = new DummyAudioInput();
        string id = MacOsAudioDeviceIds.EncodeInputUid("uid-x");
        fake.Captures[id] = dummy;

        var info = CreateInfo(fake);
        IAudioInput resolved = info.CreateCapture(id);
        Assert.Same(dummy, resolved);
    }

    [Fact]
    public void CreateCapture_ScreenCaptureKit_DelegatesToFactory()
    {
        var fake = new FakeMacOsAudioEnumerator();
        using var expected = new DummyAudioInput();
        var factory = new SubstituteSckFactory(expected);

        var info = new MacOsAudioDeviceInfo(NullLogger<MacOsAudioDeviceInfo>.Instance, fake, factory);
        IAudioInput resolved = info.CreateCapture(CrossPlatformAudioDeviceIds.MacOsScreenCaptureKitSystemAudio);

        Assert.Same(expected, resolved);
    }

    [Fact]
    public void CreateCapture_UnknownMacOsId_FallsBackToSynthetic()
    {
        var fake = new FakeMacOsAudioEnumerator();
        var info = CreateInfo(fake);

        using var input = info.CreateCapture(MacOsAudioDeviceIds.EncodeInputUid("missing"));
        Assert.IsType<SyntheticAudioInput>(input);
    }

    private sealed class SubstituteSckFactory : IMacOsScreenCaptureKitSystemAudioInputFactory
    {
        private readonly IAudioInput _input;

        public SubstituteSckFactory(IAudioInput input) => _input = input;

        public IAudioInput Create() => _input;
    }

    private sealed class DummyAudioInput : IAudioInput
    {
#pragma warning disable CS0067 // Required by IAudioInput; test double does not raise callbacks.
        public event EventHandler<AudioDataAvailableEventArgs>? DataAvailable;
#pragma warning restore CS0067

        public void Start()
        {
        }

        public void StopCapture()
        {
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
