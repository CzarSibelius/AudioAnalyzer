# Waveform strip (layer only)

## Description

Time-domain waveform drawn as a **horizontal strip** across the layer width: **Rekordbox-inspired** density with each column as a **vertical** stroke (envelope when **Filled**). Default path uses **mono** overview; optional **StereoStacked** draws **left** overview in the upper half and **right** overview in the lower half when the layer has enough height (inner band at least four rows after any time label). The strip uses the **full layer draw height** `ctx.Height` from `TextLayerDrawContext` (including [ADR-0058](../adr/0058-layer-render-bounds.md) **Render region** / `RenderBounds`), so shrinking the layer’s render region makes the strip shorter; gain and mapping aim for peaks to use that full height (top and bottom rows are valid extrema).

**Time axis (no separate “future” region):** The **right edge is “now”** (the newest audio in the mapped window). Moving **left** is **older history**. New audio appears at the right; the view reads like history scrolling **right → left** as overview buckets advance, even though the implementation is **column resampling** (each frame, columns map to bucket indices from oldest on the left to newest on the right), not a sliding bitmap texture.

### Mental model: “100 columns” without a scroll buffer

If the drawable strip width is **100** (`ctx.Width` after [render bounds](../adr/0058-layer-render-bounds.md) — not always the full terminal), think of **100 fixed column indices** `x = 0 … 99`. There is **no** separate array of 100 cells that is shifted left each frame and refilled on the right.

Instead, **every frame** the layer does the same thing for each `x`:

1. Pick a **chronological index** in the current visible window (oldest on the left, newest on the right), evenly spaced: column 0 → oldest visible sample, column 99 → newest.
2. Map that index through the same **overview bucket partition** as `AnalysisEngine` (8192 buckets by default).
3. Draw the envelope for that bucket at column `x`.

Stored **beat marks** use the same rule in reverse: global mono sample index → chronological index → bucket → **which `x` shows that bucket** (`ColumnForGlobalMonoSample` / `ColumnForOverviewBucket` in `WaveformStripLayer`). So beats and waveform stay **horizontally** aligned as long as you are on the **overview** path (see “When beats and columns disagree” below).

What *looks* like scrolling is only **data moving under a fixed mapping**: as `newest` advances, the same column index `x` now points at a slightly older part of history until the next overview rebuild (~50 ms); wave and beats both use the **same** snapshot and anchors for that frame.

**When beats and columns disagree**

- **Scope fallback** (512-sample `Waveform` ring when overview is unavailable): columns follow the short scope buffer, but **stored beat marks are not drawn** against that path (`useLen >= 2` is required). You may only see the **BPM-based** dashed grid, which is only an approximation of musical beats.
- **Within one overview bucket** (many mono samples aggregated to one min/max bar): the beat line is aligned to the **bucket** that contains the beat instant, not to a sub-bucket pixel inside the thick envelope.

**Visible time window** ([ADR-0079](../adr/0079-waveform-strip-fixed-visible-seconds.md)): Wall-time scale is **linear** across the strip width (**constant seconds per column**, subject to column quantization). Columns span the **last** `FixedVisibleSeconds` of audio (1–120 s, default **15**), evenly resampled across the strip width — so the **left** edge corresponds to approximately **current time minus that visible span** (or a shorter span until the ring has filled), and the **right** edge to **current time** (newest captured audio in the window). The engine still retains a longer mono ring (**Max audio history** in General Settings), but each ~50 ms overview rebuild aggregates only the **trailing** window (largest `FixedVisibleSeconds` among enabled WaveformStrip layers on the preset), so **horizontal scale stays tied to that duration** and CPU stays **Θ(window)**. Beats/samples older than that tail are not mapped onto the strip (off the left edge).

Wider layers (fullscreen, less aggressive **Render region** width) add columns and improve resolution; shorter **Max audio history** reduces RAM for the internal ring (the strip’s wall-time span is still controlled by `FixedVisibleSeconds`).

**Performance:** With any enabled WaveformStrip, the engine rebuilds the 8192 overview buckets over that **fixed wall-time tail** only. With no enabled WaveformStrip layer, overview rebuild is skipped each gate.

