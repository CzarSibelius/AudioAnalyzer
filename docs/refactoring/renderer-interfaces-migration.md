# Renderer interfaces migration roadmap

Plan to reduce component-specific renderer interfaces by using `IUiComponentRenderer<TComponent>` for leaf components.

**Current state (post-consolidation, post-ADR-0057):** The UI uses **HorizontalRowComponent** with **ScrollingTextComponent** children for all single-line rows (title bar, header rows 2–3, toolbar, settings modal hint). The dispatcher (`IUiComponentRenderer<IUiComponent>`) resolves **HorizontalRowComponent** and **VisualizerAreaComponent**. **LabeledRowComponent** and **ILabeledRowRenderer** have been removed (see [ADR-0057](adr/0057-horizontal-row-unified-single-line-rows.md)). TitleBarComponent, ToolbarRowComponent, TitleBarRenderer, and ToolbarRowRenderer were removed earlier (ADR-0052).

**Scope**

- **In scope:** Leaf components rendered by the UI component tree (title bar, labeled row, toolbar row, visualizer area). Replace `ITitleBarRenderer` and the dispatcher’s use of `ILabeledRowRenderer` with `IUiComponentRenderer<TitleBarComponent>` and `IUiComponentRenderer<LabeledRowComponent>`.
- **Out of scope (for this roadmap):** `IVisualizer` (visualization engine, different responsibility); `ISettingsModalRenderer` (modal overlay); `IVisualizationRenderer` / `IHeaderContainer` (region contracts, not leaf renderers). These can be revisited later.

**Design constraint**

The dispatcher currently owns **where** to write (`context.StartRow`), **write-if-changed** for header rows 0–2, and padding/truncation. That design may be obsolete (see “Header write-if-changed” below). If we keep it, leaf renderers must hand content back so the dispatcher can write (and optionally cache); that requires a **render result** type with “lines consumed” and “lines to write.” If we drop write-if-changed, we can simplify (e.g. always write, or have leaves write and return only lines consumed).

---

## Header write-if-changed: validate before locking in

The dispatcher today caches the last content for header rows 0–2 and skips `SetCursorPosition` + `Console.Write` when the line is unchanged (ADR-0030: “Avoid redundant writes when content is unchanged”). **This optimization may have no meaningful impact on performance:** the hot path is TextLayers frame rendering (many lines, layer work, buffers); the header is only 3 lines refreshed every frame. Skipping 0–3 console writes per frame is likely negligible compared to the rest of the frame.

**Before implementing Phase 0, validate:**

