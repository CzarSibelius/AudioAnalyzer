# ADR-0011: No empty try-catch blocks

**Status**: Accepted

## Context

Empty `catch` blocks silently swallow exceptions, hide failures, and make debugging difficult. They violate the principle that exceptions should be handled explicitly—either by logging, rethrowing with context, or handling meaningfully. Both humans and AI agents may introduce empty catches when writing C#, so the rule must be enforced consistently.

## Decision

- **Never leave empty `catch` blocks.** Either remove the try-catch or add real error handling (log, rethrow with context, or handle meaningfully).
- **Cursor rules**: An always-applied rule (`.cursor/rules/no-empty-catch.mdc`) guides agents and humans when editing C#. The rule is also documented in `.cursor/rules/csharp-standards.mdc`.
- **Build-time enforcement**: Roslynator RCS1075 is enabled via `.editorconfig` (`dotnet_diagnostic.RCS1075.severity = warning`). Empty catch blocks produce build warnings.
- **Intentional swallows**: When an exception must be swallowed (e.g. to avoid crashing when console writes fail), use an explicit discard and a comment documenting the reason: `catch (Exception ex) { _ = ex; /* Reason */ }`.

## Consequences

- All `catch` blocks must contain at least one statement or an explicit comment explaining why the exception is intentionally swallowed.
- New empty catches will cause build warnings and should be fixed.
- Cursor agents and contributors see the rule when editing C# files.
