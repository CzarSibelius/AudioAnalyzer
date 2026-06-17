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
            new SubstituteTapFactory(new DummyAudioInput()));
    }

    [Fact]
    public void GetDevices_IncludesDemoModesAndPhysicalInputs()
    {
        var fake = new FakeMacOsAudioEnumerator();
        fake.PhysicalInputs.Add(new MacOsPhysicalAudioDevice("🎤 Test Mic", "uid-test", "Test Mic"));

        var info = CreateInfo(fake);
        var devices = info.GetDevices();

        Assert.Contains(devices, d => d.Id == DemoAudioDevice.Prefix + "120");
        if (OperatingSystem.IsMacOS() && OperatingSystem.IsMacOSVersionAtLeast(14, 2))
        {
            Assert.Contains(devices, d => d.Id == CrossPlatformAudioDeviceIds.MacOsCoreAudioTapSystemAudio);
        }

        Assert.Contains(devices, d => d.Id == MacOsAudioDeviceIds.EncodeInputUid("uid-test"));
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
    public void CreateCapture_CoreAudioTap_DelegatesToFactory()
    {
        var fake = new FakeMacOsAudioEnumerator();
        using var expected = new DummyAudioInput();
        var tapFactory = new SubstituteTapFactory(expected);

        var info = new MacOsAudioDeviceInfo(
            NullLogger<MacOsAudioDeviceInfo>.Instance,
            fake,
            tapFactory);
        IAudioInput resolved = info.CreateCapture(CrossPlatformAudioDeviceIds.MacOsCoreAudioTapSystemAudio);

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

    private sealed class SubstituteTapFactory : IMacOsCoreAudioTapSystemAudioInputFactory
    {
        private readonly IAudioInput _input;

        public SubstituteTapFactory(IAudioInput input) => _input = input;

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
