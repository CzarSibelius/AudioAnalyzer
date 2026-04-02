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
