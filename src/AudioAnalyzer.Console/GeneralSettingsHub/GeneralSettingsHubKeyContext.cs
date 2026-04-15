using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Console;

/// <summary>Context for <see cref="GeneralSettingsHubKeyHandlerConfig"/>.</summary>
internal sealed class GeneralSettingsHubKeyContext : IKeyHandlerContext
{
    /// <summary>Sets whether a modal is currently open (affects render guard).</summary>
    public required Action<bool> SetModalOpen { get; init; }

    /// <summary>Persists app and visualizer settings.</summary>
    public required Action SaveSettings { get; init; }

    /// <summary>Current device display name.</summary>
    public required Func<string> GetDeviceName { get; init; }

    /// <summary>Stops current capture before device switch.</summary>
    public required Action StopCapture { get; init; }

    /// <summary>Starts or restarts capture with the given device.</summary>
    public required Action<string?, string> StartCapture { get; init; }

    /// <summary>Restarts the current capture without changing device.</summary>
    public required Action RestartCapture { get; init; }

    /// <summary>Device selection modal.</summary>
    public required IDeviceSelectionModal DeviceSelectionModal { get; init; }

    /// <summary>UI theme palette selection modal.</summary>
    public required IUiThemeSelectionModal UiThemeSelectionModal { get; init; }

    /// <summary>Current audio analysis for theme list coloring.</summary>
    public required Func<AudioAnalysisSnapshot> GetAudioAnalysisSnapshot { get; init; }

    /// <summary>UI settings (hub-edited fields include title bar name and default asset folder).</summary>
    public required UiSettings UiSettings { get; init; }

    /// <summary>Hub state.</summary>
    public required GeneralSettingsHubState State { get; init; }

    /// <summary>Display state (fullscreen).</summary>
    public required IDisplayState DisplayState { get; init; }

    /// <summary>Orchestrator for redraw.</summary>
    public required IVisualizationOrchestrator Orchestrator { get; init; }

    /// <summary>Persisted app settings (BPM source, etc.).</summary>
    public required AppSettings AppSettings { get; init; }

    /// <summary>Re-applies beat timing after <see cref="AppSettings.BpmSource"/> changes.</summary>
    public required Action ApplyBeatTimingFromSettings { get; init; }

    /// <summary>Resizes the waveform history ring when <see cref="AppSettings.MaxAudioHistorySeconds"/> changes.</summary>
    public required IWaveformHistoryConfigurator WaveformHistoryConfigurator { get; init; }
}
