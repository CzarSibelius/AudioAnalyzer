# Audio Analyzer

A real-time audio analyzer that captures and analyzes system audio output using NAudio. This application captures the audio playing on your system (loopback capture) or from a selected capture device and performs FFT analysis with multiple visualization modes: spectrum bars, oscilloscope, VU meter, Winamp-style bars, and Geiss-style visualization.

## Prerequisites

- .NET 10.0 SDK or later
- Windows operating system (uses WASAPI loopback/capture)
- NAudio package (automatically restored when building)

## How to Run

### Using Visual Studio

1. Open `AudioAnalyzer.sln` in Visual Studio
2. Set **AudioAnalyzer.Console** as the startup project (or press F5 to build and run)

### Using Command Line

From the solution root (the folder containing `AudioAnalyzer.sln`):

1. Restore and build:
   ```bash
   dotnet build
   ```

2. Run the application:
   ```bash
   dotnet run --project src/AudioAnalyzer.Console/AudioAnalyzer.Console.csproj
   ```

On Windows you can use backslashes: `src\AudioAnalyzer.Console\AudioAnalyzer.Console.csproj`.

## Usage

1. On first run (or if no device was saved), choose an audio input: loopback (system output) or a specific capture device (↑/↓ to move, ENTER to select, ESC to cancel).
2. The analyzer shows real-time volume and frequency analysis. Play audio to see it in action.
3. **Keyboard controls:**
   - **H** – Show help (all keys and visualization modes)
   - **V** – Cycle visualization mode (Spectrum, Oscilloscope, VU Meter, Winamp Style, Geiss)
   - **B** – Toggle beat circles (Geiss mode)
   - **+** / **-** – Increase / decrease beat sensitivity
   - **[** / **]** – Increase / decrease oscilloscope gain (Oscilloscope mode; 1.0–10.0)
   - **S** – Save current settings (device, mode, beat sensitivity, oscilloscope gain, beat circles)
   - **D** – Change audio input device
   - **ESC** – Quit

## What It Does

- **Audio input**: Loopback (system output) or a specific WASAPI capture device; choice is saved in settings.
- **Volume analysis**: Real-time level and peak display; stereo VU-style meters in VU Meter mode.
- **FFT analysis**: Fast Fourier Transform with log-spaced frequency bands and peak hold.
- **Visualization modes**: Spectrum bars, Oscilloscope (time-domain waveform; gain adjustable with [ ] in real time, 1.0–10.0), VU Meter, Winamp-style bars, Geiss (with optional beat circles).
- **Beat detection**: Optional beat detection and BPM estimate; sensitivity and beat circles are configurable and persist.
- **Real-time display**: Updates every 50 ms.
- **Settings**: Stored in a local file (e.g. next to the executable); device, visualization mode, beat sensitivity, oscilloscope gain, and beat circles are persisted.

## Dependencies

- **NAudio 2.2.1**: WASAPI capture/loopback and audio processing
- **Microsoft.Extensions.DependencyInjection 9.0.0**: Used by the Console host (dependency injection)

## Notes

- Requires Windows with WASAPI support.
- Use loopback to analyze system playback; use a capture device for microphone or other inputs.
- Ensure audio is playing (or that the selected device is active) to see meaningful analysis.

## Visualizer bounds (for developers)

Visualizers receive a **viewport** (`VisualizerViewport`: start row, max lines, width). They must not write more than `viewport.MaxLines` lines and no line longer than `viewport.Width`. The composite renderer validates dimensions and display start row before calling visualizers; if a visualizer throws, a one-line error is shown and the next frame can recover. This keeps resizes and bad data from corrupting the console UI.
