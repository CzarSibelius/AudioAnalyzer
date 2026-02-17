# ADR-0025: Reflection-based layer settings discovery

**Status**: Accepted

## Context

The S settings modal (per [ADR-0023](0023-settings-modal-layer-editing.md)) displays and edits layer settings: common properties (Enabled, LayerType, ZOrder, etc.) and custom properties per layer type (Gain for Oscilloscope, LlamaStyle*, AsciiImage*, etc.). Previously, GetSettingsRows, ApplySettingEdit, and CycleSetting used large switch statements with hardcoded SettingEditMode per property. Adding a new layer or property required edits in multiple places. The edit mode (Cycle vs TextEdit) was manually specified for each setting.

## Decision

1. **Reflection-based discovery**: Layer settings are discovered via reflection on `TextLayerSettings` (common properties) and layer-specific `*Settings` types (custom properties). A registry maps `TextLayerType` to custom settings type (AsciiImageSettings, OscilloscopeSettings, LlamaStyleSettings).

2. **Type-to-EditMode rules**: `SettingEditMode` is derived from property type and optional attributes:
   - `bool`, `Enum` → Cycle
   - `int`, `double` → Cycle (with `[SettingRange]` for min/max/step)
   - `string` → TextEdit (unless `[SettingChoices]` overrides to Cycle)
   - `List<string>` → TextEdit (comma-separated)

3. **Attributes for overrides**: Use `[SettingChoices("A","B")]` for string properties with fixed choices (e.g. ColorScheme, PeakMarkerStyle); `[SettingRange(min, max, step)]` for numeric bounds; `[Setting(id, label)]` for display name overrides.

4. **Explicit handling for special cases**: Palette (cycles through IPaletteRepository), LayerType (uses TextLayerSettings.CycleTypeForward/Backward), and Snippets (List&lt;string&gt; with custom display/edit) remain explicitly handled; they require external context or non-standard behavior that reflection cannot infer.

5. **SettingDescriptor**: A single descriptor abstraction holds Id, Label, EditMode, GetDisplayValue, ApplyEdit, and Cycle. ApplySettingEdit and CycleSetting delegate to descriptor lookup by Id.

## Consequences

- New layer settings: Add a `*Settings.cs` with properties; optionally add `[Setting]`, `[SettingChoices]`, `[SettingRange]`; register the type in `LayerSettingsReflection`. No edits to GetSettingsRows/ApplySettingEdit/CycleSetting switch logic.
- Attributes live in Domain (`SettingAttributes.cs`) so `*Settings` classes in Visualizers can reference them.
- References: [LayerSettingsReflection](../../src/AudioAnalyzer.Console/LayerSettingsReflection.cs), [SettingAttributes](../../src/AudioAnalyzer.Domain/SettingAttributes.cs), [ADR-0021](0021-textlayer-settings-common-custom.md), [ADR-0023](0023-settings-modal-layer-editing.md).
