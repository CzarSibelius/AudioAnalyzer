using Microsoft.Extensions.Logging;

namespace AudioAnalyzer.Console;

internal sealed partial class DeviceSelectionModal
{
    [LoggerMessage(EventId = 7790, Level = LogLevel.Information, Message = "Device UI: calling GetDevices")]
    private partial void LogDeviceModalGetDevicesBegin();

    [LoggerMessage(EventId = 7791, Level = LogLevel.Information, Message = "Device UI: GetDevices returned ({Count} entries)")]
    private partial void LogDeviceModalGetDevicesEnd(int count);

    [LoggerMessage(EventId = 7792, Level = LogLevel.Information, Message = "Device UI: entering modal (Clear + first draw)")]
    private partial void LogDeviceModalEnteringRunModal();
}
