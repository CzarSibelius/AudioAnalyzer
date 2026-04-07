using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Application.Display;
using AudioAnalyzer.Application.Palette;
using KeyHandling = AudioAnalyzer.Console.KeyHandling;

namespace AudioAnalyzer.Console;

/// <summary>Theme list, new-theme palette pick, and slot authoring keys.</summary>
internal sealed class UiThemeSelectionKeyHandlerConfig : IKeyHandlerConfig<UiThemeSelectionKeyContext>
{
    private const int SlotCount = 11;
    private const string Section = "UI theme selection";

    private static int ThemeListTotal(UiThemeSelectionKeyContext ctx) => 1 + ctx.Themes.Count;

    private static IReadOnlyList<KeyHandling.KeyBindingEntry<UiThemeSelectionKeyContext>> GetEntries() =>
    [
        new KeyHandling.KeyBindingEntry<UiThemeSelectionKeyContext>(
            Matches: static k => k.Key is ConsoleKey.UpArrow or ConsoleKey.DownArrow,
            Action: static (key, ctx) => HandleVertical(key, ctx),
            Key: "↑/↓",
            Description: "Navigate list / slots",
            Section),
        new KeyHandling.KeyBindingEntry<UiThemeSelectionKeyContext>(
            Matches: static k => k.Key is ConsoleKey.LeftArrow or ConsoleKey.RightArrow,
            Action: static (key, ctx) => HandleHorizontal(key, ctx),
            Key: "←/→",
            Description: "Change palette index (slot editor)",
            Section),
        new KeyHandling.KeyBindingEntry<UiThemeSelectionKeyContext>(
            Matches: static k => k.Key == ConsoleKey.Enter,
            Action: static (_, ctx) => HandleEnter(ctx),
            Key: "Enter",
            Description: "Apply / confirm / save new theme",
            Section),
        new KeyHandling.KeyBindingEntry<UiThemeSelectionKeyContext>(
            Matches: static k => k.Key == ConsoleKey.Escape,
            Action: static (_, ctx) => HandleEscape(ctx),
            Key: "Escape",
            Description: "Cancel or go back",
            Section),
        new KeyHandling.KeyBindingEntry<UiThemeSelectionKeyContext>(
            Matches: static k => k.Key == ConsoleKey.N,
            Action: static (_, ctx) => HandleNewTheme(ctx),
            Key: "N",
            Description: "New theme from palette (theme list)",
            Section),
    ];

    private static readonly Lazy<IReadOnlyList<KeyHandling.KeyBindingEntry<UiThemeSelectionKeyContext>>> s_entries =
        new(GetEntries);

    private static bool HandleVertical(ConsoleKeyInfo key, UiThemeSelectionKeyContext ctx)
    {
        switch (ctx.Phase)
        {
            case UiThemeAuthoringPhase.PickTheme:
                int tn = ThemeListTotal(ctx);
                if (tn <= 0)
                {
                    return false;
                }

                if (key.Key == ConsoleKey.UpArrow)
                {
                    ctx.ThemeListSelectedIndex = (ctx.ThemeListSelectedIndex - 1 + tn) % tn;
                }
                else
                {
                    ctx.ThemeListSelectedIndex = (ctx.ThemeListSelectedIndex + 1) % tn;
                }

                return false;
            case UiThemeAuthoringPhase.NewPickPalette:
                var palettes = ctx.NewPalettes;
                if (palettes is not { Count: > 0 })
                {
                    return false;
                }

                if (key.Key == ConsoleKey.UpArrow)
                {
                    ctx.NewPaletteSelectedIndex = (ctx.NewPaletteSelectedIndex - 1 + palettes.Count) % palettes.Count;
                }
                else
                {
                    ctx.NewPaletteSelectedIndex = (ctx.NewPaletteSelectedIndex + 1) % palettes.Count;
                }

                return false;
            case UiThemeAuthoringPhase.NewEditSlots:
                int totalRows = SlotCount + 1;
                if (key.Key == ConsoleKey.UpArrow)
                {
                    ctx.SlotEditSelectedRow = (ctx.SlotEditSelectedRow - 1 + totalRows) % totalRows;
                }
                else
                {
                    ctx.SlotEditSelectedRow = (ctx.SlotEditSelectedRow + 1) % totalRows;
                }

                return false;
            default:
                return false;
        }
    }

    private static bool HandleHorizontal(ConsoleKeyInfo key, UiThemeSelectionKeyContext ctx)
    {
        if (ctx.Phase != UiThemeAuthoringPhase.NewEditSlots)
        {
            return false;
        }

        if (ctx.SlotEditSelectedRow >= SlotCount)
        {
            return false;
        }

        if (ctx.SlotEditIndices is not { Length: SlotCount } || ctx.SlotEditPaletteColors is not { Count: > 0 })
        {
            return false;
        }

        int k = ctx.SlotEditPaletteColors.Count;
        int row = ctx.SlotEditSelectedRow;
        if (key.Key == ConsoleKey.LeftArrow)
        {
            ctx.SlotEditIndices[row]--;
        }
        else
        {
            ctx.SlotEditIndices[row]++;
        }

        return false;
    }

