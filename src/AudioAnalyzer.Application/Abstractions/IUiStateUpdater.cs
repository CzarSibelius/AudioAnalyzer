namespace AudioAnalyzer.Application.Abstractions;

/// <summary>
/// Updates the state of a UI component from the current context. Used before render so components
/// own their state and updaters apply context data (e.g. device name, BPM) into the component.
/// </summary>
/// <typeparam name="TComponent">The component type to update; must implement <see cref="IUiComponent"/>.</typeparam>
public interface IUiStateUpdater<TComponent> where TComponent : IUiComponent
{
    /// <summary>
    /// Updates the component's state from the context. Called by the dispatcher before rendering.
    /// </summary>
    /// <param name="component">The component instance whose state to update.</param>
    /// <param name="context">Layout and optional data (e.g. DeviceName, Snapshot).</param>
    void Update(TComponent component, RenderContext context);
}
