# UI spec example: Main view (preset mode)

This example follows the [UI spec format](ui-spec-format.md). It documents the main console layout when a preset is active and the visualizer is showing layer content (e.g. ASCII image layer). Screenshot source: `screen-dumps/screen-20260225-153434.txt`.

## Screenshot

```text
╔══════════════════════════════════════════════════════════╗
║         aUdioNLZR/pReset/pReset_2[0]:aScii_image         ║
╚══════════════════════════════════════════════════════════╝
Device(D):DIA High DefNow:Svampyr - xtalzkullz 145bpm       
BPM: 144  Beat: 1,1 (+/-)     Volume/dB:  8,3%  -21,7dB     
Press H for help, D device, F full screen, PrintScr dump, E…
Layers:123456789 (1-9 select, ←→ ty…             H=Help     
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

- **1** — Title bar top border (box-drawing).
- **2** — Title bar content: app name / mode / preset name [layer index]: layer type (e.g. `aUdioNLZR/pReset/pReset_2[0]:aScii_image`).
- **3** — Title bar bottom border (box-drawing).
- **4** — Device line: device label with hotkey (D), then now-playing (scrolling viewport): track title and BPM (e.g. `Device(D):…` `Now:Svampyr - xtalzkullz 145bpm`).
- **5** — BPM value, beat position with +/- hint, volume percentage and dB (e.g. `BPM: 144  Beat: 1,1 (+/-)     Volume/dB:  8,3%  -21,7dB`).
- **6** — Shortcuts line (scrolling if long): H help, D device, F full screen, PrintScr dump, etc.
- **7** — Toolbar: layers 1–9, selection and type hint (←→), right-aligned H=Help.
- **8** — First row of visualizer viewport (layer content; here ASCII art).
- **9** — Visualizer content.
- **10** — Visualizer content.
- **11** — Visualizer content.
- **12** — Visualizer content.
- **13** — Visualizer content.
- **14** — Visualizer content.
- **15** — Visualizer content.
- **16** — Visualizer content.
- **17** — Visualizer content.
- **18** — Visualizer content.
- **19** — Visualizer content: now-playing overlay line (e.g. track title in block characters).
- **20** — Visualizer content.
- **21** — Visualizer content.
- **22** — Visualizer content.
- **23** — Visualizer content.
- **24** — Visualizer content.
- **25** — Visualizer content.
- **26** — Visualizer content.
- **27** — Visualizer content.
- **28** — Visualizer content.
- **29** — Last row of visualizer content.
- **30** — Blank line (bottom padding / unused rows).
