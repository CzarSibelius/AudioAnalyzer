namespace AudioAnalyzer.Application.Abstractions;

/// <summary>
/// UI component for a single row of labeled viewports. Carries viewports, cell widths, and owns per-slot scroll state.
/// In the tree, rendered via <see cref="IUiComponentRenderer{TComponent}"/> for <see cref="LabeledRowComponent"/>; <see cref="ILabeledRowRenderer"/> remains for non-tree use (e.g. modal, toolbar).
/// </summary>
public sealed class LabeledRowComponent : IUiComponent
{
    private readonly List<LabeledRowSlotState> _slotStates = [];
    private IReadOnlyList<Viewport> _viewports = [];
    private IReadOnlyList<int> _widths = [];

    /// <summary>Viewports (label + value getter) in order.</summary>
    public IReadOnlyList<Viewport> Viewports => _viewports;

    /// <summary>Width in columns for each cell. Must match <see cref="Viewports"/>.Count.</summary>
    public IReadOnlyList<int> Widths => _widths;

    /// <summary>Starting index for scroll state slots when rendered via <see cref="ILabeledRowRenderer.RenderRow"/> (non-tree use).</summary>
    public int StartSlotIndex { get; }

    /// <summary>Creates a labeled row component.</summary>
    public LabeledRowComponent(
        IReadOnlyList<Viewport> viewports,
        IReadOnlyList<int> widths,
        int startSlotIndex = 0)
    {
        _viewports = viewports ?? throw new ArgumentNullException(nameof(viewports));
        _widths = widths ?? throw new ArgumentNullException(nameof(widths));
        StartSlotIndex = startSlotIndex;
    }

    /// <summary>Updates row data for stable component instances. Used by containers that reuse the same component each frame.</summary>
    public void SetRowData(IReadOnlyList<Viewport> viewports, IReadOnlyList<int> widths)
    {
        _viewports = viewports ?? throw new ArgumentNullException(nameof(viewports));
        _widths = widths ?? throw new ArgumentNullException(nameof(widths));
    }

    /// <summary>Gets or creates slot state for the given 0-based cell index. Used by the renderer when rendering this component.</summary>
    public LabeledRowSlotState GetSlotState(int index)
    {
        while (_slotStates.Count <= index)
        {
            _slotStates.Add(new LabeledRowSlotState());
        }
        return _slotStates[index];
    }

    /// <inheritdoc />
    public IReadOnlyList<IUiComponent>? GetChildren(RenderContext context) => null;
}
