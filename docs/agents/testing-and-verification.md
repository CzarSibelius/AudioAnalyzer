# Testing and verification

## Verification checklist (after making changes)

1. Run `dotnet build .\AudioAnalyzer.sln` — must succeed with 0 warnings.
2. Run `dotnet test tests\AudioAnalyzer.Tests\AudioAnalyzer.Tests.csproj` — all tests must pass.
3. Optionally run `dotnet format .\AudioAnalyzer.sln --verify-no-changes` to verify formatting.

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
