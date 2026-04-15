using System.Globalization;
using System.IO.Abstractions;
using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Visualizers;

/// <summary>Builds optional toolbar label/value rows for the palette-cycled TextLayers layer.</summary>
public static class LayerToolbarContextualRows
{
    private static readonly LayerToolbarContextualRow[] s_empty = [];

    /// <summary>Resolves contextual rows for <paramref name="layer"/> using <paramref name="snippetIndex"/> for file-based layers.</summary>
    public static IReadOnlyList<LayerToolbarContextualRow> Resolve(
        TextLayerSettings layer,
        int snippetIndex,
        IFileSystem fileSystem,
        UiSettings? uiSettings = null)
    {
        ArgumentNullException.ThrowIfNull(fileSystem);
        return layer.LayerType switch
        {
            TextLayerType.Oscilloscope => ResolveOscilloscope(layer),
            TextLayerType.WaveformStrip => ResolveWaveformStrip(layer),
            TextLayerType.AsciiImage => ResolveAsciiImage(layer, snippetIndex, uiSettings, fileSystem),
            TextLayerType.AsciiModel => ResolveAsciiModel(layer, snippetIndex, uiSettings, fileSystem),
            _ => s_empty
        };
    }

    private static IReadOnlyList<LayerToolbarContextualRow> ResolveOscilloscope(TextLayerSettings layer)
    {
        double gain = layer.GetCustom<OscilloscopeSettings>()?.Gain ?? 2.5;
        string value = gain.ToString("F1", CultureInfo.InvariantCulture);
        return [new LayerToolbarContextualRow("Gain", value)];
    }

    private static IReadOnlyList<LayerToolbarContextualRow> ResolveWaveformStrip(TextLayerSettings layer)
    {
        double gain = layer.GetCustom<WaveformStripSettings>()?.Gain ?? 2.5;
        string value = gain.ToString("F1", CultureInfo.InvariantCulture);
        return [new LayerToolbarContextualRow("Gain", value)];
    }

    private static IReadOnlyList<LayerToolbarContextualRow> ResolveAsciiImage(
        TextLayerSettings layer,
        int snippetIndex,
        UiSettings? uiSettings,
        IFileSystem fileSystem)
    {
        _ = snippetIndex;
        var s = layer.GetCustom<AsciiImageSettings>() ?? new AsciiImageSettings();
        var paths = FileBasedLayerAssetPaths.GetSortedImagePaths(s.ImageFolderPath, uiSettings, fileSystem);
        if (paths.Count == 0)
        {
            return [new LayerToolbarContextualRow("Image", "No images")];
        }

        int i = FileBasedLayerAssetPaths.ResolveIndexByFileName(paths, s.SelectedImageFileName);
        string name = Path.GetFileName(paths[i]);
        return [new LayerToolbarContextualRow("Image", string.IsNullOrEmpty(name) ? "—" : name)];
    }

    private static IReadOnlyList<LayerToolbarContextualRow> ResolveAsciiModel(
        TextLayerSettings layer,
        int snippetIndex,
        UiSettings? uiSettings,
        IFileSystem fileSystem)
    {
        _ = snippetIndex;
        var s = layer.GetCustom<AsciiModelSettings>() ?? new AsciiModelSettings();
        var paths = FileBasedLayerAssetPaths.GetSortedObjPaths(s.ModelFolderPath, uiSettings, fileSystem);
        if (paths.Count == 0)
        {
            return [new LayerToolbarContextualRow("Model", "No models")];
        }

        int i = FileBasedLayerAssetPaths.ResolveIndexByFileName(paths, s.SelectedModelFileName);
        string name = Path.GetFileName(paths[i]);
        return [new LayerToolbarContextualRow("Model", string.IsNullOrEmpty(name) ? "—" : name)];
    }
}
