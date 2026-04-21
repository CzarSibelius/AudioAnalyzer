namespace AudioAnalyzer.Domain;

/// <summary>
/// Preset ordering for keyboard navigation in Preset editor: ascending display name (trimmed, case-insensitive),
/// with empty names sorting by id; ties broken by id (case-insensitive).
/// </summary>
public static class PresetNavigationOrder
{
    /// <summary>Compares two presets for ascending navigation order.</summary>
    public static int CompareByDisplayNameThenId(Preset a, Preset b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        string sortKeyA = string.IsNullOrWhiteSpace(a.Name) ? a.Id : a.Name.Trim();
        string sortKeyB = string.IsNullOrWhiteSpace(b.Name) ? b.Id : b.Name.Trim();
        int c = string.Compare(sortKeyA, sortKeyB, StringComparison.OrdinalIgnoreCase);
        return c != 0 ? c : string.Compare(a.Id, b.Id, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Returns a new list sorted for navigation. Does not mutate <paramref name="presets"/>.</summary>
    public static List<Preset> SortForNavigation(IReadOnlyList<Preset> presets)
    {
        var list = presets.ToList();
        list.Sort(CompareByDisplayNameThenId);
        return list;
    }

    /// <summary>Id of the next preset after the active one in display-name order, wrapping. Null when the list is empty.</summary>
    public static string? GetNextPresetIdByDisplayName(IReadOnlyList<Preset> presets, string? activePresetId)
    {
        if (presets.Count == 0)
        {
            return null;
        }

        var sorted = SortForNavigation(presets);
        int idx = -1;
        for (int i = 0; i < sorted.Count; i++)
        {
            if (string.Equals(sorted[i].Id, activePresetId, StringComparison.OrdinalIgnoreCase))
            {
                idx = i;
                break;
            }
        }

        int nextIdx = idx < 0 ? 0 : (idx + 1) % sorted.Count;
        return sorted[nextIdx].Id;
    }

    /// <summary>Id of the preset before the active one in display-name order, wrapping. Null when the list is empty.</summary>
    public static string? GetPreviousPresetIdByDisplayName(IReadOnlyList<Preset> presets, string? activePresetId)
    {
        if (presets.Count == 0)
        {
            return null;
        }

        var sorted = SortForNavigation(presets);
        int idx = -1;
        for (int i = 0; i < sorted.Count; i++)
        {
            if (string.Equals(sorted[i].Id, activePresetId, StringComparison.OrdinalIgnoreCase))
            {
                idx = i;
                break;
            }
        }

        int prevIdx = idx < 0 ? sorted.Count - 1 : (idx - 1 + sorted.Count) % sorted.Count;
        return sorted[prevIdx].Id;
    }
}
