using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
        IPaletteRepository paletteRepo,
        IAsciiVideoDeviceCatalog? asciiVideoDeviceCatalog,
        ICharsetRepository charsetRepo)
    {
        var list = new List<SettingDescriptor>();

        AddCommonDescriptors(list, layer, paletteRepo);
        AddRenderBoundsDescriptor(list);

        if (s_customSettingsRegistry.TryGetValue(layer.LayerType, out var customType) && customType != null)
        {
            AddCustomDescriptors(list, layer, customType, asciiVideoDeviceCatalog, charsetRepo);
        }

        return list;
    }

    private static readonly HashSet<string> s_excludedCommonProps =
        ["Custom", "_customCache", "RenderBounds"];

    private static readonly string[] s_commonPropOrder =
        ["Enabled", "LayerType", "ZOrder", "SpeedMultiplier", "ColorIndex", "PaletteId"];

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
            "Palette", "Palette", SettingEditMode.PalettePicker,
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

    private static void AddRenderBoundsDescriptor(List<SettingDescriptor> list)
    {
        list.Add(new SettingDescriptor(
            "RenderBounds",
            "Render region",
            SettingEditMode.BoundVisualEdit,
            l =>
            {
                if (l.RenderBounds == null)
                {
                    return "Full";
                }

                var b = l.RenderBounds;
                return string.Format(CultureInfo.InvariantCulture, "Custom X={0:F2} Y={1:F2} W={2:F2} H={3:F2}", b.X, b.Y, b.Width, b.Height);
            },
            null,
            null));
    }

    private static readonly Dictionary<TextLayerType, Type?> s_customSettingsRegistry = new()
    {
        [TextLayerType.AsciiImage] = typeof(AsciiImageSettings),
        [TextLayerType.AsciiVideo] = typeof(AsciiVideoSettings),
        [TextLayerType.AsciiModel] = typeof(AsciiModelSettings),
        [TextLayerType.Oscilloscope] = typeof(OscilloscopeSettings),
        [TextLayerType.WaveformStrip] = typeof(WaveformStripSettings),
        [TextLayerType.LlamaStyle] = typeof(LlamaStyleSettings),
        [TextLayerType.NowPlaying] = typeof(NowPlayingSettings),
        [TextLayerType.Mirror] = typeof(MirrorSettings),
        [TextLayerType.BufferDistortion] = typeof(BufferDistortionSettings),
        [TextLayerType.UnknownPleasures] = typeof(UnknownPleasuresSettings),
        [TextLayerType.Maschine] = typeof(MaschineSettings),
        [TextLayerType.Fill] = typeof(FillSettings),
        [TextLayerType.ScrollingColors] = typeof(ScrollingColorsSettings),
        [TextLayerType.WaveText] = typeof(WaveTextSettings),
        [TextLayerType.GeissBackground] = typeof(GeissBackgroundSettings),
        [TextLayerType.FractalZoom] = typeof(FractalZoomSettings),
        [TextLayerType.Marquee] = typeof(MarqueeSettings),
        [TextLayerType.StaticText] = typeof(StaticTextSettings),
        [TextLayerType.FallingLetters] = typeof(FallingLettersSettings),
    };

    private static void AddCustomDescriptors(
        List<SettingDescriptor> list,
        TextLayerSettings layer,
        Type customType,
        IAsciiVideoDeviceCatalog? asciiVideoDeviceCatalog,
        ICharsetRepository charsetRepo)
    {
        var props = customType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite && p.GetIndexParameters().Length == 0
                && p.GetCustomAttribute<ExcludeFromSettingsModalAttribute>() == null)
            .ToList();

        foreach (var prop in props)
        {
            if (customType == typeof(AsciiVideoSettings) && prop.Name == "WebcamDeviceIndex")
            {
                list.Add(CreateAsciiVideoWebcamDeviceDescriptor(prop, asciiVideoDeviceCatalog));
                continue;
            }

            if (prop.GetCustomAttribute<CharsetSettingAttribute>() != null)
            {
                list.Add(CreateCharsetSettingDescriptor(prop, customType, charsetRepo));
                continue;
            }

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

    private static SettingDescriptor CreateAsciiVideoWebcamDeviceDescriptor(
        PropertyInfo prop,
        IAsciiVideoDeviceCatalog? catalog)
    {
        var attr = prop.GetCustomAttribute<SettingAttribute>();
        var id = attr?.Id ?? prop.Name;
        var label = attr?.Label ?? PascalToLabel(prop.Name);
        var range = prop.GetCustomAttribute<SettingRangeAttribute>();

        Func<TextLayerSettings, string> getDisplay = l =>
        {
            var obj = GetAsciiVideoSettings(l);
            int idx = obj.WebcamDeviceIndex;
            IReadOnlyList<AsciiVideoDeviceEntry> devices = catalog?.GetDevices() ?? Array.Empty<AsciiVideoDeviceEntry>();
            if (devices.Count == 0)
            {
                return idx.ToString(CultureInfo.InvariantCulture);
            }

            int r = ((idx % devices.Count) + devices.Count) % devices.Count;
            string name = devices[r].DisplayName;
            return string.Format(CultureInfo.InvariantCulture, "{0} · {1}", idx, name);
        };

        Action<TextLayerSettings, string>? apply = CreateCustomApply(prop, typeof(AsciiVideoSettings), null!);

        Action<TextLayerSettings, bool> cycle = (l, forward) =>
        {
            var obj = GetAsciiVideoSettings(l);
            IReadOnlyList<AsciiVideoDeviceEntry> devices = catalog?.GetDevices() ?? Array.Empty<AsciiVideoDeviceEntry>();
            if (devices.Count == 0)
            {
                if (range == null)
                {
                    return;
                }

                double val = obj.WebcamDeviceIndex;
                val = forward ? Math.Min(range.Max, val + range.Step) : Math.Max(range.Min, val - range.Step);
                obj.WebcamDeviceIndex = (int)Math.Round(val);
                InvokeSetCustom(l, typeof(AsciiVideoSettings), obj);
                return;
            }

            int n = devices.Count;
            int resolved = ((obj.WebcamDeviceIndex % n) + n) % n;
            int next = forward ? (resolved + 1) % n : (resolved - 1 + n) % n;
            obj.WebcamDeviceIndex = next;
            InvokeSetCustom(l, typeof(AsciiVideoSettings), obj);
        };

        return new SettingDescriptor(id, label, SettingEditMode.Cycle, getDisplay, apply, cycle);
    }

    private static SettingDescriptor CreateCharsetSettingDescriptor(
        PropertyInfo prop,
        Type customType,
        ICharsetRepository charsetRepo)
    {
        ArgumentNullException.ThrowIfNull(charsetRepo);

        var attr = prop.GetCustomAttribute<SettingAttribute>();
        var id = attr?.Id ?? prop.Name;
        var label = attr?.Label ?? PascalToLabel(prop.Name);

        Func<TextLayerSettings, string> getDisplay = l =>
        {
            var obj = InvokeGetCustom(l, customType) ?? Activator.CreateInstance(customType);
            if (obj == null)
            {
                return "(none)";
            }

            var cur = prop.GetValue(obj) as string;
            if (string.IsNullOrWhiteSpace(cur))
            {
                return SummarizeCharsetId(charsetRepo, GetDefaultCharsetIdForCustomType(customType));
            }

            return SummarizeCharsetId(charsetRepo, cur!);
        };

        Action<TextLayerSettings, string>? apply = CreateCustomApply(prop, customType, null!);
        Action<TextLayerSettings, bool>? cycle = (l, forward) =>
        {
            int count = SettingsSurfacesCharsetDrawing.GetEntryCount(charsetRepo, includeLegacySnippetsRow: false);
            if (count <= 0)
            {
                return;
            }

            var obj = InvokeGetCustom(l, customType) ?? Activator.CreateInstance(customType);
            if (obj == null)
            {
                return;
            }

            var cur = prop.GetValue(obj) as string;
            int idx = SettingsSurfacesCharsetDrawing.FindIndexForCharsetId(
                charsetRepo,
                includeLegacySnippetsRow: false,
                cur,
                GetDefaultCharsetIdForCustomType(customType));
            idx = forward ? (idx + 1) % count : (idx - 1 + count) % count;
            SettingsSurfacesCharsetDrawing.ApplySelectionIndex(
                charsetRepo,
                includeLegacySnippetsRow: false,
                idx,
                v => prop.SetValue(obj, v));
            InvokeSetCustom(l, customType, obj);
        };

        return new SettingDescriptor(id, label, SettingEditMode.CharsetPicker, getDisplay, apply, cycle);
    }

    private static string SummarizeCharsetId(ICharsetRepository repo, string id)
    {
        var def = repo.GetById(id);
        string? name = def?.Name?.Trim();
        return string.IsNullOrEmpty(name) ? id : string.Format(CultureInfo.InvariantCulture, "{0} — {1}", id, name);
    }

    internal static string GetDefaultCharsetIdForLayer(TextLayerSettings layer)
    {
        if (!s_customSettingsRegistry.TryGetValue(layer.LayerType, out Type? ct) || ct == null)
        {
            return CharsetIds.AsciiRampClassic;
        }

        return GetDefaultCharsetIdForCustomType(ct);
    }

    internal static string GetDefaultCharsetIdForCustomType(Type customType)
    {
        if (customType == typeof(FractalZoomSettings) || customType == typeof(GeissBackgroundSettings))
        {
            return CharsetIds.DensitySoft;
        }

        if (customType == typeof(UnknownPleasuresSettings))
        {
            return CharsetIds.UnknownPleasuresRamp;
        }

        if (customType == typeof(FallingLettersSettings))
        {
            return CharsetIds.Digits;
        }

        return CharsetIds.AsciiRampClassic;
    }

    /// <summary>Writes the layer&apos;s <c>CharsetId</c> custom property when present (ADR-0080).</summary>
    internal static void ApplyCharsetIdToLayer(TextLayerSettings layer, string? charsetId)
    {
        if (!s_customSettingsRegistry.TryGetValue(layer.LayerType, out Type? ct) || ct == null)
        {
            return;
        }

        var prop = ct.GetProperty("CharsetId");
        if (prop?.GetCustomAttribute<CharsetSettingAttribute>() == null)
        {
            return;
        }

        var obj = InvokeGetCustom(layer, ct) ?? Activator.CreateInstance(ct);
        if (obj == null)
        {
            return;
        }

        prop.SetValue(obj, charsetId);
        InvokeSetCustom(layer, ct, obj);
    }

    /// <summary>Reads raw <c>CharsetId</c> for the layer when the type supports it.</summary>
    internal static bool TryReadCharsetId(TextLayerSettings layer, out string? charsetId, out bool includeLegacyPicker)
    {
        charsetId = null;
        includeLegacyPicker = false;
        if (!s_customSettingsRegistry.TryGetValue(layer.LayerType, out Type? ct) || ct == null)
        {
            return false;
        }

        var prop = ct.GetProperty("CharsetId");
        if (prop?.GetCustomAttribute<CharsetSettingAttribute>() == null)
        {
            return false;
        }

        var obj = InvokeGetCustom(layer, ct) ?? Activator.CreateInstance(ct);
        if (obj == null)
        {
            return false;
        }

        charsetId = prop.GetValue(obj) as string;
        return true;
    }

    private static AsciiVideoSettings GetAsciiVideoSettings(TextLayerSettings layer)
    {
        return (AsciiVideoSettings?)InvokeGetCustom(layer, typeof(AsciiVideoSettings)) ?? new AsciiVideoSettings();
    }

    private static SettingEditMode DeriveEditMode(Type propType, PropertyInfo prop)
    {
        if (prop.GetCustomAttribute<CharsetSettingAttribute>() != null)
        {
            return SettingEditMode.CharsetPicker;
        }

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
            if (prop.PropertyType == typeof(List<string>) && v is List<string> list)
            {
                if (list.Count == 0) { return "(none)"; }
                return string.Join(", ", list.Take(4)) + (list.Count > 4 ? "..." : "");
            }
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
            if (prop.PropertyType == typeof(List<string>))
            {
                var parsedList = string.IsNullOrWhiteSpace(v) || v == "(none)"
                    ? new List<string>()
                    : v.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
                prop.SetValue(obj, parsedList);
                InvokeSetCustom(l, customType, obj);
                return;
            }
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
                if (propType == typeof(int))
                {
                    next = (int)Math.Round(val);
                }
                else
                {
                    next = val;
                }
            }
            if (next != null)
            {
                prop.SetValue(obj, next);
                InvokeSetCustom(l, customType, obj);
            }
        };
    }

    /// <summary>Comma-separated snippet text for S modal edit (full list; the settings row display may truncate).</summary>
    internal static string GetFullSnippetsEditText(TextLayerSettings layer)
    {
        if (!s_customSettingsRegistry.TryGetValue(layer.LayerType, out Type? ct) || ct is null)
        {
            return "";
        }

        var prop = ct.GetProperty("TextSnippets", BindingFlags.Public | BindingFlags.Instance);
        if (prop?.PropertyType != typeof(List<string>))
        {
            return "";
        }

        var obj = InvokeGetCustom(layer, ct) ?? Activator.CreateInstance(ct);
        if (obj == null)
        {
            return "";
        }

        var list = prop.GetValue(obj) as List<string>;
        if (list == null || list.Count == 0)
        {
            return "";
        }

        return string.Join(", ", list);
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
