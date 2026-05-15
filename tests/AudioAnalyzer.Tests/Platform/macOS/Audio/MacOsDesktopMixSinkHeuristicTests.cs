using AudioAnalyzer.Platform.macOS.Audio;
using Xunit;

namespace AudioAnalyzer.Tests.Platform.macOS.Audio;

public sealed class MacOsDesktopMixSinkHeuristicTests
{
    [Theory]
    [InlineData("BlackHole 2ch", "uid")]
    [InlineData("Mic", "com.existential.blackhole.uid")]
    [InlineData("Rogue Amoeba Loopback", "x")]
    public void LooksLikeDesktopMixSink_Positive(string hardwareName, string uid)
    {
        Assert.True(MacOsDesktopMixSinkHeuristic.LooksLikeDesktopMixSink(hardwareName, uid));
    }

    [Fact]
    public void LooksLikeDesktopMixSink_GenericMic_ReturnsFalse()
    {
        Assert.False(MacOsDesktopMixSinkHeuristic.LooksLikeDesktopMixSink("MacBook Pro Microphone", "builtin-mic"));
    }
}
