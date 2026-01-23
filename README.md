# Audio Analyzer

A real-time audio analyzer that captures and analyzes system audio output using NAudio. This application captures the audio playing on your system (loopback capture) and performs FFT analysis to display volume levels and frequency information.

## Prerequisites

- .NET 10.0 SDK or later
- Windows operating system (uses WASAPI loopback capture)
- NAudio package (automatically restored when building)

## How to Run

### Using Visual Studio

1. Open `AudioAnalyzer.sln` in Visual Studio
2. Press F5 to build and run the application

### Using Command Line

1. Navigate to the project directory:
   ```bash
   cd AudioAnalyzer
   ```

2. Restore dependencies and build the project:
   ```bash
   dotnet build
   ```

3. Run the application:
   ```bash
   dotnet run
   ```

Alternatively, run directly from the solution root:
```bash
dotnet run --project AudioAnalyzer/AudioAnalyzer.csproj
```

## Usage

1. Once the application starts, it will begin capturing all audio playing on your system
2. The analyzer will display real-time volume levels and frequency analysis
3. Play any audio on your system to see the analysis in action
4. Press **ESC** to stop the analyzer and exit the application

## What It Does

- **Loopback Audio Capture**: Captures all audio playing on your system output
- **Volume Analysis**: Monitors and displays audio volume levels in real-time
- **FFT Analysis**: Performs Fast Fourier Transform to analyze frequency components
- **Real-time Display**: Updates analysis every 100ms

## Dependencies

- **NAudio 2.2.1**: Audio library for .NET providing WASAPI capture and DSP functionality

## Notes

- This application requires Windows with WASAPI support
- Make sure audio is playing on your system to see analysis results
- The application captures system audio output, not microphone input
