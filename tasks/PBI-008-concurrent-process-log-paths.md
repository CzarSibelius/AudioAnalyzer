# PBI-008: Concurrent process instances — per-process log path

**Transient work item** — close after merge. **State** lives in [specs/infrastructure/concurrent-process-instances/spec.md](../specs/infrastructure/concurrent-process-instances/spec.md); this file is the **delivery delta**.

## Directive

Implement **ADR-0083** so multiple AudioAnalyzer processes can run with file logging enabled without `IOException` on the default log file.

**In scope**

- `LogFilePathResolver`: expand **`{ProcessId}`** in `Logging.FilePath` when resolving the absolute path (relative to base directory when not rooted); when `FilePath` is empty/null/whitespace, resolve to the same per-process default as ADR-0083 (e.g. under `logs/` with process id in the file name).
- Update shipped **`src/AudioAnalyzer.Console/appsettings.json`** default `Logging.FilePath` to use the placeholder (or equivalent so defaults match the spec).
- **`AppLoggingSettings.FilePath`**: XML documentation for placeholder syntax.
- **`docs/configuration-reference.md`**: placeholders, default behavior, caveat for fixed paths without `{ProcessId}` (same commit as code).
- **Unit tests** under `tests/AudioAnalyzer.Tests/` (mirror Infrastructure layout): resolver covers empty default, relative path with `{ProcessId}`, absolute path with `{ProcessId}`; assert expanded path contains `Environment.ProcessId` where applicable.

**Out of scope**

- Mutex or shared single-file multi-process append; widening `FileShare` alone as the fix.
- Preset / appsettings concurrent-write behavior (see spec **Out of scope**).
- Optional `{Guid}` placeholder unless trivial to add with tests; ADR lists it as optional later.

## Context pointer

- Primary spec: [specs/infrastructure/concurrent-process-instances/spec.md](../specs/infrastructure/concurrent-process-instances/spec.md)
- ADRs: [docs/adr/0083-concurrent-process-instances-and-log-files.md](../docs/adr/0083-concurrent-process-instances-and-log-files.md), [docs/adr/0076-configurable-application-logging.md](../docs/adr/0076-configurable-application-logging.md)

## Verification pointer

- Satisfy **Definition of Done**, **Regression guardrails**, and **Scenarios** in the spec (manual check: two processes from same `bin\...\net...` output with default logging).
- Gates: [AGENTS.md](../AGENTS.md) — `dotnet build .\AudioAnalyzer.sln` (0 warnings), `dotnet test tests\AudioAnalyzer.Tests\AudioAnalyzer.Tests.csproj`, `dotnet format .\AudioAnalyzer.sln --verify-no-changes` (PowerShell on Windows).

## Refinement rule

If implementation discovers missing constraints or wrong behavior in the spec: **update the spec in the same commit** (same-commit rule). If the change is product-level or ambiguous, stop and flag for human review.
