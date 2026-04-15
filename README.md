# Audio Analyzer

Turn what you are listening to—or a demo feed—into **live terminal art**: stacked **text layers** (spectrum bars, oscilloscope, fractals, fills and blends, marquee text, ASCII art, **ASCII webcam** on Windows, 3D OBJ, and more), driven in real time from your audio. Switch **presets** from the keyboard, chain them in **shows** for timed or beat-based sets, and use the console as a distinctive layer for listening at your desk, a venue screen, or a stream.

> **Content warning:** This project is **100% vibe coded.** If something feels suspiciously *correct*, treat it as a happy accident—**H** in-app and [docs/adr/](docs/adr/README.md) are your spirit guides.

## Who it's for

- **Listeners** — See levels, spectrum, and beat feedback for music playing on the PC; pick loopback or a device, tune presets, or use **Demo Mode** when you just want the visuals.
- **VJs** — Readable, keyboard-driven terminal visuals for live contexts; **shows** automate preset flow; optional **Ableton Link** (locally built `link_shim.dll`) can align tempo with other Link apps on the LAN.
- **Streamers (e.g. DJ sets)** — Capture the console in OBS or similar as a distinctive layer in the stream layout.

Deeper personas, typical setups, and **non-goals**: [docs/product-audience.md](docs/product-audience.md).

## Why it stands out

- **Terminal-native** — High-contrast, text-first visuals you can run in a standard Windows console.
- **Deeply configurable** — Layers, palettes, optional UI themes, presets, and shows without leaving the keyboard-driven workflow.
- **Honest about the signal** — Analysis follows your chosen input (or Demo); optional Link/Demo tempo for counters is a deliberate choice, not a hidden substitute for the waveform.

## Tempo and sync (short)

FFT, levels, and spectrum follow your **audio input**. Beat/BPM can follow **energy detection** on that stream, **Demo Mode** timing, or **[Ableton Link](https://ableton.github.io/link/)** when you build and drop **`link_shim.dll`** next to the executable. **Official releases from this repository do not ship** `link_shim.dll`. Choose the BPM source in **General settings**.

## Requirements and limitations

- **Windows** (WASAPI loopback/capture).
- **[.NET 10](https://dotnet.microsoft.com/download)** SDK or later if you build from source; NuGet restore happens on build.

The app is **not** a DAW, DJ deck, video compositor (no NDI/Spout), or cross-platform product — see **Out of scope** in [docs/product-audience.md](docs/product-audience.md).

## License

- **Audio Analyzer** (this repository): [GNU General Public License v3.0 only](LICENSE) (SPDX: `GPL-3.0-only`).
- **Ableton Link** and the optional **`link_shim.dll`** native shim (GPL-2.0+ when you build or distribute those components): see [NOTICE](NOTICE) and [docs/adr/0066-bpm-source-and-ableton-link.md](docs/adr/0066-bpm-source-and-ableton-link.md).

## Build and run from source

Step-by-step build, first launch, and in-depth tips: **[docs/getting-started.md](docs/getting-started.md)**.

Contributors and AI-assisted workflows (tests, format, project layout): [AGENTS.md](AGENTS.md) and [docs/agents/AGENTS.md](docs/agents/AGENTS.md).

## More documentation

- **Getting started** (build, run, first session): [docs/getting-started.md](docs/getting-started.md)
- **Audience and non-goals**: [docs/product-audience.md](docs/product-audience.md)
- **JSON, `appsettings.json`, folders next to the app**: [docs/configuration-reference.md](docs/configuration-reference.md)
- **Architecture decisions**: [docs/adr/README.md](docs/adr/README.md)
