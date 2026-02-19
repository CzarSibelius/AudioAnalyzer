namespace AudioAnalyzer.Console;

/// <summary>Draws the application header. Encapsulates viewports, title bar, engine state, and UI settings.</summary>
internal interface IHeaderDrawer
{
    /// <summary>Clears the console and draws the full header.</summary>
    /// <param name="deviceName">Display name of the current audio input device.</param>
    void DrawMain(string deviceName);

    /// <summary>Draws only the header lines (no clear). Used for refresh before each render.</summary>
    /// <param name="deviceName">Display name of the current audio input device.</param>
    void DrawHeaderOnly(string deviceName);
}
