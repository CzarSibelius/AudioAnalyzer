# ADR-0021: TextLayerSettings — common properties plus Custom JSON

**Status**: Accepted

## Context

`TextLayerSettings` previously held both common properties (LayerType, Enabled, ZOrder, TextSnippets, etc.) and layer-specific properties (ImageFolderPath, AsciiImageMovement for AsciiImage; Gain for Oscilloscope; LlamaStyle* for LlamaStyle) in a single monolithic class. Adding a new layer with new settings required editing Domain, TextLayersVisualizer (HandleKey), Program.cs (S modal), and DeepCopy. Layers that did not use a property still "saw" it (e.g. BeatCircles had no use for Gain). This made the Domain layer grow with every new layer type and created coupling across components.

## Decision

1. **Common + Custom structure**: `TextLayerSettings` (Domain) has only **common properties** plus a `Custom` property (JsonElement) for layer-specific settings. Other components (TextLayersVisualizer, Program.cs, persistence) treat Custom as opaque: they copy and persist it but do not deserialize or interpret it.

2. **Per-layer *Settings.cs**: Each layer that needs custom settings defines its own type in the Visualizers project next to the layer (e.g. `LlamaStyle/LlamaStyleSettings.cs`). Only the owning layer references these types.

3. **GetCustom / SetCustom pattern**: `TextLayerSettings` provides `GetCustom<T>()` and `SetCustom<T>()` so layers can deserialize Custom to their typed settings. Layers call `layer.GetCustom<LlamaStyleSettings>() ?? new LlamaStyleSettings()` in Draw; components that mutate settings (e.g. HandleKey for Oscilloscope gain) call `GetCustom`, mutate, then `SetCustom`.

4. **Legacy migration**: `[JsonExtensionData]` captures unknown top-level properties during deserialization (e.g. legacy Gain, LlamaStyle*). `MigrateExtensionDataToCustom()` moves them into Custom based on LayerType. Migration runs in FileSettingsRepository when loading.

5. **ITextLayerRenderer remains untyped**: The interface does not take a generic; layers receive `TextLayerSettings` and resolve their own T via `GetCustom<T>()`.

## Consequences

- **New layers**: Add a *Settings.cs in the layer folder; use `GetCustom<MySettings>()` in Draw. No changes to TextLayerSettings, TextLayersVisualizer, or Program.cs for the common path.
- **Components that need layer-specific knowledge** (HandleKey for [ ] gain, GetToolbarSuffix, S modal display) still branch on LayerType but read/write via GetCustom/SetCustom.
- **Persistence**: JSON has a "Custom" object per layer. Legacy configs with top-level Gain, LlamaStyle*, etc. are migrated on load.
- **References**: [TextLayerSettings](../../src/AudioAnalyzer.Domain/VisualizerSettings/TextLayerSettings.cs), [ADR-0014](0014-visualizers-as-layers.md), [ADR-0015](0015-visualizer-settings-in-domain.md).
