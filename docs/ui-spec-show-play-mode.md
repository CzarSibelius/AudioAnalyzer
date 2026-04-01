# UI spec: Show play mode

**Application mode:** `ApplicationMode.ShowPlay` ([`ApplicationMode`](../src/AudioAnalyzer.Domain/ApplicationMode.cs)). See the mode index: [ui-spec-application-modes.md](ui-spec-application-modes.md).

This spec follows [ui-spec-format.md](ui-spec-format.md). Show play uses the **same three-row header** as Preset editor (title, device/now, BPM/volume) per [ADR-0062](adr/0062-application-mode-classes.md). The **toolbar** is compact: **Show** (name), **Entry** (current index / count), **Palette** (active layer’s palette name with per-letter coloring)—see `TextLayersToolbarBuilder.BuildShowPlayViewports` in the Application project. The title breadcrumb uses mode segment **`sHow`** (Hackerized “Show”) and the **active preset** name from the current Show entry (`TitleBarBreadcrumbFormatter`). Regenerate the screenshot from a screen dump when a console is available ([ADR-0046](adr/0046-screen-dump-ascii-screenshot.md)).

## Screenshot

```text
         aUdioNLZR/sHow/pReset_1[1]:fIll                       
Device:Demo 120 BPM  Now:Svampyr - xtalzkullz 145bpm         
BPM: 144  Beat: 1,1 (+/-)     Volume/dB:  8,3%  -21,7dB     
Show:Show 1 | Entry:1/2 | Gain:2.5 | Palette:Default                    
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

- **1** — Title bar: breadcrumb `app / sHow / preset [z]:layer` (e.g. `aUdioNLZR/sHow/pReset_1[1]:fIll`). The **preset** and **layer** segments reflect the **current Show entry** and active layer, not a separate “show name” segment on this row ([ADR-0060](adr/0060-universal-title-breadcrumb.md)).
- **2** — Device line: same as Preset editor (`Device:value`, `Now:value` scrolling viewport).
- **3** — BPM / Beat / Volume line: same as Preset editor.
- **4** — Toolbar: **Show** (show name, ellipsis if truncated), **Entry** (`current/total` or `—` if no entries), optional **contextual** fields for the palette-cycled layer (same as Preset editor: e.g. Gain, Image file name, Model file name), **Palette** (palette name; screen dump strips ANSI). **No** per-layer **1–9** digits row in this mode; **S** opens **Show edit** ([ADR-0031](adr/0031-show-preset-collection.md)).
- **5** — First row of visualizer viewport (layer content).
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
- **16** — Visualizer content: optional now-playing overlay line (e.g. track title in block characters).
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
