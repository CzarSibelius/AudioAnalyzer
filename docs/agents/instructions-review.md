# Agent instructions review

This document records the outcome of organizing agent instructions into AGENTS.md and docs/agents/*. It lists contradictions that were found (and how they were resolved), and instructions flagged for possible deletion or simplification.

## 1. Contradictions

### Resolved: Screen dump hotkey

- **Conflict**: `.cursor/rules/adr.mdc` stated the screen-dump hotkey was **PrintScreen**. ADR-0046 and README/copilot-instructions state **Ctrl+Shift+E**.
- **Resolution**: Updated adr.mdc to say "hotkey is Ctrl+Shift+E" so it matches ADR-0046 and the rest of the repo.

### Clarified: Help menu updates

- **Potential conflict**: `.github/copilot-instructions.md` said "Help menu (H key) must document every available command — always update `ShowHelpMenu()` when adding new controls." ADR-0049 says help content is **driven by the active screen** and **assembled from handler bindings (GetBindings())**.
- **Resolution**: No code change. The agent doc [ui-and-console.md](ui-and-console.md) now says: when adding new key-handled features, ensure the **handler exposes its bindings** so help stays accurate (no mention of manually editing ShowHelpMenu). If your codebase still has a `ShowHelpMenu()` that needs manual updates for some entries, decide which you want:
  - **A)** Keep dynamic help only (GetBindings); remove any instruction to "update ShowHelpMenu()".
  - **B)** Keep both: some content from GetBindings, some from manual blurbs; then document when to update which.

## 2. Flagged for deletion or simplification

These are candidates to remove or shorten; they are redundant, vague, or obvious.

| Instruction | Location | Reason |
|-------------|----------|--------|
| "Write clean code" / "Use meaningful variable names" (generic) | copilot-instructions (Code Style) | Partially redundant; agents already tend to use clear names. Kept a **concrete** example in csharp-and-static-analysis.md ("e.g. barHeight, normalizedMag") and dropped the generic "write clean code" line. |
| "Do not silently contradict documented decisions" | adr.mdc, copilot | Actionable and important; **kept**. |
| Duplicate "no empty try-catch" | no-empty-catch.mdc, csharp-standards.mdc, static-analysis | Same rule in three places. **Kept** in one canonical form in docs/agents/csharp-and-static-analysis.md; .cursor rules remain for glob-specific application. No deletion recommended; consolidation only. |
| Duplicate "one file per class" | one-file-per-class.mdc, csharp-standards.mdc, ADR-0016 | Same rule in multiple places. **Kept** in csharp-and-static-analysis.md and ADR; .cursor rules stay. No deletion. |
| "Build must succeed before committing" | copilot, git-workflow.md | Kept in root AGENTS.md and git-workflow.md; useful. |
| "Use descriptive commit messages" | copilot | Kept in git-workflow.md; actionable. |
| "Update display when terminal is resized" | copilot (Terminal Output) | Actionable; kept in ui-and-console.md. |
| High-level "minimize allocations in hot paths" | copilot | Kept in testing-and-verification.md and ADR-0030 reference; actionable. |

Nothing was **deleted** from the repo in this pass; redundant content was **consolidated** into the new docs/agents/* files. You can trim `.github/copilot-instructions.md` by removing sections that are now fully covered by AGENTS.md + docs/agents/* (and ADR index), if you want a single source of truth.

## 3. Suggested docs folder structure

```
docs/
  adr/                    # Existing ADR index and numbered ADRs
  configuration-reference.md  # Preset/show/palette JSON, appsettings, dependency versions
  agents/                 # Agent instructions by topic
    AGENTS.md             # Index of agent docs (topic table)
    README.md             # Stub: points to AGENTS.md for folder landing
    csharp-and-static-analysis.md
    documentation.md
    ui-and-console.md
    visualizers.md
    testing-and-verification.md
    git-workflow.md
    architecture-overview.md
    instructions-review.md  # This file
  refactoring/            # Existing
  visualizers/            # Existing visualizer specs
  ui-spec-format.md        # Existing
  ui-components.md         # Existing (if present)
```

Root: **README.md** (human-focused: what the app is, audience, prerequisites, limitations; link **docs/getting-started.md** for build/run and first session). Root **AGENTS.md**: build/test/format, link to docs/adr/README.md, and table of links to docs/agents/*. **docs/agents/AGENTS.md**: same topic index inside the agents folder (with **docs/agents/README.md** stub for folder landing).
