# ADR-0041: God object refactoring strategy

**Status**: Accepted

## Context

ApplicationShell, AnalysisEngine, and SettingsModal had grown into large, multi-responsibility classes (many dependencies, long methods, mixed concerns). This made changes risky and testing difficult. A consistent strategy was needed to break them down without changing user-facing behavior, while staying aligned with ADR-0040 (prefer DI) and ADR-0018 (lock ordering), and with existing patterns (interfaces in Abstractions, injectable services).

## Decision

1. **Refactor in three phases**, one per “god object”:
   - **ApplicationShell**: Extract header drawing (IHeaderDrawer), key handling (IMainLoopKeyHandler + MainLoopKeyContext), device lifecycle (IDeviceCaptureController), and settings persistence (IAppSettingsPersistence). Shell keeps the main loop and delegates to these services.
   - **AnalysisEngine**: Extract beat detection (IBeatDetector), volume analysis (IVolumeAnalyzer), and FFT band pipeline (IFftBandProcessor). Engine coordinates and fills the snapshot; hot path remains in-process with DI-injected services unless profiling justifies otherwise (ADR-0030, ADR-0040).
   - **SettingsModal**: Extract modal state (SettingsModalState), rendering (ISettingsModalRenderer), and key handling (ISettingsModalKeyHandler + SettingsModalKeyContext). Modal runs the overlay loop and delegates draw/handle to the renderer and handler.

2. **Use a single task list** for tracking: [docs/refactoring/god-object-plan.md](../refactoring/god-object-plan.md). Mark tasks `[x]` when implemented. Optional items (e.g. panel components, profiling) stay in the plan as unchecked until needed.

3. **Interfaces and placement**:
   - New interfaces live in `Abstractions/` (Console) or the appropriate assembly.
   - Implementations live next to existing related code (e.g. KeyHandling/, SettingsModal/, Application/BeatDetection/).
   - All new services are registered in ServiceConfiguration and injected; no new god objects.

4. **State and context**:
   - Modal and key-handler state is explicit (e.g. SettingsModalState, MainLoopKeyContext, SettingsModalKeyContext) so transitions are clear and testable.
   - Context objects carry runtime data and callbacks (e.g. saveSettings, SortedLayers) so handlers stay free of direct static or shell-specific dependencies where possible.

## Consequences

- Refactoring follows a single documented strategy; agents and developers use the plan as the checklist.
- ApplicationShell, AnalysisEngine, and SettingsModal become coordinators with fewer dependencies; new behavior in header, keys, device, settings, beat, volume, FFT, or modal UI is added in the extracted components.
- ADR-0040 and ADR-0018 are respected (DI by default; lock ordering for device shutdown). Optional profiling (e.g. ProcessAudio) remains in the plan rather than mandatory.
- When extending or refactoring these areas, consult this ADR and the task list in docs/refactoring/god-object-plan.md.
