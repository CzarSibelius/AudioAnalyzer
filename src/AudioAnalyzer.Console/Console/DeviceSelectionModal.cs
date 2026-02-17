using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Console;

/// <summary>Device selection modal per ADR-0006. Uses RunModal with draw + handleKey, returns selection.</summary>
internal static class DeviceSelectionModal
{
    /// <summary>
    /// Shows the device selection menu. Returns (deviceId, name) on selection, or (null, "") on cancel.
    /// </summary>
    /// <param name="setModalOpen">Called with true when modal opens and false when it closes.</param>
    public static (string? deviceId, string name) Show(
        IAudioDeviceInfo deviceInfo,
        ISettingsRepository settingsRepo,
        AppSettings settings,
        string? currentDeviceName,
        Action<bool> setModalOpen)
    {
        var devices = deviceInfo.GetDevices();
        if (devices.Count == 0)
        {
            System.Console.WriteLine("No audio devices found!");
            return (null, "");
        }

        int selectedIndex = 0;
        if (!string.IsNullOrEmpty(currentDeviceName))
        {
            for (int i = 0; i < devices.Count; i++)
            {
                if (devices[i].Name == currentDeviceName) { selectedIndex = i; break; }
            }
        }
        else if (settings.InputMode == "loopback")
        {
            selectedIndex = 0;
        }
        else if (!string.IsNullOrEmpty(settings.DeviceName))
        {
            for (int i = 0; i < devices.Count; i++)
            {
                if (devices[i].Name.Contains(settings.DeviceName, StringComparison.OrdinalIgnoreCase))
                {
                    selectedIndex = i;
                    break;
                }
            }
        }

        string? resultId = null;
        string resultName = "";

        void DrawDeviceContent()
        {
            int width = ConsoleHeader.GetConsoleWidth();
            string title = " SELECT AUDIO INPUT ";
            int pad = Math.Max(0, (width - title.Length - 2) / 2);
            System.Console.WriteLine("╔" + new string('═', width - 2) + "╗");
            System.Console.WriteLine("║" + new string(' ', pad) + title + new string(' ', width - pad - title.Length - 2) + "║");
            System.Console.WriteLine("╚" + new string('═', width - 2) + "╝");
            System.Console.WriteLine();
            System.Console.WriteLine("  Use ↑/↓ to select, ENTER to confirm, ESC to cancel");
            System.Console.WriteLine();

            for (int i = 0; i < devices.Count; i++)
            {
                bool isCurrent = currentDeviceName != null && devices[i].Name == currentDeviceName;
                if (i == selectedIndex)
                {
                    System.Console.BackgroundColor = ConsoleColor.White;
                    System.Console.ForegroundColor = ConsoleColor.Black;
                }
                else if (isCurrent)
                {
                    System.Console.ForegroundColor = ConsoleColor.Cyan;
                }

                string prefix = i == selectedIndex ? " ► " : "   ";
                string suffix = isCurrent ? " (current)" : "";
                string line = $"{prefix}{devices[i].Name}{suffix}";
                if (line.Length < width - 1)
                {
                    line = line.PadRight(width - 1);
                }
                else
                {
                    line = line[..(width - 1)];
                }

                System.Console.WriteLine(line);
                System.Console.ResetColor();
            }
            System.Console.WriteLine(new string(' ', width - 1));
        }

        bool HandleDeviceKey(ConsoleKeyInfo key)
        {
            switch (key.Key)
            {
                case ConsoleKey.UpArrow:
                    selectedIndex = (selectedIndex - 1 + devices.Count) % devices.Count;
                    return false;
                case ConsoleKey.DownArrow:
                    selectedIndex = (selectedIndex + 1) % devices.Count;
                    return false;
                case ConsoleKey.Enter:
                    var selected = devices[selectedIndex];
                    settings.InputMode = selected.Id == null ? "loopback" : "device";
                    if (selected.Id != null)
                    {
                        if (selected.Id.StartsWith("capture:", StringComparison.Ordinal))
                        {
                            settings.DeviceName = selected.Id.Substring(8);
                        }
                        else if (selected.Id.StartsWith("loopback:", StringComparison.Ordinal))
                        {
                            settings.DeviceName = selected.Id.Substring(9);
                        }
                        else
                        {
                            settings.DeviceName = selected.Id;
                        }
                    }
                    else
                    {
                        settings.DeviceName = null;
                    }
                    settingsRepo.SaveAppSettings(settings);
                    resultId = selected.Id;
                    resultName = selected.Name;
                    return true;
                case ConsoleKey.Escape:
                    return true;
                default:
                    return false;
            }
        }

        ModalSystem.RunModal(DrawDeviceContent, HandleDeviceKey, onClose: () => setModalOpen(false), onEnter: () => setModalOpen(true));
        return (resultId, resultName);
    }
}
