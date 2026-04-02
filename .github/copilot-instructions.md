# AudioAnalyzer - Agent Steering Instructions

> **Note**: These instructions apply to ALL AI coding assistants working on this project, including GitHub Copilot, Claude, and other AI agents.

## Project Overview
Real-time audio frequency spectrum analyzer for Windows using NAudio library. Captures system audio via WASAPI loopback and displays frequency spectrum with BPM detection in terminal.

## Critical Rules

### Build Verification
**ALWAYS verify the code builds after making changes:**
```powershell
dotnet build .\AudioAnalyzer.sln
```
Never complete a task without confirming successful compilation. **The build must succeed with 0 warnings.** Fix any new analyzer warnings (CA*, IDE*, RCS*, MSB*) before marking work done.

**Windows-only**: This project runs on Windows with PowerShell. Do **not** use Unix utilities like `head`, `tail`, or `grep` in shell commands (e.g. `dotnet build 2>&1 | head -50`). Use plain `dotnet build`; if output must be limited, use PowerShell: `dotnet build 2>&1 | Select-Object -First 50`.

### Verification checklist (after making changes)
1. Run `dotnet build .\AudioAnalyzer.sln` — must succeed with 0 warnings.
2. Run `dotnet test tests\AudioAnalyzer.Tests\AudioAnalyzer.Tests.csproj` — all tests must pass.
3. **New or moved production code**: follow per-project folder layout in `docs/agents/project-structure/` (see README there).
4. **New tests**: place files under `tests/AudioAnalyzer.Tests/` mirroring production project folders (see ADR-0064 in docs/adr/); shared helpers in `TestSupport/`, cross-assembly tests in `Integration/` when appropriate.
5. Optionally run `dotnet format .\AudioAnalyzer.sln --verify-no-changes` to verify formatting.
6. If modifying UI/display: manually test with Demo Mode (D → select Demo) at 80x24 and 200x50. When debugging visual problems, use **screen dump** (Ctrl+Shift+E in-app, or `--dump-after N` to run then dump) to capture the terminal state as text.
7. **Optional `link_shim.dll` (Ableton Link BPM)**: Building the C++ shim is **not** required for `dotnet build` / `dotnet test`. When you need it, follow **`docs/agents/native-link-shim-build.md`** — CMake on PATH, **MSVC Build Tools** (Desktop C++), and **Developer PowerShell for VS** (or x64 Native Tools prompt) so the compiler is available.

### Static analysis
After making code changes, check linter diagnostics for the modified files and fix any reported errors; fix warnings unless the rule is explicitly disabled for that line. Do not introduce new build warnings. Optionally run `dotnet format --verify-no-changes` to verify formatting (or `dotnet format` to fix); this uses .editorconfig.

