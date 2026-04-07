# Refactoring Plans

Ongoing refactoring task lists for agents and developers. Mark tasks complete with `[x]` when implemented.

## God Object Refactoring

Targets: ApplicationShell, AnalysisEngine, SettingsModal.

**[god-object-plan.md](god-object-plan.md)** — Phase 1 (ApplicationShell), Phase 2 (AnalysisEngine), Phase 3 (SettingsModal), and documentation tasks.

## Renderer interfaces

Reduce component-specific renderer interfaces by using `IUiComponentRenderer<TComponent>` for leaf components.

**[renderer-interfaces-migration.md](renderer-interfaces-migration.md)** — Phase 0 (result type), Phase 1 (title bar), Phase 2 (labeled row), Phase 3 optional (toolbar + visualizer area).

## Ableton Link and external BPM

**[ableton-link/README.md](ableton-link/README.md)** — Phased tasks: (1) external beat sources without Link, (2) native Link wrapper, (3) app integration + UI.

## Deferred: Application UI palette in General settings

**General Settings hub** ([ADR-0061](../adr/0061-general-settings-mode.md)): Hub includes audio input, **Application name**, **Default asset folder** (`UiSettings.DefaultAssetFolderPath`), and **UI theme** (`UiSettings.UiThemeId` → `themes/*.json`, [ADR-0071](../adr/0071-ui-themes-separate-from-palettes.md)). **Editing `UiSettings.Palette`** (semantic UI colors in appsettings) from the hub is **not implemented** when using **(Custom)** — users can author themes via **N** in the theme modal or edit JSON.
