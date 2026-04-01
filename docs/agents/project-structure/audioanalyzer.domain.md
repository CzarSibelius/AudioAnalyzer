# AudioAnalyzer.Domain — folder layout

**Root** (`src/AudioAnalyzer.Domain/`): core domain types that are not scoped to a sub-area — e.g. `AppSettings`, `ApplicationMode`, palette types, `TextLayersLimits`, UI/title settings, reflection attributes for settings (`SettingAttribute`, `SettingRangeAttribute`, `SettingChoicesAttribute`), and similar shared definitions.

**`Show/`**: show collection model (`Show`, `ShowEntry`, duration types).

**`VisualizerSettings/`**: preset and text-layer configuration (`Preset`, `TextLayerSettings`, `TextLayerType`, per-layer setting enums and DTOs, `VisualizerSettings`, render bounds, etc.).

## Rules

- Prefer the subfolder that matches the feature (show vs visualizer settings vs root shared types).
- Do not introduce a second top-level tree for “visualizers”; layer types belong under `VisualizerSettings/` or in Application/Visualizers per ADR-0014.
