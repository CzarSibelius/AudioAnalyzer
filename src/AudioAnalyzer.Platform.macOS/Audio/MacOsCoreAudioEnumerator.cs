using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Platform.macOS.Audio.CoreAudio;
using Microsoft.Extensions.Logging;

namespace AudioAnalyzer.Platform.macOS.Audio;

/// <summary>Lists Core Audio input endpoints and builds <see cref="MacOsCoreAudioAudioInput"/> instances.</summary>
public sealed partial class MacOsCoreAudioEnumerator : IMacOsAudioEnumerator
{
    private readonly ILogger<MacOsCoreAudioEnumerator> _logger;
    private readonly ILoggerFactory _loggerFactory;

    /// <summary>Initializes a new instance of the <see cref="MacOsCoreAudioEnumerator"/> class.</summary>
    public MacOsCoreAudioEnumerator(ILogger<MacOsCoreAudioEnumerator> logger, ILoggerFactory loggerFactory)
    {
        _logger = logger;
        _loggerFactory = loggerFactory;
    }

    /// <inheritdoc />
    public IReadOnlyList<MacOsPhysicalAudioDevice> GetPhysicalInputs()
    {
        if (!OperatingSystem.IsMacOS())
        {
            return Array.Empty<MacOsPhysicalAudioDevice>();
        }

        LogEnumeratePhysicalInputsBegin();

        string? defaultUid = null;
        if (MacOsCoreAudioDeviceQueries.TryGetDefaultInputDeviceId(_logger, out uint defaultId))
        {
            MacOsCoreAudioDeviceQueries.TryReadCfStringProperty(
                defaultId,
                MacOsCoreAudioConstants.kAudioDevicePropertyDeviceUID,
                MacOsCoreAudioConstants.kAudioObjectPropertyScopeGlobal,
                _logger,
                out defaultUid);
        }

        if (!MacOsCoreAudioDeviceQueries.TryGetAudioDevices(_logger, out uint[] ids))
        {
            LogEnumeratePhysicalInputsEnd(0);
            return Array.Empty<MacOsPhysicalAudioDevice>();
        }

        var list = new List<MacOsPhysicalAudioDevice>();
        foreach (uint id in ids)
        {
            if (!MacOsCoreAudioDeviceQueries.TryReadStreamBasicDescription(id, _logger, out _))
            {
                continue;
            }

            if (!MacOsCoreAudioDeviceQueries.TryReadCfStringProperty(
                    id,
                    MacOsCoreAudioConstants.kAudioDevicePropertyDeviceUID,
                    MacOsCoreAudioConstants.kAudioObjectPropertyScopeGlobal,
                    _logger,
                    out string uid))
            {
                continue;
            }

            if (!MacOsCoreAudioDeviceQueries.TryReadCfStringProperty(
                    id,
                    MacOsCoreAudioConstants.kAudioDevicePropertyDeviceNameCFString,
                    MacOsCoreAudioConstants.kAudioObjectPropertyScopeGlobal,
                    _logger,
                    out string name))
            {
                name = uid;
            }

            string label = "🎤 " + name;
            if (defaultUid != null && string.Equals(uid, defaultUid, StringComparison.Ordinal))
            {
                label += " (Default)";
            }

            list.Add(new MacOsPhysicalAudioDevice(label, uid, name));
        }

        LogEnumeratePhysicalInputsEnd(list.Count);
        return list;
    }

    /// <inheritdoc />
    public bool TryCreateCapture(string deviceId, out IAudioInput? input)
    {
        input = null;
        if (!OperatingSystem.IsMacOS())
        {
            return false;
        }

        if (!MacOsAudioDeviceIds.TryDecodeInputUid(deviceId, out string? uid))
        {
            return false;
        }

        if (!MacOsCoreAudioDeviceQueries.TryFindDeviceIdByUid(uid, _logger, out _))
        {
            LogUnknownUid(uid);
            return false;
        }

        try
        {
            input = new MacOsCoreAudioAudioInput(uid, _loggerFactory.CreateLogger<MacOsCoreAudioAudioInput>());
            return true;
        }
        catch (Exception ex)
        {
            LogConstructFailed(ex, uid);
            return false;
        }
    }
}
