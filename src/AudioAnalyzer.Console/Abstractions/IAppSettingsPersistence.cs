namespace AudioAnalyzer.Console;

/// <summary>Persists application and visualizer settings to storage.</summary>
internal interface IAppSettingsPersistence
{
    /// <summary>Saves current app and visualizer settings.</summary>
    void Save();
}
