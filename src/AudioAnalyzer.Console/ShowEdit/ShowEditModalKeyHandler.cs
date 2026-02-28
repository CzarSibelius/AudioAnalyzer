using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;
using KeyHandling = AudioAnalyzer.Console.KeyHandling;

namespace AudioAnalyzer.Console;

/// <summary>Config for show edit overlay keys: rename, new show, add/delete/reorder entries, duration edit. Per ADR-0047.</summary>
internal sealed class ShowEditModalKeyHandlerConfig : IKeyHandlerConfig<ShowEditModalKeyContext>
{
    private const string Section = "Show edit modal (S when in Show play)";

    private static IReadOnlyList<KeyHandling.KeyBindingEntry<ShowEditModalKeyContext>> GetEntries()
    {
        return
        [
            new KeyHandling.KeyBindingEntry<ShowEditModalKeyContext>(
                Matches: k => k.Key == ConsoleKey.Escape,
                Action: (_, _) => true,
                Key: "Escape",
                Description: "Close modal",
                Section),
            new KeyHandling.KeyBindingEntry<ShowEditModalKeyContext>(
                Matches: k => k.Key == ConsoleKey.R,
                Action: (_, context) =>
                {
                    var showToRename = context.ShowRepo.GetById(context.CurrentShowId ?? "");
                    if (showToRename != null)
                    {
                        context.RenameBuffer = showToRename.Name?.Trim() ?? "";
                        context.Renaming = true;
                    }
                    return false;
                },
                Key: "R",
                Description: "Rename show",
                Section),
            new KeyHandling.KeyBindingEntry<ShowEditModalKeyContext>(
                Matches: k => k.Key == ConsoleKey.N,
                Action: (_, context) =>
                {
                    var newShow = new Show { Name = $"Show {context.AllShows.Count + 1}", Entries = [] };
                    var createdId = context.ShowRepo.Create(newShow);
                    context.CurrentShowId = createdId;
                    context.VisualizerSettings.ActiveShowId = createdId;
                    context.VisualizerSettings.ActiveShowName = newShow.Name;
                    context.SaveVisualizerSettings();
                    return false;
                },
                Key: "N",
                Description: "New show",
                Section),
            new KeyHandling.KeyBindingEntry<ShowEditModalKeyContext>(
                Matches: k => k.Key == ConsoleKey.A,
                Action: (_, context) =>
                {
                    var showRef = context.ShowRepo.GetById(context.CurrentShowId ?? "");
                    if (showRef == null || context.AllPresets.Count == 0)
                    {
                        return false;
                    }
                    var firstPresetId = context.AllPresets[0].Id;
                    showRef.Entries.Add(new ShowEntry
                    {
                        PresetId = firstPresetId,
                        Duration = new DurationConfig { Unit = DurationUnit.Seconds, Value = 30 }
                    });
                    context.ShowRepo.Save(context.CurrentShowId!, showRef);
                    context.SelectedIndex = showRef.Entries.Count - 1;
                    context.SaveVisualizerSettings();
                    return false;
                },
                Key: "A",
                Description: "Add preset entry",
                Section),
            new KeyHandling.KeyBindingEntry<ShowEditModalKeyContext>(
                Matches: k => k.Key == ConsoleKey.D,
                Action: (_, context) =>
                {
                    var showRef = context.ShowRepo.GetById(context.CurrentShowId ?? "");
                    var entries = showRef?.Entries ?? new List<ShowEntry>();
                    if (showRef == null || entries.Count == 0 || context.SelectedIndex >= entries.Count)
                    {
                        return false;
                    }
                    showRef.Entries.RemoveAt(context.SelectedIndex);
                    if (context.SelectedIndex >= showRef.Entries.Count && showRef.Entries.Count > 0)
                    {
                        context.SelectedIndex = showRef.Entries.Count - 1;
                    }
                    context.ShowRepo.Save(context.CurrentShowId!, showRef);
                    context.SaveVisualizerSettings();
                    return false;
                },
                Key: "D",
                Description: "Delete selected entry",
                Section),
            new KeyHandling.KeyBindingEntry<ShowEditModalKeyContext>(
                Matches: k => k.Key == ConsoleKey.P,
                Action: (_, context) =>
                {
                    var showRef = context.ShowRepo.GetById(context.CurrentShowId ?? "");
                    var entries = showRef?.Entries ?? new List<ShowEntry>();
                    if (showRef == null || entries.Count == 0 || context.SelectedIndex >= entries.Count || context.AllPresets.Count == 0)
                    {
                        return false;
                    }
                    var entry = entries[context.SelectedIndex];
                    int idx = 0;
                    for (int i = 0; i < context.AllPresets.Count; i++)
                    {
                        if (string.Equals(context.AllPresets[i].Id, entry.PresetId, StringComparison.OrdinalIgnoreCase))
                        {
                            idx = (i + 1) % context.AllPresets.Count;
                            break;
                        }
                    }
                    entry.PresetId = context.AllPresets[idx].Id;
                    context.ShowRepo.Save(context.CurrentShowId!, showRef);
                    context.SaveVisualizerSettings();
                    return false;
                },
                Key: "P",
                Description: "Cycle preset for selected entry",
                Section),
            new KeyHandling.KeyBindingEntry<ShowEditModalKeyContext>(
                Matches: k => k.Key == ConsoleKey.U,
                Action: (_, context) =>
                {
                    var showRef = context.ShowRepo.GetById(context.CurrentShowId ?? "");
                    var entries = showRef?.Entries ?? new List<ShowEntry>();
                    if (showRef == null || entries.Count == 0 || context.SelectedIndex >= entries.Count)
                    {
                        return false;
                    }
                    var entry = entries[context.SelectedIndex];
                    entry.Duration ??= new DurationConfig();
                    entry.Duration.Unit = entry.Duration.Unit == DurationUnit.Seconds ? DurationUnit.Beats : DurationUnit.Seconds;
                    context.ShowRepo.Save(context.CurrentShowId!, showRef);
                    context.SaveVisualizerSettings();
                    return false;
                },
                Key: "U",
                Description: "Toggle duration unit (Seconds/Beats)",
                Section),
            new KeyHandling.KeyBindingEntry<ShowEditModalKeyContext>(
                Matches: k => k.Key == ConsoleKey.Enter,
                Action: (_, context) =>
                {
                    var showRef = context.ShowRepo.GetById(context.CurrentShowId ?? "");
                    var entries = showRef?.Entries ?? new List<ShowEntry>();
                    if (showRef == null || entries.Count == 0 || context.SelectedIndex >= entries.Count)
                    {
                        return false;
                    }
                    var entry = entries[context.SelectedIndex];
                    context.DurationBuffer = (entry.Duration?.Value ?? 30).ToString(System.Globalization.CultureInfo.InvariantCulture);
                    context.EditingDuration = true;
                    return false;
                },
                Key: "Enter",
                Description: "Edit duration value",
                Section),
            new KeyHandling.KeyBindingEntry<ShowEditModalKeyContext>(
                Matches: k => k.Key == ConsoleKey.UpArrow || k.Key == ConsoleKey.DownArrow,
                Action: (key, context) =>
                {
                    var showRef = context.ShowRepo.GetById(context.CurrentShowId ?? "");
                    var entries = showRef?.Entries ?? new List<ShowEntry>();
                    if (key.Key == ConsoleKey.UpArrow && context.SelectedIndex > 0)
                    {
                        context.SelectedIndex--;
                        return false;
                    }
                    if (key.Key == ConsoleKey.DownArrow && context.SelectedIndex < entries.Count - 1)
                    {
                        context.SelectedIndex++;
                        return false;
                    }
                    return false;
                },
                Key: "Up/Down",
                Description: "Select entry",
                Section),
        ];
    }

