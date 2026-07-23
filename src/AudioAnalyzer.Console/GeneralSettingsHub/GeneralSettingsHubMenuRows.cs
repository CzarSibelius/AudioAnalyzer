namespace AudioAnalyzer.Console;

/// <summary>Row indices for the General Settings hub menu (single source of truth).</summary>
internal static class GeneralSettingsHubMenuRows
{
    public const int Audio = 0;
    public const int BpmSource = 1;
    public const int ApplicationName = 2;
    public const int MaxAudioHistorySeconds = 3;
    public const int DefaultAssetFolder = 4;
    public const int UiTheme = 5;
    public const int ShowRenderFps = 6;
    public const int ShowLayerRenderTime = 7;

    /// <summary>Number of menu rows (for wrap).</summary>
    public const int Count = 8;

    /// <summary>
    /// Wraps a selection index by <paramref name="delta"/> within the selectable menu rows. The result
    /// is always in <c>[0, <see cref="Count"/>)</c>, so navigation never lands on the read-only Feature
    /// status section rendered below the menu (ADR-0095).
    /// </summary>
    public static int WrapSelection(int index, int delta) =>
        ((index + delta) % Count + Count) % Count;
}
