# Agent instructions

**AudioAnalyzer** is a real-time audio analyzer for Windows that captures system audio (WASAPI loopback or device), runs FFT analysis, and renders configurable text-layer visualizations in the terminal (BPM, spectrum, oscilloscope, etc.).

- **Package manager / runtime**: .NET 10.0 SDK; NuGet for packages. No npm.
- **Build**: `dotnet build .\AudioAnalyzer.sln` — must succeed with **0 warnings**. Use PowerShell on Windows; do not use Unix shell utilities in commands.
- **Tests**: `dotnet test tests\AudioAnalyzer.Tests\AudioAnalyzer.Tests.csproj`
- **Format check**: `dotnet format .\AudioAnalyzer.sln --verify-no-changes` (or `dotnet format .\AudioAnalyzer.sln` to fix)

Before changing architecture, persistence, or user-facing behavior, read the relevant ADRs and align implementation with them: **[docs/adr/README.md](docs/adr/README.md)**.

When adding or moving **source files**, place them per **[docs/agents/project-structure/README.md](docs/agents/project-structure/README.md)** for the target project (tests still mirror production per ADR-0064).

Detailed agent instructions are split by topic:

| Topic | File |
|-------|------|
| C# standards and static analysis | [docs/agents/csharp-and-static-analysis.md](docs/agents/csharp-and-static-analysis.md) |
| Documentation (README, specs, deferred work) | [docs/agents/documentation.md](docs/agents/documentation.md) |
| UI and console (specs, viewport, key handling) | [docs/agents/ui-and-console.md](docs/agents/ui-and-console.md) |
| Visualizers (layers, viewport, specs) | [docs/agents/visualizers.md](docs/agents/visualizers.md) |
| Testing and verification | [docs/agents/testing-and-verification.md](docs/agents/testing-and-verification.md) |
| Git workflow | [docs/agents/git-workflow.md](docs/agents/git-workflow.md) |
| Architecture overview (reference) | [docs/agents/architecture-overview.md](docs/agents/architecture-overview.md) |
| **Project file structure** | [docs/agents/project-structure/README.md](docs/agents/project-structure/README.md) |
| **Native Link shim** (`link_shim.dll`, CMake + MSVC) | [docs/agents/native-link-shim-build.md](docs/agents/native-link-shim-build.md) |

For ADR summaries applied in this repo (e.g. DI, presets, screen dump), see **.cursor/rules/adr.mdc**. For rules that apply only to specific file globs, see the other files under **.cursor/rules/**.

**Contradictions and instructions flagged for review:** [docs/agents/instructions-review.md](docs/agents/instructions-review.md).
