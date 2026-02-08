# ADR-0001: Automatic persistence of settings (no manual save)

**Status**: Accepted

## Context

Users should not have to remember to press a key to save. Settings (device, visualization mode, beat sensitivity, oscilloscope gain, beat circles) should persist when they change, without a dedicated "Save" action.

## Decision

Persist settings automatically whenever they change. No dedicated "Save" action is required from the user. The application must call the settings repository (e.g. `ISettingsRepository.Save`) after any change to persisted settings.

## Consequences

- **Implementation**: After any user action that changes persisted settings (visualization mode V, beat sensitivity +/- , beat circles B, oscilloscope gain [ / ], and device selection on D+ENTER—device is already saved on selection), sync the in-memory settings from the engine/renderer and call the settings repository to save.
- **Optional**: Debounce writes (e.g. 500–1000 ms) to avoid excessive file I/O when the user holds +/- or [ / ].
- **S key**: The S key can be removed or repurposed (e.g. "Save now" only if using debounce). The UI and help must not imply that saving is mandatory or that S is the primary way to persist settings.
- **UI**: Help text and status/toolbar lines must not show "S = Save" as a required or primary action; settings are saved automatically when changed.
