using AudioAnalyzer.Infrastructure.Logging;
using Xunit;

namespace AudioAnalyzer.Tests.Infrastructure;

public sealed class LogFilePathResolverTests
{
    [Fact]
    public void Resolve_null_uses_default_under_base()
    {
        string baseDir = OperatingSystem.IsWindows() ? @"C:\app" : "/app";
        string expected = Path.GetFullPath(Path.Combine(baseDir, "logs", "audioanalyzer.log"));
        string path = LogFilePathResolver.Resolve(null, baseDir);
        Assert.Equal(expected, path);
    }

    [Fact]
    public void Resolve_relative_combines_with_base()
    {
        string baseDir = OperatingSystem.IsWindows() ? @"C:\app" : "/app";
        string expected = Path.GetFullPath(Path.Combine(baseDir, "my.log"));
        string path = LogFilePathResolver.Resolve("my.log", baseDir);
        Assert.Equal(expected, path);
    }
}
