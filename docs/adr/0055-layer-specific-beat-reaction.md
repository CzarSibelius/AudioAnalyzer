# ADR-0055: Layer-specific beat reaction (no common BeatReaction)

**Status**: Accepted

## Context

Previously, `TextLayerSettings` had a common `BeatReaction` property (type `TextLayerBeatReaction`: None, SpeedBurst, Flash, SpawnMore, Pulse, ColorPop). Every layer saw the same enum in the S modal and in JSON, but only some layer types actually used beat reaction in their Draw logic, and each supporting layer used only a subset of the options (e.g. ScrollingColors uses SpeedBurst and ColorPop; MatrixRain uses only Flash). Layers that do not use beat reaction (BeatCircles, Mirror, Fill, Oscilloscope, UnknownPleasures, VuMeter, LlamaStyle, Maschine) still had the setting exposed, which was confusing and allowed invalid combinations.

## Decision

1. **No common BeatReaction**: Remove `BeatReaction` from `TextLayerSettings`. Remove the global `TextLayerBeatReaction` enum from Domain.

2. **Layer-specific enums**: Each layer type that uses beat reaction has its own enum (e.g. `MarqueeBeatReaction`, `ScrollingColorsBeatReaction`) with only the values that layer supports. Enums live in the Visualizers project (one file per type per RCS1060).

3. **Storage in Custom**: BeatReaction is stored in each layer's Custom settings. Layers that support beat reaction have a `*Settings` type with a `BeatReaction` property (the layer-specific enum). The S modal discovers it via reflection (ADR-0025); no special-case handling for BeatReaction in common descriptors.

4. **Layers without beat reaction**: Layer types that do not use beat reaction have no BeatReaction property and no beat-reaction enum. The S modal does not show a BeatReaction row for those layers.

5. **Defaults**: Default and padded layers that support beat reaction get appropriate Custom set (e.g. `SetCustom(new MarqueeSettings { BeatReaction = MarqueeBeatReaction.None })`) in FileSettingsRepository. Infrastructure references Visualizers for this.

## Consequences

- **S modal**: BeatReaction appears only for layers that have a *Settings type with a BeatReaction property; cycling uses that layer's enum.
- **Persistence**: Preset/layer JSON stores BeatReaction inside `Custom` for the relevant layers. Top-level `"BeatReaction"` in old presets is ignored (no migration per ADR-0029).
- **New layers**: To add beat reaction to a new layer, add a layer-specific enum, add a *Settings class with a BeatReaction property, register the type in LayerSettingsReflection, and read `layer.GetCustom<XSettings>()?.BeatReaction` in Draw.
- **References**: [TextLayerSettings](../../src/AudioAnalyzer.Domain/VisualizerSettings/TextLayerSettings.cs), [ADR-0021](0021-textlayer-settings-common-custom.md), [ADR-0025](0025-reflection-based-layer-settings.md), [ADR-0029](0029-no-settings-migration.md).
