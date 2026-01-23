using NAudio.CoreAudioApi;
using NAudio.Wave;

Console.Clear();
Console.CursorVisible = false;

// Draw header that scales with terminal width
int width = Console.WindowWidth;
string title = " AUDIO ANALYZER - Real-time Frequency Spectrum ";
int padding = Math.Max(0, (width - title.Length - 2) / 2);
Console.WriteLine("╔" + new string('═', width - 2) + "╗");
Console.WriteLine("║" + new string(' ', padding) + title + new string(' ', width - padding - title.Length - 2) + "║");
Console.WriteLine("╚" + new string('═', width - 2) + "╝");
Console.WriteLine("\nPress ESC to stop...\n");

var captureDevice = new WasapiLoopbackCapture();
var audioAnalyzer = new AudioAnalyzer();

captureDevice.DataAvailable += (sender, e) =>
{
    audioAnalyzer.ProcessAudio(e.Buffer, e.BytesRecorded, captureDevice.WaveFormat);
};

captureDevice.RecordingStopped += (sender, e) =>
{
    Console.Clear();
    Console.CursorVisible = true;
    Console.WriteLine("\nRecording stopped.");
};

// Start capturing
captureDevice.StartRecording();

// Wait for ESC key to stop
while (Console.ReadKey(true).Key != ConsoleKey.Escape)
{
    Thread.Sleep(100);
}

// Stop capturing
captureDevice.StopRecording();
captureDevice.Dispose();
