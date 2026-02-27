using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace AudioAnalyzer.Console;

/// <summary>
/// Captures the visible console screen buffer (Windows) and writes it to a text file.
/// Returns null on non-Windows or when the console API fails.
/// </summary>
internal sealed class ScreenDumpService : IScreenDumpService
{
    private const string DefaultDirectoryName = "screen-dumps";

    /// <inheritdoc />
    public string? DumpToFile(bool stripAnsi = true, string? directory = null)
    {
        if (!OperatingSystem.IsWindows())
        {
            return null;
        }

        string? content = ReadVisibleConsoleContent();
        if (string.IsNullOrEmpty(content))
        {
            return null;
        }

        if (stripAnsi)
        {
            content = StripAnsiEscapes(content);
        }

        string dir = directory ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DefaultDirectoryName);
        try
        {
            Directory.CreateDirectory(dir);
        }
        catch (Exception)
        {
            return null;
        }

        string fileName = $"screen-{DateTime.Now:yyyyMMdd-HHmmss}.txt";
        string path = Path.Combine(dir, fileName);
        try
        {
            File.WriteAllText(path, content);
            return path;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static string? ReadVisibleConsoleContent()
    {
        try
        {
            IntPtr handle = NativeMethods.GetStdHandle(NativeMethods.STD_OUTPUT_HANDLE);
            if (handle == IntPtr.Zero || handle == new IntPtr(-1))
            {
                return null;
            }

            if (!NativeMethods.GetConsoleScreenBufferInfo(handle, out NativeMethods.CONSOLE_SCREEN_BUFFER_INFO info))
            {
                return null;
            }

            var sr = info.srWindow;
            int width = sr.Right - sr.Left + 1;
            int height = sr.Bottom - sr.Top + 1;
            if (width <= 0 || height <= 0)
            {
                return null;
            }

            var lines = new List<string>(height);
            for (short row = sr.Top; row <= sr.Bottom; row++)
            {
                string? line = ReadConsoleRow(handle, sr.Left, row, width);
                if (line == null)
                {
                    return null;
                }
                lines.Add(line);
            }

            return string.Join(Environment.NewLine, lines);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static string? ReadConsoleRow(IntPtr handle, short col, short row, int length)
    {
        int bufferSize = length * 2; // UTF-16
        IntPtr buffer = Marshal.AllocHGlobal(bufferSize);
        try
        {
            if (!NativeMethods.ReadConsoleOutputCharacterW(handle, buffer, (uint)length, new NativeMethods.COORD { X = col, Y = row }, out uint read))
            {
                return null;
            }
            return Marshal.PtrToStringUni(buffer, (int)read);
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static string StripAnsiEscapes(string text)
    {
        // Remove CSI sequences: \x1b[ ... final byte (e.g. m, H, J, K)
        return Regex.Replace(text, @"\x1b\[[\x20-\x3f]*[\x40-\x7e]", "");
    }

    private static class NativeMethods
    {
        internal const int STD_OUTPUT_HANDLE = -11;

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool GetConsoleScreenBufferInfo(IntPtr hConsoleOutput, out CONSOLE_SCREEN_BUFFER_INFO lpConsoleScreenBufferInfo);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern bool ReadConsoleOutputCharacterW(IntPtr hConsoleOutput, IntPtr lpCharacter, uint nLength, COORD dwReadCoord, out uint lpNumberOfCharsRead);

        [StructLayout(LayoutKind.Sequential)]
        internal struct COORD
        {
            public short X;
            public short Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct SMALL_RECT
        {
            public short Left;
            public short Top;
            public short Right;
            public short Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct CONSOLE_SCREEN_BUFFER_INFO
        {
            public COORD dwSize;
            public COORD dwCursorPosition;
            public ushort wAttributes;
            public SMALL_RECT srWindow;
            public COORD dwMaximumWindowSize;
        }
    }
}
