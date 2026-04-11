# UI specs: Application modes

Top-level **`ApplicationMode`** values ([`ApplicationMode`](../src/AudioAnalyzer.Domain/ApplicationMode.cs)) each have a dedicated UI spec with an **ASCII screenshot** (from the [screen dump](../adr/0046-screen-dump-ascii-screenshot.md)) and a **line-by-line reference**. Shared format: [ui-spec-format.md](ui-spec-format.md).

| Mode | Enum | Spec | Toolbar (lines 1–4) | Main area |
|------|------|------|----------------------|-----------|
| **Preset editor** | `PresetEditor` | [ui-spec-preset-editor-mode.md](ui-spec-preset-editor-mode.md) | [ui-spec-toolbar.md](ui-spec-toolbar.md) — rows 1–3 from `HeaderContainer`, row 4 from main content | Visualizer from line 5 |
| **Show play** | `ShowPlay` | [ui-spec-show-play-mode.md](ui-spec-show-play-mode.md) | Same [Toolbar](ui-spec-toolbar.md) layout as Preset | Visualizer from line 5; presets auto-cycle per Show ([ADR-0031](adr/0031-show-preset-collection.md)) |
| **General settings** | `Settings` | [ui-spec-general-settings-hub.md](ui-spec-general-settings-hub.md) | 1 (title breadcrumb only) | Hub menu (**HorizontalRowComponent**); no visualizer ([ADR-0061](adr/0061-general-settings-mode.md), [ADR-0062](adr/0062-application-mode-classes.md)) |

**Visualizer fullscreen (F)** — edge-to-edge layout in Preset editor and Show play, per-mode `AllowsVisualizerFullscreen`, and interaction with overlay modals: [ui-spec-fullscreen-visualizer.md](ui-spec-fullscreen-visualizer.md).

**Tab** cycles: Preset editor → Show play (if at least one show has entries) → General settings → Preset editor ([README](../README.md) — **Usage** / keyboard controls).

## Settings surfaces and modals (all modes)

These are not separate `ApplicationMode` values but are documented with the same screenshot + line reference pattern:

- Index: [ui-spec-settings-surfaces.md](ui-spec-settings-surfaces.md)
- General settings hub (also listed above)
- [ui-spec-device-selection-modal.md](ui-spec-device-selection-modal.md)
- [ui-spec-preset-settings-modal.md](ui-spec-preset-settings-modal.md)
- Universal title breadcrumb (row 0): [ui-spec-title-breadcrumb.md](ui-spec-title-breadcrumb.md)

## Regenerating screenshots

1. **Interactive**: Run the app in a real Windows console, switch to the desired mode, press **Ctrl+Shift+E** (or use the **Print Screen** flow per [ADR-0046](adr/0046-screen-dump-ascii-screenshot.md)). Paste the `.txt` output into the spec’s fenced block and refresh the line reference.
2. **Automation**: `--dump-after N` / `--dump-path` work when the process has a **valid console** (e.g. local `cmd` or PowerShell). In environments without a console (some CI or automated hosts), the app may fail on `Console` APIs; use interactive capture instead.
3. Persisted mode is stored in `VisualizerSettings.ApplicationMode` in `appsettings.json` next to the executable ([ADR-0029](adr/0029-no-settings-migration.md)); set it before running if you need a specific mode on startup.
