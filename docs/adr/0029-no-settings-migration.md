# ADR-0029: No settings migration — backup and reset

**Status**: Accepted

## Context

The codebase previously had migration logic for backward compatibility: `MigrateExtensionDataToCustom()` in TextLayerSettings, and FileSettingsRepository migrations for Unknown Pleasures, spectrum/vumeter/winamp modes, legacy Presets, and SelectedPaletteId. Each schema change required new migration code across multiple methods. This added maintenance burden and complexity. Migration has been removed; per this ADR, we use backup-and-reset instead.

## Decision

1. **No new migration logic**: Do not add or maintain migration logic for settings format changes.

2. **Backup and reset when incompatible**: When old settings fail to load or become incompatible with the current schema, copy the original file to `{name}.{timestamp}.bak` (e.g. `appsettings.2025-02-17T14-30-00.123.bak`) and create new settings from defaults. The timestamp uses UTC in format `yyyy-MM-ddTHH-mm-ss.fff`. Users can recover values from the `.bak` file if needed.

3. **Migration removed**: Legacy migration code has been removed. When settings fail to load, backup-and-reset applies.

## Consequences

- **Users**: Upgrading across a breaking change may require reconfiguration; settings can be recovered from `.bak` manually.
- **Codebase**: Simpler; no per-format migration branches for future schema changes.
- **Load logic**: Must handle parse failures and incompatibility gracefully and trigger backup-then-reset when needed.
- **Related**: [TextLayerSettings](../../src/AudioAnalyzer.Domain/VisualizerSettings/TextLayerSettings.cs), [FileSettingsRepository](../../src/AudioAnalyzer.Infrastructure/FileSettingsRepository.cs), [ADR-0021](0021-textlayer-settings-common-custom.md).
