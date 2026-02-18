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
3. Optionally run `dotnet format .\AudioAnalyzer.sln --verify-no-changes` to verify formatting.
4. If modifying UI/display: manually test with Demo Mode (D → select Demo) at 80x24 and 200x50.

### Static analysis
After making code changes, check linter diagnostics for the modified files and fix any reported errors; fix warnings unless the rule is explicitly disabled for that line. Do not introduce new build warnings. Optionally run `dotnet format --verify-no-changes` to verify formatting (or `dotnet format` to fix); this uses .editorconfig.

### Architecture decisions (ADRs)
- **Follow docs/adr/**: When changing architecture, persistence, or user-facing behavior, read the relevant ADRs in `docs/adr/` and align implementation with them.
- If a change would conflict with an accepted ADR, either update/supersede the ADR or align the implementation with it; do not silently contradict documented decisions.
- **NuGet packages**: When adding or changing NuGet dependencies, follow ADR-0013 — avoid insecure or obsolete packages. Check `dotnet list package --vulnerable` before merging.
- **New visualizers**: Implement `ITextLayerRenderer` and add to TextLayersVisualizer; do not create new standalone `IVisualizer` modes (see ADR-0014 in docs/adr/).
- **Layer settings**: TextLayerSettings has common props plus Custom (JSON); layer-specific settings go in *Settings.cs next to the layer; use `GetCustom<TSettings>()` in Draw (see ADR-0021 in docs/adr/). New settings are discovered via reflection; add *Settings.cs, use [SettingRange]/[SettingChoices]/[Setting] attributes, register in LayerSettingsReflection (see ADR-0025 in docs/adr/).
- **Presets**: TextLayers configs are Presets in presets/*.json; V cycles presets; S modal: R rename, N new preset (see ADR-0019, ADR-0022 in docs/adr/).
- **UI text overflow**: Use ScrollingTextViewport for dynamic text that may exceed width; use ellipsis truncation for static text (see ADR-0020 in docs/adr/).
- **Settings migration**: Do not add migration logic for settings format changes; use backup (`{name}.{timestamp}.bak`) and reset per ADR-0029 in docs/adr/.
- **Performance**: Console writes, polling, and timing must be performant; follow ADR-0030 in docs/adr/ when adding console I/O, key polling, or frame-rate logic.

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
- **Keep feature documentation updated**: When adding or changing features, update any project feature documentation (README, and if present, `docs/` or a dedicated feature doc). Document new capabilities, options, and behavior in the appropriate place.
- **Keep the README updated**: When changing behavior, dependencies, or usage, update README.md: prerequisites, run instructions, "What It Does", usage steps, dependencies, and notes so they stay accurate.

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
- Volume too low: Increase auto-gain multiplier (currently 0.8)
- Bars jumping: Increase `SmoothingFactor` (0.7 → 0.8)
- BPM inaccurate: Adjust `BeatThreshold` (currently 1.3x average)
- Peaks falling too fast/slow: Modify `PeakFallRate`
