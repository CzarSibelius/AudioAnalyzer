# AudioAnalyzer.Infrastructure — folder layout

**Project root**: file-based repositories (`FilePresetRepository`, `FileSettingsRepository`, `FilePaletteRepository`, `FileShowRepository`, `FileUiThemeRepository`), NAudio-backed audio input (`NAudioAudioInput`, `NAudioDeviceInfo`), synthetic/demo audio (`SyntheticAudioInput`), and similar adapters that are not grouped under a dedicated subfolder yet.

**`Logging/`**: `Microsoft.Extensions.Logging` file provider (`BackgroundFileLoggerProvider`, background writer) per ADR-0076.

**`NowPlaying/`**: `NullNowPlayingProvider` and other infrastructure implementations of now-playing that live in this assembly.

## Optional future layout (not required)

Grouping `File*Repository` under `Persistence/` and audio types under `Audio/` would be consistent; only do this when refactoring deliberately to avoid churn.

## Rules

- Platform-specific implementations that belong in `AudioAnalyzer.Platform.Windows` must not be added here.
- New repository or audio types: default to project root unless a small subfolder already exists for the concern (e.g. `NowPlaying/`).
