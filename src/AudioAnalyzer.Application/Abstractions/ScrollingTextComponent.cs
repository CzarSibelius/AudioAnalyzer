using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Application.Abstractions;

/// <summary>
/// Leaf UI component for a single scrolling text cell. Owns scroll state and last-text for content-change reset.
/// Cell data (label, getter, colors) is set each frame via <see cref="SetFromDescriptor"/> when used in a horizontal row.
/// </summary>
public sealed class ScrollingTextComponent : IUiComponent
{
    private ScrollingTextViewportState _scrollState;
    private string? _lastText;

    private string _label = "";
    private string? _hotkey;
    private Func<IDisplayText>? _getValue;
    private PaletteColor? _labelColor;
    private PaletteColor? _textColor;
    private bool _preformattedAnsi;

    /// <summary>Label text (e.g. "Device", "Palette").</summary>
    public string Label => _label;

    /// <summary>Optional hotkey for the label (e.g. "Label(K):").</summary>
    public string? Hotkey => _hotkey;

    /// <summary>Getter for the current display value. Set by <see cref="SetFromDescriptor"/>.</summary>
    public Func<IDisplayText>? GetValue => _getValue;

    /// <summary>Optional label color.</summary>
    public PaletteColor? LabelColor => _labelColor;

    /// <summary>Optional text color.</summary>
    public PaletteColor? TextColor => _textColor;

    /// <summary>When true, value is preformatted ANSI; renderer uses truncate-with-ellipsis or scroll without applying palette colors.</summary>
    public bool PreformattedAnsi => _preformattedAnsi;

    /// <summary>Current text value last rendered; used to reset scroll when content changes. Used by the renderer.</summary>
    public string? LastText
    {
        get => _lastText;
        set => _lastText = value;
    }

    /// <summary>Scroll state for this cell. Used by the renderer.</summary>
    public ref ScrollingTextViewportState GetScrollStateRef() => ref _scrollState;

    /// <summary>Updates cell data from a labeled value descriptor. Call each frame when used in a horizontal row.</summary>
    public void SetFromDescriptor(LabeledValueDescriptor descriptor)
    {
        _label = descriptor?.Label ?? "";
        _hotkey = descriptor?.Hotkey;
        _getValue = descriptor?.GetValue;
        _labelColor = descriptor?.LabelColor;
        _textColor = descriptor?.TextColor;
        _preformattedAnsi = descriptor?.PreformattedAnsi ?? false;
    }

    /// <inheritdoc />
    public IReadOnlyList<IUiComponent>? GetChildren(RenderContext context) => null;
}
