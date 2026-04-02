using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Application.Display;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Application;

/// <summary>Stateful scrolling viewport that delegates to the engine for scroll logic and applies label/ANSI formatting.</summary>
public sealed class ScrollingTextViewport : IScrollingTextViewport
{
    private readonly IScrollingTextEngine _engine;
    private ScrollingTextViewportState _state;
    private string? _lastText;

    /// <summary>Creates a new viewport with its own scroll state.</summary>
    public ScrollingTextViewport(IScrollingTextEngine engine)
    {
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
    }

    /// <inheritdoc />
    public string FormatLabel(string? label) => LabelFormatting.FormatLabel(label);

    /// <inheritdoc />
    public string Render<T>(T text, int width, double speedPerFrame)
        where T : IDisplayText
    {
        if (text.Value != _lastText)
        {
            _state.Reset();
            _lastText = text.Value;
        }
        return _engine.GetVisibleSlice(text, width, ref _state, speedPerFrame);
    }

    /// <inheritdoc />
    public string RenderWithLabel<T>(string label, T text, int totalWidth, double speedPerFrame,
        PaletteColor? labelColor = null, PaletteColor? textColor = null)
        where T : IDisplayText
    {
        if (totalWidth <= 0)
        {
            return "";
        }

        var effectiveLabel = FormatLabel(label);
        int labelDisplayWidth = string.IsNullOrEmpty(effectiveLabel)
            ? 0
            : DisplayWidth.GetDisplayWidth(effectiveLabel);
        int scrollWidth = Math.Max(0, totalWidth - labelDisplayWidth);

        string scrollPart = string.IsNullOrEmpty(text.Value)
            ? new string(' ', scrollWidth)
            : Render(text, scrollWidth, speedPerFrame);

        string labelSegment = labelColor.HasValue
            ? AnsiConsole.ColorCode(labelColor.Value) + effectiveLabel + AnsiConsole.ResetCode
            : effectiveLabel;
        string scrollSegment = textColor.HasValue
            ? AnsiConsole.ColorCode(textColor.Value) + scrollPart + AnsiConsole.ResetCode
            : scrollPart;

        string result = labelSegment + scrollSegment;
        int displayWidth = AnsiConsole.GetDisplayWidth(result);
        return displayWidth > totalWidth
            ? AnsiConsole.GetDisplaySubstring(result, 0, totalWidth)
            : AnsiConsole.PadToDisplayWidth(result, totalWidth);
    }
}
