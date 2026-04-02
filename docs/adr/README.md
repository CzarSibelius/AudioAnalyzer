# Architecture Decision Records (ADRs)

This project uses **Architecture Decision Records** to capture important design and behavior decisions in a single place. ADRs help both humans and AI agents understand why the codebase works the way it does and what to follow when adding or changing features.

## What are ADRs?

An ADR is a short document that records:

- **Context**: What situation or requirement led to the decision
- **Decision**: What we decided to do
- **Consequences**: Trade-offs and how the decision affects code and UX

Decisions are numbered and kept under version control so the rationale stays with the project.

## Index of ADRs

| ADR | Title | Status |
|-----|--------|--------|
| [0001](0001-automatic-save.md) | Automatic persistence of settings (no manual save) | Accepted |
| [0002](0002-per-visualizer-settings-and-color-palette.md) | Per-visualizer settings and reusable color palette | Accepted |
| [0003](0003-24bit-palettes-and-cycling.md) | 24-bit true color, JSON palette files, and palette cycling | Accepted |
| [0004](0004-visualizer-encapsulation.md) | Visualizer encapsulation — keep visualizer logic inside visualizers | Accepted |
| [0005](0005-layered-visualizer-cell-buffer.md) | Layered visualizer with cell buffer and per-layer config | Accepted |
| [0006](0006-modal-system.md) | Modal system for console UI | Accepted |
| [0007](0007-visualizer-subfolder-structure.md) | Visualizer subfolder structure | Accepted |
| [0008](0008-visualizer-settings-di.md) | Visualizer settings via Dependency Injection | Accepted |
| [0009](0009-per-visualizer-palette.md) | Per-visualizer palette selection | Accepted |
| [0010](0010-appsettings-visualizer-settings-separation.md) | AppSettings and VisualizerSettings separation | Accepted |
| [0011](0011-no-empty-catch-blocks.md) | No empty try-catch blocks | Accepted |
| [0012](0012-visualizer-exception-handling.md) | Visualizer exception handling — show error in viewport | Accepted |
| [0013](0013-secure-nuget-packages.md) | Avoid insecure or obsolete NuGet packages | Accepted |
| [0014](0014-visualizers-as-layers.md) | Visualizers as layers — deprecate IVisualizer for new development | Accepted |
| [0015](0015-visualizer-settings-in-domain.md) | Visualizer settings in Domain, IVisualizerSettingsRepository in Application | Accepted |
| [0016](0016-csharp-documentation-and-file-organization.md) | C# documentation and file organization | Accepted |
| [0017](0017-demo-mode-synthetic-audio.md) | Demo mode — synthetic audio input | Accepted |
| [0018](0018-shutdown-lock-ordering.md) | Shutdown and device-switch lock ordering | Accepted |
| [0019](0019-preset-textlayers-configuration.md) | Preset — named TextLayers configuration | Accepted |
| [0020](0020-ui-text-components-scrolling-and-ellipsis.md) | UI text components — scrolling for dynamic, ellipsis for static | Accepted |
| [0021](0021-textlayer-settings-common-custom.md) | TextLayerSettings — common properties plus Custom JSON | Accepted |
| [0022](0022-presets-in-own-files.md) | Presets in own files (palette-style storage) | Accepted |
| [0023](0023-settings-modal-layer-editing.md) | Settings modal for layer settings editing | Accepted |
| [0024](0024-analysissnapshot-frame-context.md) | AnalysisSnapshot as frame context | Accepted |
| [0025](0025-reflection-based-layer-settings.md) | Reflection-based layer settings discovery | Accepted |
| [0026](0026-console-ui-architecture.md) | Console UI architecture — modular presentation layer | Accepted |
| [0027](0027-now-playing-header.md) | Now-playing song in header | Accepted |
| [0028](0028-layer-dependency-injection.md) | Layer dependency injection | Accepted |
| [0029](0029-no-settings-migration.md) | No settings migration — backup and reset | Accepted |
| [0030](0030-performance-priority.md) | Performance as top priority | Accepted |
| [0031](0031-show-preset-collection.md) | Show — preset collection for performance auto-cycling | Accepted |
| [0032](0032-typed-display-text.md) | Typed display text (PlainText vs AnsiText) | Accepted |
| [0033](0033-ui-principles-and-configurable-settings.md) | UI principles and configurable settings | Accepted |
| [0034](0034-viewport-label-hotkey-hints.md) | Viewport label hotkey hints (superseded; no label hotkey API) | Superseded |
| [0035](0035-modal-dependency-injection.md) | Modal dependency injection | Accepted |
| [0036](0036-title-bar-injectable-component.md) | Title bar as injectable component | Accepted |
| [0037](0037-scrolling-text-viewport-injectable-service.md) | Scrolling text viewport as injectable service with data/render split | Accepted |
| [0038](0038-header-refresh-independent-of-audio.md) | Header refresh independent of audio pipeline | Accepted |
| [0039](0039-display-width-terminal-columns.md) | Display width for terminal columns (emoji, wide characters) | Accepted |
| [0040](0040-dependency-injection-preference.md) | Prefer dependency injection unless performance requires otherwise | Accepted |
| [0041](0041-god-object-refactoring-strategy.md) | God object refactoring strategy (ApplicationShell, AnalysisEngine, SettingsModal) | Accepted |
| [0042](0042-ui-component-renderer-keyhandler-pattern.md) | UI component pattern — renderer plus IKeyHandler&lt;TContext&gt; | Accepted |
| [0043](0043-textlayer-state-store.md) | Text layer state store — per-layer state via ITextLayerStateStore | Accepted |
| [0044](0044-textlayer-renderer-generic-state-type.md) | Text layer renderer generic interface — state type in contract | Accepted |
| [0045](0045-max-text-layers-constant.md) | Global maximum of 9 text layers as a single constant (not user-editable) | Accepted |
| [0046](0046-screen-dump-ascii-screenshot.md) | Screen dump (ASCII screenshot) for bug reports and AI agents | Accepted |
| [0047](0047-all-key-handling-via-ikeyhandler.md) | All key handling via IKeyHandler | Accepted |
| [0048](0048-key-handlers-expose-bindings.md) | Key handlers expose bindings for discovery | Accepted |
| [0049](0049-dynamic-help-screen.md) | Dynamic help screen — content based on active view | Accepted |
| [0050](0050-ui-alignment-blocks-label-format.md) | UI alignment, 8-character blocks, and label format | Accepted |
| [0051](0051-viewport-as-data-layouts-compose.md) | Viewport as data; layouts compose viewports; row renderer owns scroll state | Accepted |
| [0052](0052-ui-container-component-renderer.md) | UI container, component, and generic component renderer | Accepted |
| [0053](0053-iuicomponent-all-ui.md) | Use IUiComponent for all UI (including modals) | Accepted |
| [0054](0054-ui-component-state-ownership.md) | UI component state ownership and IUiStateUpdater | Accepted |
| [0055](0055-layer-specific-beat-reaction.md) | Layer-specific beat reaction (no common BeatReaction) | Accepted |
| [0056](0056-scrolling-text-as-uicomponent.md) | Scrolling text as IUiComponent with state (ScrollingTextComponent, HorizontalRowComponent) | Accepted |
| [0057](0057-horizontal-row-unified-single-line-rows.md) | HorizontalRowComponent for all single-line viewport rows; LabeledRowComponent/ILabeledRowRenderer removed | Accepted |
| [0058](0058-layer-render-bounds.md) | Per-layer render bounds (normalized rect), clip-on-Set, Mirror in-region, visual bounds editor | Accepted |
| [0059](0059-fill-blendover-space-as-black.md) | Fill BlendOver — optional treat space cells as black for blending | Accepted |
| [0060](0060-universal-title-breadcrumb.md) | Universal title breadcrumb — all surfaces, navigation context, two tracks | Accepted |
| [0061](0061-general-settings-mode.md) | General Settings mode — Tab cycle, hub UI, app-wide settings (MVP) | Accepted |
| [0062](0062-application-mode-classes.md) | Application modes as `IApplicationMode` classes — layout, toolbar, header chrome | Accepted |
| [0063](0063-uniform-settings-list-and-palette-drawing.md) | Uniform settings list and palette drawing (shared helpers, hub rows via HorizontalRowComponent) | Accepted |
| [0064](0064-test-project-mirrors-production-layout.md) | Test project folder layout mirrors production (single test project) | Accepted |
| [0065](0065-ui-theme-from-layer-palettes.md) | UI theme from shared layer palette files — `UiThemePaletteId`, hub + modal, `IUiThemeResolver` | Accepted |
| [0066](0066-bpm-source-and-ableton-link.md) | BPM source (audio / demo / Ableton Link), `IBeatTimingSource`, native `link_shim` | Accepted |
| [0067](0067-60fps-target-and-render-fps-overlay.md) | 60 FPS target for main redraw, `MeasuredMainRenderFps`, optional toolbar FPS overlay | Accepted |

