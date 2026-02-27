# ADR-0048: Key handlers expose bindings for discovery

**Status**: Accepted

## Context

Help content and other features need to know which keys each key handler uses. Today this information is duplicated: handlers implement `Handle(ConsoleKeyInfo, TContext)` with switch/case logic, while the help modal (HelpModal.DrawContent) hardcodes the same bindings as literal strings. When a handler gains a new key or changes behavior, the help can drift out of sync. A single source of truth should be the handlers themselves. A future dynamic help screen (and possibly other consumers such as accessibility or config UI) will need to discover bindings from all handlers without hardcoding.

## Decision

1. **Method on IKeyHandler**: Add a method on the existing `IKeyHandler<TContext>` interface that returns the key bindings that handler supports. The preferred way to expose bindings is this method on the current interface so all handlers are discoverable through one contract.

2. **Return type**: The method returns a list of binding entries. Each entry describes:
   - **Key** (and modifiers where relevant, e.g. Ctrl+Shift+E) so consumers can display "Ctrl+Shift+E" or "Tab".
   - **Description** (short text for help, e.g. "Dump screen to file").
   - Optionally a **section/category** (e.g. "Main", "Preset modal") so the help UI can group by handler or section without hardcoding section names.

   Define a small DTO (e.g. `KeyBinding` or `KeyBindingInfo`) in Application.Abstractions so both Application and Console/Visualizers can reference it. The method is parameterless; bindings are static per handler and do not depend on TContext.

3. **Implementations**: Every `IKeyHandler<TContext>` implementation must implement this method. The returned list must stay in sync with what `Handle()` actually handles.

4. **Consumers**: The primary consumer is a future dynamic help screen that replaces or augments the current hardcoded HelpModal content; other uses (e.g. accessibility, config UI) may follow.

## Implementation: binding table drives both discovery and handling

To keep `GetBindings()` and `Handle()` in sync, handlers use a **single source of truth**: a list of matchable binding entries. Each entry has:

- **Matcher**: `Func<ConsoleKeyInfo, bool>` — returns true when the key matches (e.g. one entry can match "+/-" by accepting OemPlus, Add, OemMinus, Subtract).
- **Action**: `Func<ConsoleKeyInfo, TContext, bool>` — invoked when the key matches; return value is the `Handle()` result (true = handled).
- **Display**: Key string, Description, Section — used to build the `KeyBinding` DTO for discovery.

Then:

- `GetBindings()` returns `entries.Select(e => e.ToKeyBinding()).ToList()`.
- `Handle()` loops: `foreach (var e in entries) { if (e.Matches(key)) return e.Action(key, context); } return false;`.

The public type remains `KeyBinding` (display-only DTO in Application.Abstractions). Matchable entries are an internal implementation detail:

- **Console handlers**: Use [KeyBindingEntry&lt;TContext&gt;](../../src/AudioAnalyzer.Console/KeyHandling/KeyBindingEntry.cs) in the Console project. Handlers with sub-states (e.g. Renaming, focus) keep a short guard at the top of `Handle()` for those states, then run the binding loop for the main key set.
- **Visualizers**: The Console project is not referenced. Handlers (e.g. TextLayersKeyHandler) define a local binding-entry type (same shape: Matcher, Action, Key, Description, Section) and use it the same way.

New key handlers should implement both `GetBindings()` and `Handle()` from one binding table so help and behavior cannot drift.

## Consequences

- Every existing key handler gains a new method and must return an accurate list of bindings.
- Future dynamic help can aggregate bindings from all registered handlers via the same interface.
- Agent instructions (`.cursor/rules/adr.mdc`, `.github/copilot-instructions.md`) are updated so new key handlers must implement the bindings method.
- Bindings are implemented as a table (matchable entries) that drives both discovery and handling; see Implementation section above.
- References: [IKeyHandler](../../src/AudioAnalyzer.Application/Abstractions/IKeyHandler.cs), [ADR-0047](0047-all-key-handling-via-ikeyhandler.md), [ADR-0042](0042-ui-component-renderer-keyhandler-pattern.md).
