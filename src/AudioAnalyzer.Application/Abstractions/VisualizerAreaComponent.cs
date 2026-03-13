namespace AudioAnalyzer.Application.Abstractions;

/// <summary>
/// UI component for the visualizer area (spectrum/layers). No component data; bounds and snapshot
/// come from <see cref="RenderContext"/>. Used by <see cref="IUiComponentRenderer"/> to dispatch to <see cref="IVisualizer"/>.
/// </summary>
public sealed class VisualizerAreaComponent : IUiComponent
{
    /// <summary>Singleton instance; viewport and snapshot come from context.</summary>
    public static readonly VisualizerAreaComponent Instance = new();

    private VisualizerAreaComponent() { }

    /// <inheritdoc />
    public IReadOnlyList<IUiComponent>? GetChildren(RenderContext context) => null;
}
