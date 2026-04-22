using AudioAnalyzer.Infrastructure.Logging;
using Xunit;

namespace AudioAnalyzer.Tests.Infrastructure;

public sealed class LogFilePathResolverTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Resolve_empty_uses_default_under_base_with_process_id(string? filePath)
    {
        string baseDir = OperatingSystem.IsWindows() ? @"C:\app" : "/app";
        string pid = Environment.ProcessId.ToString(System.Globalization.CultureInfo.InvariantCulture);
        string expected = Path.GetFullPath(Path.Combine(baseDir, "logs", $"audioanalyzer-{pid}.log"));
        string path = LogFilePathResolver.Resolve(filePath, baseDir);
        Assert.Equal(expected, path);
        Assert.Contains(pid, path, StringComparison.Ordinal);
    }

    [Fact]
    public void Resolve_relative_combines_with_base()
    {
        string baseDir = OperatingSystem.IsWindows() ? @"C:\app" : "/app";
        string expected = Path.GetFullPath(Path.Combine(baseDir, "my.log"));
        string path = LogFilePathResolver.Resolve("my.log", baseDir);
        Assert.Equal(expected, path);
    }

    [Fact]
    public void Resolve_relative_with_process_id_placeholder_contains_process_id()
    {
        string baseDir = OperatingSystem.IsWindows() ? @"C:\app" : "/app";
        string pid = Environment.ProcessId.ToString(System.Globalization.CultureInfo.InvariantCulture);
        string path = LogFilePathResolver.Resolve("logs/diag-{ProcessId}.log", baseDir);
        Assert.Contains(pid, path, StringComparison.Ordinal);
        Assert.True(Path.IsPathRooted(path));
        string normalized = path.Replace('\\', '/');
        Assert.Contains("logs", normalized, StringComparison.Ordinal);
        Assert.Contains($"diag-{pid}.log", normalized, StringComparison.Ordinal);
    }

    [Fact]
    public void Resolve_absolute_with_process_id_placeholder_is_rooted_and_contains_process_id()
    {
        string pid = Environment.ProcessId.ToString(System.Globalization.CultureInfo.InvariantCulture);
        string absoluteTemplate = OperatingSystem.IsWindows()
            ? @"C:\tmp\logs\app-{ProcessId}.log"
            : "/tmp/logs/app-{ProcessId}.log";
        string ignoredBase = OperatingSystem.IsWindows() ? @"C:\app" : "/app";
        string path = LogFilePathResolver.Resolve(absoluteTemplate, ignoredBase);

        Assert.True(Path.IsPathRooted(path));
        Assert.Contains(pid, path, StringComparison.Ordinal);
        Assert.EndsWith($"{pid}.log", path, StringComparison.Ordinal);
    }
}
