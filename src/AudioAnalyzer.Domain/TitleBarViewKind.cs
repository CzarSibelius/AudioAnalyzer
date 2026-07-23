namespace AudioAnalyzer.Domain;

/// <summary>
/// Which surface is drawing the title breadcrumb (see ADR-0060). Drives path shape together with <see cref="ApplicationMode"/>.
/// </summary>
public enum TitleBarViewKind
{
    /// <summary>Main header: full path including layer when in Preset/Show mode.</summary>
    Main,

    /// <summary>Preset/layer settings modal (S).</summary>
    PresetSettingsModal,

    /// <summary>Show edit modal in Show play mode.</summary>
    ShowEditModal,

    /// <summary>Help modal (H).</summary>
    HelpModal,

    /// <summary>Audio input device selection.</summary>
    DeviceAudioInputModal,

    /// <summary>Layer picker overlay (L) in Preset editor.</summary>
    LayerPickerModal,

    /// <summary>Reserved for a future full-screen settings hub (<see cref="ApplicationMode.Settings"/>).</summary>
    SettingsHub,

    /// <summary>Reusable yes/no confirmation modal (e.g. quit confirmation); app-name track plus a suffix (ADR-0093).</summary>
    ConfirmationModal
}
