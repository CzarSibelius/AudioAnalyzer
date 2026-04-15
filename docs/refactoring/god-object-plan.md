# God Object Refactoring Plan

Refactoring tasks for ApplicationShell, AnalysisEngine, and SettingsModal.
See original analysis for context. Agents: mark `[x]` when a task is implemented.

---

## ApplicationShell vs VisualizationOrchestrator

- **ApplicationShell** is the host: main loop, device lifecycle, key and modal handling, app logic (mode/preset/palette). It configures the orchestrator (header callbacks, render guard, console lock) and triggers redraws; it does not perform rendering or audio processing.
- **VisualizationOrchestrator** owns the display pipeline: overlay, header row, when to refresh the header and when to run one frame (guard, dimensions), and execution of one frame (header + engine snapshot + renderer). Full-screen is in **IDisplayState** (injected); orchestrator reacts to display state changes. `OnAudioData` calls `ProcessAudio` only; full renders are **not** scheduled from the audio callback.
- Redraw triggers: Shell calls `Redraw` / `RedrawWithFullHeader` each main-loop tick (display cadence per ADR-0067) and on user or app events; orchestrator executes one frame when those methods run. So shell = display cadence + explicit redraw; orchestrator = “execute one frame” + audio → analysis only.

---

## Phase 1: ApplicationShell (17 deps → ~7)

### 1.1 IHeaderDrawer

- Add `IHeaderDrawer` interface in Abstractions (DrawMain, DrawHeaderOnly)
- Implement `HeaderDrawer` that encapsulates viewports, title bar, engine state, UiSettings
- Register in ServiceConfiguration; inject into ApplicationShell
- Replace all `ConsoleHeader.DrawMain(...)` calls with `_headerDrawer.DrawMain()`
- Remove duplicated header argument lists from ApplicationShell

### 1.2 Key handlers / dispatcher

- Add `IKeyHandler<MainLoopKeyContext>` (Handle(key, context)) and `MainLoopKeyContext`
- Implement `MainLoopKeyHandler` with switch for Tab, V, S, D, H, +/-, P, F, Escape
- Register IKeyHandler; inject into ApplicationShell
- Replace switch(key.Key) in ApplicationShell with `_keyHandler.Handle(key, ctx)`

### 1.3 Device lifecycle

- Add `IDeviceCaptureController` interface (StartCapture, StopCapture, RestartCapture, Shutdown)
- Implement DeviceCaptureController with _currentInput, _deviceLock, per ADR-0018
- Inject IDeviceCaptureController into ApplicationShell; remove direct IAudioInput handling

### 1.4 Settings persistence

- Add `IAppSettingsPersistence` interface with Save()
- Implement AppSettingsPersistence pulling from engine, visualizerSettings, settings; use settingsRepo, visualizerSettingsRepo
- Inject into ApplicationShell; replace SaveSettings() with _settingsPersistence.Save()

---

## Phase 2: AnalysisEngine (~518 lines)

### 2.1 Beat detection

- Add `IBeatDetector` interface (ProcessFrame, DecayFlashFrame, CurrentBpm, BeatFlashActive, BeatCount, BeatSensitivity)
- Implement BeatDetector; move energy history, beat times, BPM estimation, threshold, flash state
- Register and inject into AnalysisEngine; call _beatDetector.ProcessFrame, read properties for snapshot/header

### 2.2 Volume analysis

- Add `IVolumeAnalyzer` interface (ProcessFrame, Volume, LeftChannel, RightChannel, LeftPeakHold, RightPeakHold)
- Implement VolumeAnalyzer; move left/right channel, peaks, peak hold
- Inject into AnalysisEngine; delegate per-frame volume processing

### 2.3 FFT band pipeline

- Add `IFftBandProcessor` with Process(fftBuffer, sampleRate, numBands), SmoothedMagnitudes, PeakHold
- Implement FftBandProcessor; move band creation, smoothing, peak hold from AnalysisEngine
- Inject into AnalysisEngine; window+FFT in engine, feed buffer to processor

### 2.4 Engine as coordinator

- AnalysisEngine delegates to volume, beat, FFT; fills snapshot; calls renderer
- AnalysisEngine analysis-only: ProcessAudio only does analysis, returns results via GetSnapshot(); display/rendering moved to IVisualizationOrchestrator (VisualizationOrchestrator)
- Profile ProcessAudio if desired (ADR-0030, ADR-0040); extraction used DI per ADR-0040

---

## Phase 3: SettingsModal (~376 lines)

### 3.1 Settings modal state

- Extract SettingsModalState (focus enum, selected indices, buffers, renaming)
- Replace scattered locals in Show() with state object; make transitions explicit

### 3.2 Settings modal renderer

- Add ISettingsModalRenderer (or equivalent) with Draw(state, palette, dimensions, layers/settings rows)
- Move DrawSettingsContent logic into renderer; SettingsModal calls _renderer.Draw(state)

### 3.3 Settings modal key handler

- Add IKeyHandler with Handle(key, context)
- Move layer selection, type cycling, preset rename/create, setting edit logic into handler
- SettingsModal: read key → handler updates state → modal reacts (redraw, close)

### 3.4 Optional: panel components

- If renderer or handler grows: extract ILayerListPanel, ISettingsListPanel

---

## Phase 4: TextLayersVisualizer

### 4.1 Key handler

- Add TextLayersKeyContext and IKeyHandler (Handle(key, context))
- Implement TextLayersKeyHandler (P, [ ], I, Left/Right, 1–9, Shift+1–9)
- Register and inject into TextLayersVisualizer; HandleKey delegates to handler

### 4.2 Toolbar builder

- Add TextLayersToolbarContext and ITextLayersToolbarBuilder (BuildSuffix(context))
- Implement TextLayersToolbarBuilder; GetToolbarSuffix delegates to builder
- Register and inject into TextLayersVisualizer
- Moved toolbar builder (interface, context, implementation) to **Application** as general UI; visualizer only supplies context (including OscilloscopeGain).

### 4.3 Optional: palette resolver / layer state

- If desired: extract ITextLayersPaletteResolver; per-layer state holder

---

## Documentation

- Add ADR (e.g. 0041-god-object-refactoring-strategy.md) documenting the extraction strategy
- Update docs/adr/README.md and .cursor/rules/adr.mdc to reference the ADR
- Update README.md if user-facing behavior or setup changes