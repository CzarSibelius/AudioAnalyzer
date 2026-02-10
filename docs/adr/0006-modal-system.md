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
