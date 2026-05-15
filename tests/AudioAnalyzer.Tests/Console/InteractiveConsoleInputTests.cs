using Xunit;

namespace AudioAnalyzer.Tests.Console;

/// <summary>Tests for <see cref="AudioAnalyzer.Console.InteractiveConsoleInput"/>.</summary>
public sealed class InteractiveConsoleInputTests
{
    [Fact]
    public void IsSupported_does_not_throw()
    {
        bool supported = AudioAnalyzer.Console.InteractiveConsoleInput.IsSupported;
        Assert.True(supported || !supported);
    }

    [Fact]
    public void KeyAvailable_does_not_throw()
    {
        _ = AudioAnalyzer.Console.InteractiveConsoleInput.KeyAvailable;
    }
}
