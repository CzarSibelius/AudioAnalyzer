using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;
using KeyHandling = AudioAnalyzer.Console.KeyHandling;

namespace AudioAnalyzer.Console;

/// <summary>Config for device selection modal keys: up/down to select, Enter to confirm, Escape to cancel.</summary>
internal sealed class DeviceSelectionKeyHandlerConfig : IKeyHandlerConfig<DeviceSelectionKeyContext>
{
    private const string Section = "Device selection";

    private static IReadOnlyList<KeyHandling.KeyBindingEntry<DeviceSelectionKeyContext>> GetEntries()
    {
        return
        [
            new KeyHandling.KeyBindingEntry<DeviceSelectionKeyContext>(
                Matches: k => k.Key == ConsoleKey.UpArrow || k.Key == ConsoleKey.DownArrow,
                Action: (key, context) =>
                {
                    var devices = context.Devices;
                    if (key.Key == ConsoleKey.UpArrow)
                    {
                        context.SelectedIndex = (context.SelectedIndex - 1 + devices.Count) % devices.Count;
                    }
                    else
                    {
                        context.SelectedIndex = (context.SelectedIndex + 1) % devices.Count;
                    }
                    return false;
                },
                Key: "↑/↓",
                Description: "Navigate devices",
                Section),
            new KeyHandling.KeyBindingEntry<DeviceSelectionKeyContext>(
                Matches: k => k.Key == ConsoleKey.Enter,
                Action: (_, context) =>
                {
                    var devices = context.Devices;
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
                },
                Key: "Enter",
                Description: "Select device",
                Section),
            new KeyHandling.KeyBindingEntry<DeviceSelectionKeyContext>(
                Matches: k => k.Key == ConsoleKey.Escape,
                Action: (_, _) => true,
                Key: "Escape",
                Description: "Cancel and return",
                Section),
        ];
    }

    private static readonly Lazy<IReadOnlyList<KeyHandling.KeyBindingEntry<DeviceSelectionKeyContext>>> s_entries =
        new(GetEntries);

    /// <inheritdoc />
    public IReadOnlyList<KeyBinding> GetBindings() =>
        s_entries.Value.Select(e => e.ToKeyBinding()).ToList();

    /// <inheritdoc />
    public bool Handle(ConsoleKeyInfo key, DeviceSelectionKeyContext context)
    {
        foreach (var entry in s_entries.Value)
        {
            if (entry.Matches(key))
            {
                return entry.Action(key, context);
            }
        }
        return false;
    }
}
