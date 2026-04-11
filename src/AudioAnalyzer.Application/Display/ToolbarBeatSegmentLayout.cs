using AudioAnalyzer.Application.Abstractions;

namespace AudioAnalyzer.Application.Display;

/// <summary>
/// Reserved display width for the header **Beat:** cell when audio BPM can show the <c>*BEAT*</c> flash,
/// so toolbar spread layout does not shift when the suffix toggles.
/// </summary>
public static class ToolbarBeatSegmentLayout
{
    /// <summary>Worst-case sensitivity digits plus <c>(+/-)</c> and <c>*BEAT*</c> suffix (wide chars count as in <see cref="PlainText"/>).</summary>
    public const string ReservedBeatValueExample = "99.9 (+/-) *BEAT*";

    /// <summary>Full segment display width: <c>Beat:</c> plus <see cref="ReservedBeatValueExample"/>.</summary>
    public static int ReservedBeatSegmentDisplayWidth()
    {
        string lab = LabelFormatting.FormatLabel("Beat");
        int labelCols = string.IsNullOrEmpty(lab) ? 0 : DisplayWidth.GetDisplayWidth(lab);
        int valueCols = new PlainText(ReservedBeatValueExample).GetDisplayWidth();
        return labelCols + valueCols;
    }
}
