# PBI-012: macOS platform foundation (ADR, multi-target, CI)

**Transient work item** — close after merge.

## Directive

Establish **architecture and build plumbing** so AudioAnalyzer can compile and execute on macOS without forcing WASAPI types into every configuration.

**In scope**

- Author and land **ADR-0084** (adjust number per [docs/adr/README.md](../docs/adr/README.md) sequence): multi-target TFMs for Console/test projects, conditional references to `AudioAnalyzer.Platform.Windows`, placement of macOS-specific code in **`AudioAnalyzer.Platform.macOS`** (new project), and rule that Infrastructure remains free of unconditional Windows-only audio APIs.
- Refactor DI registration (`ServiceConfiguration`) so **audio device creation** is resolved via platform implementation(s), not hard-coded `NAudioDeviceInfo` where it implies WASAPI on all OSes.
- Add **macOS CI** workflow step(s): build + unit tests (`FullyQualifiedName!~Integration` acceptable initially if documented).
- Ensure **`dotnet format`** and **0-warning** policy hold on both targets.

**Out of scope** (see sibling PBIs)

- Full macOS Core Audio capture behavior — **PBI-013**.
- End-user docs polish — **PBI-014**.
- macOS Link `dylib` — backlog unless ADR-0084 folds it in.

## Context pointer

- Primary spec: [`specs/platform-macos/spec.md`](../specs/platform-macos/spec.md)
- Related ADRs: planned **0084** (see [`docs/adr/README.md`](../docs/adr/README.md) for next number); [`docs/adr/0040-dependency-injection-preference.md`](../docs/adr/0040-dependency-injection-preference.md); [`docs/adr/0064-test-project-mirrors-production-layout.md`](../docs/adr/0064-test-project-mirrors-production-layout.md)

## Verification pointer

- Spec: **Definition of Done**, **Regression guardrails**, **Scenarios** (CI build, Windows remains healthy).
- Commands: [`AGENTS.md`](../AGENTS.md) — macOS agents may use POSIX shells; document any deltas in PBI-014 if needed.

## Acceptance criteria

- ADR accepted; Console (+ tests as agreed) multi-targets without WASAPI types on the macOS graph.
- macOS CI green for build + unit tests; Windows lane unchanged or intentionally superseded with ADR note.
- **`dotnet format ./AudioAnalyzer.sln --verify-no-changes`** runs on the **macOS** workflow job (same repo-wide formatting gate as **AGENTS.md**; evidences the portable target graph). Windows contributors still format before push; the Windows job does not duplicate the step unless we consolidate later.

## Refinement rule

If multi-targeting forces test or tooling splits, update **ADR-0084** and the platform spec in the **same commit** as the structural change.
