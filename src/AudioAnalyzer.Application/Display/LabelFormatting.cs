namespace AudioAnalyzer.Application.Display;

/// <summary>
/// Shared label formatting for viewports and toolbar. Produces "Label:" or "Label(K):" when hotkey is set.
/// </summary>
public static class LabelFormatting
{
    /// <summary>Formats a label with optional hotkey. When hotkey is provided, returns "Label(K):"; otherwise "Label:".</summary>
    public static string FormatLabel(string label, string? hotkey)
    {
        var baseLabel = label ?? "";
        if (string.IsNullOrWhiteSpace(hotkey))
        {
            return string.IsNullOrEmpty(baseLabel) ? "" : baseLabel + ":";
        }
        return string.IsNullOrEmpty(baseLabel) ? "" : baseLabel + "(" + hotkey + "):";
    }
}
