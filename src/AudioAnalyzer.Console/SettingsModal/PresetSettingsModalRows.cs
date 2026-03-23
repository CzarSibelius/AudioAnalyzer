using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Console;

/// <summary>Fixed settings rows for the Preset line in the S modal (name + default palette).</summary>
internal static class PresetSettingsModalRows
{
    /// <summary>Text-edit row for active preset display name.</summary>
    public const string PresetNameId = "PresetName";

    /// <summary>Palette picker row for <see cref="TextLayersVisualizerSettings.PaletteId"/>.</summary>
    public const string DefaultPaletteId = "DefaultPalette";

    /// <summary>Builds preset-level rows (not layer reflection).</summary>
    public static List<(string Id, string Label, string DisplayValue, SettingEditMode EditMode)> Build(
        VisualizerSettings vs,
        TextLayersVisualizerSettings textLayers,
        IPaletteRepository paletteRepo)
    {
        string name = "";
        if (vs.Presets is { Count: > 0 } && !string.IsNullOrWhiteSpace(vs.ActivePresetId))
        {
            var p = vs.Presets.FirstOrDefault(x => string.Equals(x.Id, vs.ActivePresetId, StringComparison.OrdinalIgnoreCase));
            name = p?.Name?.Trim() ?? "";
        }

        return
        [
            (PresetNameId, "Name", name, SettingEditMode.TextEdit),
            (DefaultPaletteId, "Default palette", SettingsSurfacesPaletteDrawing.GetPresetDefaultPaletteDisplaySummary(paletteRepo, textLayers), SettingEditMode.PalettePicker)
        ];
    }
}
