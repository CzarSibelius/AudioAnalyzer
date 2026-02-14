# ADR-0015: Visualizer settings in Domain, IVisualizerSettingsRepository in Application

**Status**: Accepted

## Context

Infrastructure previously depended on Visualizers because `FileSettingsRepository` implemented `IVisualizerSettingsRepository` (defined in Visualizers) and used concrete settings types (`VisualizerSettings`, `GeissVisualizerSettings`, etc.) from that project. This violated Clean Architecture: Infrastructure (technical implementations) should not depend on Visualizers (feature/plugin layer).

## Decision

1. **Visualizer settings types in Domain**: `VisualizerSettings` and all per-visualizer settings types (`GeissVisualizerSettings`, `OscilloscopeVisualizerSettings`, `UnknownPleasuresVisualizerSettings`, `TextLayersVisualizerSettings`, `TextLayerSettings`, `TextLayerType`, `TextLayerBeatReaction`, `AsciiImageMovement`) are moved to the Domain project. They are configuration DTOs used for persistence and injection.

2. **IVisualizerSettingsRepository in Application**: The interface is moved to `Application.Abstractions`. It uses `Domain.VisualizerSettings` as its return/parameter type.

3. **Infrastructure no longer references Visualizers**: `FileSettingsRepository` now uses Domain types exclusively. Infrastructure depends only on Application and Domain.

## Consequences

- **Cleaner layering**: Infrastructure → Application → Domain; no Infrastructure → Visualizers.
- **Domain** holds all configuration models; Application defines repository contracts.
- **Visualizers** reference Domain and use the settings types from there.
- **JSON schema** in `appsettings.json` is unchanged; backward compatibility preserved.