### Architecture decisions (ADRs)
- **Follow docs/adr/**: When changing architecture, persistence, or user-facing behavior, read the relevant ADRs in `docs/adr/` and align implementation with them.
- If a change would conflict with an accepted ADR, either update/supersede the ADR or align the implementation with it; do not silently contradict documented decisions.
- **NuGet packages**: When adding or changing NuGet dependencies, follow ADR-0013 — avoid insecure or obsolete packages. Check `dotnet list package --vulnerable` before merging.
- **New visualizers**: Implement a text layer renderer (inherit TextLayerRendererBase, implement ITextLayerRenderer&lt;TState&gt;) and add to TextLayersVisualizer; do not create new standalone IVisualizer modes (see ADR-0014 in docs/adr/).
- **Layer settings**: TextLayerSettings has common props plus Custom (JSON); layer-specific settings go in *Settings.cs next to the layer; use `GetCustom<TSettings>()` in Draw (see ADR-0021 in docs/adr/). New settings are discovered via reflection; add *Settings.cs, use [SettingRange]/[SettingChoices]/[Setting] attributes, register in LayerSettingsReflection (see ADR-0025 in docs/adr/).
- **Presets**: TextLayers configs are Presets in presets/*.json; V cycles presets; S modal: R rename, N new preset (see ADR-0019, ADR-0022 in docs/adr/).
- **UI text overflow**: Use IScrollingTextViewport (from IScrollingTextViewportFactory.CreateViewport()) for dynamic text that may exceed width; use StaticTextViewport.TruncateWithEllipsis for static text (see ADR-0020, ADR-0037 in docs/adr/).
- **Viewport labels**: Use `Label:` only in labeled UI; do not embed hotkeys in labels. Use LabelFormatting / IScrollingTextViewport.FormatLabel for punctuation; key discovery is via help (H) and GetBindings() (see ADR-0034 in docs/adr/, superseded).
- **Settings migration**: Do not add migration logic for settings format changes; use backup (`{name}.{timestamp}.bak`) and reset per ADR-0029 in docs/adr/.
- **Performance**: Console writes, polling, and timing must be performant; follow ADR-0030 and ADR-0067 in docs/adr/ when adding console I/O, key polling, frame-rate logic, or the render FPS overlay.
- **Dependency injection**: Prefer DI for new components (constructor injection, register in ServiceConfiguration). Deviate only when profiling shows it harms performance on hot paths; document rationale. See ADR-0040 in docs/adr/.
- **God object refactoring**: When refactoring ApplicationShell, AnalysisEngine, or SettingsModal, follow ADR-0041 and the task list in docs/refactoring/god-object-plan.md; mark tasks `[x]` when implemented.
- **Screen dump**: Screen capture (ASCII screenshot) uses IScreenDumpService; default output is plain ASCII. Hotkey Ctrl+Shift+E; CLI: --dump-after N, --dump-path (see ADR-0046 in docs/adr/). **When debugging or testing visual/UI problems, use screen dump** (Ctrl+Shift+E in the running app, or `--dump-after N` to run N seconds and dump then exit) to capture the current state as plain text for inspection or sharing.
- **Key handling**: Every component/class that handles keypresses must implement IKeyHandler&lt;TContext&gt; or delegate to one; no inline key-handling logic (see ADR-0047 in docs/adr/). Key handlers must expose their bindings via the interface method for dynamic help and other consumers (see ADR-0048 in docs/adr/).
- **Fill BlendOver / space cells**: Default: blend using char + color from `ViewportCellBuffer.Get`. Optional `FillSettings.BlendSpaceAsBlack`: treat space (`' '`) as black **under** for BlendOver only — see ADR-0059 in docs/adr/.

### User Control Requirements
- **Every feature must be toggleable by the user in realtime** via keyboard shortcuts
- New features should have a dedicated key binding (e.g., B for beat circles, V for preset cycling)
- **Help menu (H key) must document every available command** - always update `ShowHelpMenu()` when adding new controls
- Settings are persisted automatically when changed (see ADR-0001 in docs/adr/); no manual save key is required

### Code Style
- Use C# 10 top-level statements
- Prefer explicit types over `var` for clarity in audio processing code
- Keep methods under 50 lines; extract complex logic into separate methods
- Use meaningful variable names (e.g., `barHeight`, `normalizedMag`, not `h`, `n`)
- Non-empty XML summaries for classes and interfaces; prefer one file per class (see ADR-0016 in docs/adr/)

### Terminal Output Requirements
- **All output must scale dynamically with terminal size**
- Check `Console.WindowWidth` and `Console.WindowHeight` before rendering
- Account for margins/labels when calculating available space
- Prevent line wraps by ensuring content fits within `termWidth`
- Update display when terminal is resized

### Performance Constraints
- Display update interval: 50ms (20 FPS)
- FFT length: 8192 samples (fixed)
- Minimize allocations in hot paths (ProcessAudio, DisplaySpectrum)
- Reuse arrays; only reallocate when terminal size changes

### Documentation
- **Keep feature documentation updated**: When adding or changing features, update the appropriate doc: root README for product description, prerequisites, and how to run; `docs/configuration-reference.md` for preset/show/palette JSON, `appsettings.json`, and NuGet version lists; `docs/visualizers/`, UI specs, and ADRs as applicable.
- **Keep the README human-focused**: When changing behavior or run requirements, update README.md (short intro, prerequisites, run, first-run, tips). Do not restore long JSON blocks or contributor-only build/format detail there—use `docs/configuration-reference.md` and `docs/agents/`.

## Architecture

### Key Components
1. **Audio Capture**: `WasapiLoopbackCapture` - captures system audio
2. **FFT Analysis**: `FastFourierTransform.FFT` - frequency domain conversion
3. **Display Engine**: Terminal-based visualization with ANSI colors
4. **BPM Detection**: Energy-based beat detection algorithm
5. **Auto-Gain**: Adaptive normalization for quiet/loud sources

### Data Flow
```
Audio Buffer → FFT Processing → Frequency Bands → Smoothing → Peak Hold → Display
                                                 ↓
                                           Beat Detection → BPM
```

### Critical Fields
- `NumBands`: Dynamic, 8-60 based on terminal width
- `smoothedMagnitudes[]`: Current smoothed frequency values
- `peakHold[]`: Peak hold values for visual feedback
- `targetMaxMagnitude`: Auto-gain normalization target

## Color Scheme
Amplitude-based gradient (consistent across volume bar and spectrum):
- Blue (0-25%): Very quiet
- Cyan (25-40%): Quiet
- Green (40-55%): Medium
- Yellow (55-70%): Loud
- Magenta (70-85%): Very loud
- Red (85-100%): Loudest
- White: Peak hold markers

## Testing Requirements
When modifying display code:
1. Test in narrow terminal (80 chars)
2. Test in wide terminal (200+ chars)
3. Test in short terminal (24 rows)
4. Test in tall terminal (50+ rows)
5. Resize terminal during runtime to verify dynamic scaling
6. **When debugging or testing visual/UI issues**: use **screen dump** to capture the current state — press **Ctrl+Shift+E** in the running app, or run with `--dump-after N` (e.g. `--dump-after 5`) to capture after N seconds. Output is written to the `screen-dumps` folder as plain ASCII for inspection or sharing.

When modifying audio processing:
1. Test with music (wide frequency range)
2. Test with speech/conversation (narrow frequency range, low energy)
3. Verify auto-gain adapts appropriately

## Common Modifications

### Adding New Visual Elements
1. Calculate space requirements in characters
2. Update `UpdateDisplayDimensions()` to account for new element
3. Adjust `NumBands` calculation if horizontal space changes
4. Test with various terminal sizes

### Adjusting Sensitivity
- `SmoothingFactor`: Controls bar smoothness (0.7 = 70% previous, 30% new)
- `PeakHoldFrames`: How long peaks hold (20 frames ≈ 1 second)
- `PeakFallRate`: How fast peaks fall (0.08 = 8% per frame)
- Auto-gain: Modify gain calculation in `DisplaySpectrum()`

### Frequency Band Configuration
- Min frequency: 20 Hz (human hearing lower limit)
- Max frequency: 20,000 Hz (human hearing upper limit)
- Distribution: Logarithmic scale for perceptual accuracy
- Labels: Update `allLabels` array for new frequency markers

## Dependencies
- **NAudio.CoreAudioApi**: System audio capture
- **NAudio.Wave**: Audio format handling
- **NAudio.Dsp**: FFT implementation
- **.NET 10.0**: Target framework

## Git Workflow
- Commit after each logical feature completion
- Build must succeed before committing
- Use descriptive commit messages referencing feature/fix

## Debugging Tips
- **Visual/UI issues**: Use **screen dump** to capture the terminal state as text. In the running app press **Ctrl+Shift+E**; or run with `--dump-after N` (e.g. `--dump-after 5`) to dump after N seconds and exit. Output goes to the `screen-dumps` folder — use it to inspect layout, overflow, or rendering problems.
- Volume too low: Increase auto-gain multiplier (currently 0.8)
- Bars jumping: Increase `SmoothingFactor` (0.7 → 0.8)
- BPM inaccurate: Adjust `BeatThreshold` (currently 1.3x average)
- Peaks falling too fast/slow: Modify `PeakFallRate`
