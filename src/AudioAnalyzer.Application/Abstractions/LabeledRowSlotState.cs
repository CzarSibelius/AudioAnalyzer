namespace AudioAnalyzer.Application.Abstractions;

/// <summary>
/// Per-slot state for a labeled row component (scroll position and last text for content-change detection).
/// Owned by <see cref="LabeledRowComponent"/>.
/// </summary>
public sealed class LabeledRowSlotState
{
    private ScrollingTextViewportState _scrollState;

    /// <summary>Current scroll state for this slot.</summary>
    public ScrollingTextViewportState ScrollState
    {
        get => _scrollState;
        set => _scrollState = value;
    }

    /// <summary>Returns a reference to the scroll state for use with <see cref="IScrollingTextEngine.GetVisibleSlice"/>.</summary>
    public ref ScrollingTextViewportState GetScrollStateRef() => ref _scrollState;

    /// <summary>Last rendered text value; when it changes, scroll is reset.</summary>
    public string? LastText { get; set; }
}