**Data source** (in order):

1. When `WaveformOverviewLength > 0` and overview min/max arrays are present, the strip maps **columns → overview buckets** (long history, decimated in `AnalysisEngine` — [ADR-0077](../adr/0077-waveform-overview-snapshot.md), extended for stereo and spectral details in [ADR-0078](../adr/0078-waveform-strip-stereo-beat-marks-goertzel.md)). Each bucket is a **time slice** summarized as **min/max** (an **envelope / density** view), not a single-sample trace across the whole history—complex material can look thick or busy compared to the short 512-sample scope fallback.
2. Otherwise it uses `Waveform`, `WaveformPosition`, `WaveformSize` like [oscilloscope.md](oscilloscope.md) (512-sample **mono** ring, legacy/tests). Stereo layout does not add a separate scope ring for the right channel.

Column indices map evenly from the **oldest** overview bucket (left) to the **newest** (right), including when the terminal is much narrower than the internal bucket count.

## Snapshot usage

- **Overview** (preferred): `WaveformOverviewMin`, `WaveformOverviewMax`, heuristic band arrays (`WaveformOverviewBandLow` / `Mid` / `High`), Goertzel band arrays (`WaveformOverviewBandLowGoertzel` / `Mid` / `High`), right-channel mirrors (`WaveformOverview*Right`, `*GoertzelRight`), `WaveformOverviewLength`, `WaveformOverviewSpanSeconds`, timeline helpers `WaveformOverviewValidSampleCount`, `WaveformOverviewOldestMonoSampleIndex`, `WaveformOverviewNewestMonoSampleIndex`
- **Overview build anchor** (beat/grid alignment): `WaveformOverviewBuiltValidSampleCount`, `WaveformOverviewBuiltOldestMonoSampleIndex`, `WaveformOverviewBuiltNewestMonoSampleIndex` — stamped on each overview rebuild (~50 ms). The strip draws decimated buckets from that rebuild; beat columns and the dashed grid use these anchors so markers stay on the same envelope pixels until the next rebuild (the live `WaveformOverview*…` timeline fields can advance every frame for other consumers).
- **Beat marks** (for grid when present): `WaveformBeatMarkMonoSampleIndex`, `WaveformBeatMarkBeatOrdinal`, `WaveformBeatMarkLength` — each beat increments `BeatCount`; the engine stores the global mono sample index at that instant
- **Scope fallback**: `Waveform`, `WaveformPosition`, `WaveformSize` — same as [oscilloscope.md](oscilloscope.md)
- `CurrentBpm`, `BeatFlashActive` — BPM **fallback** grid when no beat marks are available yet; marker accent on “now”

## Settings

- **Schema**: `TextLayerSettings` when `LayerType == WaveformStrip`
- **Gain** (double, default: 2.5): Amplitude gain (1.0–10.0). Adjustable with **[** / **]** when that layer is the palette-selected layer (same as Oscilloscope).
- **Filled** (bool, default: **true**): When true, each overview column draws a **bipolar bar** from min to max for that bucket (dense vertical envelope, default look). When false, the overview path draws a **midpoint** sample per column and connects **only** between columns whose bucket indices differ by at most **one** (avoids long spurious diagonals when many buckets are skipped per column). The scope fallback still connects consecutive samples along the 512-sample ring.
- **PaletteId** (string, optional): Layer palette; inherits from `TextLayers.PaletteId` when null/empty. Used when `ColorMode` is `PaletteDistance` (distance from center → palette index, oscilloscope-like; reads as brighter near the midline, more saturated toward the edges).
- **ColorMode** (`PaletteDistance` | `SpectralApprox` | `SpectralGoertzel`, default `SpectralApprox`): `SpectralApprox` uses engine **heuristic** band proxies per bucket. `SpectralGoertzel` uses **Goertzel** magnitudes at fixed frequencies (150 Hz / 1.8 kHz / 6 kHz) on up to 256 samples per bucket — frequency-resolved but **not** a full multiband STFT per column.
- **StereoLayout** (`Mono` | `StereoStacked`, default `Mono`): Stacked L/R strips when height allows and right overview arrays are present.
- **FixedVisibleSeconds** (double **1–120**, default **15**): Trailing wall seconds mapped across the strip width (see **Visible time window** above).
- **BeatsPerBar** (int 1–16, default 4): For **bar** markers when using stored beat marks (`(beatOrdinal - 1) % BeatsPerBar == 0`). Also scales BPM **fallback** bar spacing.
- **BeatGridOffsetColumns** (int −16…16, default 0): Nudges beat/bar columns horizontally after mapping.
- **ShowBeatGrid** (bool, default: true): **Dim dashed** verticals at **stored beat** columns when marks exist; otherwise falls back to BPM + span spacing (approximate grid).
- **ShowBeatMarkers** (bool, default: true): **^** / **v** cues at bar-aligned beat marks (from stored ordinals) or BPM fallback; the **rightmost** column uses a stronger accent when `BeatFlashActive`.
- **ShowTimeLabel** (bool, default: false): When enabled and `ctx.Height >= 2`, **one top row** of the layer is reserved for a short **visible span** label in seconds (matches the strip’s horizontal scale: effective visible seconds from `FixedVisibleSeconds` and the built partition); the waveform uses the remaining rows below so the label does not overlap the envelope.

