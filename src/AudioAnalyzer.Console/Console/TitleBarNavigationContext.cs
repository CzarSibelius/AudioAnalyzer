using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Console;

/// <summary>Mutable navigation state for universal title breadcrumb (ADR-0060).</summary>
internal sealed class TitleBarNavigationContext : ITitleBarNavigationContext
{
    /// <inheritdoc />
    public TitleBarViewKind View { get; set; } = TitleBarViewKind.Main;

    /// <inheritdoc />
    public bool PresetSettingsPalettePickerActive { get; set; }

    /// <inheritdoc />
    public int? PresetSettingsLayerOneBased { get; set; }

    /// <inheritdoc />
    public string? PresetSettingsLayerTypeRaw { get; set; }

    /// <inheritdoc />
    public string? PresetSettingsFocusedSettingId { get; set; }
}
