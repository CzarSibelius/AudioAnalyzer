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
        lock (_deviceLock)
        {
            oldInput = _currentInput;
            _currentInput = null;
        }
        oldInput?.StopCapture();
        oldInput?.Dispose();
        _beatTiming.NotifyAudioCaptureStopped();

        IAudioInput? startedInput;
        lock (_deviceLock)
        {
            _currentInput = _deviceInfo.CreateCapture(deviceId);
            _currentDeviceName = name;
            _currentDeviceId = deviceId;
            _beatTiming.ApplyFromSettings(_appSettings.BpmSource, deviceId);
            _currentInput.DataAvailable += OnCapturedAudioAvailable;
            startedInput = _currentInput;
        }

        // Avoid acquisition order input-lock → device-lock vs device-lock → input-lock (e.g. macOS Audio Queue callback).
        startedInput?.Start();
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
        lock (_deviceLock)
        {
            input = _currentInput;
        }

        if (input != null)
        {
            input.Start();
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
