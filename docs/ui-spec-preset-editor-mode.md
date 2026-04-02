# UI spec: Preset editor mode

**Application mode:** `ApplicationMode.PresetEditor` ([`ApplicationMode`](../src/AudioAnalyzer.Domain/ApplicationMode.cs)). See the mode index: [ui-spec-application-modes.md](ui-spec-application-modes.md).

This spec follows [ui-spec-format.md](ui-spec-format.md). It documents the main console layout when a preset is active and the visualizer is showing layer content (e.g. ASCII image layer). Layout follows [ADR-0050](adr/0050-ui-alignment-blocks-label-format.md): left-aligned UI, 8-character block sizing, and label format `Label:value` (no space after colon). Row 0 is the universal title breadcrumb ([ui-spec-title-breadcrumb.md](ui-spec-title-breadcrumb.md)). Regenerate the screenshot from a screen dump (Ctrl+Shift+E or `--dump-after N` when a console is available) so the dump matches the current build.

## Screenshot

```text
         aUdioNLZR/pReset/pReset_2[1]:aScii_image         
Device:DIA High DefNow:Svampyr - xtalzkullz 145bpm         
BPM:144        Beat:1,1 (+/-)     Volume/dB:8,3%  -21,7dB     
Layers:123456789 | Image:example.png | Palette:Default                         
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

- **1** — Title bar: single-line breadcrumb, left-aligned (app name / mode / preset name [layer index 1–9]: layer type, e.g. `aUdioNLZR/pReset/pReset_2[1]:aScii_image`). Mode segment is `pReset` (Hackerized “Preset”). Same breadcrumb style appears on row 0 in modals with path suffixes per [ui-spec-title-breadcrumb.md](ui-spec-title-breadcrumb.md). Padding on the right to fill width.
- **2** — Device line: left-aligned. Device label and value (`Device:value`), then now-playing scrolling viewport (`Now:value`). Label format uses colon with no space before value; cells use 8-char block sizing where applicable.
- **3** — Three separate horizontal cells (each a labeled scrolling viewport): **BPM** (numeric or `—` when audio BPM is not locked), **Beat** (when **Bpm source** is audio analysis, value includes sensitivity and a `(+/-)` hint; labels are `Beat:` only — key discovery via help **H**, [ADR-0034](adr/0034-viewport-label-hotkey-hints.md)), **Volume/dB**. Demo and Link sources leave the Beat cell value empty. Label format `Label:value` with no space after colon. Column widths follow [ADR-0050](adr/0050-ui-alignment-blocks-label-format.md) (three bands across the row).
- **4** — Toolbar: left-aligned. Separate labeled fields: Layers (digits 1–9), optional **contextual** fields for the palette-cycled layer (e.g. **Gain** when Oscilloscope, **Image** file name when AsciiImage, **Model** file name when AsciiModel), then Palette (palette for the layer selected in the title bar; display name with **each letter colored** from the palette in rotation; phase advances with beat count when BPM is active, otherwise a slow tick-based rotation). Long contextual values truncate with an ellipsis. When content exceeds width, overflowing cells scroll so all layer numbers remain visible. Label format `Label:value`; key bindings are in the help modal (H). Screen dumps strip ANSI, so the screenshot shows plain name text.
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
