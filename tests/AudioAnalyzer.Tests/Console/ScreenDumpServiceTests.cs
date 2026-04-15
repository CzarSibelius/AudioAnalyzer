using System.IO.Abstractions.TestingHelpers;
using AudioAnalyzer.Console;
using Xunit;

namespace AudioAnalyzer.Tests.Console;

/// <summary>Tests for screen dump (ASCII screenshot) functionality.</summary>
public sealed class ScreenDumpServiceTests
{
    private sealed class FixedScreenDumpContentProvider : IScreenDumpContentProvider
    {
        private readonly string? _content;

        public FixedScreenDumpContentProvider(string? content) => _content = content;

        public string? ReadVisibleConsoleContent() => _content;
    }

    [Fact]
    public void DumpToFile_WithContent_WritesToMockFileSystem()
    {
        var fs = new MockFileSystem();
        var service = new ScreenDumpService(fs, new FixedScreenDumpContentProvider("line1"));
        const string dir = @"D:\screen-dump-test";

        string? path = service.DumpToFile(stripAnsi: true, directory: dir);

        Assert.NotNull(path);
        Assert.True(fs.File.Exists(path), "Dump file should exist in mock FS when path is returned");
        Assert.Equal("line1", fs.File.ReadAllText(path!));
    }

    [Fact]
    public void DumpToFile_StripsAnsiSequences()
    {
        var fs = new MockFileSystem();
        var service = new ScreenDumpService(fs, new FixedScreenDumpContentProvider("\x1b[31mZ\x1b[0m"));
        const string dir = @"D:\screen-dump-ansi";

        string? path = service.DumpToFile(stripAnsi: true, directory: dir);

        Assert.NotNull(path);
        Assert.Equal("Z", fs.File.ReadAllText(path!));
    }

    [Fact]
    public void DumpToFile_NoContent_ReturnsNull_DoesNotThrow()
    {
        var fs = new MockFileSystem();
        var service = new ScreenDumpService(fs, new FixedScreenDumpContentProvider(null));

        string? path = service.DumpToFile();

        Assert.Null(path);
    }
}
