using AudioAnalyzer.Application;
using AudioAnalyzer.Application.Abstractions;

namespace AudioAnalyzer.Console;

/// <summary>Manages audio capture lifecycle with correct lock ordering per ADR-0018.</summary>
internal sealed class DeviceCaptureController : IDeviceCaptureController
{
    private readonly IAudioDeviceInfo _deviceInfo;
    private readonly AnalysisEngine _engine;
    private readonly object _deviceLock = new();

    private IAudioInput? _currentInput;
    private string _currentDeviceName = "";

    public DeviceCaptureController(IAudioDeviceInfo deviceInfo, AnalysisEngine engine)
    {
        _deviceInfo = deviceInfo ?? throw new ArgumentNullException(nameof(deviceInfo));
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
    }

    /// <inheritdoc />
    public string CurrentDeviceName => _currentDeviceName;

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
            _currentInput.DataAvailable += (_, e) =>
            {
                lock (_deviceLock)
                {
                    if (_currentInput == null)
                    {
                        return;
                    }

                    _engine.ProcessAudio(e.Buffer, e.BytesRecorded, e.Format);
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
