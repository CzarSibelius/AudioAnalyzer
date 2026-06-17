using System.IO.Abstractions;
using AudioAnalyzer.Application.Abstractions;

namespace AudioAnalyzer.Infrastructure;

/// <summary>
/// Resolves read-only shipped content and writable data directories for the console host.
/// On macOS app bundles, content lives under <c>Contents/Resources</c> while user data is under Application Support.
/// </summary>
public sealed class HostContentPaths
{
    /// <summary>Directory containing shipped palettes, presets, themes, and default appsettings.</summary>
    public string ContentRoot { get; }

    /// <summary>Directory for writable user data (settings, created presets, logs when relative).</summary>
    public string WritableDataRoot { get; }

    /// <summary>Path to the persisted <c>appsettings.json</c> file.</summary>
    public string SettingsFilePath { get; }

    /// <summary>Writable presets directory (seeded from <see cref="ContentRoot"/> on first run when needed).</summary>
    public string PresetsDirectory { get; }

    /// <summary>Writable palettes directory.</summary>
    public string PalettesDirectory { get; }

    /// <summary>Writable themes directory.</summary>
    public string ThemesDirectory { get; }

    /// <summary>Writable charsets directory.</summary>
    public string CharsetsDirectory { get; }

    /// <summary>Writable shows directory.</summary>
    public string ShowsDirectory { get; }

    private HostContentPaths(
        string contentRoot,
        string writableDataRoot,
        string settingsFilePath,
        string presetsDirectory,
        string palettesDirectory,
        string themesDirectory,
        string charsetsDirectory,
        string showsDirectory)
    {
        ContentRoot = contentRoot;
        WritableDataRoot = writableDataRoot;
        SettingsFilePath = settingsFilePath;
        PresetsDirectory = presetsDirectory;
        PalettesDirectory = palettesDirectory;
        ThemesDirectory = themesDirectory;
        CharsetsDirectory = charsetsDirectory;
        ShowsDirectory = showsDirectory;
    }

    /// <summary>Resolves paths for the current process and ensures writable folders exist.</summary>
    /// <param name="fileSystem">File system abstraction for directory/file operations.</param>
    /// <param name="contentLocator">Platform content locator; when it resolves roots they override the base directory.</param>
    public static HostContentPaths Resolve(IFileSystem fileSystem, IHostContentLocator contentLocator)
    {
        ArgumentNullException.ThrowIfNull(fileSystem);
        ArgumentNullException.ThrowIfNull(contentLocator);

        string baseDirectory = AppContext.BaseDirectory;
        string contentRoot = baseDirectory;
        string writableRoot = baseDirectory;

        if (contentLocator.TryGetContentRoots(out string locatedContentRoot, out string locatedWritableRoot))
        {
            contentRoot = locatedContentRoot;
            writableRoot = locatedWritableRoot;
        }

        fileSystem.Directory.CreateDirectory(writableRoot);

        string presetsDirectory = Path.Combine(writableRoot, "presets");
        string palettesDirectory = Path.Combine(writableRoot, "palettes");
        string themesDirectory = Path.Combine(writableRoot, "themes");
        string charsetsDirectory = Path.Combine(writableRoot, "charsets");
        string showsDirectory = Path.Combine(writableRoot, "shows");
        string settingsPath = Path.Combine(writableRoot, "appsettings.json");

        fileSystem.Directory.CreateDirectory(presetsDirectory);
        fileSystem.Directory.CreateDirectory(palettesDirectory);
        fileSystem.Directory.CreateDirectory(themesDirectory);
        fileSystem.Directory.CreateDirectory(charsetsDirectory);
        fileSystem.Directory.CreateDirectory(showsDirectory);

        SeedDirectoryIfEmpty(fileSystem, Path.Combine(contentRoot, "presets"), presetsDirectory);
        SeedDirectoryIfEmpty(fileSystem, Path.Combine(contentRoot, "palettes"), palettesDirectory);
        SeedDirectoryIfEmpty(fileSystem, Path.Combine(contentRoot, "themes"), themesDirectory);
        SeedDirectoryIfEmpty(fileSystem, Path.Combine(contentRoot, "charsets"), charsetsDirectory);

        if (!fileSystem.File.Exists(settingsPath))
        {
            string bundledDefault = Path.Combine(contentRoot, "appsettings.json");
            if (fileSystem.File.Exists(bundledDefault))
            {
                fileSystem.File.Copy(bundledDefault, settingsPath, overwrite: false);
            }
        }

        return new HostContentPaths(
            contentRoot,
            writableRoot,
            settingsPath,
            presetsDirectory,
            palettesDirectory,
            themesDirectory,
            charsetsDirectory,
            showsDirectory);
    }

    private static void SeedDirectoryIfEmpty(IFileSystem fileSystem, string sourceDirectory, string targetDirectory)
    {
        if (!fileSystem.Directory.Exists(sourceDirectory))
        {
            return;
        }

        if (fileSystem.Directory.EnumerateFileSystemEntries(targetDirectory).Any())
        {
            return;
        }

        foreach (string sourcePath in fileSystem.Directory.EnumerateFiles(sourceDirectory))
        {
            string fileName = Path.GetFileName(sourcePath);
            string destPath = Path.Combine(targetDirectory, fileName);
            if (!fileSystem.File.Exists(destPath))
            {
                fileSystem.File.Copy(sourcePath, destPath, overwrite: false);
            }
        }
    }
}
