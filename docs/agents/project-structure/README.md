# Project file structure (agents)

**Agents must place new or moved source files according to the spec for the target project.** Namespaces stay flat per assembly (`AudioAnalyzer.Domain`, `AudioAnalyzer.Console`, etc.); folder paths are for discoverability and consistency.

| Project | Spec |
|---------|------|
| [AudioAnalyzer.Domain](audioanalyzer.domain.md) | `src/AudioAnalyzer.Domain/` |
| [AudioAnalyzer.Application](audioanalyzer.application.md) | `src/AudioAnalyzer.Application/` |
| [AudioAnalyzer.Infrastructure](audioanalyzer.infrastructure.md) | `src/AudioAnalyzer.Infrastructure/` |
| [AudioAnalyzer.Platform.Windows](audioanalyzer.platform.windows.md) | `src/AudioAnalyzer.Platform.Windows/` |
| [AudioAnalyzer.Visualizers](audioanalyzer.visualizers.md) | `src/AudioAnalyzer.Visualizers/` |
| [AudioAnalyzer.Console](audioanalyzer.console.md) | `src/AudioAnalyzer.Console/` |
| [AsciiShapeTableGen](asciishapetablegen.md) | `tools/AsciiShapeTableGen/` |

**Tests** (single project `tests/AudioAnalyzer.Tests/`): mirror production paths per [ADR-0064](../../adr/0064-test-project-mirrors-production-layout.md); see [testing-and-verification.md](../testing-and-verification.md).

**Also follow**: [ADR-0016](../../adr/0016-csharp-documentation-and-file-organization.md) (one file per class, XML summaries), and ADRs referenced from each spec.
