# C# and static analysis

## No empty try-catch

Never leave empty `catch` blocks. Either remove the try-catch or add real error handling (log, rethrow with context, or handle meaningfully). Intentional swallow is allowed only with an explicit comment (e.g. "Console unavailable: swallow to avoid crash").

## Braces for control flow

Always use braces for `if`, `else`, `for`, `foreach`, `while`, and `using` bodies, even when the body is a single statement.

## XML documentation

Summaries for public classes and interfaces must not be empty. If a documentation comment exists, the `<summary>` tag must contain meaningful text. See ADR-0016.

## One file per class

Prefer one file per class, interface, or struct. File name should match the primary type. Small, tightly coupled types (e.g. DTOs used only by one consumer) may stay in the same file when justified. Enforced by Roslynator RCS1060 at build time.

## After making code changes

1. Check linter diagnostics for the modified files; fix all errors and fix warnings unless the rule is explicitly disabled for that line.
2. Run `dotnet build .\AudioAnalyzer.sln`. The build must succeed with **0 warnings**. Fix any new analyzer warnings (CA*, IDE*, RCS*, MSB*) before marking work done.

Code style enforced at build time includes: empty catch blocks (RCS1075), one file per class (RCS1060), braces for control flow (IDE0011), locale-invariant APIs (CA1305), platform compatibility (CA1416), and static members where applicable (CA1822). See `.editorconfig` and `.cursor/rules/csharp-standards.mdc`.

Do not complete C# edits without addressing linter errors in the files you changed.

## Code style preferences

- C# 10+; top-level statements where appropriate.
- Prefer explicit types over `var` for clarity in audio/display code.
- Keep methods under 50 lines; extract complex logic into separate methods.
- Use meaningful variable names (e.g. `barHeight`, `normalizedMag`, not `h`, `n`).