    private static readonly Lazy<IReadOnlyList<KeyHandling.KeyBindingEntry<ShowEditModalKeyContext>>> s_entries =
        new(GetEntries);

    /// <inheritdoc />
    public IReadOnlyList<KeyBinding> GetBindings() =>
        s_entries.Value.Select(e => e.ToKeyBinding()).ToList();

    /// <inheritdoc />
    public bool Handle(ConsoleKeyInfo key, ShowEditModalKeyContext context)
    {
        if (context.Renaming)
        {
            if (key.Key == ConsoleKey.Escape) { context.Renaming = false; return false; }
            if (key.Key == ConsoleKey.Enter && !string.IsNullOrWhiteSpace(context.RenameBuffer))
            {
                var showToRename = context.ShowRepo.GetById(context.CurrentShowId ?? "");
                if (showToRename != null)
                {
                    showToRename.Name = context.RenameBuffer.Trim();
                    context.ShowRepo.Save(context.CurrentShowId!, showToRename);
                    context.VisualizerSettings.ActiveShowName = showToRename.Name;
                }
                context.Renaming = false;
                context.SaveVisualizerSettings();
                return false;
            }
            if (key.Key == ConsoleKey.Backspace && context.RenameBuffer.Length > 0) { context.RenameBuffer = context.RenameBuffer[..^1]; return false; }
            if (key.KeyChar is >= ' ' and <= '~') { context.RenameBuffer += key.KeyChar; return false; }
            return false;
        }

        if (context.EditingDuration)
        {
            if (key.Key == ConsoleKey.Escape) { context.EditingDuration = false; return false; }
            if (key.Key == ConsoleKey.Enter && !string.IsNullOrWhiteSpace(context.DurationBuffer) && double.TryParse(context.DurationBuffer.Trim(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var val) && val > 0)
            {
                var show = context.ShowRepo.GetById(context.CurrentShowId!);
                if (show?.Entries is { Count: > 0 } && context.SelectedIndex < show.Entries.Count)
                {
                    var entry = show.Entries[context.SelectedIndex];
                    entry.Duration ??= new DurationConfig();
                    entry.Duration.Value = val;
                    context.ShowRepo.Save(context.CurrentShowId!, show);
                }
                context.EditingDuration = false;
                context.SaveVisualizerSettings();
                return false;
            }
            if (key.Key == ConsoleKey.Backspace && context.DurationBuffer.Length > 0) { context.DurationBuffer = context.DurationBuffer[..^1]; return false; }
            if (key.KeyChar is >= ' ' and <= '~' && (char.IsDigit(key.KeyChar) || key.KeyChar == '.'))
            {
                context.DurationBuffer += key.KeyChar;
                return false;
            }
            return false;
        }

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
