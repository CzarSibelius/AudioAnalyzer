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

## Consequences

- **New and moved tests** must follow the mirror layout and exceptions above.
- Navigating from production code to tests (and vice versa) is predictable: same relative path under `src/AudioAnalyzer.<Suffix>/` vs `tests/AudioAnalyzer.Tests/<Suffix>/`.
