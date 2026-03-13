using AudioAnalyzer.Application.Abstractions;

namespace AudioAnalyzer.Console;

/// <summary>
/// Dispatcher that updates the UI component tree: updates stateful components (e.g. <see cref="HeaderContainer"/>)
/// and recurses into composites so children are updated before render.
/// </summary>
internal sealed class UiComponentStateUpdater : IUiStateUpdater<IUiComponent>
{
    private readonly IUiStateUpdater<HeaderContainer> _headerUpdater;

    public UiComponentStateUpdater(IUiStateUpdater<HeaderContainer> headerUpdater)
    {
        _headerUpdater = headerUpdater ?? throw new ArgumentNullException(nameof(headerUpdater));
    }

    /// <inheritdoc />
    public void Update(IUiComponent component, RenderContext context)
    {
        if (component is HeaderContainer header)
        {
            _headerUpdater.Update(header, context);
        }

        IReadOnlyList<IUiComponent>? children = component.GetChildren(context);
        if (children != null && children.Count > 0)
        {
            foreach (IUiComponent child in children)
            {
                Update(child, context);
            }
        }
    }
}
