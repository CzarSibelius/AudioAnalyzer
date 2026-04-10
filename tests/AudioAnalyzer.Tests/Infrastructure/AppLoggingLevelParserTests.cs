using AudioAnalyzer.Infrastructure.Logging;
using Microsoft.Extensions.Logging;
using Xunit;

namespace AudioAnalyzer.Tests.Infrastructure;

public sealed class AppLoggingLevelParserTests
{
    [Theory]
    [InlineData("Error", LogLevel.Error)]
    [InlineData("error", LogLevel.Error)]
    [InlineData("Warning", LogLevel.Warning)]
    [InlineData("Information", LogLevel.Information)]
    [InlineData("Debug", LogLevel.Debug)]
    [InlineData("Trace", LogLevel.Trace)]
    public void Parse_valid_names_returns_level(string input, LogLevel expected)
    {
        Assert.Equal(expected, AppLoggingLevelParser.Parse(input));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-level")]
    [InlineData("None")]
    public void Parse_invalid_or_empty_defaults_to_error(string? input)
    {
        Assert.Equal(LogLevel.Error, AppLoggingLevelParser.Parse(input));
    }
}
