namespace AudioAnalyzer.Console;

/// <summary>Container for the application header (title bar + device/now row + BPM/volume row). DrawMain clears and draws; DrawHeaderOnly refreshes lines only.</summary>
internal interface IHeaderContainer
{
    /// <summary>Clears the console and draws the full header.</summary>
    /// <param name="deviceName">Display name of the current audio input device.</param>
    void DrawMain(string deviceName);

    /// <summary>Draws only the header lines (no clear). Used for refresh before each render.</summary>
    /// <param name="deviceName">Display name of the current audio input device.</param>
    void DrawHeaderOnly(string deviceName);
}
