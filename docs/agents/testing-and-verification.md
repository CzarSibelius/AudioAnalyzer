# Testing and verification

## Test project layout

Place new test files under `tests/AudioAnalyzer.Tests/` so folders mirror the production project and path (for example `Application/Display/` for code under `src/AudioAnalyzer.Application/Display/`). Shared helpers belong in `TestSupport/` (or `Common/`); cross-assembly tests with no single primary SUT may use `Integration/`. See [ADR-0064](../adr/0064-test-project-mirrors-production-layout.md).

## Unit vs integration tests

**Not a unit test** if the test: talks to a **database**; uses the **network**; touches the **real host file system** (`File.*`, `Directory.*`, or a real-disk `System.IO.Abstractions.IFileSystem`). **Does not** count as file I/O: **`MockFileSystem`** and other in-memory fakes. **Unit tests** must also be able to run **in parallel** with other unit tests (no hidden global exclusivity) and must **not** require manual environment setup (editing real config on disk, secrets, special drivers) beyond `dotnet test`.

**Integration tests** live under `tests/AudioAnalyzer.Tests/Integration/` with namespace **`AudioAnalyzer.Tests.Integration`**. They may be slower or broader (full DI render, preset round-trips, performance thresholds). **Prefer** `MockFileSystem` and fakes in integration tests too when the goal is not specifically to prove real OS or hardware behavior.

**I/O hardening (production and tests):** Prefer injecting **`IFileSystem`** (and small test seams such as content providers) so tests assert behavior against **`MockFileSystem`** instead of temp directories on disk.

**Parallelism:** If a test truly cannot run in parallel with others, place it under `Integration/`, disable parallelization for that case with an xUnit **`[Collection("…")]`** (or equivalent), and document why in the test class summary.

### Running a subset (agents / local iteration)

- **Unit tests only** (exclude `AudioAnalyzer.Tests.Integration` namespace):  
  `dotnet test tests\AudioAnalyzer.Tests\AudioAnalyzer.Tests.csproj --filter "FullyQualifiedName!~AudioAnalyzer.Tests.Integration"`
- **Integration tests only**:  
  `dotnet test tests\AudioAnalyzer.Tests\AudioAnalyzer.Tests.csproj --filter "FullyQualifiedName~AudioAnalyzer.Tests.Integration"`
- **Full suite** (before completing work; matches CI):  
  `dotnet test tests\AudioAnalyzer.Tests\AudioAnalyzer.Tests.csproj`  
  or `dotnet test .\AudioAnalyzer.sln`

During tight edit loops, run **unit only** often; before finishing, run the **full** suite at least once.

## Slow test reports (TRX and JUnit)

To find slow tests (triage production code vs test setup), emit **VSTest TRX** (per-test `duration` in XML) and **JUnit XML** (Ant-style `testcase` `time` attributes). The test project references **JunitXml.TestLogger** 8.0.0 (MIT; compatible with GPL-3.0-only distribution per [ADR-0075](../adr/0075-nuget-license-compatibility.md)). TRX is built into the test SDK; no extra package.

From the repo root (paths are under the test project’s `TestResults/` folder):

```powershell
dotnet test .\tests\AudioAnalyzer.Tests\AudioAnalyzer.Tests.csproj `
  --logger "trx;LogFileName=tests.trx" `
  --logger "junit;LogFilePath=TestResults/junit.xml"
```

Outputs (typical):

- `tests/AudioAnalyzer.Tests/TestResults/tests.trx` — open in Visual Studio Test Explorer or parse XML (`UnitTestResult` → `duration`, `testName`).
- `tests/AudioAnalyzer.Tests/TestResults/junit.xml` — for tools and actions that consume JUnit (e.g. GitHub Checks summaries).

