# UI spec: Quit confirmation modal

## Blueprint

### Context

Console UI surface documented with ASCII screen dumps and line references per [format](../format/spec.md) and [ADR-0046](../../../docs/adr/0046-screen-dump-ascii-screenshot.md).

Quitting the application is a destructive action (it ends the running capture and visualization). Per [ADR-0093](../../../docs/adr/0093-confirm-before-quit-and-deliberate-quit-keys.md), every quit path goes through this confirmation so the app cannot be quit by a single accidental keypress. The trigger keys are **`Q`** and **`Ctrl+Q`** (deliberate quit), top-level **`Escape`**, and intercepted **`Ctrl+C`** — all open this same modal. Inside the modal, **`Y` / `Enter`** confirms (quits) and **`N` / `Esc`** cancels, so a double-Escape can never quit.

### Architecture

Reusable yes/no confirmation modal (`IConfirmationModal`) injected per [ADR-0035](../../../docs/adr/0035-modal-dependency-injection.md), built as an `IUiComponent` tree per [ADR-0053](../../../docs/adr/0053-iuicomponent-all-ui.md), key handling via `IKeyHandler<TContext>` per [ADR-0047](../../../docs/adr/0047-all-key-handling-via-ikeyhandler.md) with bindings exposed per [ADR-0048](../../../docs/adr/0048-key-handlers-expose-bindings.md). Selection affordance follows [menu selection](../menu-selection/spec.md) ([ADR-0069](../../../docs/adr/0069-unified-menu-selection-affordance.md)); palette + `Label:value` per [ADR-0033](../../../docs/adr/0033-ui-principles-and-configurable-settings.md) / [ADR-0050](../../../docs/adr/0050-ui-alignment-blocks-label-format.md). Uses **ModalSystem.RunModal** (full redraw on key). Closing on cancel restores the previous main-view breadcrumb; confirming proceeds to orderly shutdown per [ADR-0018](../../../docs/adr/0018-shutdown-lock-ordering.md).

## Screenshot

```text
aUdioNLZR/qUit
  Quit AudioAnalyzer?
   Y/Enter: quit    N/Esc: cancel
```

*(Representative layout; regenerate from a screen dump when verifying. The breadcrumb suffix is hacker-styled like other segments, so `quit` renders as `qUit`.)*

## Line reference

**1** — Title breadcrumb (row 0): app-name track plus a hacker-styled suffix (`/qUit`) per [title breadcrumb](../title-breadcrumb/spec.md). The modal sets `TitleBarViewKind.ConfirmationModal` with `ConfirmationBreadcrumbSuffix` = the action verb (here `quit`).

**2** — Prompt line: `  Quit AudioAnalyzer?` (two leading spaces), using the UI palette normal color.

**3** — Choice hint line: three leading spaces (aligning with the unselected [menu selection](../menu-selection/spec.md) prefix), then `Y/Enter: <verb>` in the normal color and `N/Esc: cancel` in the highlighted color to mark cancel as the default. The confirm verb is the modal's `title` (here `quit`); `Y`/`Enter` confirm, `N`/`Esc` cancel.

The modal redraws fully on each key (ModalSystem.RunModal). Closing restores the main view breadcrumb; the quit binding (`Q` / `Esc`) is advertised in the toolbar hint and the dynamic help screen (H) via `GetBindings()` per [ADR-0049](../../../docs/adr/0049-dynamic-help-screen.md).

### Constraints

- **8-column blocks** and **Label:value** formatting per [ADR-0050](../../../docs/adr/0050-ui-alignment-blocks-label-format.md).
- Regenerate screenshot + **Line reference** when layout or semantics change.

## Contract

### Definition of Done

- Screenshot block matches a fresh screen dump when rows or labels change.
- Every screen line in the dump has a matching **Line reference** entry.
- `Q`, `Ctrl+Q`, top-level `Escape`, and `Ctrl+C` all open this modal; none quits the app directly.

### Regression guardrails

- A single accidental `Escape` / `Q` / `Ctrl+C` does **not** terminate the session; it opens (or, for Escape inside the modal, dismisses) this confirmation.
- Escape inside other modals and edit sessions retains its existing cancel/back behavior (only top-level main-loop Escape changes).
- Cross-links to other console-ui specs and ADRs resolve after moves under specs/console-ui/.

### Scenarios

```gherkin
Scenario: Accidental Escape does not quit
  Given the Preset editor mode is active with no modal open
  When the operator presses Escape
  Then the quit confirmation modal opens
  And pressing Escape again cancels and returns to the visualization

Scenario: Deliberate quit is confirmed
  Given any top-level mode is active
  When the operator presses Q and then Y
  Then the application begins orderly shutdown

Scenario: Ctrl+C is intercepted
  Given the application is running
  When the operator presses Ctrl+C
  Then the process is not terminated immediately
  And the quit confirmation modal opens
```
