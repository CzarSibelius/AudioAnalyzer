# ADR-0045: Global maximum of 9 text layers as a single constant (not user-editable)

**Status**: Accepted

## Context

The application has a fixed maximum of 9 text layers: keys 1–9 select or toggle layers, the settings modal shows up to 9 layers, and default presets are created with 9 layers. The number 9 was hardcoded in several places (FileSettingsRepository, SettingsModalRenderer, and comments). We want a single global constant for this limit, with no plan to increase it soon. The constant should live in application configuration but must not be user-editable (no UI, no encouragement to edit appsettings).

## Decision

1. **Single global constant**: The maximum number of text layers is **9** and is defined by a single global constant: `TextLayersLimits.MaxLayerCount` in the Domain layer (application configuration in code).

2. **Not user-editable**: The value is **not** read from or written to `appsettings.json` and is **not** exposed in the settings UI. Users cannot change it.

3. **Code and docs use the constant**: All code that refers to the layer count limit (padding, default config, UI cap) uses `TextLayersLimits.MaxLayerCount`. Comments and documentation that mention "9 layers" or "keys 1–9" reference the constant (e.g. "keys 1–9 (MaxLayerCount layers)" or "see TextLayersLimits.MaxLayerCount"). New code (key handlers, UI that assumes 1–9) must use this constant.

4. **Optional cap when loading**: When loading or applying presets, the application may cap the number of layers to `MaxLayerCount` so that config or legacy data with more than 9 layers does not exceed the limit.

## Consequences

- One place to change the limit if it is ever increased (code change only).
- No migration or user-facing setting; no need to preserve a "Limits" section in the settings file.
- If a config-driven value is needed later for ops/deployment (still not exposed in UI), the ADR can be updated to allow a read-only source while still forbidding user editing.
