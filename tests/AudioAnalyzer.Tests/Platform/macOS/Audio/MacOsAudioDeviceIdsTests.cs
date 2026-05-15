using AudioAnalyzer.Platform.macOS.Audio;
using Xunit;

namespace AudioAnalyzer.Tests.Platform.macOS.Audio;

public sealed class MacOsAudioDeviceIdsTests
{
    [Fact]
    public void TryDecodeInputUid_RejectsNonPrefixed()
    {
        Assert.False(MacOsAudioDeviceIds.TryDecodeInputUid("capture:foo", out _));
        Assert.False(MacOsAudioDeviceIds.TryDecodeInputUid("", out _));
    }

    [Fact]
    public void EncodeDecode_RoundTrips_UidsWithReservedCharacters()
    {
        string uid = "x:y/z";
        string id = MacOsAudioDeviceIds.EncodeInputUid(uid);
        Assert.StartsWith(MacOsAudioDeviceIds.InputPrefix, id, StringComparison.Ordinal);
        Assert.True(MacOsAudioDeviceIds.TryDecodeInputUid(id, out string? round));
        Assert.Equal(uid, round);
    }
}
