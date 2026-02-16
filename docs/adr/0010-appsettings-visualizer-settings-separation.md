# ADR-0010: AppSettings and VisualizerSettings separation

**Status**: Accepted

## Context

Per-visualizer settings types (e.g. `TextLayerSettings`, `GeissVisualizerSettings`, `VisualizerSettings`) were originally in the Domain layer. Moving them into the Visualizers project (each in its visualizer subfolder per [ADR-0007](0007-visualizer-subfolder-structure.md)) would improve cohesion but risks a circular dependency: if `AppSettings` retains a `VisualizerSettings` property, Domain would need to reference Visualizers, while Visualizers already references Domain for `PaletteColor`, `AnalysisSnapshot`, etc.

## Decision

1. **Separate AppSettings from VisualizerSettings**: `AppSettings` (Domain) contains only app-level configuration: `InputMode`, `DeviceName`, `BeatSensitivity`, `SelectedPaletteId`, and deprecated `BeatCircles`/`OscilloscopeGain` for backward compatibility. It does **not** reference `VisualizerSettings`. (*VisualizationMode* was removed from AppSettings; the app uses a single TextLayers visualizer only—see [ADR-0014](0014-visualizers-as-layers.md), [ADR-0019](0019-preset-textlayers-configuration.md).)

2. **VisualizerSettings and per-visualizer types in Visualizers**: `VisualizerSettings`, `TextLayerSettings`, `TextLayersVisualizerSettings`, `GeissVisualizerSettings`, `OscilloscopeVisualizerSettings`, `UnknownPleasuresVisualizerSettings`, `TextLayerType`, and `TextLayerBeatReaction` live in the Visualizers project. Each per-visualizer settings type resides in its visualizer subfolder (e.g. `TextLayers/`, `Geiss/`).

3. **Split repository interfaces**: `ISettingsRepository` (Application) handles app settings: `AppSettings LoadAppSettings()`, `void SaveAppSettings(AppSettings)`. `IVisualizerSettingsRepository` (Visualizers) handles visualizer settings: `VisualizerSettings LoadVisualizerSettings()`, `void SaveVisualizerSettings(VisualizerSettings)`.

4. **Shared persistence**: A single `appsettings.json` file stores both. `FileSettingsRepository` (Infrastructure) implements both interfaces, reads the file once per operation, and merges the app/visualizer sections on load/save. Migration logic merges legacy `BeatCircles`, `OscilloscopeGain`, and `SelectedPaletteId` into `VisualizerSettings` when loading older config files.

## Consequences

- **No circular dependency**: Domain never references Visualizers; Application references only Domain for `AppSettings`; Visualizers owns its settings types and interface.
- **Single file for users**: No change to the on-disk format; `appsettings.json` still contains `VisualizerSettings` and app-level keys.
- **DI**: The Console host loads both via the same `FileSettingsRepository` instance, registers `IVisualizerSettingsRepository` separately so both interfaces are available.
- **Backward compatibility**: Legacy config files continue to work; migration runs on load.
