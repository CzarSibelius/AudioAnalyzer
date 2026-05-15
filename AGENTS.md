# Agent instructions

**GitHub Copilot** reads [.github/copilot-instructions.md](.github/copilot-instructions.md), which defers here—keep that file a short pointer, not a second copy of long instructions.

**AudioAnalyzer** is a real-time audio analyzer for Windows that captures system audio (WASAPI loopback or device), runs FFT analysis, and renders configurable text-layer visualizations in the terminal (BPM, spectrum, oscilloscope, etc.).

- **Package manager / runtime**: .NET 10.0 SDK; NuGet for packages. No npm.
- **Build**: `dotnet build .\AudioAnalyzer.sln` — must succeed with **0 warnings**. Use PowerShell on Windows; do not use Unix shell utilities in commands. **macOS / full solution on Windows:** install the **`.NET macOS` workload** once (`dotnet workload install macos`) so the pinned **`net10.0-macos*`** host TFM restores; the pin lives in **`Directory.Build.props`** as **`AudioAnalyzerMacOsHostTfm`** (currently **`net10.0-macos26.0`**). On macOS, pass **`-f net10.0-macos26.0`** (or the current pin) to **`dotnet build`**, **`dotnet run`**, and **`dotnet test`** when you do not want the default (first-listed) Windows TFM. **macOS host builds** also need **full Xcode** selected via **`xcode-select`** (see [getting-started.md](docs/getting-started.md#prerequisites) and https://aka.ms/macios-missing-xcode).
- **Tests**: `dotnet test tests\AudioAnalyzer.Tests\AudioAnalyzer.Tests.csproj` (full suite). **Unit tests only** (faster iteration):  
  `dotnet test tests\AudioAnalyzer.Tests\AudioAnalyzer.Tests.csproj --filter "FullyQualifiedName!~AudioAnalyzer.Tests.Integration"`  
  Run the full suite before completing work; see [docs/agents/testing-and-verification.md](docs/agents/testing-and-verification.md#unit-vs-integration-tests). **Per-test timing (TRX / JUnit)** for slow-test triage: [Slow test reports](docs/agents/testing-and-verification.md#slow-test-reports-trx-and-junit). If the suite slows down again, follow [Slow test diagnosis workflow (agents)](docs/agents/testing-and-verification.md#slow-test-diagnosis-workflow-agents) (measure with TRX, then fix outliers—lock convoy, sleeps, real I/O, repeated DI).
- **Performance changes**: Verify the optimization with a unit or integration test (or a justified tighter perf regression threshold); see [docs/agents/testing-and-verification.md](docs/agents/testing-and-verification.md#performance-optimizations-agents).
- **Format check**: `dotnet format .\AudioAnalyzer.sln --verify-no-changes` (or `dotnet format .\AudioAnalyzer.sln` to fix)

Before changing architecture, persistence, or user-facing behavior, read the relevant ADRs and align implementation with them: **[docs/adr/README.md](docs/adr/README.md)**.

**The Spec (ASDLC, repo-only):** Feature and UI contracts live under **`specs/`** — Blueprint (Context, Architecture, Constraints) and Contract (Definition of Done, Regression guardrails, Scenarios). Copy **[specs/TEMPLATE.md](specs/TEMPLATE.md)** for new domains. **Hubs:** [specs/text-layers-visualizer/spec.md](specs/text-layers-visualizer/spec.md), [specs/console-ui/spec.md](specs/console-ui/spec.md). Stubs under `docs/visualizers/` and `docs/ui-spec-*.md` point here; **edit the canonical `specs/**/spec.md`**, not the stubs. **Same-commit rule:** any behavior change updates the relevant spec in the same change. Optional **delta** items: copy [tasks/PBI-000.md](tasks/PBI-000.md) to `tasks/PBI-NNN-<slug>.md` (001, 002, …) for each backlog item. **ASDLC MCP:** optional **knowledge base** only (e.g. [Adversarial Code Review](https://asdlc.io/patterns/adversarial-code-review)) — use for Critic-session alignment, not as a substitute for `specs/` or build/test gates; see [docs/agents/agentic-personas.md](docs/agents/agentic-personas.md). **Do not** use it for routine implementation work.

When adding or moving **source files**, place them per **[docs/agents/project-structure/AGENTS.md](docs/agents/project-structure/AGENTS.md)** for the target project (tests still mirror production per ADR-0064).

**Personas (session-scoped):** For distinct workflows, attach one Cursor project skill at a time — **Lead** (specs, ADRs, planning), **Dev** (implementation), **Critic** (review, no code changes). Definitions live under **`.cursor/skills/persona-*/SKILL.md`**; overview and links: **[docs/agents/agentic-personas.md](docs/agents/agentic-personas.md)**.

Detailed agent instructions are split by topic:


| Topic                                                | File                                                                                   |
| ---------------------------------------------------- | -------------------------------------------------------------------------------------- |
| Agentic personas (Lead / Dev / Critic)               | [docs/agents/agentic-personas.md](docs/agents/agentic-personas.md) |
| C# standards and static analysis                     | [docs/agents/csharp-and-static-analysis.md](docs/agents/csharp-and-static-analysis.md) |
| Documentation (README, specs, deferred work)         | [docs/agents/documentation.md](docs/agents/documentation.md)                           |
| UI and console (specs, viewport, key handling)       | [docs/agents/ui-and-console.md](docs/agents/ui-and-console.md)                         |
| Visualizers (layers, viewport, specs)                | [docs/agents/visualizers.md](docs/agents/visualizers.md)                               |
| Testing and verification                             | [docs/agents/testing-and-verification.md](docs/agents/testing-and-verification.md)     |
| Git workflow                                         | [docs/agents/git-workflow.md](docs/agents/git-workflow.md)                             |
| Architecture overview (reference)                    | [docs/agents/architecture-overview.md](docs/agents/architecture-overview.md); root [ARCHITECTURE.md](ARCHITECTURE.md) |
| **The Spec** (features, console UI, visualizer)      | [specs/TEMPLATE.md](specs/TEMPLATE.md), [specs/text-layers-visualizer/spec.md](specs/text-layers-visualizer/spec.md), [specs/console-ui/spec.md](specs/console-ui/spec.md) |
| Known bugs / TODOs                                   | [docs/agents/known-bugs-and-todos.md](docs/agents/known-bugs-and-todos.md)             |
| **Project file structure**                           | [docs/agents/project-structure/AGENTS.md](docs/agents/project-structure/AGENTS.md)     |
| **Native Link shim** (`link_shim.dll`, CMake + MSVC) | [docs/agents/native-link-shim-build.md](docs/agents/native-link-shim-build.md)         |


For ADR summaries applied in this repo (e.g. DI, presets, screen dump), see **.cursor/rules/adr.mdc**. For rules that apply only to specific file globs, see the other files under **.cursor/rules/**.

**Contradictions and instructions flagged for review:** [docs/agents/instructions-review.md](docs/agents/instructions-review.md).