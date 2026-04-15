# Testing and verification

## Test project layout

Place new test files under `tests/AudioAnalyzer.Tests/` so folders mirror the production project and path (for example `Application/Display/` for code under `src/AudioAnalyzer.Application/Display/`). Shared helpers belong in `TestSupport/` (or `Common/`); cross-assembly tests with no single primary SUT may use `Integration/`. See [ADR-0064](../adr/0064-test-project-mirrors-production-layout.md).

## Verification checklist (after making changes)

1. Run `dotnet build .\AudioAnalyzer.sln` — must succeed with 0 warnings.
2. Run `dotnet test tests\AudioAnalyzer.Tests\AudioAnalyzer.Tests.csproj` — all tests must pass.
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

Integration tests and several performance cases use **System.IO.Abstractions.TestingHelpers** (`MockFileSystem`) for the app `IFileSystem`, so presets/palettes and AsciiImage/AsciiModel assets can share the same virtual tree without disk I/O. Some unit tests still use `new FileSystem()` with a temp directory when exercising real OS path behavior.

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
