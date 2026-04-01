using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Visualizers;

/// <summary>
/// Resolves effective filesystem folders for AsciiImage / AsciiModel using optional
/// <see cref="UiSettings.DefaultAssetFolderPath"/> and per-layer folder settings.
/// </summary>
public static class LayerAssetFolder
{
    /// <summary>
    /// Global base directory: <see cref="UiSettings.DefaultAssetFolderPath"/> when set (full path), otherwise <c>AppContext.BaseDirectory</c>.
    /// </summary>
    public static string ResolveGlobalBase(UiSettings? uiSettings)
    {
        string? custom = uiSettings?.DefaultAssetFolderPath?.Trim();
        if (string.IsNullOrWhiteSpace(custom))
        {
            return Path.GetFullPath(AppContext.BaseDirectory);
        }

        return Path.GetFullPath(custom);
    }

    /// <summary>
    /// Effective folder for layer asset enumeration: empty layer path uses global base; rooted layer path is normalized; relative paths combine with global base.
    /// </summary>
    public static string ResolveEffectiveFolder(string? layerFolderPath, UiSettings? uiSettings)
    {
        string globalBase = ResolveGlobalBase(uiSettings);
        if (string.IsNullOrWhiteSpace(layerFolderPath))
        {
            return globalBase;
        }

        string trimmed = layerFolderPath.Trim();
        if (Path.IsPathRooted(trimmed))
        {
            return Path.GetFullPath(trimmed);
        }

        return Path.GetFullPath(Path.Combine(globalBase, trimmed));
    }
}
