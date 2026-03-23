namespace AudioAnalyzer.Console;

/// <summary>
/// Modal system per ADR-0006: dialogs drawn on top, capture input until closed,
/// dismiss by key, on close run onClose to restore base view.
/// </summary>
internal static class ModalSystem
{
    /// <summary>
    /// Runs a full-screen modal. Clears the console, draws content, and captures input until handleKey returns true.
    /// </summary>
    /// <param name="drawContent">Draws the modal content.</param>
    /// <param name="handleKey">Handles key input. Returns true to close the modal.</param>
    /// <param name="onClose">Called when the modal closes.</param>
    /// <param name="onEnter">Called when the modal opens (e.g. to set render guard).</param>
    /// <param name="onIdleTick">When set, polled every ~50ms when no key is available (e.g. palette name animation).</param>
    public static void RunModal(Action drawContent, Func<ConsoleKeyInfo, bool> handleKey, Action? onClose = null, Action? onEnter = null, Action? onIdleTick = null)
    {
        System.Console.CursorVisible = false;
        onEnter?.Invoke();
        System.Console.Clear();
        drawContent();
        while (true)
        {
            if (System.Console.KeyAvailable)
            {
                var keys = new List<ConsoleKeyInfo> { System.Console.ReadKey(true) };
                while (System.Console.KeyAvailable)
                {
                    keys.Add(System.Console.ReadKey(true));
                }

                foreach (var k in keys)
                {
                    if (handleKey(k))
                    {
                        goto closed;
                    }
                }

                System.Console.Clear();
                drawContent();
            }
            else if (onIdleTick != null)
            {
                onIdleTick();
            }

            Thread.Sleep(50);
        }

    closed:
        onClose?.Invoke();
    }

    /// <summary>
    /// Runs an overlay modal that draws only into the top overlayRowCount rows, leaving the visualizer visible below.
    /// </summary>
    /// <param name="overlayRowCount">Number of rows for the overlay.</param>
    /// <param name="consoleWidth">Console width in columns (from IConsoleDimensions; used for clearing overlay rows).</param>
    /// <param name="drawContent">Draws the overlay content.</param>
    /// <param name="handleKey">Handles key input. Returns true to close.</param>
    /// <param name="consoleLock">When set, acquired during clear+draw to avoid interleaving with engine render.</param>
    /// <param name="onClose">Called when the overlay closes.</param>
    /// <param name="onEnter">Called when the overlay opens.</param>
    /// <param name="onScrollTick">Called on each poll when no key available; use for lightweight updates (e.g. hint line). Ignored when <paramref name="idleFullRedraw"/> is true.</param>
    /// <param name="idleFullRedraw">When true, idle polls (no key) redraw the overlay without a keypress. Uses in-place redraw (no blank row clear) to avoid flicker; key handling still uses full clear+draw.</param>
    public static void RunOverlayModal(int overlayRowCount, int consoleWidth, Action drawContent, Func<ConsoleKeyInfo, bool> handleKey, object? consoleLock = null, Action? onClose = null, Action? onEnter = null, Action? onScrollTick = null, bool idleFullRedraw = false)
    {
        System.Console.CursorVisible = false;
        onEnter?.Invoke();

        void ClearAndDraw()
        {
            string blank = new string(' ', consoleWidth);
            for (int r = 0; r < overlayRowCount; r++)
            {
                try
                {
                    System.Console.SetCursorPosition(0, r);
                    System.Console.Write(blank);
                }
                catch (Exception ex) { _ = ex; /* Console write failed in overlay clear */ }
            }
            drawContent();
        }

        /// <summary>Redraw overlay content without clearing rows first. Avoids blank-frame flicker on idle animation; callers must paint full-width lines.</summary>
        void DrawOverlayInPlace()
        {
            drawContent();
        }

        // Draw overlay immediately so it's visible before first key press
        if (consoleLock != null)
        {
            lock (consoleLock)
            {
                ClearAndDraw();
            }
        }
        else
        {
            ClearAndDraw();
        }

        long lastIdleOverlayRedrawMs = 0;
        const int IdleOverlayRedrawMinIntervalMs = 100;

        while (true)
        {
            if (System.Console.KeyAvailable)
            {
                if (consoleLock != null)
                {
                    lock (consoleLock)
                    {
                        ClearAndDraw();
                    }
                }
                else
                {
                    ClearAndDraw();
                }
                // Collect queued keys and process in order
                var keys = new List<ConsoleKeyInfo> { System.Console.ReadKey(true) };
                while (System.Console.KeyAvailable)
                {
                    keys.Add(System.Console.ReadKey(true));
                }
                foreach (var k in keys)
                {
                    if (handleKey(k))
                    {
                        goto closed;
                    }
                }
                // Redraw so selection and other state changes are visible
                if (consoleLock != null)
                {
                    lock (consoleLock)
                    {
                        ClearAndDraw();
                    }
                }
                else
                {
                    ClearAndDraw();
                }
            }
            else if (idleFullRedraw)
            {
                long nowMs = Environment.TickCount64;
                if (nowMs - lastIdleOverlayRedrawMs >= IdleOverlayRedrawMinIntervalMs)
                {
                    lastIdleOverlayRedrawMs = nowMs;
                    if (consoleLock != null)
                    {
                        lock (consoleLock)
                        {
                            DrawOverlayInPlace();
                        }
                    }
                    else
                    {
                        DrawOverlayInPlace();
                    }
                }
            }
            else if (onScrollTick != null)
            {
                if (consoleLock != null)
                {
                    lock (consoleLock)
                    {
                        onScrollTick();
                    }
                }
                else
                {
                    onScrollTick();
                }
            }
            Thread.Sleep(50);
        }
    closed:
        onClose?.Invoke();
    }
}
