using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Application.Abstractions;

/// <summary>Stateful viewport that renders scrolling text with optional labels and ANSI colors. Resets scroll when content changes.</summary>
public interface IScrollingTextViewport
{
    /// <summary>Formats a label with optional hotkey. When hotkey is provided, returns "Label (K): "; otherwise "Label: ".</summary>
    string FormatLabel(string label, string? hotkey);

    /// <summary>Renders scrollable text. Advances scroll state; resets when text changes.</summary>
    string Render<T>(T text, int width, double speedPerFrame)
        where T : IDisplayText;

    /// <summary>Renders a label followed by scrollable content. Label is static; text scrolls when it overflows.</summary>
    string RenderWithLabel<T>(string label, T text, int totalWidth, double speedPerFrame,
        PaletteColor? labelColor = null, PaletteColor? textColor = null, string? hotkey = null)
        where T : IDisplayText;
}
