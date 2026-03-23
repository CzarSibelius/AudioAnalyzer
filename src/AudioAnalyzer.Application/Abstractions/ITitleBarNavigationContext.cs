using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Application.Abstractions;

/// <summary>
/// Current title-bar navigation surface for universal breadcrumb (ADR-0060). Modals set <see cref="View"/> while open and reset to <see cref="TitleBarViewKind.Main"/> on close.
/// </summary>
public interface ITitleBarNavigationContext
{
    /// <summary>Active UI surface for breadcrumb path shaping.</summary>
    TitleBarViewKind View { get; set; }

    /// <summary>When <see cref="View"/> is <see cref="TitleBarViewKind.PresetSettingsModal"/> and a focused setting segment is shown, true appends <c>/editor</c> after that segment (palette picker open).</summary>
    bool PresetSettingsPalettePickerActive { get; set; }

    /// <summary>When in preset settings modal, 1-based index of the focused layer (list order), or null if none.</summary>
    int? PresetSettingsLayerOneBased { get; set; }

    /// <summary>When in preset settings modal, layer type name for the focused layer (e.g. Fill); formatted with hacker style in the breadcrumb.</summary>
    string? PresetSettingsLayerTypeRaw { get; set; }

    /// <summary>When in preset settings modal with focus on the settings column, the id of the highlighted setting row (e.g. Palette, Speed); formatted with hacker style after <c>[n]:layerType</c>.</summary>
    string? PresetSettingsFocusedSettingId { get; set; }
}
