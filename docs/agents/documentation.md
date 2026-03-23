# Documentation

When adding or changing features:

1. **Update feature documentation**: Update the README and any other feature docs (e.g. `docs/` or a dedicated feature file). Document new capabilities, options, and behavior in the appropriate place.

2. **Visualizer specs**: When adding or changing visualizers, create or update the corresponding spec in `docs/visualizers/` (see `docs/visualizers/README.md` for the index and format).

3. **UI specs**: When changing console layout, header, modals, or any documented screen, read and update the relevant UI spec in `docs/` (see `docs/ui-spec-format.md`, `docs/ui-spec-application-modes.md` for top-level modes, and `.cursor/rules/ui-specs.mdc`). Regenerate screenshot and line reference when the number or meaning of lines changes. Use screen dump (Ctrl+Shift+E or `--dump-after N` / `--dump-path`) to capture ASCII output for specs.

4. **Update the README**: When changing behavior, dependencies, or usage, update README.md so these sections stay accurate: Prerequisites, How to Run, Usage, What It Does, Dependencies, Notes.

5. **Document deferred work**: Every "will do later" task must be documented. When deferring work, add it to a task list in `docs/refactoring/`, a new or existing ADR, the refactoring README, or a dedicated TODO/backlog file. Do not leave deferred work only in code comments or conversation.

Do not complete feature work without updating the relevant documentation.
