# ADR-0022: Presets in own files

**Status**: Accepted

**Supersedes**: [ADR-0019](0019-preset-textlayers-configuration.md) (preset model and UX unchanged; persistence moved to files)

## Context

Presets (named TextLayers configurations) were stored inline in `appsettings.json` under `VisualizerSettings.Presets`. Users requested presets to be saved in their own files, similar to palettes (`palettes/` directory with one JSON file per palette), for easier sharing, editing, and version control.

## Decision

1. **Presets directory**: Presets are stored as **JSON files** in a **`presets`** directory next to the executable (same pattern as `palettes/`). Id = filename without extension (e.g. `preset-1.json` → id `preset-1`).

2. **Main settings file**: `appsettings.json` stores only `ActivePresetId` (reference to the active preset) in `VisualizerSettings`. `Presets` and `TextLayers` are not persisted in the main file; they are discovered and loaded from preset files.

3. **Preset file format**: Each file has `Name` and `Config` (TextLayersVisualizerSettings):

   ```json
   {
     "Name": "Preset 1",
     "Config": { "PaletteId": "default", "Layers": [...] }
   }
   ```

4. **IPresetRepository**: Application interface (similar to `IPaletteRepository`) with `GetAll()`, `GetById(id)`, `Save(id, preset)`, `Create(preset)`, `Delete(id)`. `FilePresetRepository` scans `presets/*.json`, loads and saves per file.

5. **Migration**: Legacy `Presets` in `appsettings.json` are migrated on load: each preset is written to `presets/{id}.json`. Id is sanitized for filenames. `ActivePresetId` is preserved. Subsequent saves omit `Presets` and `TextLayers` from the main file.

6. **Create (N key)**: New presets get ids like `preset-1`, `preset-2` (first available). File is created via `Create()`.

7. **Rename (R key)**: Updates `Name` in the preset file via `Save()`. Id (filename) stays the same.

## Consequences

- Presets can be shared, edited externally, and versioned separately.
- Main settings file stays slim; preset list is discovered by scanning the directory.
- `FileSettingsRepository` depends on `IPresetRepository`; `LoadVisualizerSettings` populates `Presets` and `TextLayers` from preset files.
- **References**: [FilePresetRepository](../../src/AudioAnalyzer.Infrastructure/FilePresetRepository.cs), [IPresetRepository](../../src/AudioAnalyzer.Application/Abstractions/IPresetRepository.cs), [FileSettingsRepository](../../src/AudioAnalyzer.Infrastructure/FileSettingsRepository.cs), [ADR-0019](0019-preset-textlayers-configuration.md).
