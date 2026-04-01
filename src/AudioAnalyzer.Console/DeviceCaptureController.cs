using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Console;

/// <summary>Manages audio capture lifecycle with correct lock ordering per ADR-0018.</summary>
internal sealed class DeviceCaptureController : IDeviceCaptureController
{
    private readonly IAudioDeviceInfo _deviceInfo;
    private readonly IVisualizationOrchestrator _orchestrator;
    private readonly AppSettings _appSettings;
    private readonly IBeatTimingConfigurator _beatTiming;
    private readonly object _deviceLock = new();

    private IAudioInput? _currentInput;
    private string _currentDeviceName = "";
    private string? _currentDeviceId;

    public DeviceCaptureController(
        IAudioDeviceInfo deviceInfo,
        IVisualizationOrchestrator orchestrator,
        AppSettings appSettings,
        IBeatTimingConfigurator beatTiming)
    {
        _deviceInfo = deviceInfo ?? throw new ArgumentNullException(nameof(deviceInfo));
        _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
        _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
        _beatTiming = beatTiming ?? throw new ArgumentNullException(nameof(beatTiming));
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

        lock (_deviceLock)
        {
            _currentInput = _deviceInfo.CreateCapture(deviceId);
            _currentDeviceName = name;
            _currentDeviceId = deviceId;
            _beatTiming.ApplyFromSettings(_appSettings.BpmSource, deviceId);
            _currentInput.DataAvailable += (_, e) =>
            {
                lock (_deviceLock)
                {
                    if (_currentInput == null)
                    {
                        return;
                    }

                    _orchestrator.OnAudioData(e.Buffer, e.BytesRecorded, e.Format);
                }
            };
            _currentInput.Start();
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
    }

    /// <inheritdoc />
    public void RestartCapture()
    {
        lock (_deviceLock)
        {
            _currentInput?.Start();
        }
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
    }
}
