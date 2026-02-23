using AudioAnalyzer.Application;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Console;

/// <summary>Context passed to main loop key handlers. Provides shared state and operations.</summary>
internal sealed class MainLoopKeyContext
{
    /// <summary>Orchestrator for display state and redraw (fullscreen, overlay, Redraw, RedrawWithFullHeader).</summary>
    public required IVisualizationOrchestrator Orchestrator { get; init; }
    /// <summary>When set to true, the main loop will exit.</summary>
    public bool ShouldQuit { get; set; }

    /// <summary>Sets whether a modal is currently open (affects render guard).</summary>
    public required Action<bool> SetModalOpen { get; init; }

    /// <summary>Lock for console output synchronization.</summary>
    public required object ConsoleLock { get; init; }

    /// <summary>Refreshes header and triggers engine redraw. Use for full redraw (preset/device change).</summary>
    public required Action RefreshHeaderAndRedraw { get; init; }

    /// <summary>Persists app and visualizer settings.</summary>
    public required Action SaveSettings { get; init; }

    /// <summary>Persists visualizer settings only.</summary>
    public required Action SaveVisualizerSettings { get; init; }

    /// <summary>Current device display name for header.</summary>
    public required Func<string> GetDeviceName { get; init; }

    /// <summary>Analysis engine for layout and beat sensitivity.</summary>
    public required AnalysisEngine Engine { get; init; }

    /// <summary>Header drawer for non-fullscreen header updates.</summary>
    public required IHeaderDrawer HeaderDrawer { get; init; }

    /// <summary>Switches between Preset editor and Show play modes.</summary>
    public required Action OnModeSwitch { get; init; }

    /// <summary>Cycles to the next preset.</summary>
    public required Action OnPresetCycle { get; init; }

    /// <summary>Settings modal for Preset editor (layer/preset editing).</summary>
    public required ISettingsModal SettingsModal { get; init; }

    /// <summary>Show edit modal for Show play mode.</summary>
    public required IShowEditModal ShowEditModal { get; init; }

    /// <summary>Stops current capture before device switch.</summary>
    public required Action StopCapture { get; init; }

    /// <summary>Starts or restarts capture with the given device.</summary>
    public required Action<string?, string> StartCapture { get; init; }

    /// <summary>Restarts the current capture without changing device.</summary>
    public required Action RestartCapture { get; init; }

    /// <summary>Device selection modal for switching audio input.</summary>
    public required IDeviceSelectionModal DeviceSelectionModal { get; init; }

    /// <summary>Help modal for keyboard controls.</summary>
    public required IHelpModal HelpModal { get; init; }

    /// <summary>Current application mode (Preset editor vs Show play).</summary>
    public required Func<ApplicationMode> GetApplicationMode { get; init; }

    /// <summary>Cycles to the next palette.</summary>
    public required Action OnPaletteCycle { get; init; }
}