    private static bool HandleEnter(UiThemeSelectionKeyContext ctx)
    {
        switch (ctx.Phase)
        {
            case UiThemeAuthoringPhase.PickTheme:
                if (ctx.ThemeListSelectedIndex == 0)
                {
                    ctx.UiSettings.UiThemeId = null;
                }
                else
                {
                    ctx.UiSettings.UiThemeId = ctx.Themes[ctx.ThemeListSelectedIndex - 1].Id;
                }

                ctx.SaveSettings();
                return true;
            case UiThemeAuthoringPhase.NewPickPalette:
                var list = ctx.NewPalettes;
                if (list is not { Count: > 0 })
                {
                    return false;
                }

                PaletteInfo p = list[ctx.NewPaletteSelectedIndex];
                var def = ctx.PaletteRepo.GetById(p.Id);
                var colors = ColorPaletteParser.Parse(def);
                if (colors is not { Count: > 0 })
                {
                    return false;
                }

                ctx.SlotEditPaletteId = p.Id;
                ctx.SlotEditPaletteColors = colors.ToList();
                ctx.SlotEditIndices = new int[SlotCount];
                for (int i = 0; i < SlotCount; i++)
                {
                    ctx.SlotEditIndices[i] = i;
                }

                ctx.SlotEditSelectedRow = 0;
                ctx.Phase = UiThemeAuthoringPhase.NewEditSlots;
                return false;
            case UiThemeAuthoringPhase.NewEditSlots:
                if (ctx.SlotEditSelectedRow != SlotCount)
                {
                    return false;
                }

                CommitNewTheme(ctx);
                return false;
            default:
                return false;
        }
    }

    private static void CommitNewTheme(UiThemeSelectionKeyContext ctx)
    {
        if (ctx.SlotEditPaletteId == null
            || ctx.SlotEditPaletteColors is not { Count: > 0 }
            || ctx.SlotEditIndices is not { Length: SlotCount })
        {
            return;
        }

        var definition = UiThemeDefinitionBuilder.FromPaletteSlotIndices(
            displayName: null,
            ctx.SlotEditPaletteId,
            ctx.SlotEditPaletteColors,
            ctx.SlotEditIndices.AsSpan());
        string newId = ctx.ThemeRepo.Create(definition);
        ctx.UiSettings.UiThemeId = newId;
        ctx.SaveSettings();
        ctx.Phase = UiThemeAuthoringPhase.PickTheme;
        ctx.NewPalettes = null;
        ctx.SlotEditPaletteId = null;
        ctx.SlotEditPaletteColors = null;
        ctx.SlotEditIndices = null;
        ctx.SlotEditSelectedRow = 0;
        ctx.Themes = ctx.ThemeRepo.GetAll();
        for (int i = 0; i < ctx.Themes.Count; i++)
        {
            if (string.Equals(ctx.Themes[i].Id, newId, StringComparison.OrdinalIgnoreCase))
            {
                ctx.ThemeListSelectedIndex = i + 1;
                return;
            }
        }

        ctx.ThemeListSelectedIndex = 0;
    }

    private static bool HandleEscape(UiThemeSelectionKeyContext ctx)
    {
        switch (ctx.Phase)
        {
            case UiThemeAuthoringPhase.PickTheme:
                return true;
            case UiThemeAuthoringPhase.NewPickPalette:
                ctx.Phase = UiThemeAuthoringPhase.PickTheme;
                ctx.NewPalettes = null;
                return false;
            case UiThemeAuthoringPhase.NewEditSlots:
                ctx.Phase = UiThemeAuthoringPhase.NewPickPalette;
                ctx.SlotEditPaletteId = null;
                ctx.SlotEditPaletteColors = null;
                ctx.SlotEditIndices = null;
                ctx.SlotEditSelectedRow = 0;
                return false;
            default:
                return true;
        }
    }

    private static bool HandleNewTheme(UiThemeSelectionKeyContext ctx)
    {
        if (ctx.Phase != UiThemeAuthoringPhase.PickTheme)
        {
            return false;
        }

        IReadOnlyList<PaletteInfo> palettes = ctx.PaletteRepo.GetAll();
        if (palettes.Count == 0)
        {
            return false;
        }

        ctx.NewPalettes = palettes;
        ctx.NewPaletteSelectedIndex = 0;
        ctx.Phase = UiThemeAuthoringPhase.NewPickPalette;
        return false;
    }

    /// <inheritdoc />
    public IReadOnlyList<KeyBinding> GetBindings() =>
        s_entries.Value.Select(e => e.ToKeyBinding()).ToList();

    /// <inheritdoc />
    public bool Handle(ConsoleKeyInfo key, UiThemeSelectionKeyContext context)
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
