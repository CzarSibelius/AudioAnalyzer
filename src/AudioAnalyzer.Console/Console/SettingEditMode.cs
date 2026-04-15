namespace AudioAnalyzer.Console;

/// <summary>Edit mode for a setting in the settings modal.</summary>
internal enum SettingEditMode
{
    Cycle,

    /// <summary>Enter opens a palette list; +/- cycle like <see cref="Cycle"/>.</summary>
    PalettePicker,

    /// <summary>Enter opens charset list (ADR-0080); +/- cycle like <see cref="Cycle"/>.</summary>
    CharsetPicker,
    TextEdit,

    /// <summary>Enter opens live visual bounds edit and closes the modal.</summary>
    BoundVisualEdit,
}
