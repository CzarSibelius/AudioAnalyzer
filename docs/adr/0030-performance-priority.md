# ADR-0030: Performance as top priority

**Status**: Accepted

## Context

The application is a real-time console audio visualizer. It processes audio and keyboard input while redrawing the terminal; **full main-area redraws** should sustain **at least ~60 FPS** on capable hosts when workload allows—the shell does not cap the loop there (see [ADR-0067](0067-60fps-target-and-render-fps-overlay.md)). Console I/O and polling are on the hot path; any inefficiency causes frame drops, input lag, or visual stutter.

## Decision

1. **Performance is a top priority**: Console writes, polling, and timing must be as performant as practical. New code that touches these areas must follow the guidelines below.

2. **Console write guidelines**:
   - Prefer whole-line or batched writes over many small writes (`SetCursorPosition` + `Write` is costly per call).
   - Use ANSI escape sequences for color (per existing [AnsiConsole](../../src/AudioAnalyzer.Application/Display/AnsiConsole.cs)) rather than `Console.ForegroundColor` + `Write` per segment.
   - For cell buffers and repetitive UI, consider diff-based rendering: only write rows/lines that changed.
   - Reuse `StringBuilder` instances where feasible to reduce allocations.
   - Avoid redundant writes when content is unchanged (e.g. toolbar line, header when now-playing unchanged).

3. **Polling and timing guidelines**:
   - Use `Environment.TickCount64` or `Stopwatch` for frame-rate throttling instead of `DateTime.Now` when only relative time is needed.
   - Prefer `PeriodicTimer` for background polling (as in [WindowsNowPlayingProvider](../../src/AudioAnalyzer.Platform.Windows/NowPlaying/WindowsNowPlayingProvider.cs)).
   - For key polling, keep `Thread.Sleep` intervals reasonable (e.g. 20–50 ms); avoid polling faster than human input needs.
   - Prefer event-driven updates where platform APIs support it (e.g. GSMTC `MediaPropertiesChanged`).

4. **Measurement**: When optimizing, prefer profiling (e.g. `dotnet-trace`, perf counters) over guesswork. Document benchmark results if they drive a design change.

## Consequences

- New visualizers and UI code should align with these guidelines.
- Future improvements (e.g. diff-based `ViewportCellBuffer`, batched header writes) are explicitly encouraged.
- This ADR does not mandate immediate refactoring; it establishes a principle and a direction for future work.

## Recommended future work

1. **ViewportCellBuffer diff-based rendering** (implemented): Maintains a "previous frame" buffer; only calls `SetCursorPosition` + `Console.Write` for rows whose content changed. Reuses a single `StringBuilder` per buffer. Reduces console I/O when visualizer output is largely static.

2. **Toolbar and header skip-write**: Header skip-write was removed during the renderer-interfaces migration ([renderer-interfaces-migration.md](../../docs/refactoring/renderer-interfaces-migration.md)): dispatcher always writes header lines (no cache). Toolbar in main content still writes every frame; optional skip-write not implemented.

3. **Cheaper time check**: `VisualizationOrchestrator` uses `Stopwatch.GetTimestamp()` for main-render FPS interval sampling ([ADR-0067](0067-60fps-target-and-render-fps-overlay.md)). **`AnalysisEngine`** may still use `DateTime.Now` where applicable; prefer high-resolution or tick APIs on hot paths when touched.

4. **Header batching**: If beneficial, build the 6-line header into a single string and write once (with newlines) instead of 6 separate writes. Profile first.

5. **StringBuilder pooling**: Consider `ArrayPool<char>` or similar for large string building in the hot path; benchmark before adopting.
