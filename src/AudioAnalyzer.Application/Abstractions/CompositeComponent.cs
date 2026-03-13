namespace AudioAnalyzer.Application.Abstractions;

/// <summary>
/// A composite UI component that holds a list of children. The renderer recurses over
/// <see cref="GetChildren"/> instead of dispatching to a leaf renderer.
/// </summary>
public sealed class CompositeComponent : IUiComponent
{
    private readonly Func<RenderContext, IReadOnlyList<IUiComponent>> _getChildren;

    /// <summary>Creates a composite that returns the given list from <see cref="GetChildren"/>.</summary>
    public CompositeComponent(IReadOnlyList<IUiComponent> children)
    {
        ArgumentNullException.ThrowIfNull(children);
        _getChildren = _ => children;
    }

    /// <summary>Creates a composite that builds children from the render context each frame.</summary>
    public CompositeComponent(Func<RenderContext, IReadOnlyList<IUiComponent>> getChildren)
    {
        ArgumentNullException.ThrowIfNull(getChildren);
        _getChildren = getChildren;
    }

    /// <inheritdoc />
    public IReadOnlyList<IUiComponent>? GetChildren(RenderContext context) =>
        _getChildren(context ?? new RenderContext());
}
