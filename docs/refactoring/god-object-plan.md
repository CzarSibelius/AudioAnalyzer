# God Object Refactoring Plan

Refactoring tasks for ApplicationShell, AnalysisEngine, and SettingsModal.
See original analysis for context. Agents: mark `[x]` when a task is implemented.

---

## Phase 1: ApplicationShell (17 deps → ~7)

### 1.1 IHeaderDrawer
- [x] Add `IHeaderDrawer` interface in Abstractions (DrawMain, DrawHeaderOnly)
- [x] Implement `HeaderDrawer` that encapsulates viewports, title bar, engine state, UiSettings
- [x] Register in ServiceConfiguration; inject into ApplicationShell
- [x] Replace all `ConsoleHeader.DrawMain(...)` calls with `_headerDrawer.DrawMain()`
- [x] Remove duplicated header argument lists from ApplicationShell

### 1.2 Key handlers / dispatcher
- [x] Add `IMainLoopKeyHandler` (TryHandle(key, context)) and `MainLoopKeyContext`
- [x] Implement `MainLoopKeyHandler` with switch for Tab, V, S, D, H, +/-, P, F, Escape
- [x] Register IMainLoopKeyHandler; inject into ApplicationShell
- [x] Replace switch(key.Key) in ApplicationShell with `_keyHandler.TryHandle(key, ctx)`

### 1.3 Device lifecycle
- [x] Add `IDeviceCaptureController` interface (StartCapture, StopCapture, RestartCapture, Shutdown)
- [x] Implement DeviceCaptureController with _currentInput, _deviceLock, per ADR-0018
- [x] Inject IDeviceCaptureController into ApplicationShell; remove direct IAudioInput handling

### 1.4 Settings persistence
- [x] Add `IAppSettingsPersistence` interface with Save()
- [x] Implement AppSettingsPersistence pulling from engine, visualizerSettings, settings; use settingsRepo, visualizerSettingsRepo
- [x] Inject into ApplicationShell; replace SaveSettings() with _settingsPersistence.Save()

---

## Phase 2: AnalysisEngine (~518 lines)

### 2.1 Beat detection
- [x] Add `IBeatDetector` interface (ProcessFrame, DecayFlashFrame, CurrentBpm, BeatFlashActive, BeatCount, BeatSensitivity)
- [x] Implement BeatDetector; move energy history, beat times, BPM estimation, threshold, flash state
- [x] Register and inject into AnalysisEngine; call _beatDetector.ProcessFrame, read properties for snapshot/header

### 2.2 Volume analysis
- [x] Add `IVolumeAnalyzer` interface (ProcessFrame, Volume, LeftChannel, RightChannel, LeftPeakHold, RightPeakHold)
- [x] Implement VolumeAnalyzer; move left/right channel, peaks, peak hold
- [x] Inject into AnalysisEngine; delegate per-frame volume processing

### 2.3 FFT band pipeline
- [x] Add `IFftBandProcessor` with Process(fftBuffer, sampleRate, numBands), SmoothedMagnitudes, PeakHold
- [x] Implement FftBandProcessor; move band creation, smoothing, peak hold from AnalysisEngine
- [x] Inject into AnalysisEngine; window+FFT in engine, feed buffer to processor

### 2.4 Engine as coordinator
- [x] AnalysisEngine delegates to volume, beat, FFT; fills snapshot; calls renderer
- [ ] Profile ProcessAudio if desired (ADR-0030, ADR-0040); extraction used DI per ADR-0040

---

## Phase 3: SettingsModal (~376 lines)

### 3.1 Settings modal state
- [x] Extract SettingsModalState (focus enum, selected indices, buffers, renaming)
- [x] Replace scattered locals in Show() with state object; make transitions explicit

### 3.2 Settings modal renderer
- [x] Add ISettingsModalRenderer (or equivalent) with Draw(state, palette, dimensions, layers/settings rows)
- [x] Move DrawSettingsContent logic into renderer; SettingsModal calls _renderer.Draw(state)

### 3.3 Settings modal key handler
- [x] Add ISettingsModalKeyHandler with HandleSettingsKey(key, state) returning Continue/Close/Refresh
- [x] Move layer selection, type cycling, preset rename/create, setting edit logic into handler
- [x] SettingsModal: read key → handler updates state → modal reacts (redraw, close)

### 3.4 Optional: panel components
- [ ] If renderer or handler grows: extract ILayerListPanel, ISettingsListPanel

---

## Documentation

- [x] Add ADR (e.g. 0041-god-object-refactoring-strategy.md) documenting the extraction strategy
- [x] Update docs/adr/README.md and .cursor/rules/adr.mdc to reference the ADR
- [ ] Update README.md if user-facing behavior or setup changes
