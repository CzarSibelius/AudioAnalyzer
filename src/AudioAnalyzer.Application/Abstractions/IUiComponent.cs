namespace AudioAnalyzer.Application.Abstractions;

/// <summary>
/// A node in the UI tree. May be a leaf (carries data for a concrete renderer) or a composite
/// (has children; the renderer recurses). The actual renderer is selected by
/// <see cref="IUiComponentRenderer"/> based on the concrete component type.
/// </summary>
public interface IUiComponent
{
    /// <summary>
    /// Children in render order, or null/empty for leaf components. When non-empty,
    /// <see cref="IUiComponentRenderer"/> recurses instead of dispatching by type.
    /// </summary>
    IReadOnlyList<IUiComponent>? GetChildren(RenderContext context);
}