- [x] **Decide (done without measurement):** Profile or benchmark with write-if-changed **enabled** vs **disabled** (e.g. always write header lines). Compare frame time or console I/O cost; confirm whether the cache moves the needle.
- [x] Header write-if-changed removed. Dispatcher always writes; cache/WriteLineIfChanged deleted. Documented in ADR-0030. (Original: **Decide:** If the difference is negligible, treat header write-if-changed as **obsolete** and remove it. Then:
  - Dispatcher can **always write** the line (no cache, no `_lastWrittenLine0/1/2`, no `InvalidateHeaderCache`), or
  - Leaf renderers can **write directly** at `context.StartRow` and return only “lines consumed,” so the generic interface need not return line content and `ComponentRenderResult` can be just `LinesConsumed` (simpler migration).
- [ ] If we remove it: delete the cache and `WriteLineIfChanged` from `UiComponentRenderer`; document in ADR-0030 that header skip-write was removed after validation. Optionally update ADR-0030 “Recommended future work” to mark “Toolbar and header skip-write (header implemented)” as reverted.

**Impact on roadmap:** If write-if-changed is removed, Phase 0 can use a simpler result type (e.g. only `LinesConsumed`; leaves that need to output content either write themselves or we still pass content for a single “always write” path without cache). Phases 1–3 are unchanged.

---

## Phase 0: Result type and interface shape

**Goal:** Define a return type that carries “lines consumed” and, if we keep write-if-changed, optional “line content” so the dispatcher can write (and cache) without each leaf doing console I/O. If write-if-changed was removed in validation, “line content” is optional and the design can be simplified (leaves write and return lines consumed, or dispatcher always writes from returned content with no cache).

### 0.1 Introduce `ComponentRenderResult` (Application.Abstractions)

- [ ] Add `ComponentRenderResult` with:
  - `int LinesConsumed` (lines taken, e.g. 1 for title bar).
  - `IReadOnlyList<string>? LineContents`: if non-null, dispatcher writes these at `context.StartRow` (and, if write-if-changed was kept, applies cache); if null, the renderer already wrote (e.g. visualizer area). **If write-if-changed was removed:** we can still use this for “dispatcher always writes” or simplify to leaves writing and only return `LinesConsumed`.
- [ ] Use a struct or small readonly record to avoid allocations where possible.
- [ ] Add static helpers, e.g. `ComponentRenderResult.Line(int linesConsumed, string line)` and `ComponentRenderResult.Written(int linesConsumed)` for “I already wrote.”

### 0.2 Change `IUiComponentRenderer<T>` to return `ComponentRenderResult`

- [ ] Replace `int Render(T component, RenderContext context)` with `ComponentRenderResult Render(T component, RenderContext context)`.
- [ ] Update the single implementation of `IUiComponentRenderer<IUiComponent>` (UiComponentRenderer): in recursion, advance `context.StartRow` by `result.LinesConsumed`; when dispatching to a leaf, use `result.LineContents` to write at `context.StartRow` (with write-if-changed only if that optimization was kept; otherwise always write), or treat null as “already written.”
- [ ] Ensure all current call sites (HeaderContainer, MainContentContainer) still work: they only need `LinesConsumed` to advance; dispatcher handles writing internally.

**Exit criteria:** Build and tests pass; behavior unchanged. If write-if-changed was kept: dispatcher still does header write-if-changed and padding. If it was removed: dispatcher always writes header lines (no cache).

---

## Phase 1: Title bar → `IUiComponentRenderer<TitleBarComponent>`

**Goal:** Remove `ITitleBarRenderer`; title bar is rendered via the generic contract.

### 1.1 Implement `IUiComponentRenderer<TitleBarComponent>` in Console

- [x] TitleBarRenderer implements `IUiComponentRenderer<TitleBarComponent>`; returns `ComponentRenderResult.Line(1, line)`. Registered in ServiceConfiguration.

### 1.2 Dispatcher resolves and calls generic renderer

- [x] In `UiComponentRenderer`, inject `IUiComponentRenderer<TitleBarComponent>` (or resolve from `IServiceProvider` by type when dispatching). For `TitleBarComponent`, call that renderer and use its `ComponentRenderResult` (write `LineContents` at `context.StartRow`, return `LinesConsumed`).
- [x] Remove `ITitleBarRenderer` from `UiComponentRenderer` constructor and from the dispatcher’s switch.
- [x] Remove `ITitleBarRenderer` registration and the interface + file from Console.Abstractions.

### 1.3 Update ADRs and docs

- [x] ADR-0042/0052 already allow leaf renderers as `IUiComponentRenderer<TComponent>`. Component summaries updated.

**Exit criteria:** No references to `ITitleBarRenderer`; title bar still renders correctly. (If write-if-changed was kept: unchanged; if removed: header is still drawn every frame.)

---

## Phase 2: Labeled row → `IUiComponentRenderer<LabeledRowComponent>` (superseded by ADR-0057)

**Note:** As of [ADR-0057](../adr/0057-horizontal-row-unified-single-line-rows.md), all single-line rows use **HorizontalRowComponent**; LabeledRowComponent and ILabeledRowRenderer have been removed. The phase items below are retained for history.

**Goal:** Dispatcher uses `IUiComponentRenderer<LabeledRowComponent>` instead of `ILabeledRowRenderer` for `LabeledRowComponent`. Keep `ILabeledRowRenderer` only if the settings modal (or others) still need “render a row of viewports” without going through the component tree.

### 2.1 Implement `IUiComponentRenderer<LabeledRowComponent>` in Application

- [x] Make `LabeledRowRenderer` implement `IUiComponentRenderer<LabeledRowComponent>`: in `Render(LabeledRowComponent component, RenderContext context)` call existing `RenderRow(component.Viewports, component.Widths, context.Width, context.Palette, context.ScrollSpeed, component.StartSlotIndex)` and return `ComponentRenderResult.Line(1, line)`.
- [x] Register `IUiComponentRenderer<LabeledRowComponent>` → `LabeledRowRenderer` (Application already has LabeledRowRenderer; registration may be in Console’s ServiceConfiguration today for ILabeledRowRenderer — adjust as needed).

### 2.2 Dispatcher uses generic renderer for LabeledRowComponent

- [x] In `UiComponentRenderer`, inject or resolve `IUiComponentRenderer<LabeledRowComponent>`. For `LabeledRowComponent`, call it and use its result; remove the `ILabeledRowRenderer` call from the dispatcher’s labeled-row branch.
- [x] Keep `ILabeledRowRenderer` in constructor for toolbar/modal; dispatcher uses generic for tree. and from the dispatcher.

### 2.3 Consumers of `ILabeledRowRenderer`

- [x] **SettingsModalRenderer** currently uses `ILabeledRowRenderer.RenderRow` for the hint line and any other rows. Options:
  - **A:** Keep `ILabeledRowRenderer` and register it as before; only the dispatcher stops using it. Then we have one fewer consumer (dispatcher) but the interface remains for the modal.
  - **B:** Have the modal build a one-row `LabeledRowComponent` and call `IUiComponentRenderer<LabeledRowComponent>.Render(component, context)` with a synthetic `RenderContext`; the renderer returns content and the modal writes it. That removes `ILabeledRowRenderer` entirely but requires the modal to construct context and possibly a small “write line at row” helper.
- [x] Chose A (keep ILabeledRowRenderer for modal/toolbar). If B: remove `ILabeledRowRenderer` interface and registration; update SettingsModalRenderer to use `IUiComponentRenderer<LabeledRowComponent>`.

### 2.4 Docs and ADRs

- [x] Labeled rows in tree rendered via `IUiComponentRenderer<LabeledRowComponent>`; `ILabeledRowRenderer` retained for non-tree use (modal, toolbar).

**Exit criteria:** No use of `ILabeledRowRenderer` in the dispatcher; header and main content labeled rows render correctly; modal behavior unchanged.

---

## Phase 3 (optional): Toolbar and visualizer area as generic renderers

**Goal:** Make the toolbar row and visualizer area explicit components rendered via generic renderers, so the dispatcher doesn’t special-case them with `IVisualizer` calls inside the same class. Keeps `IVisualizer` as the backend but wraps it behind `IUiComponentRenderer<ToolbarRowComponent>` and `IUiComponentRenderer<VisualizerAreaComponent>`.

### 3.1 `IUiComponentRenderer<ToolbarRowComponent>`

- [x] Added `ToolbarRowRenderer` (Console) holding `IVisualizer`, `ILabeledRowRenderer` (or the new labeled-row renderer), and `UiSettings`; in `Render(ToolbarRowComponent, context)` replicate current `RenderToolbarRow` logic, return `ComponentRenderResult.Line(1, line)` or `Written(1)` depending on whether the dispatcher or the implementation does the write. (Current code writes in the dispatcher for the toolbar; so return line content.)
- [x] Registered; dispatcher calls it for `ToolbarRowComponent` instead of `RenderToolbarRow(context)`.

### 3.2 `IUiComponentRenderer<VisualizerAreaComponent>`

- [ ] Add implementation that holds `IVisualizer`; in `Render(VisualizerAreaComponent, context)` call `_visualizer.Render(context.Snapshot, viewport)` and return `ComponentRenderResult.Written(context.MaxLines)` (already wrote).
- [x] Registered; dispatcher calls it for `VisualizerAreaComponent`; `ResetVisualizerAreaCleared` delegates to it instead of `RenderVisualizerArea(context)`.

### 3.3 Simplify dispatcher

- [ ] Remove direct `IVisualizer` dependency from `UiComponentRenderer`; it only depends on `IUiComponentRenderer<T>` for each leaf type (and optionally `IServiceProvider` to resolve by component type if we don’t want to inject four renderers).
- [x] Dispatcher has no component-type-specific logic beyond resolve-and-call per leaf (four constructor-injected renderers) or `IEnumerable<IUiComponentRenderer<IUiComponent>>` to avoid N constructor parameters (optional).

**Exit criteria:** Dispatcher has no component-type-specific logic beyond “get renderer for T, call Render(component, context), apply result.” IVisualizer remains the visualization engine but is only used inside the toolbar and visualizer-area renderers.

---

## Summary

| Phase | Focus | Interfaces removed / reduced |
|-------|--------|------------------------------|
| 0 | Result type + `IUiComponentRenderer` return value | — |
| 1 | Title bar | Remove `ITitleBarRenderer` |
| 2 | Labeled row | Remove dispatcher’s use of `ILabeledRowRenderer`; optionally remove interface if modal migrates |
| 3 (optional) | Toolbar + visualizer area | Dispatcher no longer depends on `IVisualizer`; only on `IUiComponentRenderer<T>` |

**Risks / mitigations**

- **Signature change:** Changing `IUiComponentRenderer` to return `ComponentRenderResult` touches the single dispatcher and all containers; keep the change localized and run full regression (manual or automated).
- **Write-if-changed:** If kept, ensure the result type and dispatcher logic preserve exact behavior for header rows 0–2. If removed after validation, delete the cache, `WriteLineIfChanged`, and `InvalidateHeaderCache` and ensure no callers depend on them.
- **DI:** Resolving `IUiComponentRenderer<TitleBarComponent>` etc. by type may require registering each T in DI; document the convention so new leaf types are easy to add.

**References**

- ADR-0042 (UI component renderer + key handler; “renderers stay component-specific”).
- ADR-0052 (UI container, component, generic component renderer).
- [god-object-plan.md](god-object-plan.md) (existing refactoring tasks).
