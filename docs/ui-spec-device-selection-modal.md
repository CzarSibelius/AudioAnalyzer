# UI spec: Device selection modal

Full-screen modal for choosing an audio input device. Opened from **D** in Preset / Show modes, or **Enter** on **Audio input devices** in [General settings hub](ui-spec-general-settings-hub.md). See [ui-spec-settings-surfaces.md](ui-spec-settings-surfaces.md) for shared patterns. List selection: [ui-spec-menu-selection.md](ui-spec-menu-selection.md).

## Screenshot

```text
aUdioNLZR/auDioinput
  Use ↑/↓ to select, ENTER to confirm, ESC to cancel

 ► Speakers (Realtek Audio) (current)
   Microphone (USB)
```

*(Representative layout; regenerate from a screen dump when verifying.)*

## Line reference

**1** — Title breadcrumb (row 0): app-settings track includes `/audioinput` per [ui-spec-title-breadcrumb.md](ui-spec-title-breadcrumb.md).

**2** — Hint: `  Use ↑/↓ to select, ENTER to confirm, ESC to cancel` (two leading spaces).

**3** — Blank line.

**4+** — Device list: one line per device. Selected line uses leading ` ► ` (space + U+25BA); other lines use three spaces. The active capture device is suffixed with ` (current)` when its name matches. Selected row uses UI palette background + highlighted foreground; the current (non-selected) device line may use highlighted foreground only. Long names truncate to terminal width minus margin.

The modal uses **ModalSystem.RunModal** (full redraw on key). Closing restores the main view breadcrumb.
