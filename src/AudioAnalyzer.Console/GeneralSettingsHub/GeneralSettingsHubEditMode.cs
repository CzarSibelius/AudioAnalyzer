namespace AudioAnalyzer.Console;

/// <summary>Which text field is being edited in the General Settings hub.</summary>
internal enum GeneralSettingsHubEditMode
{
    /// <summary>No modal text edit.</summary>
    None,

    /// <summary>Editing title bar application name.</summary>
    ApplicationName,

    /// <summary>Editing default asset folder path.</summary>
    DefaultAssetFolder
}
