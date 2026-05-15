using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace AudioAnalyzer.Platform.macOS.Audio.CoreAudio;

internal static class MacOsCoreAudioDeviceQueries
{
    internal static bool TryGetAudioDevices(ILogger logger, out uint[] deviceIds)
    {
        deviceIds = Array.Empty<uint>();
        try
        {
            var addr = new AudioObjectPropertyAddress(
                MacOsCoreAudioConstants.kAudioHardwarePropertyDevices,
                MacOsCoreAudioConstants.kAudioObjectPropertyScopeGlobal,
                MacOsCoreAudioConstants.kAudioObjectPropertyElementMain);

            uint size = 0;
            int status = MacOsCoreAudioInterop.AudioObjectGetPropertyDataSize(
                MacOsCoreAudioConstants.kAudioObjectSystemObject,
                ref addr,
                0,
                IntPtr.Zero,
                ref size);

            if (status != MacOsCoreAudioConstants.noErr || size == 0 || size % sizeof(uint) != 0)
            {
                MacOsCoreAudioQueryLogs.EnumerationStepFailed(logger, "AudioObjectGetPropertyDataSize(devices)", status, null);
                return false;
            }

            IntPtr buffer = Marshal.AllocHGlobal((int)size);
            try
            {
                uint ioSize = size;
                status = MacOsCoreAudioInterop.AudioObjectGetPropertyData(
                    MacOsCoreAudioConstants.kAudioObjectSystemObject,
                    ref addr,
                    0,
                    IntPtr.Zero,
                    ref ioSize,
                    buffer);

                if (status != MacOsCoreAudioConstants.noErr)
                {
                    MacOsCoreAudioQueryLogs.EnumerationStepFailed(logger, "AudioObjectGetPropertyData(devices)", status, null);
                    return false;
                }

                int count = (int)(ioSize / sizeof(uint));
                var ids = new uint[count];
                for (int i = 0; i < count; i++)
                {
                    ids[i] = unchecked((uint)Marshal.ReadInt32(buffer, i * sizeof(uint)));
                }

                deviceIds = ids;
                return true;
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }
        catch (Exception ex)
        {
            MacOsCoreAudioQueryLogs.UnexpectedEnumerationError(logger, ex);
            return false;
        }
    }

    internal static bool TryReadCfStringProperty(
        uint deviceId,
        uint selector,
        uint scope,
        ILogger logger,
        out string value)
    {
        value = string.Empty;
        var addr = new AudioObjectPropertyAddress(selector, scope, MacOsCoreAudioConstants.kAudioObjectPropertyElementMain);
        uint size = (uint)IntPtr.Size;
        IntPtr cfPtrStorage = Marshal.AllocHGlobal(IntPtr.Size);
        try
        {
            int status = MacOsCoreAudioInterop.AudioObjectGetPropertyData(
                deviceId,
                ref addr,
                0,
                IntPtr.Zero,
                ref size,
                cfPtrStorage);

            if (status != MacOsCoreAudioConstants.noErr)
            {
                MacOsCoreAudioQueryLogs.PropertyReadFailed(logger, selector, scope, deviceId, status, null);
                return false;
            }

            IntPtr cfStr = Marshal.ReadIntPtr(cfPtrStorage);
            if (cfStr == IntPtr.Zero)
            {
                return false;
            }

            try
            {
                value = MacOsCfInterop.CfStringToUtf8(cfStr);
                return !string.IsNullOrEmpty(value);
            }
            finally
            {
                MacOsCfInterop.CFRelease(cfStr);
            }
        }
        finally
        {
            Marshal.FreeHGlobal(cfPtrStorage);
        }
    }

    internal static bool TryReadStreamBasicDescription(
        uint deviceId,
        ILogger logger,
        out AudioStreamBasicDescription asbd)
    {
        asbd = default;
        var addr = new AudioObjectPropertyAddress(
            MacOsCoreAudioConstants.kAudioDevicePropertyStreamFormat,
            MacOsCoreAudioConstants.kAudioDevicePropertyScopeInput,
            MacOsCoreAudioConstants.kAudioObjectPropertyElementMain);

        uint structSize = (uint)Marshal.SizeOf<AudioStreamBasicDescription>();
        IntPtr buffer = Marshal.AllocHGlobal((int)structSize);
        try
        {
            uint ioSize = structSize;
            int status = MacOsCoreAudioInterop.AudioObjectGetPropertyData(
                deviceId,
                ref addr,
                0,
                IntPtr.Zero,
                ref ioSize,
                buffer);

            if (status != MacOsCoreAudioConstants.noErr)
            {
                MacOsCoreAudioQueryLogs.InputFormatUnavailable(logger, deviceId, status, null);
                return false;
            }

            asbd = Marshal.PtrToStructure<AudioStreamBasicDescription>(buffer);
            return asbd.mChannelsPerFrame > 0 && asbd.mSampleRate > 0 && asbd.mBytesPerFrame > 0;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    /// <summary>
    /// Copies the device UID as a CFString (+1 retained) for <see cref="MacOsCoreAudioInterop.AudioQueueSetProperty"/>.
    /// Caller must <see cref="MacOsCfInterop.CFRelease"/> the returned pointer.
    /// </summary>
    internal static bool TryCopyDeviceUidCfString(uint deviceId, uint uidPropertyScope, out IntPtr cfStringRef)
    {
        cfStringRef = IntPtr.Zero;
        var addr = new AudioObjectPropertyAddress(
            MacOsCoreAudioConstants.kAudioDevicePropertyDeviceUID,
            uidPropertyScope,
            MacOsCoreAudioConstants.kAudioObjectPropertyElementMain);

        IntPtr storage = Marshal.AllocHGlobal(IntPtr.Size);
        try
        {
            uint ioSize = (uint)IntPtr.Size;
            int status = MacOsCoreAudioInterop.AudioObjectGetPropertyData(
                deviceId,
                ref addr,
                0,
                IntPtr.Zero,
                ref ioSize,
                storage);

            if (status != MacOsCoreAudioConstants.noErr)
            {
                return false;
            }

            cfStringRef = Marshal.ReadIntPtr(storage);
            return cfStringRef != IntPtr.Zero;
        }
        finally
        {
            Marshal.FreeHGlobal(storage);
        }
    }

    internal static bool TryFindDeviceIdByUid(string uid, ILogger logger, out uint deviceId)
    {
        deviceId = 0;
        if (!TryGetAudioDevices(logger, out uint[] ids) || ids.Length == 0)
        {
            return false;
        }

        foreach (uint id in ids)
        {
            if (TryReadCfStringProperty(
                    id,
                    MacOsCoreAudioConstants.kAudioDevicePropertyDeviceUID,
                    MacOsCoreAudioConstants.kAudioObjectPropertyScopeGlobal,
                    logger,
                    out string candidate) &&
                string.Equals(candidate, uid, StringComparison.Ordinal))
            {
                deviceId = id;
                return true;
            }
        }

        return false;
    }

    internal static bool TryGetDefaultInputDeviceId(ILogger logger, out uint deviceId)
    {
        deviceId = 0;
        var addr = new AudioObjectPropertyAddress(
            MacOsCoreAudioConstants.kAudioHardwarePropertyDefaultInputDevice,
            MacOsCoreAudioConstants.kAudioObjectPropertyScopeGlobal,
            MacOsCoreAudioConstants.kAudioObjectPropertyElementMain);

        IntPtr ptr = Marshal.AllocHGlobal(sizeof(uint));
        try
        {
            uint ioSize = sizeof(uint);
            int status = MacOsCoreAudioInterop.AudioObjectGetPropertyData(
                MacOsCoreAudioConstants.kAudioObjectSystemObject,
                ref addr,
                0,
                IntPtr.Zero,
                ref ioSize,
                ptr);

            if (status != MacOsCoreAudioConstants.noErr)
            {
                MacOsCoreAudioQueryLogs.DefaultInputReadFailed(logger, status, null);
                return false;
            }

            deviceId = unchecked((uint)Marshal.ReadInt32(ptr));
            return deviceId != 0;
        }
        finally
        {
            Marshal.FreeHGlobal(ptr);
        }
    }
}
