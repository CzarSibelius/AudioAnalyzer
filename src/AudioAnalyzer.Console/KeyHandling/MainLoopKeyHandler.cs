using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;
using KeyHandling = AudioAnalyzer.Console.KeyHandling;

namespace AudioAnalyzer.Console;

/// <summary>Config for main loop keys: Tab, V, S, D, H, +/-, P, F, Ctrl+R (full layer reset), Ctrl+Shift+E (screen dump), Escape.</summary>
internal sealed class MainLoopKeyHandlerConfig : IKeyHandlerConfig<MainLoopKeyContext>
{
    private const string Section = "Keyboard controls";

    private static IReadOnlyList<KeyHandling.KeyBindingEntry<MainLoopKeyContext>> GetEntries()
    {
        return
        [
            new KeyHandling.KeyBindingEntry<MainLoopKeyContext>(
                Matches: k => k.Key == ConsoleKey.Tab,
                Action: (_, ctx) =>
                {
                    ctx.OnModeSwitch();
                    if (!ctx.DisplayState.FullScreen)
                    {
                        ctx.Orchestrator.RedrawWithFullHeader();
                    }
                    else
                    {
                        ctx.Orchestrator.Redraw();
                    }
                    return true;
                },
                Key: "Tab",
                Description: "Switch between Preset editor, Show play, and General settings",
                Section),
            new KeyHandling.KeyBindingEntry<MainLoopKeyContext>(
                Matches: k => k.Key == ConsoleKey.V,
                Action: (_, ctx) =>
                {
                    if (ctx.GetApplicationMode() == ApplicationMode.PresetEditor)
                    {
                        ctx.OnPresetCycle();
                        ctx.SaveSettings();
                        if (!ctx.DisplayState.FullScreen)
                        {
                            ctx.Orchestrator.RedrawWithFullHeader();
                        }
                        else
                        {
                            ctx.Orchestrator.Redraw();
                        }
                    }
                    return true;
                },
                Key: "V",
                Description: "Cycle to next preset (Preset editor only)",
                Section,
                ApplicableMode: ApplicationMode.PresetEditor),
            new KeyHandling.KeyBindingEntry<MainLoopKeyContext>(
                Matches: k => k.Key == ConsoleKey.S,
                Action: (_, ctx) =>
                {
                    var mode = ctx.GetApplicationMode();
                    if (mode == ApplicationMode.Settings)
                    {
                        if (!ctx.DisplayState.FullScreen)
                        {
                            ctx.Orchestrator.RedrawWithFullHeader();
                        }
                        else
                        {
                            ctx.Orchestrator.Redraw();
                        }

                        return true;
                    }

                    if (mode == ApplicationMode.PresetEditor)
                    {
                        ctx.SettingsModal.Show(ctx.ConsoleLock, ctx.SaveSettings);
                    }
                    else
                    {
                        ctx.ShowEditModal.Show(ctx.ConsoleLock, () =>
                        {
                            ctx.SaveSettings();
                            ctx.SaveVisualizerSettings();
                        });
                    }

                    if (!ctx.DisplayState.FullScreen)
                    {
                        ctx.Orchestrator.RedrawWithFullHeader();
                    }
                    else
                    {
                        ctx.Orchestrator.Redraw();
                    }

                    return true;
                },
                Key: "S",
                Description: "Preset modal (Preset editor) or Show edit modal (Show play)",
                Section),
            new KeyHandling.KeyBindingEntry<MainLoopKeyContext>(
                Matches: k => k.Key == ConsoleKey.Escape,
                Action: (_, ctx) =>
                {
                    ctx.ShouldQuit = true;
                    return true;
                },
                Key: "Escape",
                Description: "Quit the application",
                Section),
            new KeyHandling.KeyBindingEntry<MainLoopKeyContext>(
                Matches: k => k.Key == ConsoleKey.D,
                Action: (_, ctx) =>
                {
                    ctx.StopCapture();
                    var (newId, newName) = ctx.DeviceSelectionModal.Show(ctx.GetDeviceName(), ctx.SetModalOpen);
                    if (newName != "")
                    {
                        ctx.StartCapture(newId, newName);
                    }
                    else
                    {
                        ctx.RestartCapture();
                    }
                    if (!ctx.DisplayState.FullScreen)
                    {
                        ctx.Orchestrator.RedrawWithFullHeader();
                    }
                    else
                    {
                        ctx.Orchestrator.Redraw();
                    }
                    return true;
                },
                Key: "D",
                Description: "Change audio input device",
                Section),
            new KeyHandling.KeyBindingEntry<MainLoopKeyContext>(
                Matches: k => k.Key == ConsoleKey.H,
                Action: (_, ctx) =>
                {
                    ctx.HelpModal.Show(
                        ctx.GetApplicationMode(),
                        onEnter: () => ctx.SetModalOpen(true),
                        onClose: () =>
                        {
                            ctx.SetModalOpen(false);
                            if (ctx.DisplayState.FullScreen)
                            {
                                ctx.Orchestrator.Redraw();
                            }
                            else
                            {
                                ctx.RefreshHeaderAndRedraw();
                            }
                        });
                    return true;
                },
                Key: "H",
                Description: "Show this help menu",
                Section),
            new KeyHandling.KeyBindingEntry<MainLoopKeyContext>(
                Matches: k => k.Key is ConsoleKey.OemPlus or ConsoleKey.Add or ConsoleKey.OemMinus or ConsoleKey.Subtract,
                Action: (key, ctx) =>
                {
                    if (ctx.AppSettings.BpmSource != BpmSource.AudioAnalysis)
                    {
                        return true;
                    }

                    if (key.Key is ConsoleKey.OemPlus or ConsoleKey.Add)
                    {
                        ctx.Engine.BeatSensitivity += 0.1;
                    }
                    else
                    {
                        ctx.Engine.BeatSensitivity -= 0.1;
                    }
                    ctx.SaveSettings();
                    return true;
                },
                Key: "+/-",
                Description: "Adjust beat sensitivity (audio BPM source only)",
                Section),
            new KeyHandling.KeyBindingEntry<MainLoopKeyContext>(
                Matches: k => k.Key == ConsoleKey.P,
                Action: (_, ctx) =>
                {
                    ctx.OnPaletteCycle();
                    return true;
                },
                Key: "P",
                Description: "Cycle color palette",
                Section),
            new KeyHandling.KeyBindingEntry<MainLoopKeyContext>(
                Matches: k => k.Key == ConsoleKey.F,
                Action: (_, ctx) =>
                {
                    if (ctx.GetApplicationMode() == ApplicationMode.Settings)
                    {
                        return true;
                    }

                    ctx.DisplayState.FullScreen = !ctx.DisplayState.FullScreen;
                    if (ctx.DisplayState.FullScreen)
                    {
                        System.Console.Clear();
                        System.Console.CursorVisible = false;
                        ctx.Orchestrator.Redraw();
                    }
                    else
                    {
                        ctx.Orchestrator.RedrawWithFullHeader();
                    }

                    return true;
                },
                Key: "F",
                Description: "Toggle full screen (visualizer only)",
                Section),
            new KeyHandling.KeyBindingEntry<MainLoopKeyContext>(
                Matches: static k =>
                    k.Key == ConsoleKey.R
                    && k.Modifiers.HasFlag(ConsoleModifiers.Control)
                    && (k.Modifiers & (ConsoleModifiers.Shift | ConsoleModifiers.Alt)) == 0,
                Action: (_, ctx) =>
                {
                    if (ctx.GetApplicationMode() == ApplicationMode.Settings)
                    {
                        return false;
                    }

                    ctx.PerformFullLayerRuntimeReset();
                    if (!ctx.DisplayState.FullScreen)
                    {
                        ctx.Orchestrator.RedrawWithFullHeader();
                    }
                    else
                    {
                        ctx.Orchestrator.Redraw();
                    }

                    return true;
                },
                Key: "Ctrl+R",
                Description: "Full layer reset (clear waveform history and all layer runtime caches)",
                Section),
            new KeyHandling.KeyBindingEntry<MainLoopKeyContext>(
                Matches: k => k.Key == ConsoleKey.E && k.Modifiers.HasFlag(ConsoleModifiers.Control) && k.Modifiers.HasFlag(ConsoleModifiers.Shift),
                Action: (_, ctx) =>
                {
                    ctx.DumpScreen(); // discard file path return value
                    return true;
                },
                Key: "Ctrl+Shift+E",
                Description: "Dump screen to text file",
                Section),
        ];
    }

    private static readonly Lazy<IReadOnlyList<KeyHandling.KeyBindingEntry<MainLoopKeyContext>>> s_entries =
        new(GetEntries);

    /// <inheritdoc />
    public IReadOnlyList<KeyBinding> GetBindings() =>
        s_entries.Value.Select(e => e.ToKeyBinding()).ToList();

    /// <inheritdoc />
    public bool Handle(ConsoleKeyInfo key, MainLoopKeyContext ctx)
    {
        foreach (var entry in s_entries.Value)
        {
            if (entry.Matches(key))
            {
                return entry.Action(key, ctx);
            }
        }
        return false;
    }
}