## Process

1. **Creating a new ADR**: Copy [0000-template.md](0000-template.md) to a new file `docs/adr/NNNN-short-title.md` (next number in sequence). Fill in Title, Status, Context, Decision, and Consequences. One decision per ADR.
2. **Updating this index**: Add the new ADR to the table above with a link and status.
3. **Agent instructions**: If the decision affects how agents should implement features, ensure `.cursor/rules/adr.mdc` and `.github/copilot-instructions.md` are updated so agents follow the ADR.
4. **Superseding or deprecating**: To replace an ADR, add a new ADR that supersedes it and set the old one’s status to "Superseded by [NNNN](NNNN-title.md)".

## For agents

When changing architecture, persistence, or user-facing behavior:

- Read the relevant ADRs in `docs/adr/` and align your implementation with them.
- When making a new architectural decision, add a new ADR and update this index.

## Related documentation

- **UI components** ([ui-components.md](../ui-components.md)): Inventory of console UI components (shell, header, modals, viewport primitives, key handlers). Use when navigating or extending the console UI.
- **Visualizer specs** ([../visualizers/README.md](../visualizers/README.md)): Per-layer and TextLayers reference (behavior, settings, viewport constraints). Use when adding or changing layers; ADRs describe decisions, specs describe what each layer does.
