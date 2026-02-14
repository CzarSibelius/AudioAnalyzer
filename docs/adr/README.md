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

- **Visualizer specs** ([docs/visualizers/](../visualizers/README.md)): Per-visualizer reference (behavior, settings, viewport constraints). Use when adding or changing visualizers; ADRs describe decisions, specs describe what each visualizer does.
