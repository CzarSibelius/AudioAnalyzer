namespace AudioAnalyzer.Application.Display;

/// <summary>
/// Shared label formatting for viewports and toolbar. Produces "Label:" with no space before the value.
/// </summary>
public static class LabelFormatting
{
    /// <summary>Formats a label as "Label:" or empty string when label is null/empty.</summary>
    public static string FormatLabel(string? label)
    {
        var baseLabel = label ?? "";
        return string.IsNullOrEmpty(baseLabel) ? "" : baseLabel + ":";
    }
}
