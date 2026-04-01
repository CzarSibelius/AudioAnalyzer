# Phase 3: Use Link in the application

**Prerequisite:** Phase 1 (external beat sources) and Phase 2 (native wrapper) complete.

## Domain and persistence

- [ ] Ensure `BpmSource` includes `AbletonLink` and persists in `appsettings.json`.
- [ ] Optional settings: Link **quantum** (default 4), port override — only if shim exposes them.

## Application: Link timing

- [ ] Implement `LinkBeatTimingSource : IBeatTimingSource` using Phase 2 wrapper.
- [ ] Use Link **application-thread** capture for tempo/beat queries from `OnVisualTick` (or equivalent), not from audio callback unless you add a dedicated RT path later.
- [ ] **Read-only session:** do not commit tempo changes unless product requires “this app as master” (document if added).
- [ ] Drive `BeatCount` / `BeatFlashActive` from Link beat timeline (recommended) for alignment with Rekordbox/Live and `ShowPlaybackController`.

## DI and lifecycle

- [ ] Register Link session lifetime in `ServiceConfiguration` (singleton vs per-run — justify).
- [ ] Enable Link when `BpmSource == AbletonLink`; disable or dispose when switching away.
- [ ] Handle missing DLL: graceful degradation message and fallback `BpmSource` or clear error in header.

## UI / UX

- [ ] General Settings hub: expose **Link** in BPM source cycle; update key handler indices, `GeneralSettingsHubMenuLines`, `GeneralSettingsHubState` comments.
- [ ] Header: show Link BPM + optional peer/session hint (e.g. “Link: 120” or peer count).
- [ ] Dynamic help: new or updated bindings ([ADR-0048](../../adr/0048-key-handlers-expose-bindings.md)).

## Tests

- [ ] `LinkBeatTimingSource` tests with mock native interface (tempo changes, beat boundary → `BeatCount`).
- [ ] Integration smoke: optional, manual doc only.

## Documentation (finalize)

- [ ] ADR updates: Link dependency, threading, license, BPM source matrix (Audio / Demo / Link).
- [ ] [docs/ui-spec-general-settings-hub.md](../../ui-spec-general-settings-hub.md): new row, screenshots/line refs if required by [ui-spec rules](../../../.cursor/rules/ui-specs.mdc).
- [ ] Root `README.md`: Rekordbox / LAN / build native DLL.
- [ ] [docs/agents/visualizers.md](../../agents/visualizers.md): remind that spectrum data remains audio-driven when using Link.
