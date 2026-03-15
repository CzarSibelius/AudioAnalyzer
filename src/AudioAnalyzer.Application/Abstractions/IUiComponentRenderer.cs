namespace AudioAnalyzer.Application.Abstractions;

/// <summary>
/// Dispatches rendering to the correct concrete renderer based on the typed <see cref="IUiComponent"/>.
/// Containers call this for each component; leaf renderers implement this for their component type.
/// </summary>
/// <typeparam name="TComponent">The component type to render; must implement <see cref="IUiComponent"/>.</typeparam>
public interface IUiComponentRenderer<TComponent> where TComponent : IUiComponent
{
    /// <summary>
    /// Renders the component at context.StartRow. Returns lines consumed and optional line content;
    /// when LineContents is non-null the dispatcher writes it; when null the renderer already wrote.
    /// </summary>
    /// <param name="component">The component to render (e.g. HorizontalRowComponent, VisualizerAreaComponent).</param>
    /// <param name="context">Layout and optional data; StartRow is the console row to write to.</param>
    /// <returns>Lines consumed and optional content for the dispatcher to write.</returns>
    ComponentRenderResult Render(TComponent component, RenderContext context);

    /// <summary>Resets any internal cache for the visualizer area (e.g. so the region is cleared again on next render). No-op by default.</summary>
    void ResetVisualizerAreaCleared() { }
}