## Key bindings

- **[** — Decrease gain (when Waveform strip layer is palette-selected)
- **]** — Increase gain
- Toolbar: contextual **Gain** when Waveform strip is the palette-cycled layer
- **Ctrl+R** — Not strip-specific: **full layer reset** clears engine waveform history (including this strip’s overview data) and all layer runtime caches; see [text-layers.md](text-layers.md) Key bindings.

## Viewport constraints

- Uses `ctx.Width` × `ctx.Height` (respects `RenderBounds` via `TextLayerDrawContext` width/height and origin).
- With overview: each column maps to a **chronological** index (evenly across the **visible** trailing window of up to `FixedVisibleSeconds`), then to a decimated bucket via the same partition as the engine rebuild—matching beat column placement. `w == 1` uses the newest chronological slice only. When `width` exceeds the filled overview length, multiple columns may repeat the same bucket. Fallback: `min(width, WaveformSize)` with the same step rule as Oscilloscope.

## Implementation notes

- **Layer**: `WaveformStripLayer` in `TextLayers/WaveformStrip/WaveformStripLayer.cs` (`OverviewBucketIndexForColumn` with valid-sample count, `ColumnForOverviewBucket`, `ColumnForGlobalMonoSample` with `WaveformOverviewBucketIndex` partition for beat columns; overview non-filled connector policy as above). **Column→bucket cache**: when registered with DI, the layer uses `ITextLayerStateStore<WaveformStripLayerState>` ([ADR-0043](../adr/0043-textlayer-state-store.md)) to keep a fixed-width `int[]` of overview bucket indices per layer slot while `ctx.Width`, `WaveformOverviewLength`, and the visible chronology triple are unchanged—**envelope min/max are still taken from the current snapshot each frame** (no sliding texture of pixels; only the mapping arithmetic is reused until the time window or width changes).
- **Settings**: `WaveformStripSettings`, `WaveformStripColorMode`, `WaveformStripStereoLayout` in the same folder (no separate overview time mode; trailing seconds only).
- **Internal state**: Stateless (`ITextLayerRenderer<NoLayerState>`)
- **References**: [text-layers.md](text-layers.md), [oscilloscope.md](oscilloscope.md), [ADR-0014](../adr/0014-visualizers-as-layers.md), [ADR-0077](../adr/0077-waveform-overview-snapshot.md), [ADR-0078](../adr/0078-waveform-strip-stereo-beat-marks-goertzel.md), [ADR-0079](../adr/0079-waveform-strip-fixed-visible-seconds.md)

## Deferred / future

- Full **multiband short-time FFT** per column (true Rekordbox RGB density) and a **DAW-locked** musical phase (beyond stored discrete beats) remain out of scope unless product priorities change.
- Optional **dual 512-sample** scope rings for stereo **scope fallback** (currently mono blend only in scope mode).