# Refactoring Plans

Ongoing refactoring task lists for agents and developers. Mark tasks complete with `[x]` when implemented.

## God Object Refactoring

Targets: ApplicationShell, AnalysisEngine, SettingsModal.

**[god-object-plan.md](god-object-plan.md)** — Phase 1 (ApplicationShell), Phase 2 (AnalysisEngine), Phase 3 (SettingsModal), and documentation tasks.

## Renderer interfaces

Reduce component-specific renderer interfaces by using `IUiComponentRenderer<TComponent>` for leaf components.

**[renderer-interfaces-migration.md](renderer-interfaces-migration.md)** — Phase 0 (result type), Phase 1 (title bar), Phase 2 (labeled row), Phase 3 optional (toolbar + visualizer area).
