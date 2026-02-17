namespace AudioAnalyzer.Tests;

/// <summary>Creates a temporary directory and deletes it on dispose.</summary>
public sealed class TempDirectory : IDisposable
{
    public string RootPath { get; }
    public string PresetsPath => Path.Combine(RootPath, "presets");
    public string PalettesPath => Path.Combine(RootPath, "palettes");

    public TempDirectory()
    {
        RootPath = Path.Combine(Path.GetTempPath(), "AudioAnalyzer.Tests." + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(RootPath);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(RootPath))
            {
                Directory.Delete(RootPath, recursive: true);
            }
        }
        catch
        {
            /* Best-effort cleanup */
        }
    }
}