**CI:** [.github/workflows/build.yml](../../.github/workflows/build.yml) runs the same loggers, uploads **`test-reports`** artifacts, and publishes a **Test results** check via [EnricoMi/publish-unit-test-result-action](https://github.com/EnricoMi/publish-unit-test-result-action) (timing tables in the Actions / Checks UI). Download the artifact for offline analysis.

Use `LogFileName=tests.trx` (not `TestResults\tests.trx`) for TRX so the file is not nested under `TestResults\TestResults\`.

**Sorted table (local):** After producing `tests.trx`, run [`scripts/Summarize-TrxTestDurations.ps1`](../../scripts/Summarize-TrxTestDurations.ps1) to write `slow-tests.csv` and `slow-tests.md` next to the TRX. Very long per-test durations often come from tests that hammer code under a single coarse lock (parallel tasks still serialize); use the sorted list to spot that pattern.

### Slow test diagnosis workflow (agents)

When `dotnet test` becomes unexpectedly slow (CI or local), **do not guess**—measure, then fix the right layer.

1. **Isolate test time from build time**  
   Run `dotnet build .\AudioAnalyzer.sln -warnaserror`, then `dotnet test … --no-build` with the TRX/JUnit loggers above. A long run that includes compile is often MSBuild/JIT, not individual tests.

2. **Find the outliers**  
   Open `tests.trx` `<Times start` / `finish` for wall clock, then sort `UnitTestResult` by `@duration`, or run `Summarize-TrxTestDurations.ps1` and read `slow-tests.md`. If **one** test accounts for most wall time, fix that test (or the code it stresses) first.

3. **Interpret common patterns**  
   - **One test, multi-minute duration, uses `Task.Run` + shared lock:** Often **lock convoy** (each critical section does heavy work; “parallel” tasks serialize). Fix by **lowering iteration counts**, using **smaller/cheaper inputs**, or **alternating** calls in one thread if the goal is interleaving, not false parallelism—see `AnalysisEngineTests.ProcessAudio_and_GetSnapshot_concurrent_stress_remain_consistent` history.  
   - **`Thread.Sleep` / `Task.Delay` in tests:** Prefer **virtual time**, tight waits on signals, or **smaller delays**; each fixed sleep adds up across the suite.  
   - **Integration tests rebuilding full DI per `[Theory]` row:** Reuse a **fixture** or shared builder when safe; override **platform services** in [`TestHelpers.BuildTestServiceProvider`](../../tests/AudioAnalyzer.Tests/TestSupport/TestHelpers.cs) (e.g. `NullAsciiVideoDeviceCatalog`, `NullNowPlayingProvider`, `MockFileSystem`) so tests never hit **real WinRT, NAudio enumeration, or disk** unless that is the SUT.  
   - **Wall clock ≫ sum of per-test durations:** Investigate **host/adapters**, **deadlocks**, or **serial** collections—not only test bodies.

4. **Fix and verify**  
   After changes, run the **full** suite again with TRX; confirm top entries dropped and total duration is acceptable. Keep assertions meaningful (still thread-safe, still catches regressions); do not only weaken tests to go green.

## Verification checklist (after making changes)

1. Run `dotnet build .\AudioAnalyzer.sln` — must succeed with 0 warnings.
2. Run `dotnet test tests\AudioAnalyzer.Tests\AudioAnalyzer.Tests.csproj` — all tests must pass (full suite).
3. Optionally run `dotnet format .\AudioAnalyzer.sln --verify-no-changes` to verify formatting.

## Test suite overview

Run all tests:

```bash
dotnet test tests/AudioAnalyzer.Tests/AudioAnalyzer.Tests.csproj
```

Coverage includes:

- **Render performance**: Single-frame render completes within 10 ms (guards against regressions; target is 50 ms for 20 FPS per [ADR-0030](../adr/0030-performance-priority.md)). A separate case registers a minimal OBJ on the **mock `IFileSystem`** and asserts **AsciiModel** end-to-end render stays within a modest ms budget (`RenderPerformanceTests.AsciiModelSingleFrameCompletesWithinThreshold`).
- **Layer rendering**: Each layer type (GeissBackground, Marquee, Oscilloscope, etc.) renders without throwing.
- **Preset loading**: Preset load/save round-trip and render-with-preset.
- **Smoke**: Multiple frames render without exception.

Integration tests and several performance cases use **System.IO.Abstractions.TestingHelpers** (`MockFileSystem`) for the app `IFileSystem`, so presets/palettes and AsciiImage/AsciiModel assets can share the same virtual tree without disk I/O. **Unit** tests under mirrored paths should **not** use real-disk `File` / `Directory` or `new FileSystem()` for test data; use **`MockFileSystem`** (or path-only assertions where the API does not touch disk).

## When modifying UI/display

- Manually test with Demo Mode (D → select Demo) at 80x24 and 200x50.
- When debugging visual problems, use **screen dump**: Ctrl+Shift+E in-app, or `--dump-after N` to run then dump and exit. Use `--dump-path <dir>` to choose the output directory. Output is plain ASCII in the `screen-dumps` folder (see ADR-0046).

## When modifying audio processing

- Test with music (wide frequency range) and with speech/conversation (narrow frequency range, low energy).
- Verify auto-gain adapts appropriately.

## Test scope (display)

- Narrow terminal (80 chars), wide (200+ chars), short (24 rows), tall (50+ rows).
- Resize terminal during runtime to verify dynamic scaling.

## Performance

- Display update interval: 50 ms (20 FPS). FFT length: 8192 samples (fixed).
- Minimize allocations in hot paths (e.g. ProcessAudio, display pipeline). Reuse arrays; reallocate only when terminal size changes. Follow ADR-0030.

### Performance optimizations (agents)

Do not treat a performance change as complete without automated verification in the test suite:

- **New or updated test**: Add or extend a **unit** or **integration** test that encodes the expected outcome (faster wall-clock for a fixed workload, fewer allocations, or correct hot-path behavior under load).
- **Tightened regression guard**: Alternatively, lower the threshold in an existing performance-style test after local measurement proves the new budget is safe on representative hardware—keep margins generous enough for CI variance and document the constant (e.g. link to [ADR-0030](../adr/0030-performance-priority.md) / [ADR-0067](../adr/0067-60fps-target-and-render-fps-overlay.md) in a comment where it helps).

**Patterns that fit this repo:**

- **Time budget**: Warm up (discard first iterations), then measure with `Stopwatch` and assert under a documented ms ceiling—see [`RenderPerformanceTests`](../../tests/AudioAnalyzer.Tests/Integration/RenderPerformanceTests.cs) for integration examples.
- **Allocations**: When the optimization targets allocations, prefer wrapping the workload with `GC.GetAllocatedBytesForCurrentThread()` (or the project’s existing allocation-testing patterns) instead of relying on wall-clock alone in CI.
- **A/B timing in one test**: Use only when two implementations temporarily coexist (e.g. during a refactor). Otherwise prefer a regression threshold or allocation assertions over comparing “old vs new” elapsed time in a single CI run.

**Anti-patterns:** Shipping micro-optimizations based only on informal “feels faster” or one-off local runs, with no test that would fail if the win regressed.

Profiling first remains the primary way to choose what to optimize; see ADR-0030. Tests complement profiling by locking in the claimed improvement or stricter budget.

### Profiling the main loop (CPU)

On a **Release** build, capture stacks for a few seconds while the app runs a representative preset (wide terminal, layers you care about):

1. Install once: `dotnet tool install --global dotnet-trace` (see Microsoft’s `dotnet-trace` documentation).
2. Example: `dotnet trace collect --format speedscope --output trace.nettrace -- dotnet run -c Release --project src\AudioAnalyzer.Console\AudioAnalyzer.Console.csproj -- …` (add your usual CLI flags after `--`).
3. Open the trace (or a Speedscope export) and rank hot frames: `ApplicationShell`, `VisualizationOrchestrator`, `AnalysisEngine.GetSnapshot` / `CloneSnapshotArrays`, `ViewportCellBuffer.FlushTo`, `TextLayersVisualizer.Render`, and `AnalysisEngine.ProcessAudio` / waveform overview rebuild.

That ranking shows whether work is dominated by snapshot copies, console flush, UI composition, or the audio/overview path.

**Regression tests tied to this path:** [`ViewportCellBufferFlushToTests`](../../tests/AudioAnalyzer.Tests/Application/Viewports/ViewportCellBufferFlushToTests.cs) (diff flush skips redundant `IConsoleWriter` calls); [`TextLayersSortedLayersCacheTests`](../../tests/AudioAnalyzer.Tests/Integration/TextLayersSortedLayersCacheTests.cs) (sorted-layer cache cleared after `OnTextLayersStructureChanged`). Waveform overview partition / policy: [`AnalysisEngineOverviewRebuildTests`](../../tests/AudioAnalyzer.Tests/Application/AnalysisEngineOverviewRebuildTests.cs), [`VisualizerSettingsWaveformOverviewRebuildPolicyTests`](../../tests/AudioAnalyzer.Tests/Console/VisualizerSettingsWaveformOverviewRebuildPolicyTests.cs). Deferred overview/snapshot lock work is noted under [ADR-0077](../adr/0077-waveform-overview-snapshot.md) (performance follow-ups).
