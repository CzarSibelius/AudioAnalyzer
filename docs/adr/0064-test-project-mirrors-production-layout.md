# ADR-0064: Test project folder layout mirrors production

**Status**: Accepted

## Context

The solution uses multiple production projects under `src/AudioAnalyzer.*` with nested folders (for example `Application/Display/`, `Console/SettingsModal/`). Tests live in a single project, `tests/AudioAnalyzer.Tests`. Without a documented convention, test files tend to accumulate at the project root, making it harder to find tests for a given type and to see which assembly an area belongs to.

## Decision

- **Single test project**: Keep one test project (`AudioAnalyzer.Tests`) referencing the production projects as today. Do **not** require a separate test csproj per production project unless the team later chooses to split.

- **Mirror production paths**: Under `tests/AudioAnalyzer.Tests/`, place test source files so the path mirrors production relative to each production project root:

  `tests/AudioAnalyzer.Tests/<ProjectSuffix>/<same relative path as under src/AudioAnalyzer.<ProjectSuffix>/`

  `<ProjectSuffix>` is the part after `AudioAnalyzer.` in the production project name: `Domain`, `Application`, `Console`, `Infrastructure`, `Visualizers`, or `Platform.Windows` (when tests reference that project).

- **Examples**:

  | Production | Test (illustrative) |
  |------------|---------------------|
  | `src/AudioAnalyzer.Application/Display/StaticTextViewport.cs` | `tests/AudioAnalyzer.Tests/Application/Display/StaticTextViewportTests.cs` |
  | `src/AudioAnalyzer.Console/ApplicationModeFactory.cs` | `tests/AudioAnalyzer.Tests/Console/ApplicationModeFactoryTests.cs` |
  | `src/AudioAnalyzer.Visualizers/TextLayers/Fill/FillSettings.cs` | `tests/AudioAnalyzer.Tests/Visualizers/TextLayers/Fill/…Tests.cs` |

- **Naming**: Use a `*Tests.cs` suffix for test files. Prefer one primary test class per file, consistent with [ADR-0016](0016-csharp-documentation-and-file-organization.md).

- **Exceptions**:
  - **Shared test utilities** (fixtures, fakes, dimension helpers used by many tests): place under **`TestSupport/`** at the test project root (or `Common/` if preferred consistently). These paths do not mirror production.
  - **Cross-cutting or multi-assembly tests** without a single primary SUT: place under **`Integration/`** (or a similarly named root folder). Document briefly in the test file summary why it is not under a single production mirror path.

- **Namespaces**: Prefer aligning C# `namespace` with the folder path under the test project (for example `AudioAnalyzer.Tests.Application.Display` for files under `Application/Display/`). This is encouraged when adding or moving tests; it is not a substitute for the folder mirror rule.

### Unit vs integration classification

Policy detail: [docs/agents/testing-and-verification.md](../agents/testing-and-verification.md).

- **Unit tests** (default under the mirror paths above): fast, deterministic, safe to run repeatedly during development. They must **not** use a real database, real network, or **real host file system** I/O (`File.*`, `Directory.*`, or a real-disk `IFileSystem`). Prefer **`MockFileSystem`** (or fakes) and path-only checks when exercising file-oriented code. They must be able to run **in parallel** with other unit tests (no global exclusive resources unless isolated via an explicit xUnit collection). They must **not** require manual environment edits to run.

- **Integration tests**: live under **`tests/AudioAnalyzer.Tests/Integration/`** with namespace **`AudioAnalyzer.Tests.Integration`** (optional extra suffix segments if mirroring under `Integration/`, e.g. `Integration/Visualizers/...`). Use for cross-assembly / full-pipeline checks, performance budgets, preset+render smoke, or any test that still needs **real** I/O, real console/OS APIs, or **cannot** run in parallel with other tests without coordination. Prefer mocks where the scenario does not specifically require proving real OS behavior.

- **Run commands** (single test project unchanged): agents may run **unit only** with  
  `dotnet test tests\AudioAnalyzer.Tests\AudioAnalyzer.Tests.csproj --filter "FullyQualifiedName!~AudioAnalyzer.Tests.Integration"`  
  and the **full** suite (including integration) with unfiltered `dotnet test`. CI should continue to run the **full** suite.

- **Exception to mirror-only placement**: A test whose primary SUT lives under a mirrored path but **must** use real disk, network, or DB belongs under **`Integration/`** (optionally `Integration/<Suffix>/...` mirroring production) with the integration namespace—not under the unit mirror tree.

- **Optional later split**: A second test `.csproj` would require an explicit ADR update; until then, keep one test project.

## Consequences

- **New and moved tests** must follow the mirror layout and exceptions above.
- Navigating from production code to tests (and vice versa) is predictable: same relative path under `src/AudioAnalyzer.<Suffix>/` vs `tests/AudioAnalyzer.Tests/<Suffix>/`.
- **Unit vs integration** placement and filters are part of the test layout contract; see [testing and verification](../agents/testing-and-verification.md).
