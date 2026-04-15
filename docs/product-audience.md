# Product audience

This document describes who **Audio Analyzer** is built for, what each group typically wants from the app, and where the product intentionally does not try to compete. The [README](../README.md) has a short, audience-led summary; this page is the canonical detail for maintainers, contributors, and agents.

## Summary

| Audience | Primary goal | Typical setup |
|----------|--------------|----------------|
| **Listeners** | See live analysis and visuals for music they play on the PC | Windows, **loopback** (or device) + headphones/speakers; presets for taste |
| **VJs** | Visuals for live music shows, readable from a distance, controllable flow | Larger terminal or capture of the window; **shows** for timed/beat-based preset chains; optional **[Ableton Link](adr/0066-bpm-source-and-ableton-link.md)** when tempo should follow other Link apps |
| **Streamers (e.g. DJ sets)** | Terminal visuals as part of a stream layout | Loopback or capture matching the stream mix; **screen dump** (Ctrl+Shift+E, or CLI flags) for clips; presets/shows aligned with set structure |

From source (clone, build, run, first session): [getting-started.md](getting-started.md).

## Listeners

People who play music on their PC and want **spectrum, levels, beat/BPM feedback**, and **text-layer art** in the same console window—without treating the app as a performance tool.

- **Inputs**: System **loopback** is the default story (“what I hear”). **Demo Mode** helps explore visuals without audio.
- **Success**: Stable run, understandable device picker, presets that look good out of the box, **H** help when they forget keys.

## VJs

People who drive **live visuals** in venues or club contexts where the terminal is part of the show.

- **Inputs**: May use **capture device** or **loopback** depending on the mixer/interface; **Link** when tempo should lock to DJ software or other Link peers on the LAN (requires locally built `link_shim.dll`; see [getting-started.md](getting-started.md) and [ADR-0066](adr/0066-bpm-source-and-ableton-link.md)).
- **Success**: Smooth redraw (see [ADR-0067](adr/0067-60fps-target-and-render-fps-overlay.md)), **shows** for automatic preset progression, palettes/themes that read under stage lighting (contrast is on the operator—terminal and font choice matter).

## Streamers (DJs and similar)

People who **broadcast** and want the terminal visualization in OBS or similar—not necessarily as the only visual, but as a distinctive layer.

- **Inputs**: Often **loopback** on the streaming PC so the viz matches what viewers hear; or a dedicated capture device if the mix is routed that way.
- **Success**: Predictable framing (terminal size), optional **plain screen dumps** for social clips or bug reports, shows/presets that match segments of a set.

## Shared behavior

All audiences use the same core product: **Windows** console app, **WASAPI** loopback or capture (or Demo), **presets** (layer stacks), optional **shows**, **General settings** for audio/BPM source and hub options, and the same **GPL-3.0-only** app license.

## Out of scope (non-goals)

These are common adjacent products; stating them here avoids scope confusion:

- **Not a DAW or DJ application** — no decks, clips, or stem separation; audio is analyzed and drawn, not remixed inside this repo.
- **Not a video compositor** — no NDI, Spout, or built-in OBS plugin; streamers integrate by **capturing the console window** (or using dumps for stills).
- **Not cross-platform** — Windows-focused (WASAPI, optional Windows-only layers such as webcam ASCII per [ADR-0074](adr/0074-ascii-video-layer-and-frame-source.md)).

When a feature request clearly targets a different product category, prefer updating this section (or an ADR) rather than expanding scope silently.
