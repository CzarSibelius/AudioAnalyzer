using AudioAnalyzer.Application;
using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Console;

/// <summary>Show edit overlay modal. Edit Show entries: add/remove/reorder presets, set duration. Per ADR-0031.</summary>
internal static class ShowEditModal
{
    private const int OverlayRowCount = 16;
    private const int LeftColWidth = 32;

    /// <summary>Shows the Show edit overlay modal. Blocks until user closes with ESC.</summary>
    /// <param name="uiSettings">Optional UI settings for palette colors per ADR-0033.</param>
    public static void Show(
        AnalysisEngine analysisEngine,
        VisualizerSettings visualizerSettings,
        IShowRepository showRepo,
        IPresetRepository presetRepo,
        object consoleLock,
        Action saveVisualizerSettings,
        UiSettings? uiSettings = null)
    {
        var allShows = showRepo.GetAll();
        var allPresets = presetRepo.GetAll();

        var currentShowId = visualizerSettings.ActiveShowId;
        if (string.IsNullOrWhiteSpace(currentShowId) && allShows.Count > 0)
        {
            currentShowId = allShows[0].Id;
            visualizerSettings.ActiveShowId = currentShowId;
            var show = showRepo.GetById(currentShowId);
            visualizerSettings.ActiveShowName = show?.Name?.Trim();
        }

        bool renaming = false;
        string renameBuffer = "";
        int selectedIndex = 0;
        bool editingDuration = false;
        string durationBuffer = "";
        int width = ConsoleHeader.GetConsoleWidth();
        int rightColWidth = Math.Max(10, width - LeftColWidth - 1);

        var palette = (uiSettings ?? new UiSettings()).Palette ?? new UiPalette();
        var selBg = palette.Background ?? PaletteColor.FromConsoleColor(ConsoleColor.DarkBlue);
        var selFg = palette.Highlighted;

        void DrawContent()
        {
            if (width < 40)
            {
                return;
            }

            try
            {
                var show = string.IsNullOrWhiteSpace(currentShowId) ? null : showRepo.GetById(currentShowId);
                var showName = show?.Name?.Trim() ?? "Show 1";
                var title = renaming
                    ? $" New show name (Enter confirm, Esc cancel): {renameBuffer}_ "
                    : $" Show: {showName} (R rename, N new) ";
                var titleTruncated = StaticTextViewport.TruncateWithEllipsis(new PlainText(title), width - 2);
                int pad = Math.Max(0, (width - titleTruncated.Length - 2) / 2);
                System.Console.SetCursorPosition(0, 0);
                System.Console.Write(StaticTextViewport.TruncateToWidth(new PlainText("╔" + new string('═', width - 2) + "╗"), width).PadRight(width));
                System.Console.SetCursorPosition(0, 1);
                System.Console.Write(StaticTextViewport.TruncateToWidth(new PlainText("║" + new string(' ', pad) + titleTruncated + new string(' ', width - pad - titleTruncated.Length - 2) + "║"), width).PadRight(width));
                System.Console.SetCursorPosition(0, 2);
                System.Console.Write(StaticTextViewport.TruncateToWidth(new PlainText("╚" + new string('═', width - 2) + "╝"), width).PadRight(width));

                string hint = renaming
                    ? "  Type new name, Enter save, Esc cancel"
                    : editingDuration
                        ? "  Type duration value, Enter confirm, Esc cancel"
                        : "  \u2191\u2193 select, A add D delete, P preset, Enter duration, U unit, Esc close";
                System.Console.SetCursorPosition(0, 3);
                System.Console.Write(StaticTextViewport.TruncateWithEllipsis(new PlainText(hint), width).PadRight(width));
                System.Console.SetCursorPosition(0, 4);
                System.Console.Write(StaticTextViewport.TruncateToWidth(new PlainText("  ─" + new string('─', LeftColWidth - 2) + "┬" + new string('─', rightColWidth) + "─"), width).PadRight(width));

                var entries = show?.Entries ?? new List<ShowEntry>();
                for (int i = 0; i < Math.Min(10, entries.Count + 2); i++)
                {
                    int row = 5 + i;
                    if (row >= OverlayRowCount)
                    {
                        break;
                    }

                    System.Console.SetCursorPosition(0, row);
                    if (i < entries.Count)
                    {
                        var entry = entries[i];
                        var preset = presetRepo.GetById(entry.PresetId);
                        var presetName = preset?.Name?.Trim() ?? entry.PresetId;
                        var dur = entry.Duration ?? new DurationConfig();
                        var durStr = dur.Unit == DurationUnit.Beats ? $"{dur.Value:F0} beats" : $"{dur.Value:F0}s";
                        string prefix = i == selectedIndex ? " ► " : "   ";
                        string line = $"{prefix}{i + 1}. {StaticTextViewport.TruncateWithEllipsis(new PlainText(presetName), LeftColWidth - 10)}  {durStr}";
                        line = StaticTextViewport.TruncateWithEllipsis(new PlainText(line), LeftColWidth).PadRight(LeftColWidth);
                        string lineToWrite = i == selectedIndex
                            ? AnsiConsole.BackgroundCode(selBg) + AnsiConsole.ColorCode(selFg) + line + AnsiConsole.ResetCode
                            : line;
                        System.Console.Write(lineToWrite);
                        System.Console.Write(" ");
                        if (i == selectedIndex && !editingDuration)
                        {
                            var cfg = entry.Duration ?? new DurationConfig();
                            string rightLine = $"Unit: {cfg.Unit}  Value: {cfg.Value} (Enter to edit)";
                            System.Console.Write(StaticTextViewport.TruncateWithEllipsis(new PlainText(rightLine), rightColWidth).PadRight(rightColWidth));
                        }
                        else if (i == selectedIndex && editingDuration)
                        {
                            string rightLine = $"Value: {durationBuffer}_";
                            System.Console.Write(StaticTextViewport.TruncateWithEllipsis(new PlainText(rightLine), rightColWidth).PadRight(rightColWidth));
                        }
                        else
                        {
                            System.Console.Write(new string(' ', rightColWidth));
                        }
                    }
                    else
                    {
                        System.Console.Write(new string(' ', LeftColWidth + 1 + rightColWidth));
                    }
                }
            }
            catch (Exception ex) { _ = ex; /* Draw Show edit modal failed */ }
        }

        bool HandleKey(ConsoleKeyInfo key)
        {
            if (renaming)
            {
                if (key.Key == ConsoleKey.Escape) { renaming = false; return false; }
                if (key.Key == ConsoleKey.Enter && !string.IsNullOrWhiteSpace(renameBuffer))
                {
                    var showToRename = showRepo.GetById(currentShowId ?? "");
                    if (showToRename != null)
                    {
                        showToRename.Name = renameBuffer.Trim();
                        showRepo.Save(currentShowId!, showToRename);
                        visualizerSettings.ActiveShowName = showToRename.Name;
                    }
                    renaming = false;
                    saveVisualizerSettings();
                    return false;
                }
                if (key.Key == ConsoleKey.Backspace && renameBuffer.Length > 0) { renameBuffer = renameBuffer[..^1]; return false; }
                if (key.KeyChar is >= ' ' and <= '~') { renameBuffer += key.KeyChar; return false; }
                return false;
            }

            if (editingDuration)
            {
                if (key.Key == ConsoleKey.Escape) { editingDuration = false; return false; }
                if (key.Key == ConsoleKey.Enter && !string.IsNullOrWhiteSpace(durationBuffer) && double.TryParse(durationBuffer.Trim(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var val) && val > 0)
                {
                    var show = showRepo.GetById(currentShowId!);
                    if (show?.Entries is { Count: > 0 } && selectedIndex < show.Entries.Count)
                    {
                        var entry = show.Entries[selectedIndex];
                        entry.Duration ??= new DurationConfig();
                        entry.Duration.Value = val;
                        showRepo.Save(currentShowId!, show);
                    }
                    editingDuration = false;
                    saveVisualizerSettings();
                    return false;
                }
                if (key.Key == ConsoleKey.Backspace && durationBuffer.Length > 0) { durationBuffer = durationBuffer[..^1]; return false; }
                if (key.KeyChar is >= ' ' and <= '~' && (char.IsDigit(key.KeyChar) || key.KeyChar == '.'))
                {
                    durationBuffer += key.KeyChar;
                    return false;
                }
                return false;
            }

            var showRef = showRepo.GetById(currentShowId ?? "");
            var entries = showRef?.Entries ?? new List<ShowEntry>();

            if (key.Key == ConsoleKey.Escape)
            {
                return true;
            }

            if (key.Key == ConsoleKey.R && !renaming)
            {
                var showToRename = showRepo.GetById(currentShowId ?? "");
                if (showToRename != null)
                {
                    renameBuffer = showToRename.Name?.Trim() ?? "";
                    renaming = true;
                }
                return false;
            }

            if (key.Key == ConsoleKey.N && !renaming)
            {
                var newShow = new Show { Name = $"Show {allShows.Count + 1}", Entries = [] };
                var createdId = showRepo.Create(newShow);
                currentShowId = createdId;
                visualizerSettings.ActiveShowId = createdId;
                visualizerSettings.ActiveShowName = newShow.Name;
                saveVisualizerSettings();
                return false;
            }

            if (key.Key == ConsoleKey.A && showRef != null && allPresets.Count > 0)
            {
                var firstPresetId = allPresets[0].Id;
                showRef.Entries.Add(new ShowEntry
                {
                    PresetId = firstPresetId,
                    Duration = new DurationConfig { Unit = DurationUnit.Seconds, Value = 30 }
                });
                showRepo.Save(currentShowId!, showRef);
                selectedIndex = showRef.Entries.Count - 1;
                saveVisualizerSettings();
                return false;
            }

            if (key.Key == ConsoleKey.D && showRef != null && entries.Count > 0 && selectedIndex < entries.Count)
            {
                showRef.Entries.RemoveAt(selectedIndex);
                if (selectedIndex >= showRef.Entries.Count && showRef.Entries.Count > 0)
                {
                    selectedIndex = showRef.Entries.Count - 1;
                }
                showRepo.Save(currentShowId!, showRef);
                saveVisualizerSettings();
                return false;
            }

            if (key.Key == ConsoleKey.P && showRef != null && entries.Count > 0 && selectedIndex < entries.Count && allPresets.Count > 0)
            {
                var entry = entries[selectedIndex];
                int idx = 0;
                for (int i = 0; i < allPresets.Count; i++)
                {
                    if (string.Equals(allPresets[i].Id, entry.PresetId, StringComparison.OrdinalIgnoreCase))
                    {
                        idx = (i + 1) % allPresets.Count;
                        break;
                    }
                }
                entry.PresetId = allPresets[idx].Id;
                showRepo.Save(currentShowId!, showRef);
                saveVisualizerSettings();
                return false;
            }

            if (key.Key == ConsoleKey.U && showRef != null && entries.Count > 0 && selectedIndex < entries.Count)
            {
                var entry = entries[selectedIndex];
                entry.Duration ??= new DurationConfig();
                entry.Duration.Unit = entry.Duration.Unit == DurationUnit.Seconds ? DurationUnit.Beats : DurationUnit.Seconds;
                showRepo.Save(currentShowId!, showRef);
                saveVisualizerSettings();
                return false;
            }

            if (key.Key == ConsoleKey.Enter && showRef != null && entries.Count > 0 && selectedIndex < entries.Count)
            {
                var entry = entries[selectedIndex];
                durationBuffer = (entry.Duration?.Value ?? 30).ToString(System.Globalization.CultureInfo.InvariantCulture);
                editingDuration = true;
                return false;
            }

            if (key.Key == ConsoleKey.UpArrow && selectedIndex > 0)
            {
                selectedIndex--;
                return false;
            }
            if (key.Key == ConsoleKey.DownArrow && selectedIndex < entries.Count - 1)
            {
                selectedIndex++;
                return false;
            }

            return false;
        }

        ModalSystem.RunOverlayModal(
            OverlayRowCount,
            DrawContent,
            HandleKey,
            consoleLock,
            onClose: () => analysisEngine.SetOverlayActive(false),
            onEnter: () => analysisEngine.SetOverlayActive(true, OverlayRowCount));
    }
}
