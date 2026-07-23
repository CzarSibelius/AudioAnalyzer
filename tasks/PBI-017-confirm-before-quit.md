# PBI-017: Confirm-before-quit and deliberate quit keys

**Transient work item** — close after merge. **The Spec** (`specs/console-ui/quit-confirmation-modal/spec.md`) holds **state**; this file holds **delta**.

## Directive

Stop the application from quitting on a single accidental keypress. Implement [ADR-0093](../../docs/adr/0093-confirm-before-quit-and-deliberate-quit-keys.md):

1. **Reusable confirmation modal.** Add a generic yes/no modal `IConfirmationModal` + `ConfirmationModal` (Console), an `IUiComponent` tree per [ADR-0053](../../docs/adr/0053-iuicomponent-all-ui.md), with a `ConfirmationKeyContext` + `IKeyHandler<ConfirmationKeyContext>` (config-based, per [ADR-0047](../../docs/adr/0047-all-key-handling-via-ikeyhandler.md) / open-generic `GenericKeyHandler`) exposing `GetBindings()` ([ADR-0048](../../docs/adr/0048-key-handlers-expose-bindings.md)). `Show(...)` takes a title + prompt and returns a bool. Mapping: **Y / Enter = confirm**, **N / Esc = cancel**. Render via **ModalSystem.RunModal**; selection affordance per [ADR-0069](../../docs/adr/0069-unified-menu-selection-affordance.md). Register in `ServiceConfiguration` per [ADR-0035](../../docs/adr/0035-modal-dependency-injection.md) / [ADR-0040](../../docs/adr/0040-dependency-injection-preference.md). Keep it generic (reusable later for delete-preset etc.) but **wire only the quit flow** in this PBI.

2. **Route all quit paths through the modal.** In [`MainLoopKeyHandler`](../../src/AudioAnalyzer.Console/KeyHandling/MainLoopKeyHandler.cs): change the **Escape** binding so it no longer sets `ShouldQuit` directly; instead it requests the quit confirmation. Add **`Q`** and **`Ctrl+Q`** bindings that request the same confirmation. Only set `ShouldQuit` (via [`MainLoopKeyContext`](../../src/AudioAnalyzer.Console/KeyHandling/MainLoopKeyContext.cs)) when the confirmation returns true. Add a `MainLoopKeyContext` member (e.g. `ConfirmQuit` callback or the modal handle) so the handler can invoke the modal; the modal must be shown under the existing `ConsoleLock` / modal-open guard used by other modals (see [`ApplicationShell.CreateKeyContext`](../../src/AudioAnalyzer.Console/ApplicationShell.cs)).

3. **Intercept Ctrl+C.** In [`ApplicationShell`](../../src/AudioAnalyzer.Console/ApplicationShell.cs) (Run/startup), hook `Console.CancelKeyPress`, set `e.Cancel = true`, and route to the same quit confirmation (set `running = false` only on confirm). Quit after confirmation must still follow shutdown / lock ordering per [ADR-0018](../../docs/adr/0018-shutdown-lock-ordering.md) (existing `Shutdown()`). Unhook on shutdown.

4. **Discoverability.** Update the toolbar hint and dynamic help (H, [ADR-0049](../../docs/adr/0049-dynamic-help-screen.md)) so the quit binding reads `Q` (and `Esc`) → "Quit". Keep `GetBindings()` accurate.

5. **Amend [ADR-0061](../../docs/adr/0061-general-settings-mode.md)** wording: Escape in the General Settings hub menu (not editing) now falls through to the quit **confirmation**, not a direct quit. Cross-reference ADR-0093. (Hub inline-edit Escape behavior unchanged.)

**In scope:** new `IConfirmationModal`/`ConfirmationModal` + key context/handler/config, `MainLoopKeyHandler` + `MainLoopKeyContext`, `ApplicationShell` (Ctrl+C hook + quit flow + DI wiring), `ServiceConfiguration`, toolbar/help bindings, the spec + ADR-0061 text fix.

**Out of scope:** wiring the confirmation modal to any non-quit action (delete preset etc.); changing Escape inside other modals / edit sessions (only top-level main-loop Escape changes); a mouse-driven dialog; persisting a "don't ask again" preference.

## Context pointer

- Primary spec: [`specs/console-ui/quit-confirmation-modal/spec.md`](../specs/console-ui/quit-confirmation-modal/spec.md)
- Hub: [`specs/console-ui/spec.md`](../specs/console-ui/spec.md); related: [`general-settings-hub/spec.md`](../specs/console-ui/general-settings-hub/spec.md), [`toolbar/spec.md`](../specs/console-ui/toolbar/spec.md), [`menu-selection/spec.md`](../specs/console-ui/menu-selection/spec.md)
- ADRs: [ADR-0093](../../docs/adr/0093-confirm-before-quit-and-deliberate-quit-keys.md) (this decision), [ADR-0035](../../docs/adr/0035-modal-dependency-injection.md), [ADR-0047](../../docs/adr/0047-all-key-handling-via-ikeyhandler.md), [ADR-0048](../../docs/adr/0048-key-handlers-expose-bindings.md), [ADR-0049](../../docs/adr/0049-dynamic-help-screen.md), [ADR-0053](../../docs/adr/0053-iuicomponent-all-ui.md), [ADR-0018](../../docs/adr/0018-shutdown-lock-ordering.md), [ADR-0061](../../docs/adr/0061-general-settings-mode.md), [ADR-0069](../../docs/adr/0069-unified-menu-selection-affordance.md)

## Verification pointer

- Contract: **Definition of Done**, **Regression guardrails**, **Scenarios** in the quit-confirmation-modal spec (accidental Escape no-quit; deliberate Q+Y quits; Ctrl+C intercepted).
- Add unit tests for the key handler bindings (Escape/Q/Ctrl+Q request confirmation; confirm vs cancel sets/clears quit) following existing key-handler test patterns; mirror production layout per [ADR-0064](../../docs/adr/0064-test-project-mirrors-production-layout.md).
- Build / test / format: root [`AGENTS.md`](../AGENTS.md) — `dotnet build` (0 warnings), tests, `dotnet format --verify-no-changes`. macOS host: pass the pinned `-f net10.0-macos26.0` TFM.

## Refinement rule

If implementation reveals a better key mapping, modal layout, or Ctrl+C handling than the spec/ADR describe, **update** [`quit-confirmation-modal/spec.md`](../specs/console-ui/quit-confirmation-modal/spec.md) (and ADR-0093 if the decision itself changes) **in the same commit** (same-commit rule). If the change is product-level or ambiguous, stop and flag for human review.
