using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Console;

/// <summary>Handles key input for the device selection modal: up/down to select, Enter to confirm, Escape to cancel.</summary>
internal sealed class DeviceSelectionKeyHandler : IKeyHandler<DeviceSelectionKeyContext>
{
    /// <inheritdoc />
    public bool Handle(ConsoleKeyInfo key, DeviceSelectionKeyContext context)
    {
        var devices = context.Devices;
        switch (key.Key)
        {
            case ConsoleKey.UpArrow:
                context.SelectedIndex = (context.SelectedIndex - 1 + devices.Count) % devices.Count;
                return false;
            case ConsoleKey.DownArrow:
                context.SelectedIndex = (context.SelectedIndex + 1) % devices.Count;
                return false;
            case ConsoleKey.Enter:
                var selected = devices[context.SelectedIndex];
                context.Settings.InputMode = selected.Id == null ? "loopback" : "device";
                if (selected.Id != null)
                {
                    if (selected.Id.StartsWith("capture:", StringComparison.Ordinal))
                    {
                        context.Settings.DeviceName = selected.Id.Substring(8);
                    }
                    else if (selected.Id.StartsWith("loopback:", StringComparison.Ordinal))
                    {
                        context.Settings.DeviceName = selected.Id.Substring(9);
                    }
                    else
                    {
                        context.Settings.DeviceName = selected.Id;
                    }
                }
                else
                {
                    context.Settings.DeviceName = null;
                }
                context.SettingsRepo.SaveAppSettings(context.Settings);
                context.ResultId = selected.Id;
                context.ResultName = selected.Name;
                return true;
            case ConsoleKey.Escape:
                return true;
            default:
                return false;
        }
    }
}
