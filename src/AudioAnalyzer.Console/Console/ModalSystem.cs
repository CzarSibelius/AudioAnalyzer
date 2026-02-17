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
    public static void RunModal(Action drawContent, Func<ConsoleKeyInfo, bool> handleKey, Action? onClose = null, Action? onEnter = null)
    {
        System.Console.CursorVisible = false;
        onEnter?.Invoke();
        while (true)
        {
            System.Console.Clear();
            drawContent();
            var key = System.Console.ReadKey(true);
            if (handleKey(key))
            {
                break;
            }
        }
        onClose?.Invoke();
    }

    /// <summary>
    /// Runs an overlay modal that draws only into the top overlayRowCount rows, leaving the visualizer visible below.
    /// </summary>
    /// <param name="overlayRowCount">Number of rows for the overlay.</param>
    /// <param name="drawContent">Draws the overlay content.</param>
    /// <param name="handleKey">Handles key input. Returns true to close.</param>
    /// <param name="consoleLock">When set, acquired during clear+draw to avoid interleaving with engine render.</param>
    /// <param name="onClose">Called when the overlay closes.</param>
    /// <param name="onEnter">Called when the overlay opens.</param>
    /// <param name="onScrollTick">Called on each poll when no key available; use for auto-scrolling content.</param>
    public static void RunOverlayModal(int overlayRowCount, Action drawContent, Func<ConsoleKeyInfo, bool> handleKey, object? consoleLock = null, Action? onClose = null, Action? onEnter = null, Action? onScrollTick = null)
    {
        System.Console.CursorVisible = false;
        onEnter?.Invoke();

        void ClearAndDraw()
        {
            int width = ConsoleHeader.GetConsoleWidth();
            string blank = new string(' ', width);
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
