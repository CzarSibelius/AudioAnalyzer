using System.Globalization;
using System.Reflection;
using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;
using AudioAnalyzer.Visualizers;

namespace AudioAnalyzer.Console;

/// <summary>Descriptor for a single layer setting used by the S modal. Supports get, apply (text edit), and cycle.</summary>
internal sealed class SettingDescriptor
{
    public string Id { get; }
    public string Label { get; }
    public SettingEditMode EditMode { get; }
    private readonly Func<TextLayerSettings, string> _getDisplayValue;
    private readonly Action<TextLayerSettings, string>? _applyEdit;
    private readonly Action<TextLayerSettings, bool>? _cycle;

    private SettingDescriptor(string id, string label, SettingEditMode editMode,
        Func<TextLayerSettings, string> getDisplayValue,
        Action<TextLayerSettings, string>? applyEdit,
        Action<TextLayerSettings, bool>? cycle)
    {
        Id = id;
        Label = label;
        EditMode = editMode;
        _getDisplayValue = getDisplayValue;
        _applyEdit = applyEdit;
        _cycle = cycle;
    }

    public string GetDisplayValue(TextLayerSettings layer) => _getDisplayValue(layer);

    public void ApplyEdit(TextLayerSettings layer, string value)
    {
        _applyEdit?.Invoke(layer, value);
    }

    public void Cycle(TextLayerSettings layer, bool forward)
    {
        _cycle?.Invoke(layer, forward);
    }

    public static IReadOnlyList<SettingDescriptor> BuildAll(
        TextLayerSettings layer,
        IPaletteRepository paletteRepo)
    {
        var list = new List<SettingDescriptor>();

        AddCommonDescriptors(list, layer, paletteRepo);

        if (s_customSettingsRegistry.TryGetValue(layer.LayerType, out var customType) && customType != null)
        {
            AddCustomDescriptors(list, layer, customType);
        }

        return list;
    }

    private static readonly HashSet<string> s_excludedCommonProps =
        ["Custom", "_customCache"];

    private static readonly string[] s_commonPropOrder =
        ["Enabled", "LayerType", "ZOrder", "BeatReaction", "SpeedMultiplier", "ColorIndex", "PaletteId", "TextSnippets"];

    private static void AddCommonDescriptors(
        List<SettingDescriptor> list,
        TextLayerSettings layer,
        IPaletteRepository paletteRepo)
    {
        var layerType = typeof(TextLayerSettings);
        var props = layerType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && !s_excludedCommonProps.Contains(p.Name))
            .ToDictionary(p => p.Name);

