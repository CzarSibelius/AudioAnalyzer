namespace AudioAnalyzer.Application.Abstractions;

/// <summary>
/// Optional Show play metadata for the TextLayers toolbar (entry position within the active show).
/// </summary>
public interface IShowPlayToolbarInfo
{
    /// <summary>0-based index of the current show entry.</summary>
    int CurrentEntryIndex { get; }

    /// <summary>Number of entries in the active show, or 0 if none.</summary>
    int GetActiveShowEntryCount();
}
