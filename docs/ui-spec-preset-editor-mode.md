# UI spec: Preset editor mode

**Application mode:** `ApplicationMode.PresetEditor` ([`ApplicationMode`](../src/AudioAnalyzer.Domain/ApplicationMode.cs)). See the mode index: [ui-spec-application-modes.md](ui-spec-application-modes.md).

This spec follows [ui-spec-format.md](ui-spec-format.md). It documents the main console layout when a preset is active and the visualizer is showing layer content (e.g. ASCII image layer). **Lines 1–4** are the unified **Toolbar** region (same packed 8-column rules on each row): see [ui-spec-toolbar.md](ui-spec-toolbar.md). Layout follows [ADR-0050](adr/0050-ui-alignment-blocks-label-format.md): left-aligned UI, 8-character block sizing, and label format `Label:value` (no space after colon). Row 1 is the universal title breadcrumb ([ui-spec-title-breadcrumb.md](ui-spec-title-breadcrumb.md)). Regenerate the screenshot from a screen dump (Ctrl+Shift+E or `--dump-after N` when a console is available) so the dump matches the current build.

## Screenshot

```text
         aUdioNLZR/pReset/pReset_2[1]:aScii_image         
Device:DIA High DefNow:Svampyr - xtalzkullz 145bpm         
BPM:144        Beat:1,1 (+/-)     Volume/dB:8,3%  -21,7dB     
Layers:1 | Image:example.png | Palette:Default                                
      ..       .:.        -#=+-+=+:        .:.       ..     
       :.      ::          -=*.*+-          ::      .:       
       :       .:           :: -.           ::       .       
       :.      ::         ..      .         ::      .:       
       ..      .:      ..  ..   ..  .       :.      ..       
... .. ::      :. ........  .   .  ..... .. .:      :: .....
....  :...    ...:  .......          ....  :...    ...:  ...
:.:-.  ..:    :.:   :-::.:::.   ::::::.:-.  :.:    :..   :-:
. .=.  : -.   -::   .:-:.::-.   .:---- .=.  .:-.   - :   .:-
   :.  -.-    -:.    .:..::--   :--=--  :.  .--    -.:    .:
████████████████Svampyr - xtalzkullz 145bpm█████████████████
  ...  ::-.  .-:.  :   .:----   ::--:  ...  .:-.  .-::  :   
....   ..=    =..  ......-=--   :===: ...   ..=    =..  ....
.:-.   . -    -.: . :...:-===   +***:.:-.   :.-    - . . :..
-=.    ..:    :..   :.==+*=.    -**=::=.    ..:    :..   :.=
... .. ::      :. ........  .   .  ..... .. .:      :: .....
       ..      .:      ..  ..   ..  .       :.      ..       
       :.      ::         ..      .         ::      .:       
       :       .:           :: -.           ::       .       
       :.      ::          -=*.*+-          ::      .:       
      ..       .:.        -#=+-+=+:        .:.       ..      
                                                             
```

## Line reference

- **1** — **Toolbar** row 1 — Title bar: single-line breadcrumb, left-aligned (app name / mode / preset name [layer index 1–9]: layer type, e.g. `aUdioNLZR/pReset/pReset_2[1]:aScii_image`). Mode segment is `pReset` (Hackerized “Preset”). Same breadcrumb style appears on row 0 in modals with path suffixes per [ui-spec-title-breadcrumb.md](ui-spec-title-breadcrumb.md). Padding on the right to fill width. See [ui-spec-toolbar.md](ui-spec-toolbar.md).
- **2** — **Toolbar** row 2 — Device / Now: `Device:value`, then `Now:value` (scrolling when long). **Spread** layout: starts on 8 columns, Device left, Now right ([ui-spec-toolbar.md](ui-spec-toolbar.md)).
- **3** — **Toolbar** row 3 — BPM / Beat / Volume: **BPM** (numeric or `—` when audio BPM is not locked), **Beat** (when **Bpm source** is audio analysis, value includes sensitivity and `(+/-)`; labels are `Beat:` only — keys via help **H**, [ADR-0034](adr/0034-viewport-label-hotkey-hints.md)), **Volume/dB**. Demo and Link sources leave the Beat cell value empty. **Spread** three-way (left / near-center / right); audio BPM reserves Beat width for `*BEAT*`. [ui-spec-toolbar.md](ui-spec-toolbar.md).
- **4** — **Toolbar** row 4 — TextLayers row: Layers (**one digit per configured layer**, 1-based; palette-cycled layer highlighted, disabled dimmed), optional **contextual** fields (e.g. **Gain**, **Image**, **Model**), **Palette** (per-letter colors; phase from beat or tick). Same spread rules as rows 2–3 (many segments use interpolated interior positions). Long values truncate with ellipsis; overflowing cells scroll. Key bindings in help (H). Screen dumps strip ANSI (regenerate dump when validating layout). [ui-spec-toolbar.md](ui-spec-toolbar.md).
- **5** — First row of visualizer viewport (layer content; here ASCII art).
- **6** — Visualizer content.
- **7** — Visualizer content.
- **8** — Visualizer content.
- **9** — Visualizer content.
- **10** — Visualizer content.
- **11** — Visualizer content.
- **12** — Visualizer content.
- **13** — Visualizer content.
- **14** — Visualizer content.
- **15** — Visualizer content.
- **16** — Visualizer content: now-playing overlay line (e.g. track title in block characters).
- **17** — Visualizer content.
- **18** — Visualizer content.
- **19** — Visualizer content.
- **20** — Visualizer content.
- **21** — Visualizer content.
- **22** — Visualizer content.
- **23** — Visualizer content.
- **24** — Visualizer content.
- **25** — Visualizer content.
- **26** — Last row of visualizer content.
- **27** — Blank line (bottom padding / unused rows).
