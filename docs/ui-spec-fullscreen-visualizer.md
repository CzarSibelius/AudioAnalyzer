# UI spec: Visualizer fullscreen (F)

This spec follows [ui-spec-format.md](ui-spec-format.md). It describes **visualizer fullscreen**: hiding the header and main toolbar row so layer content uses the console from the top, and how that interacts with **overlay modals** and **per-mode** configuration.

**Related:** [ui-spec-application-modes.md](ui-spec-application-modes.md) (mode index), [ADR-0062](adr/0062-application-mode-classes.md) (`IApplicationMode`, `AllowsVisualizerFullscreen`), [ADR-0061](adr/0061-general-settings-mode.md) (General settings: **F** does not toggle fullscreen), [ADR-0067](adr/0067-60fps-target-and-render-fps-overlay.md) (FPS overlay on toolbar — omitted when toolbar is hidden), [ADR-0046](adr/0046-screen-dump-ascii-screenshot.md) (screen dump). Overlay modals: [ui-spec-settings-surfaces.md](ui-spec-settings-surfaces.md), [ui-spec-preset-settings-modal.md](ui-spec-preset-settings-modal.md).

## When fullscreen applies

| Mode | `AllowsVisualizerFullscreen` | **F** behavior |
|------|------------------------------|----------------|
| **Preset editor** (`ApplicationMode.PresetEditor`) | `true` ([`PresetEditorApplicationMode`](../src/AudioAnalyzer.Console/PresetEditorApplicationMode.cs)) | Toggles `IDisplayState.FullScreen` |
| **Show play** (`ApplicationMode.ShowPlay`) | `true` ([`ShowPlayApplicationMode`](../src/AudioAnalyzer.Console/ShowPlayApplicationMode.cs)) | Same |
| **General settings** (`ApplicationMode.Settings`) | `false` ([`SettingsApplicationMode`](../src/AudioAnalyzer.Console/SettingsApplicationMode.cs)) | **F** is ignored; entering General settings clears fullscreen ([`ModeTransitionService`](../src/AudioAnalyzer.Console/ModeTransitionService.cs)) |

Fullscreen is a **runtime** flag on [`IDisplayState`](../src/AudioAnalyzer.Console/Abstractions/IDisplayState.cs), not persisted in `appsettings`.

## Layout and rows

- **Toggle on:** [**MainLoopKeyHandler**](../src/AudioAnalyzer.Console/KeyHandling/MainLoopKeyHandler.cs) sets `FullScreen`, clears the console, hides the cursor, and triggers a redraw. [`VisualizationOrchestrator`](../src/AudioAnalyzer.Console/VisualizationOrchestrator.cs) sets `VisualizationFrameContext.DisplayStartRow` to **0** and skips drawing the header for that frame cadence.
- **Toggle off:** Fullscreen cleared; **full header** redraw restores the normal toolbar stack ([`MainContentContainer`](../src/AudioAnalyzer.Console/Console/MainContentContainer.cs) again composes the main toolbar row + `VisualizerAreaComponent`).
- **Edge-to-edge (fullscreen, no overlay):** Main content is **only** [`VisualizerAreaComponent`](../src/AudioAnalyzer.Application/Abstractions/VisualizerAreaComponent.cs) — no header rows and no TextLayers toolbar row. The visualizer viewport starts at **row 0** (0-based). One bottom line of the console is left unused for layout margin (same idea as non-fullscreen visualizer height math).
- **Fullscreen + overlay modal:** Preset settings (**S**) and Show edit (**S** in Show play) use [`ModalSystem.RunOverlayModal`](../src/AudioAnalyzer.Console/Console/ModalSystem.cs), which paints the **top N rows** (e.g. 16 for settings). [`IVisualizationOrchestrator.SetOverlayActive`](../src/AudioAnalyzer.Console/Abstractions/IVisualizationOrchestrator.cs) raises `DisplayStartRow` to that row count so the **main renderer** draws the visualizer **below** the overlay. `MainContentContainer` must **not** reset `DisplayStartRow` to 0 while fullscreen if the orchestrator already reserved top rows for the overlay.

## Keys (high level)

Key routing is unchanged: [`ApplicationShell`](../src/AudioAnalyzer.Console/ApplicationShell.cs) tries the active mode’s visualizer keys first (`MainContentContainer.HandleKey` → `IApplicationMode.TryHandleVisualizerKeys`), then the main-loop handler (**Tab**, **V**, **S**, **D**, **H**, **+/-**, **P**, **F**, **Ctrl+Shift+E**, **Escape**). Fullscreen does not disable layer keys (**1–9**, **←/→**, etc.) on the visualizer. Authoritative bindings: dynamic help (**H**) and [`GetBindings()`](../src/AudioAnalyzer.Console/KeyHandling/MainLoopKeyHandler.cs) (note **V** is labeled Preset-editor-only in help; the handler still matches **V** in other modes — see implementation if behavior is tightened later).

## Screenshot

Regenerate from a local Windows console after **F** in Preset editor (Ctrl+Shift+E per [ADR-0046](adr/0046-screen-dump-ascii-screenshot.md)) so the dump matches your terminal width and layer content. The block below is a **placeholder** (fewer lines than a typical window).

```text
      ..       .:.        -#=+-+=+:        .:.       ..     
       :.      ::          -=*.*+-          ::      .:       
       :       .:           :: -.           ::       .       
       :.      ::         ..      .         ::      .:       
       ..      .:      ..  ..   ..  .       :.      ..       
```

## Line reference

- **1** — First row of the visualizer viewport (fullscreen: no title/header/toolbar above).
- **2** — Visualizer content.
- **3** — Visualizer content.
- **4** — Visualizer content.
- **5** — Visualizer content (placeholder ends here; a real capture continues through the viewport height minus bottom margin).

## Fullscreen + preset or Show overlay (no separate screenshot)

While fullscreen, opening **S** draws the modal in rows **0 … N−1**; the visualizer continues in the rows **below** `DisplayStartRow` (see [`SettingsModal`](../src/AudioAnalyzer.Console/Console/SettingsModal.cs) / [`ShowEditModal`](../src/AudioAnalyzer.Console/Console/ShowEditModal.cs) overlay row counts). Idle animation uses `onIdleVisualizationTick` → orchestrator `Redraw` without smearing the overlay. For modal line layout and controls, use [ui-spec-preset-settings-modal.md](ui-spec-preset-settings-modal.md) and the Show edit section of [ui-spec-settings-surfaces.md](ui-spec-settings-surfaces.md).