        foreach (var propName in s_commonPropOrder)
        {
            if (!props.TryGetValue(propName, out var prop)) { continue; }

            if (propName == "LayerType") { AddLayerTypeDescriptor(list); continue; }
            if (propName == "TextSnippets") { AddSnippetsDescriptor(list); continue; }
            if (propName == "PaletteId") { AddPaletteDescriptor(list, paletteRepo); continue; }

            var (id, label) = propName switch
            {
                "SpeedMultiplier" => ("Speed", "Speed"),
                _ => (propName, PascalToLabel(propName))
            };
            var editMode = DeriveEditMode(prop.PropertyType, prop);
            var getDisplay = CreateGetter(prop, layer);
            var apply = CreateCommonApply(prop, propName);
            var cycle = CreateCommonCycle(prop, propName);
            list.Add(new SettingDescriptor(id, label, editMode, getDisplay, apply, cycle));
        }
    }

    private static void AddLayerTypeDescriptor(List<SettingDescriptor> list)
    {
        list.Add(new SettingDescriptor(
            "LayerType", "Layer type", SettingEditMode.Cycle,
            l => l.LayerType.ToString(),
            null,
            (l, f) => l.LayerType = f ? TextLayerSettings.CycleTypeForward(l) : TextLayerSettings.CycleTypeBackward(l)));
    }

    private static void AddPaletteDescriptor(List<SettingDescriptor> list, IPaletteRepository paletteRepo)
    {
        list.Add(new SettingDescriptor(
            "Palette", "Palette", SettingEditMode.Cycle,
            l => l.PaletteId ?? "(inherit)",
            (l, v) => l.PaletteId = string.IsNullOrWhiteSpace(v) || v == "(inherit)" ? null : v,
            (l, f) =>
            {
                var palettes = paletteRepo.GetAll();
                if (palettes.Count == 0) { return; }
                var currentId = l.PaletteId ?? "";
                int pi = 0;
                for (int i = 0; i < palettes.Count; i++)
                {
                    if (string.Equals(palettes[i].Id, currentId, StringComparison.OrdinalIgnoreCase))
                    {
                        pi = i;
                        break;
                    }
                }
                pi = f ? (pi + 1) % palettes.Count : (pi - 1 + palettes.Count) % palettes.Count;
                l.PaletteId = palettes[pi].Id;
            }));
    }

    private static void AddSnippetsDescriptor(List<SettingDescriptor> list)
    {
        list.Add(new SettingDescriptor(
            "Snippets", "Snippets", SettingEditMode.TextEdit,
            l => l.TextSnippets is { Count: > 0 }
                ? string.Join(", ", l.TextSnippets.Take(4)) + (l.TextSnippets.Count > 4 ? "..." : "")
                : "(none)",
            (l, v) => l.TextSnippets = v.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList(),
            null));
    }

    private static readonly Dictionary<TextLayerType, Type?> s_customSettingsRegistry = new()
    {
        [TextLayerType.AsciiImage] = typeof(AsciiImageSettings),
        [TextLayerType.Oscilloscope] = typeof(OscilloscopeSettings),
        [TextLayerType.LlamaStyle] = typeof(LlamaStyleSettings),
        [TextLayerType.NowPlaying] = typeof(NowPlayingSettings),
        [TextLayerType.Mirror] = typeof(MirrorSettings),
    };

    private static void AddCustomDescriptors(List<SettingDescriptor> list, TextLayerSettings layer, Type customType)
    {
        var props = customType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite && p.GetIndexParameters().Length == 0)
            .ToList();

        foreach (var prop in props)
        {
            var attr = prop.GetCustomAttribute<SettingAttribute>();
            var id = attr?.Id ?? prop.Name;
            var label = attr?.Label ?? PascalToLabel(prop.Name);
            var editMode = DeriveEditMode(prop.PropertyType, prop);
            var getDisplay = CreateCustomGetter(prop, customType, layer);
            var apply = CreateCustomApply(prop, customType, layer);
            var cycle = CreateCustomCycle(prop, customType, layer);
            list.Add(new SettingDescriptor(id, label, editMode, getDisplay, apply, cycle));
        }
    }

    private static SettingEditMode DeriveEditMode(Type propType, PropertyInfo prop)
    {
        if (prop.GetCustomAttribute<SettingChoicesAttribute>() != null)
        {
            return SettingEditMode.Cycle;
        }
        if (propType == typeof(bool) || propType.IsEnum)
        {
            return SettingEditMode.Cycle;
        }
        if (propType == typeof(int) || propType == typeof(double))
        {
            return SettingEditMode.Cycle;
        }
        if (propType == typeof(List<string>) || propType == typeof(IList<string>))
        {
            return SettingEditMode.TextEdit;
        }
        return SettingEditMode.TextEdit;
    }

    private static string PascalToLabel(string name)
    {
        if (string.IsNullOrEmpty(name)) { return name; }
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < name.Length; i++)
        {
            if (i > 0 && char.IsUpper(name[i]))
            {
                sb.Append(' ');
            }
            sb.Append(i == 0 ? char.ToUpperInvariant(name[i]) : char.ToLowerInvariant(name[i]));
        }
        return sb.ToString();
    }

    private static Func<TextLayerSettings, string> CreateGetter(PropertyInfo prop, TextLayerSettings layer)
    {
        return l =>
        {
            var v = prop.GetValue(l);
            if (v == null) { return "(none)"; }
            if (prop.PropertyType == typeof(double)) { return ((double)v).ToString("F1", CultureInfo.InvariantCulture); }
            return v.ToString() ?? "";
        };
    }

    private static Action<TextLayerSettings, string>? CreateCommonApply(PropertyInfo prop, string propName)
    {
        return (l, v) =>
        {
            switch (propName)
            {
                case "Enabled": l.Enabled = bool.Parse(v); break;
                case "ZOrder": l.ZOrder = Math.Clamp(int.Parse(v, CultureInfo.InvariantCulture), 0, 8); break;
                case "BeatReaction": l.BeatReaction = Enum.Parse<TextLayerBeatReaction>(v); break;
                case "SpeedMultiplier": l.SpeedMultiplier = Math.Clamp(double.Parse(v, CultureInfo.InvariantCulture), 0.1, 3.0); break;
                case "ColorIndex": l.ColorIndex = Math.Clamp(int.Parse(v, CultureInfo.InvariantCulture), 0, 99); break;
            }
        };
    }

    private static Action<TextLayerSettings, bool>? CreateCommonCycle(PropertyInfo prop, string propName)
    {
        return (l, f) =>
        {
            switch (propName)
            {
                case "Enabled": l.Enabled = !l.Enabled; break;
                case "ZOrder": l.ZOrder = f ? Math.Min(8, l.ZOrder + 1) : Math.Max(0, l.ZOrder - 1); break;
                case "BeatReaction":
                    var beatVals = Enum.GetValues<TextLayerBeatReaction>();
                    var bi = Array.IndexOf(beatVals, l.BeatReaction);
                    l.BeatReaction = beatVals[f ? (bi + 1) % beatVals.Length : (bi - 1 + beatVals.Length) % beatVals.Length];
                    break;
                case "SpeedMultiplier":
                    l.SpeedMultiplier = f ? Math.Min(3.0, l.SpeedMultiplier + 0.1) : Math.Max(0.1, l.SpeedMultiplier - 0.1);
                    break;
                case "ColorIndex":
                    l.ColorIndex = f ? Math.Min(99, l.ColorIndex + 1) : Math.Max(0, l.ColorIndex - 1);
                    break;
            }
        };
    }

    private static Func<TextLayerSettings, string> CreateCustomGetter(PropertyInfo prop, Type customType, TextLayerSettings layer)
    {
        return l =>
        {
            var obj = InvokeGetCustom(l, customType);
            if (obj == null) { return "(none)"; }
            var v = prop.GetValue(obj);
            if (v == null) { return "(none)"; }
            if (prop.PropertyType == typeof(double)) { return ((double)v).ToString("F1", CultureInfo.InvariantCulture); }
            return v.ToString() ?? "";
        };
    }

    private static Action<TextLayerSettings, string>? CreateCustomApply(PropertyInfo prop, Type customType, TextLayerSettings layer)
    {
        var range = prop.GetCustomAttribute<SettingRangeAttribute>();
        return (l, v) =>
        {
            var obj = InvokeGetCustom(l, customType) ?? Activator.CreateInstance(customType);
            if (obj == null) { return; }
            object? parsed;
            if (prop.PropertyType == typeof(string))
            {
                parsed = string.IsNullOrWhiteSpace(v) || v == "(none)" ? null : v;
            }
            else
            {
                parsed = ParseValue(prop.PropertyType, v, range);
            }
            prop.SetValue(obj, parsed);
            InvokeSetCustom(l, customType, obj);
        };
    }

    private static Action<TextLayerSettings, bool>? CreateCustomCycle(PropertyInfo prop, Type customType, TextLayerSettings layer)
    {
        var choices = prop.GetCustomAttribute<SettingChoicesAttribute>();
        var range = prop.GetCustomAttribute<SettingRangeAttribute>();
        var propType = prop.PropertyType;

        return (l, f) =>
        {
            var obj = InvokeGetCustom(l, customType) ?? Activator.CreateInstance(customType);
            if (obj == null) { return; }
            var current = prop.GetValue(obj);
            object? next = null;
            if (propType == typeof(bool))
            {
                next = !(bool)current!;
            }
            else if (propType.IsEnum)
            {
                var vals = Enum.GetValues(propType);
                var i = Array.IndexOf(vals, current);
                i = f ? (i + 1) % vals.Length : (i - 1 + vals.Length) % vals.Length;
                next = vals.GetValue(i);
            }
            else if (choices != null && propType == typeof(string))
            {
                var arr = choices.Choices;
                var i = Array.IndexOf(arr, (string?)current);
                if (i < 0) { i = 0; }
                i = f ? (i + 1) % arr.Length : (i - 1 + arr.Length) % arr.Length;
                next = arr[i];
            }
            else if (range != null && (propType == typeof(int) || propType == typeof(double)))
            {
                var val = propType == typeof(int) ? (double)(int)current! : (double)current!;
                val = f ? Math.Min(range.Max, val + range.Step) : Math.Max(range.Min, val - range.Step);
                next = propType == typeof(int) ? (int)Math.Round(val) : val;
            }
            if (next != null)
            {
                prop.SetValue(obj, next);
                InvokeSetCustom(l, customType, obj);
            }
        };
    }

    private static object? ParseValue(Type targetType, string value, SettingRangeAttribute? range)
    {
        if (targetType == typeof(string)) { return value; }
        if (targetType == typeof(bool)) { return bool.Parse(value); }
        if (targetType == typeof(int))
        {
            var n = int.Parse(value, CultureInfo.InvariantCulture);
            if (range != null) { n = (int)Math.Clamp(n, range.Min, range.Max); }
            return n;
        }
        if (targetType == typeof(double))
        {
            var d = double.Parse(value, CultureInfo.InvariantCulture);
            if (range != null) { d = Math.Clamp(d, range.Min, range.Max); }
            return d;
        }
        if (targetType.IsEnum) { return Enum.Parse(targetType, value, true); }
        return Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
    }

    private static object? InvokeGetCustom(TextLayerSettings layer, Type customType)
    {
        var method = typeof(TextLayerSettings).GetMethod("GetCustom", 1, Type.EmptyTypes)!
            .MakeGenericMethod(customType);
        return method.Invoke(layer, null);
    }

    private static void InvokeSetCustom(TextLayerSettings layer, Type customType, object value)
    {
        var method = typeof(TextLayerSettings)
            .GetMethods()
            .First(m => m.Name == "SetCustom" && m.IsGenericMethod && m.GetGenericArguments().Length == 1)
            .MakeGenericMethod(customType);
        method.Invoke(layer, [value]);
    }
}
