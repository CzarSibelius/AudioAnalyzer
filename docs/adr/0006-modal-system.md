# ADR-0006: Modal system for console UI

**Status**: Accepted

## Context

The application needs to show dialogs such as the help screen and the device selection menu that stay on top of the main visualization and UI until the user explicitly closes them. Today these are implemented ad hoc: each clears the console, draws its content, blocks on input, and when closed the main loop redraws the base view. There is no single abstraction for "a modal"; adding more dialogs would duplicate this pattern and make behavior inconsistent.

## Decision

1. **The application must have a modal system**: A defined way to show modal dialogs that (1) are drawn on top of all other content, (2) capture input until closed, (3) are dismissed explicitly (e.g. by key), and (4) on close return control so the underlying view can be shown again (by redraw or buffer restore).

2. **All such dialogs use the modal system**: The help screen, device selection menu, and any future modals (e.g. settings, about) must use this system rather than ad-hoc clear/redraw logic.

3. **Implementation flexibility**: How the console achieves "on top" (e.g. save/restore buffer vs clear and redraw after close) is an implementation detail; this ADR constrains behavior and ownership, not the exact console API.

## Consequences

- **Console layer** (e.g. Program.cs or a dedicated UI component) owns modal lifecycle: open modal, run modal loop, close and optionally redraw base content.
- **Help screen and device selection menu** are to be refactored to go through the modal system; implementation may follow in a separate change.
- **Future dialogs** (e.g. settings, about) must be implemented as modals within this system.
- When adding new overlays or dialogs, agents and developers should use the modal system per this ADR.
- **Overlay idle updates**: `RunOverlayModal` supports `onScrollTick` (polled every ~50 ms when no key is available) for lightweight redraws without clearing the overlay. **Settings** uses `ISettingsModalRenderer.DrawIdleOverlayTick`: hint line plus Palette cell only when beat/tick phase advances, wrapped in a single **synchronized output** frame (ANSI `?2026`) to reduce flicker. Optionally, `idleFullRedraw`: when true, idle polls redraw the full overlay in place **without** blanking rows first, throttled to about **100 ms**; use sparingly. Key handling still uses clear-then-draw for correctness after state changes.
- **Overlay main-area animation while blocked**: The main `ApplicationShell` loop does not run while an overlay modal is open, so **`onIdleVisualizationTick`** (optional) is invoked on each idle poll (~50 ms) **before** overlay idle work and **without** holding `consoleLock`—callers pass `IVisualizationOrchestrator.Redraw` so the visualizer keeps updating below the overlay. Must not run that redraw under `consoleLock` (orchestrator acquires the same lock and would deadlock). **Settings** and **Show edit** modals use this.
