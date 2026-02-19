using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Console;

/// <summary>Handles main loop keys: Tab, V, S, D, H, +/-, P, F, Escape.</summary>
internal sealed class MainLoopKeyHandler : IMainLoopKeyHandler
{
    /// <inheritdoc />
    public bool TryHandle(ConsoleKeyInfo key, MainLoopKeyContext ctx)
    {
        switch (key.Key)
        {
            case ConsoleKey.Tab:
                ctx.OnModeSwitch();
                if (!ctx.Engine.FullScreen)
                {
                    ctx.Engine.RedrawWithFullHeader();
                }
                else
                {
                    ctx.Engine.Redraw();
                }
                return true;

            case ConsoleKey.V:
                if (ctx.GetApplicationMode() == ApplicationMode.PresetEditor)
                {
                    ctx.OnPresetCycle();
                    ctx.SaveSettings();
                    if (!ctx.Engine.FullScreen)
                    {
                        ctx.Engine.RedrawWithFullHeader();
                    }
                    else
                    {
                        ctx.Engine.Redraw();
                    }
                }
                return true;

            case ConsoleKey.S:
                if (ctx.GetApplicationMode() == ApplicationMode.PresetEditor)
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
                if (!ctx.Engine.FullScreen)
                {
                    ctx.Engine.RedrawWithFullHeader();
                }
                else
                {
                    ctx.Engine.Redraw();
                }
                return true;

            case ConsoleKey.Escape:
                ctx.ShouldQuit = true;
                return true;

            case ConsoleKey.D:
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
                if (!ctx.Engine.FullScreen)
                {
                    ctx.Engine.RedrawWithFullHeader();
                }
                else
                {
                    ctx.Engine.Redraw();
                }
                return true;

            case ConsoleKey.H:
                ctx.HelpModal.Show(
                    onEnter: () => ctx.SetModalOpen(true),
                    onClose: () =>
                    {
                        ctx.SetModalOpen(false);
                        if (ctx.Engine.FullScreen)
                        {
                            ctx.Engine.Redraw();
                        }
                        else
                        {
                            ctx.RefreshHeaderAndRedraw();
                        }
                    });
                return true;

            case ConsoleKey.OemPlus:
            case ConsoleKey.Add:
                ctx.Engine.BeatSensitivity += 0.1;
                ctx.SaveSettings();
                return true;

            case ConsoleKey.OemMinus:
            case ConsoleKey.Subtract:
                ctx.Engine.BeatSensitivity -= 0.1;
                ctx.SaveSettings();
                return true;

            case ConsoleKey.P:
                ctx.OnPaletteCycle();
                return true;

            case ConsoleKey.F:
                ctx.Engine.FullScreen = !ctx.Engine.FullScreen;
                if (ctx.Engine.FullScreen)
                {
                    System.Console.Clear();
                    System.Console.CursorVisible = false;
                    ctx.Engine.Redraw();
                }
                else
                {
                    ctx.Engine.RedrawWithFullHeader();
                }
                return true;

            default:
                return false;
        }
    }
}
