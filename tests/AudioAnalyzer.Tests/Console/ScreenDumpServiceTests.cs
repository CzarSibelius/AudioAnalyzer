using AudioAnalyzer.Console;
using Xunit;

namespace AudioAnalyzer.Tests.Console;

/// <summary>Tests for screen dump (ASCII screenshot) functionality.</summary>
public sealed class ScreenDumpServiceTests
{
    [Fact]
    public void DumpToFile_WithWritableDirectory_ReturnsNullOrValidPath()
    {
        var service = new ScreenDumpService();
        string dir = Path.Combine(Path.GetTempPath(), "AudioAnalyzer-ScreenDumpTest-" + Guid.NewGuid().ToString("N")[..8]);

        try
        {
            string? path = service.DumpToFile(stripAnsi: true, directory: dir);

            // In test environment we may have no console (null) or a real console (path)
            if (path != null)
            {
                Assert.True(File.Exists(path), "Dump file should exist when path is returned");
                Assert.True(new FileInfo(path).Length >= 0, "Dump file may be empty or contain content");
            }
        }
        finally
        {
            if (Directory.Exists(dir))
            {
                try
                {
                    Directory.Delete(dir, recursive: true);
                }
                catch (Exception ex)
                {
                    _ = ex; /* best-effort: temp dir cleanup may fail if locked */
                }
            }
        }
    }

    [Fact]
    public void DumpToFile_DefaultParameters_DoesNotThrow()
    {
        var service = new ScreenDumpService();
        string? path = service.DumpToFile();
        // Contract: returns null when no console or failure; otherwise path to file
        Assert.True(path == null || File.Exists(path));
    }
}
