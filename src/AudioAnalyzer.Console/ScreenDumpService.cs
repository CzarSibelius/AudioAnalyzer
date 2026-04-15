using System.IO.Abstractions;
using System.Text.RegularExpressions;

namespace AudioAnalyzer.Console;

/// <summary>
/// Writes screen content from <see cref="IScreenDumpContentProvider"/> to a text file via <see cref="IFileSystem"/>.
/// Returns null when there is no content to write or directory/file creation fails.
/// </summary>
internal sealed class ScreenDumpService : IScreenDumpService
{
    private const string DefaultDirectoryName = "screen-dumps";

    private readonly IFileSystem _fileSystem;
    private readonly IScreenDumpContentProvider _contentProvider;

    /// <summary>Initializes a new instance of the <see cref="ScreenDumpService"/> class.</summary>
    public ScreenDumpService(IFileSystem fileSystem, IScreenDumpContentProvider contentProvider)
    {
        _fileSystem = fileSystem;
        _contentProvider = contentProvider;
    }

    /// <inheritdoc />
    public string? DumpToFile(bool stripAnsi = true, string? directory = null)
    {
        string? content = _contentProvider.ReadVisibleConsoleContent();
        if (string.IsNullOrEmpty(content))
        {
            return null;
        }

        if (stripAnsi)
        {
            content = StripAnsiEscapes(content);
        }

        string dir = directory ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DefaultDirectoryName);
        try
        {
            _fileSystem.Directory.CreateDirectory(dir);
        }
        catch (Exception)
        {
            return null;
        }

        string fileName = $"screen-{DateTime.Now:yyyyMMdd-HHmmss}.txt";
        string path = Path.Combine(dir, fileName);
        try
        {
            _fileSystem.File.WriteAllText(path, content);
            return path;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static string StripAnsiEscapes(string text)
    {
        // Remove CSI sequences: \x1b[ ... final byte (e.g. m, H, J, K)
        return Regex.Replace(text, @"\x1b\[[\x20-\x3f]*[\x40-\x7e]", "");
    }
}
