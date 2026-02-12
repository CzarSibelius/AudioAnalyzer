# ADR-0003: 24-bit true color, JSON palette files, and palette cycling

**Status**: Accepted

## Context

We want to support 24-bit true color in palette-aware visualizers, store palettes as editable JSON files in a dedicated directory, and let the user cycle through available palettes with a keyboard shortcut. ADR-0002 introduced a reusable `ColorPalette` (console color names only) and per-visualizer settings; we now extend the color model and move palette storage to external files.

## Decision

1. **Unified color type (24-bit and 16-color)**: A **`PaletteColor`** type (Domain) represents either 16-color (`ConsoleColor`) or 24-bit RGB (byte R, G, B). ANSI output uses either the existing 16-color codes or the 24-bit sequence `\x1b[38;2;{r};{g};{b}m`. The snapshot and palette-aware visualizers use `IReadOnlyList<PaletteColor>?`; non-palette code continues to use `ConsoleColor` where appropriate.

2. **Palettes in a directory**: Palettes are stored as **JSON files** in a **palettes directory** (e.g. next to the executable: `palettes/`). Each file has `Name` (display name) and `Colors` (array where each entry is either a string: `"#RRGGBB"` or a console color name, or an object `{ "R", "G", "B" }`). The **`PaletteDefinition`** and **`PaletteColorEntry`** types (Domain) model this format. **`IPaletteRepository`** (Application) lists and loads palettes by id (filename without extension); **`FilePaletteRepository`** (Infrastructure) implements it.

3. **Selected palette and cycling**: **`AppSettings.SelectedPaletteId`** (deprecated; see [ADR-0009](0009-per-visualizer-palette.md)) originally stored a global palette id. ADR-0009 moves palette selection to per-visualizer `PaletteId`. Each palette-aware visualizer has its own palette; P cycles and saves only the current visualizer's palette.

4. **Backward compatibility**: If `SelectedPaletteId` is missing, the app falls back to parsing `VisualizerSettings.UnknownPleasures.Palette` (legacy `ColorPalette`) and uses that with display name "Custom". A built-in default palette (e.g. `palettes/default.json`) is shipped so there is always at least one palette.

## Consequences

- **Domain**: `PaletteColor`, `PaletteDefinition`, `PaletteColorEntry`; `AppSettings.SelectedPaletteId`. `ColorPalette` remains for legacy settings read.
- **Application**: `AnsiConsole` gains 24-bit and `PaletteColor` overloads; `ColorPaletteParser` parses both `PaletteDefinition` and `ColorPalette` to `IReadOnlyList<PaletteColor>`; `IPaletteRepository` and `PaletteInfo`; snapshot carries `PaletteColor` list and optional `CurrentPaletteName`.
- **Infrastructure**: `FilePaletteRepository`; renderer accepts and passes `IReadOnlyList<PaletteColor>?` and optional display name.
- **Console**: Resolves palette at startup (repository or legacy); key P cycles palettes and persists `SelectedPaletteId`; help and toolbar document P and show palette name.
- **Documentation**: README and ADR index updated; palette JSON format and directory location documented.
