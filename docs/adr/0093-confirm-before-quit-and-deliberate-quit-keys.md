# ADR-0093: Confirm-before-quit and deliberate quit keys

**Status**: Accepted

## Context

The application is too easy to quit by accident. Today the **only** quit path is **Escape**, handled at the top of the main loop, which sets `ShouldQuit` and exits **immediately with no confirmation**:

- `src/AudioAnalyzer.Console/KeyHandling/MainLoopKeyHandler.cs` — `ConsoleKey.Escape` → `ctx.ShouldQuit = true`.
- `src/AudioAnalyzer.Console/ApplicationShell.cs` — `if (ctx.ShouldQuit) running = false;`.

Escape is the worst possible key for this, because everywhere else in the app **Escape means "cancel / back / close"**: it dismisses every modal, cancels inline text edits in the General Settings hub ([ADR-0061](0061-general-settings-mode.md)), and restores the previous bounds in the layer render-bounds edit session ([ADR-0058](0058-layer-render-bounds.md)). Operators build the reflex "Escape backs out of the current thing"; when no modal is open, that same reflex silently terminates the whole session — losing the running capture, visualization state, and unsaved in-session context. [ADR-0061](0061-general-settings-mode.md) even documents Escape in the hub menu "falling through to the main loop (**quit**)", so a misfire from a sub-surface can quit the app.

There is also no `Console.CancelKeyPress` handling, so **Ctrl+C** (SIGINT) hard-terminates the process abruptly, bypassing the orderly shutdown / lock ordering described in [ADR-0018](0018-shutdown-lock-ordering.md).

## Decision

1. **Confirm before quitting.** Quitting always goes through a confirmation step. Add a reusable **yes/no confirmation modal** (`IConfirmationModal`), injected per [ADR-0035](0035-modal-dependency-injection.md), built as an `IUiComponent` tree per [ADR-0053](0053-iuicomponent-all-ui.md), with key handling via `IKeyHandler<TContext>` per [ADR-0047](0047-all-key-handling-via-ikeyhandler.md) and bindings exposed per [ADR-0048](0048-key-handlers-expose-bindings.md). Mapping: **Y / Enter = confirm (quit)**, **N / Esc = cancel**. Styling follows the unified menu selection affordance ([ADR-0069](0069-unified-menu-selection-affordance.md)) and `Label:value` / palette conventions ([ADR-0033](0033-ui-principles-and-configurable-settings.md), [ADR-0050](0050-ui-alignment-blocks-label-format.md)).

2. **Deliberate quit keys.** Add explicit quit keys **`Q`** and **`Ctrl+Q`** at the top-level main loop; both open the quit confirmation. `Q` is the primary, discoverable quit affordance shown in the toolbar/help.

3. **Escape no longer quits directly.** Top-level Escape opens the **same** quit confirmation instead of exiting. Because Escape inside the confirmation **cancels**, a double-Escape can never quit — structurally eliminating the accidental-quit path. Escape behavior inside modals and edit sessions is unchanged; only the top-level main-loop Escape changes.

4. **Intercept Ctrl+C.** Hook `Console.CancelKeyPress`, set `e.Cancel = true`, and route to the same quit confirmation rather than hard-terminating. Actual quit (after confirmation) still follows the shutdown / lock ordering in [ADR-0018](0018-shutdown-lock-ordering.md).

5. **Discoverability.** The quit binding (`Q` / `Esc`) is surfaced in the toolbar hint and the dynamic help screen (H) via `GetBindings()` per [ADR-0049](0049-dynamic-help-screen.md). The confirmation is keyboard-only.

## Consequences

- An accidental single **Escape**, **Q**, or **Ctrl+C** no longer ends the session; it shows a dismissible dialog. This is the primary goal.
- One extra deliberate keystroke is now required to quit. This is intentional and acceptable for a long-running visualizer.
- **[ADR-0061](0061-general-settings-mode.md) is amended:** Escape in the General Settings hub menu (not editing) now falls through to the quit **confirmation**, not a direct quit. Inline-edit Escape still cancels the edit. That ADR's wording should be updated to reference this ADR.
- New console UI surface documented at `specs/console-ui/quit-confirmation-modal/spec.md` and linked from the console-ui hub; screenshot + line reference per [ADR-0046](0046-screen-dump-ascii-screenshot.md).
- The confirmation modal is intentionally generic (title + prompt + yes/no) so it can be reused later for other destructive actions (e.g. delete preset, PBI-007) — but this ADR's scope is **quit only**.
- Affected areas (for the implementing PBI): `MainLoopKeyHandler` / `MainLoopKeyContext` (Escape rebind, add `Q` / `Ctrl+Q`, route to confirmation), `ApplicationShell` (Ctrl+C hook, modal wiring, quit flow), new `ConfirmationModal` + key handler + context, `ServiceConfiguration` registration, toolbar + help bindings.
