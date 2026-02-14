# ADR-0016: C# documentation and file organization

**Status**: Accepted

## Context

Consistent C# conventions improve maintainability and help both humans and AI agents produce readable, navigable code. Two conventions were identified:

- **XML summaries**: Empty or placeholder summaries provide no value; classes and interfaces should have meaningful documentation when documented.
- **File organization**: Multiple types in one file make navigation harder and can obscure structure; one primary type per file is easier to find and reason about.

## Decision

- **Non-empty summaries**: Summaries for public classes and interfaces must not be empty. If a documentation comment exists, the `<summary>` tag must contain meaningful text. This is documented in `.cursor/rules/xml-documentation.mdc` and `.cursor/rules/csharp-standards.mdc`. No build-time analyzer is enabled (Roslynator does not cover empty summaries).

- **One file per class**: Prefer one file per class, interface, or struct. Each top-level type should generally have its own file. Allowed exceptions: small, tightly coupled types (e.g. DTOs used only by one consumer) when justified. Roslynator RCS1060 is enabled via `.editorconfig` (`dotnet_diagnostic.RCS1060.severity = warning`). Cursor rules: `.cursor/rules/one-file-per-class.mdc`, `.cursor/rules/csharp-standards.mdc`.

## Consequences

- Agents and contributors follow XML documentation and file organization rules when editing C#.
- RCS1060 produces build warnings for multiple types per file; violations were fixed by splitting files.
- Static analysis rules are summarized in `.cursor/rules/static-analysis.mdc`.
