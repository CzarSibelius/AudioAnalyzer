# Documentation

When adding or changing features:

1. **Update feature documentation**: Keep the root [README.md](../../README.md) accurate for **what the app is**, **prerequisites**, **how to run**, and short user-facing tips. Put **preset/show/palette JSON**, **`appsettings.json` structure**, and **NuGet dependency versions** in [docs/configuration-reference.md](../configuration-reference.md) when those change. Use other `docs/` files (visualizer specs, UI specs, ADRs) as today. **New source paths**: follow [project-structure/README.md](project-structure/README.md) for the target project.

2. **Visualizer specs**: When adding or changing visualizers, create or update the corresponding spec in `docs/visualizers/` (see `docs/visualizers/README.md` for the index and format).

3. **UI specs**: When changing console layout, header, modals, or any documented screen, read and update the relevant UI spec in `docs/` (see `docs/ui-spec-format.md`, `docs/ui-spec-application-modes.md` for top-level modes, and `.cursor/rules/ui-specs.mdc`). Regenerate screenshot and line reference when the number or meaning of lines changes. Use screen dump (Ctrl+Shift+E or `--dump-after N` / `--dump-path`) to capture ASCII output for specs.

4. **README vs. deep docs**: When user-facing behavior or run requirements change, update README.md. When configuration file formats, JSON examples, or pinned dependency versions change, update `docs/configuration-reference.md`. Build, test, format, static analysis, test suite overview, and `IVisualizer`/composite-renderer notes live under `docs/agents/` (see [README.md](README.md) index)—update the relevant topic file instead of expanding the root README.

5. **Document deferred work**: Every "will do later" task must be documented. When deferring work, add it to a task list in `docs/refactoring/`, a new or existing ADR, the refactoring README, or a dedicated TODO/backlog file. Do not leave deferred work only in code comments or conversation.

Do not complete feature work without updating the relevant documentation.
