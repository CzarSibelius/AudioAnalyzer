using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Application.Abstractions;

/// <summary>Stateful viewport that renders scrolling text with optional labels and ANSI colors. Resets scroll when content changes.</summary>
public interface IScrollingTextViewport
{
    /// <summary>Formats a label as "Label:" or empty when label is null/empty.</summary>
    string FormatLabel(string? label);

    /// <summary>Renders scrollable text. Advances scroll state; resets when text changes.</summary>
    /// <param name="frameDeltaSeconds">Wall-clock seconds since last scroll tick (ADR-0072).</param>
    string Render<T>(T text, int width, double speedPerReferenceFrame, double frameDeltaSeconds)
        where T : IDisplayText;

    /// <summary>Renders a label followed by scrollable content. Label is static; text scrolls when it overflows.</summary>
    string RenderWithLabel<T>(string label, T text, int totalWidth, double speedPerReferenceFrame, double frameDeltaSeconds,
        PaletteColor? labelColor = null, PaletteColor? textColor = null)
        where T : IDisplayText;
}
