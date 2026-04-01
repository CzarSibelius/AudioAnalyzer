using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Visualizers;

/// <summary>Enumerates image/OBJ paths for AsciiImage and AsciiModel. Shared so toolbar and layers use the same ordering.</summary>
public static class FileBasedLayerAssetPaths
{
    private static readonly string[] s_imageExtensions = [".bmp", ".gif", ".jpg", ".jpeg", ".png", ".webp"];
    private static readonly string[] s_objExtensions = [".obj"];

    /// <summary>Returns sorted full paths to supported images under <paramref name="folderPath"/>, or empty if missing/invalid.</summary>
    public static List<string> GetSortedImagePaths(string? folderPath) =>
        GetSortedImagePaths(folderPath, uiSettings: null);

    /// <summary>Returns sorted full paths to supported images under the effective folder (layer path + optional <see cref="UiSettings.DefaultAssetFolderPath"/>).</summary>
    public static List<string> GetSortedImagePaths(string? folderPath, UiSettings? uiSettings)
    {
        return EnumerateFiles(LayerAssetFolder.ResolveEffectiveFolder(folderPath, uiSettings), s_imageExtensions);
    }

    /// <summary>Returns sorted full paths to .obj files under <paramref name="folderPath"/>, or empty if missing/invalid.</summary>
    public static List<string> GetSortedObjPaths(string? folderPath) =>
        GetSortedObjPaths(folderPath, uiSettings: null);

    /// <summary>Returns sorted full paths to .obj files under the effective folder (layer path + optional <see cref="UiSettings.DefaultAssetFolderPath"/>).</summary>
    public static List<string> GetSortedObjPaths(string? folderPath, UiSettings? uiSettings)
    {
        return EnumerateFiles(LayerAssetFolder.ResolveEffectiveFolder(folderPath, uiSettings), s_objExtensions);
    }

    /// <summary>Resolves <paramref name="fileName"/> to an index in <paramref name="sortedFullPaths"/> (file-name match, case-insensitive). Returns 0 when the list is empty, <paramref name="fileName"/> is null/whitespace, or no match.</summary>
    public static int ResolveIndexByFileName(IReadOnlyList<string> sortedFullPaths, string? fileName)
    {
        if (sortedFullPaths == null || sortedFullPaths.Count == 0 || string.IsNullOrWhiteSpace(fileName))
        {
            return 0;
        }

        var trimmed = fileName.Trim();
        for (int i = 0; i < sortedFullPaths.Count; i++)
        {
            if (string.Equals(Path.GetFileName(sortedFullPaths[i]), trimmed, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return 0;
    }

    /// <summary>File name of the next asset after the one matching <paramref name="currentFileName"/> (wraps). Returns null if <paramref name="sortedFullPaths"/> is empty.</summary>
    public static string? NextFileNameAfter(IReadOnlyList<string> sortedFullPaths, string? currentFileName)
    {
        if (sortedFullPaths == null || sortedFullPaths.Count == 0)
        {
            return null;
        }

        int idx = ResolveIndexByFileName(sortedFullPaths, currentFileName);
        int next = (idx + 1) % sortedFullPaths.Count;
        return Path.GetFileName(sortedFullPaths[next]);
    }

    /// <summary>Advances persisted selection for an AsciiImage or AsciiModel layer. Returns false if the layer type is not file-based or the folder has no assets.</summary>
    public static bool TryAdvanceDirectoryAssetSelection(TextLayerSettings layer) =>
        TryAdvanceDirectoryAssetSelection(layer, uiSettings: null);

    /// <inheritdoc cref="TryAdvanceDirectoryAssetSelection(TextLayerSettings)"/>
    public static bool TryAdvanceDirectoryAssetSelection(TextLayerSettings layer, UiSettings? uiSettings)
    {
        ArgumentNullException.ThrowIfNull(layer);

        if (layer.LayerType == TextLayerType.AsciiImage)
        {
            var s = layer.GetCustom<AsciiImageSettings>() ?? new AsciiImageSettings();
            var paths = GetSortedImagePaths(s.ImageFolderPath, uiSettings);
            if (paths.Count == 0)
            {
                return false;
            }

            var next = NextFileNameAfter(paths, s.SelectedImageFileName);
            if (next == null)
            {
                return false;
            }

            s.SelectedImageFileName = next;
            layer.SetCustom(s);
            return true;
        }

        if (layer.LayerType == TextLayerType.AsciiModel)
        {
            var s = layer.GetCustom<AsciiModelSettings>() ?? new AsciiModelSettings();
            var paths = GetSortedObjPaths(s.ModelFolderPath, uiSettings);
            if (paths.Count == 0)
            {
                return false;
            }

            var next = NextFileNameAfter(paths, s.SelectedModelFileName);
            if (next == null)
            {
                return false;
            }

            s.SelectedModelFileName = next;
            layer.SetCustom(s);
            return true;
        }

        return false;
    }

    private static List<string> EnumerateFiles(string? folderPath, string[] extensions)
    {
        var result = new List<string>();
        if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
        {
            return result;
        }

        try
        {
            foreach (var path in Directory.EnumerateFiles(folderPath))
            {
                var ext = Path.GetExtension(path).ToLowerInvariant();
                if (ExtensionMatches(ext, extensions))
                {
                    result.Add(path);
                }
            }

            result.Sort(StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"FileBasedLayerAssetPaths: failed to enumerate {folderPath}: {ex.Message}");
        }

        return result;
    }

    private static bool ExtensionMatches(string extensionLower, string[] allowed)
    {
        foreach (var a in allowed)
        {
            if (a == extensionLower)
            {
                return true;
            }
        }

        return false;
    }
}
