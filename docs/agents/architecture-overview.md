# Architecture overview (reference)

This file is reference material for context. For mandatory rules, see ADRs in [docs/adr/](../adr/README.md) and the other agent docs.

## Key components

1. **Audio capture**: WASAPI loopback or capture device (or Demo Mode synthetic stream).
2. **FFT analysis**: Frequency domain conversion.
3. **Display engine**: Terminal-based visualization with ANSI colors; TextLayers with configurable layers.
4. **BPM detection**: Energy-based beat detection.
5. **Auto-gain**: Adaptive normalization for quiet/loud sources.

## Data flow

```
Audio Buffer → FFT Processing → Frequency Bands → Smoothing → Peak Hold → Display
                                                ↓
                                          Beat Detection → BPM
```

## Color scheme (amplitude-based)

- Blue (0–25%): Very quiet → Cyan → Green → Yellow → Magenta → Red (85–100%): Loudest. White: peak hold markers.

## Dependencies

- NAudio (CoreAudioApi, Wave, Dsp) for capture and FFT.
- .NET 10.0 target framework.

## Solution layering (brief)

- **Infrastructure** references Application and Domain only. Default TextLayers presets (typed layer `Custom` blobs) are built by `IDefaultTextLayersSettingsFactory` (Application contract), implemented in Visualizers; the host passes a factory into `FileSettingsRepository` at construction time.

## Common modifications

- **New visual elements**: Calculate space in characters; account for new elements in layout; adjust band count if horizontal space changes; test at various terminal sizes.
- **Sensitivity**: SmoothingFactor, PeakHoldFrames, PeakFallRate; auto-gain in display/volume path.
- **Frequency bands**: Min 20 Hz, max 20 kHz; logarithmic distribution; update labels if markers change.

## Debugging tips

- **Visual/UI**: Use screen dump (Ctrl+Shift+E or `--dump-after N`) to capture terminal state as text.
- Volume too low: increase auto-gain multiplier.
- Bars jumping: increase SmoothingFactor.
- BPM inaccurate: adjust beat threshold.
- Peaks falling too fast/slow: modify PeakFallRate.
