using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;
using Microsoft.Extensions.Logging;

namespace AudioAnalyzer.Console;

/// <summary>Manages audio capture lifecycle with correct lock ordering per ADR-0018.</summary>
internal sealed partial class DeviceCaptureController : IDeviceCaptureController
{
    private readonly IAudioDeviceInfo _deviceInfo;
    private readonly IVisualizationOrchestrator _orchestrator;
    private readonly AppSettings _appSettings;
    private readonly IBeatTimingConfigurator _beatTiming;
    private readonly ILogger<DeviceCaptureController> _logger;
    private readonly object _deviceLock = new();

    // Serializes the blocking start/stop transition so overlapping device switches (and the single
    // native Core Audio tap state) cannot run concurrently. Held on a background thread, never the UI thread.
    private readonly object _transitionLock = new();

    private IAudioInput? _currentInput;
    private string _currentDeviceName = "";
    private string? _currentDeviceId;

    public DeviceCaptureController(
        IAudioDeviceInfo deviceInfo,
        IVisualizationOrchestrator orchestrator,
        AppSettings appSettings,
        IBeatTimingConfigurator beatTiming,
        ILogger<DeviceCaptureController> logger)
    {
        _deviceInfo = deviceInfo ?? throw new ArgumentNullException(nameof(deviceInfo));
        _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
        _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
        _beatTiming = beatTiming ?? throw new ArgumentNullException(nameof(beatTiming));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public string CurrentDeviceName => _currentDeviceName;

    /// <inheritdoc />
    public string? CurrentDeviceId => _currentDeviceId;

    /// <inheritdoc />
    public void StartCapture(string? deviceId, string name)
    {
        IAudioInput? oldInput;
        IAudioInput newInput;
        lock (_deviceLock)
        {
            oldInput = _currentInput;
            newInput = _deviceInfo.CreateCapture(deviceId);
            _currentInput = newInput;
            _currentDeviceName = name;
            _currentDeviceId = deviceId;
            newInput.DataAvailable += OnCapturedAudioAvailable;
        }

        // Stopping the old input and starting the new one can block the caller for a long time:
        // Core Audio capture (system-audio tap / mic) triggers TCC consent prompts and aggregate-device
        // creation synchronously. Run that transition off the UI thread so startup and device switches
        // stay responsive (ADR-0030); the device name is already published above for the header.
        _ = Task.Run(() => RunCaptureTransition(oldInput, newInput, deviceId));
    }

    private void RunCaptureTransition(IAudioInput? oldInput, IAudioInput newInput, string? deviceId)
    {
        // Serialize transitions; never hold _deviceLock across blocking Start/Stop calls so we keep the
        // input-lock → device-lock ordering used by capture callbacks (ADR-0018).
        lock (_transitionLock)
        {
            try
            {
                oldInput?.StopCapture();
                oldInput?.Dispose();
                _beatTiming.NotifyAudioCaptureStopped();

                bool isCurrent;
                lock (_deviceLock)
                {
                    isCurrent = ReferenceEquals(_currentInput, newInput);
                    if (isCurrent)
                    {
                        _beatTiming.ApplyFromSettings(_appSettings.BpmSource, deviceId);
                    }
                }

                if (isCurrent)
                {
                    newInput.Start();
                }
            }
            catch (Exception ex)
            {
                LogCaptureTransitionFailed(ex);
            }
        }
    }

    private void OnCapturedAudioAvailable(object? sender, AudioDataAvailableEventArgs e)
    {
        lock (_deviceLock)
        {
            if (_currentInput == null || !ReferenceEquals(sender, _currentInput))
            {
                return;
            }

            _orchestrator.OnAudioData(e.Buffer, e.BytesRecorded, e.Format);
        }
    }

    /// <inheritdoc />
    public void StopCapture()
    {
        IAudioInput? input;
        lock (_deviceLock)
        {
            input = _currentInput;
        }
        input?.StopCapture();
        _beatTiming.NotifyAudioCaptureStopped();
    }

    /// <inheritdoc />
    public void ReleaseCaptureForDeviceSelection()
    {
        LogReleaseCaptureForSelectionBegin(Environment.CurrentManagedThreadId);
        IAudioInput? old;
        lock (_deviceLock)
        {
            old = _currentInput;
            _currentInput = null;
        }

        old?.StopCapture();
        old?.Dispose();
        _beatTiming.NotifyAudioCaptureStopped();
        LogReleaseCaptureForSelectionEnd();
    }

    /// <inheritdoc />
    public void RestartCapture()
    {
        IAudioInput? input;
        string? deviceId;
        lock (_deviceLock)
        {
            input = _currentInput;
            deviceId = _currentDeviceId;
        }

        if (input != null)
        {
            // Restarting an existing input can also block (Core Audio); keep the UI thread responsive.
            _ = Task.Run(() => RunCaptureTransition(null, input, deviceId));
            return;
        }

        StartCapture(_currentDeviceId, _currentDeviceName);
    }

    /// <inheritdoc />
    public void Shutdown()
    {
        IAudioInput? toDispose;
        lock (_deviceLock)
        {
            toDispose = _currentInput;
            _currentInput = null;
        }
        toDispose?.StopCapture();
        toDispose?.Dispose();
        _beatTiming.NotifyAudioCaptureStopped();
    }
}
