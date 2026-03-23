namespace AudioAnalyzer.Application.Abstractions;

/// <summary>Main-area component for General Settings hub (menu + optional rename line). Snapshot and layout come from <see cref="RenderContext"/>.</summary>
public sealed class GeneralSettingsHubAreaComponent : IUiComponent
{
    /// <summary>Singleton instance.</summary>
    public static readonly GeneralSettingsHubAreaComponent Instance = new();

    private GeneralSettingsHubAreaComponent() { }

    /// <inheritdoc />
    public IReadOnlyList<IUiComponent>? GetChildren(RenderContext context) => null;
}
