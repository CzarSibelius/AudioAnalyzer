namespace AudioAnalyzer.Application.Abstractions;

/// <summary>
/// Composite UI component that lays out <see cref="ScrollingTextComponent"/> children horizontally on one row.
/// Use <see cref="SetRowData"/> each frame to set viewports and widths; children are reused and updated.
/// </summary>
public sealed class HorizontalRowComponent : IUiComponent
{
    private readonly List<ScrollingTextComponent> _children = [];
    private IReadOnlyList<int> _widths = [];

    /// <summary>Child cell components in order. Grown as needed by <see cref="SetRowData"/>.</summary>
    public IReadOnlyList<ScrollingTextComponent> Children => _children;

    /// <summary>Width in columns for each cell. Must match the number of viewports passed to <see cref="SetRowData"/>.</summary>
    public IReadOnlyList<int> Widths => _widths;

    /// <summary>
    /// Updates row data: ensures enough child components exist, sets each child from the corresponding viewport, and stores widths.
    /// Call each frame before render when used as the toolbar row.
    /// </summary>
    public void SetRowData(IReadOnlyList<Viewport> viewports, IReadOnlyList<int> widths)
    {
        if (viewports == null || widths == null)
        {
            return;
        }
        while (_children.Count < viewports.Count)
        {
            _children.Add(new ScrollingTextComponent());
        }
        for (int i = 0; i < viewports.Count; i++)
        {
            _children[i].SetFromViewport(viewports[i]);
        }
        _widths = widths;
    }

    /// <inheritdoc />
    /// <remarks>Returns null so the dispatcher treats this as a leaf and uses <see cref="IUiComponentRenderer{HorizontalRowComponent}"/> which lays out children horizontally.</remarks>
    public IReadOnlyList<IUiComponent>? GetChildren(RenderContext context) => null;
}
