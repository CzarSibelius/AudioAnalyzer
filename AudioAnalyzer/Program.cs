using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Dsp;

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

public class AudioAnalyzer
{
    private const int FftLength = 8192;
    private readonly Complex[] fftBuffer = new Complex[FftLength];
    private int bufferPosition = 0;
    private DateTime lastUpdate = DateTime.Now;
    private const int UpdateIntervalMs = 50; // Update display every 50ms
    private int NumBands; // Number of frequency bands to display (dynamic)
    private double[] bandMagnitudes = Array.Empty<double>();
    private double[] smoothedMagnitudes = Array.Empty<double>();
    private const double SmoothingFactor = 0.7; // For smoothing the display
    private int lastTerminalWidth = 0;
    private int lastTerminalHeight = 0;
    private double maxMagnitudeEver = 0.001; // Track maximum magnitude for auto-gain
    private double targetMaxMagnitude = 0.001;
    
    // BPM detection fields
    private readonly Queue<double> energyHistory = new Queue<double>();
    private readonly Queue<DateTime> beatTimes = new Queue<DateTime>();
    private const int EnergyHistorySize = 20; // ~1 second at 20 updates/sec
    private const double BeatThreshold = 1.3; // Energy must be 1.3x average
    private DateTime lastBeatTime = DateTime.MinValue;
    private const int MinBeatInterval = 250; // Min 250ms between beats (240 BPM max)
    private double currentBPM = 0;
    private const int BPMHistorySize = 8; // Average last 8 beats
    private double instantEnergy = 0;

    public AudioAnalyzer()
    {
        UpdateDisplayDimensions();
    }

    private void UpdateDisplayDimensions()
    {
        // Calculate number of bands based on terminal width (each band needs 2 characters minimum)
        // Wider terminals = more frequency detail
        NumBands = Math.Max(8, Math.Min(60, (Console.WindowWidth - 4) / 2));
        
        // Resize arrays if needed
        if (bandMagnitudes == null || bandMagnitudes.Length != NumBands)
        {
            bandMagnitudes = new double[NumBands];
            smoothedMagnitudes = new double[NumBands];
        }
        
        lastTerminalWidth = Console.WindowWidth;
        lastTerminalHeight = Console.WindowHeight;
    }

    public void ProcessAudio(byte[] buffer, int bytesRecorded, WaveFormat format)
    {
        // Convert bytes to floats
        int bytesPerSample = format.BitsPerSample / 8;
        int samplesRecorded = bytesRecorded / bytesPerSample;

        float maxVolume = 0;

        for (int i = 0; i < samplesRecorded; i++)
        {
            float sample = 0;

            if (format.BitsPerSample == 16)
            {
                sample = BitConverter.ToInt16(buffer, i * bytesPerSample) / 32768f;
            }
            else if (format.BitsPerSample == 32)
            {
                sample = BitConverter.ToSingle(buffer, i * bytesPerSample);
            }

            // Track max volume and energy
            maxVolume = Math.Max(maxVolume, Math.Abs(sample));
            instantEnergy += sample * sample; // Accumulate energy

            // Fill FFT buffer
            if (bufferPosition < FftLength)
            {
                fftBuffer[bufferPosition].X = sample;
                fftBuffer[bufferPosition].Y = 0;
                bufferPosition++;
            }
        }

        // Perform FFT analysis when buffer is full
        if (bufferPosition >= FftLength)
        {
            AnalyzeFrequencies(format.SampleRate);
            bufferPosition = 0;
        }

        // Detect beats for BPM using averaged energy
        double avgEnergy = samplesRecorded > 0 ? Math.Sqrt(instantEnergy / samplesRecorded) : 0;
        DetectBeat(avgEnergy);
        instantEnergy = 0; // Reset for next buffer

        // Update display periodically
        if ((DateTime.Now - lastUpdate).TotalMilliseconds >= UpdateIntervalMs)
        {
            DisplaySpectrum(maxVolume);
            lastUpdate = DateTime.Now;
        }
    }

    private void AnalyzeFrequencies(int sampleRate)
    {
        // Apply Hamming window
        for (int i = 0; i < FftLength; i++)
        {
            double window = 0.54 - 0.46 * Math.Cos(2 * Math.PI * i / (FftLength - 1));
            fftBuffer[i].X *= (float)window;
        }

        // Perform FFT
        FastFourierTransform.FFT(true, (int)Math.Log(FftLength, 2), fftBuffer);

        // Define frequency bands (logarithmic scale for better visualization)
        // Bass: 20-250 Hz, Mid: 250-2000 Hz, High: 2000-20000 Hz
        var bandRanges = new List<(int start, int end, string label)>();
        
        // Create logarithmic frequency bands
        double minFreq = 20;
        double maxFreq = 20000;
        double logMin = Math.Log10(minFreq);
        double logMax = Math.Log10(maxFreq);
        double step = (logMax - logMin) / NumBands;

        for (int band = 0; band < NumBands; band++)
        {
            double logStart = logMin + band * step;
            double logEnd = logMin + (band + 1) * step;
            int startFreq = (int)Math.Pow(10, logStart);
            int endFreq = (int)Math.Pow(10, logEnd);
            
            int startBin = (int)(startFreq * FftLength / (double)sampleRate);
            int endBin = (int)(endFreq * FftLength / (double)sampleRate);
            
            string label = startFreq < 1000 ? $"{startFreq}" : $"{startFreq / 1000}k";
            bandRanges.Add((startBin, endBin, label));
        }

        // Calculate magnitude for each band
        for (int b = 0; b < NumBands; b++)
        {
            double totalMagnitude = 0;
            int count = 0;
            
            for (int i = bandRanges[b].start; i < bandRanges[b].end && i < FftLength / 2; i++)
            {
                double magnitude = Math.Sqrt(fftBuffer[i].X * fftBuffer[i].X + fftBuffer[i].Y * fftBuffer[i].Y);
                totalMagnitude += magnitude;
                count++;
            }
            
            bandMagnitudes[b] = count > 0 ? totalMagnitude / count : 0;
            
            // Apply smoothing
            smoothedMagnitudes[b] = smoothedMagnitudes[b] * SmoothingFactor + 
                                      bandMagnitudes[b] * (1 - SmoothingFactor);
            
            // Track maximum for auto-gain
            if (smoothedMagnitudes[b] > maxMagnitudeEver)
                maxMagnitudeEver = smoothedMagnitudes[b];
        }
        
        // Smooth the target max magnitude
        targetMaxMagnitude = targetMaxMagnitude * 0.95 + maxMagnitudeEver * 0.05;
    }

    private void DetectBeat(double energy)
    {
        // Add to history
        energyHistory.Enqueue(energy);
        if (energyHistory.Count > EnergyHistorySize)
            energyHistory.Dequeue();
        
        // Need enough history to calculate average
        if (energyHistory.Count < EnergyHistorySize / 2)
            return;
        
        // Calculate average energy (excluding current to avoid bias)
        double avgEnergy = energyHistory.Take(energyHistory.Count - 1).Average();
        
        // Detect beat if current energy exceeds threshold
        DateTime now = DateTime.Now;
        if (energy > avgEnergy * BeatThreshold && 
            energy > 0.01 && // Minimum energy threshold
            (now - lastBeatTime).TotalMilliseconds > MinBeatInterval)
        {
            beatTimes.Enqueue(now);
            lastBeatTime = now;
            
            // Keep only recent beats (last 8 seconds)
            while (beatTimes.Count > 0 && (now - beatTimes.Peek()).TotalSeconds > 8)
                beatTimes.Dequeue();
            
            // Calculate BPM from recent beat intervals
            if (beatTimes.Count >= 2)
            {
                var recentBeats = beatTimes.TakeLast(Math.Min(BPMHistorySize + 1, beatTimes.Count)).ToList();
                if (recentBeats.Count >= 2)
                {
                    var intervals = new List<double>();
                    for (int i = 1; i < recentBeats.Count; i++)
                    {
                        double intervalMs = (recentBeats[i] - recentBeats[i - 1]).TotalMilliseconds;
                        // Filter out outliers
                        if (intervalMs >= 250 && intervalMs <= 2000)
                            intervals.Add(intervalMs);
                    }
                    
                    if (intervals.Count > 0)
                    {
                        double avgInterval = intervals.Average();
                        double newBPM = 60000.0 / avgInterval; // Convert ms to BPM
                        
                        // Smooth BPM changes
                        if (currentBPM == 0)
                            currentBPM = newBPM;
                        else
                            currentBPM = currentBPM * 0.8 + newBPM * 0.2;
                    }
                }
            }
        }
    }

    private void DisplaySpectrum(float volume)
    {
        // Check if terminal size changed and update dimensions
        if (Console.WindowWidth != lastTerminalWidth || Console.WindowHeight != lastTerminalHeight)
        {
            UpdateDisplayDimensions();
            Console.Clear();
            Console.SetCursorPosition(0, 0);
            
            // Redraw header
            int width = Console.WindowWidth;
            string title = " AUDIO ANALYZER - Real-time Frequency Spectrum ";
            int padding = Math.Max(0, (width - title.Length - 2) / 2);
            Console.WriteLine("╔" + new string('═', width - 2) + "╗");
            Console.WriteLine("║" + new string(' ', padding) + title + new string(' ', width - padding - title.Length - 2) + "║");
            Console.WriteLine("╚" + new string('═', width - 2) + "╝");
            Console.WriteLine("\nPress ESC to stop...\n");
        }
        
        Console.SetCursorPosition(0, 5);
        int termWidth = Console.WindowWidth;

        // Convert to dB
        double db = 20 * Math.Log10(Math.Max(volume, 0.00001));
        string bpmDisplay = currentBPM > 0 ? $" | BPM: {currentBPM:F0}" : "";
        Console.WriteLine($"Volume: {volume * 100:F1}% ({db:F1} dB){bpmDisplay}".PadRight(termWidth));

        // Display volume bar - scale to terminal width with color gradient
        int availableWidth = Math.Max(20, termWidth - 10);
        int volBarLength = (int)(volume * availableWidth);
        Console.Write("[");
        for (int i = 0; i < availableWidth; i++)
        {
            if (i < volBarLength)
            {
                // Color gradient based on position
                double position = (double)i / availableWidth;
                if (position >= 0.85)
                    Console.ForegroundColor = ConsoleColor.Red; // Loudest
                else if (position >= 0.7)
                    Console.ForegroundColor = ConsoleColor.Magenta; // Very loud
                else if (position >= 0.55)
                    Console.ForegroundColor = ConsoleColor.Yellow; // Loud
                else if (position >= 0.4)
                    Console.ForegroundColor = ConsoleColor.Green; // Medium
                else if (position >= 0.25)
                    Console.ForegroundColor = ConsoleColor.Cyan; // Quiet
                else
                    Console.ForegroundColor = ConsoleColor.Blue; // Very quiet
                Console.Write("█");
                Console.ResetColor();
            }
            else
            {
                Console.Write(" ");
            }
        }
        Console.WriteLine("]".PadRight(termWidth - availableWidth - 1));
        Console.WriteLine();

        // Display frequency spectrum as vertical bars - scale height to terminal
        int barHeight = Math.Max(10, Math.Min(30, Console.WindowHeight - 15));
        
        // Draw from top to bottom
        for (int row = barHeight; row > 0; row--)
        {
            Console.Write(" ");
            for (int band = 0; band < NumBands; band++)
            {
                // Normalize and scale magnitude for display with auto-gain
                double gain = targetMaxMagnitude > 0.0001 ? 1.0 / targetMaxMagnitude : 1000;
                gain = Math.Min(gain, 1000); // Cap the gain
                double normalizedMag = Math.Min(smoothedMagnitudes[band] * gain * 0.8, 1.0);
                int height = (int)(normalizedMag * barHeight);
                
                if (height >= row)
                {
                    // Color scale based on amplitude
                    if (row <= barHeight * 0.25)
                    {
                        Console.ForegroundColor = ConsoleColor.Red; // Loudest - Red
                        Console.Write("██");
                    }
                    else if (row <= barHeight * 0.4)
                    {
                        Console.ForegroundColor = ConsoleColor.Magenta; // Very loud - Magenta
                        Console.Write("██");
                    }
                    else if (row <= barHeight * 0.55)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow; // Loud - Yellow
                        Console.Write("██");
                    }
                    else if (row <= barHeight * 0.7)
                    {
                        Console.ForegroundColor = ConsoleColor.Green; // Medium - Green
                        Console.Write("██");
                    }
                    else if (row <= barHeight * 0.85)
                    {
                        Console.ForegroundColor = ConsoleColor.Cyan; // Quiet - Cyan
                        Console.Write("██");
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Blue; // Very quiet - Blue
                        Console.Write("██");
                    }
                    Console.ResetColor();
                }
                else
                {
                    Console.Write("  ");
                }
            }
            Console.WriteLine();
        }
        
        // Draw baseline
        Console.Write(" ");
        Console.WriteLine(new string('─', Math.Min(NumBands * 2, termWidth - 2)));
        
        // Draw frequency labels (scale labels with terminal width)
        Console.Write(" ");
        var allLabels = new[] { "20", "30", "50", "80", "100", "150", "200", "300", "500", "800", "1k", "1.5k", "2k", "3k", "5k", "8k", "10k", "15k", "20k" };
        
        // Determine how many labels to show based on width
        int maxLabels = Math.Max(4, Math.Min(allLabels.Length, NumBands / 3));
        int labelInterval = Math.Max(1, NumBands / maxLabels);
        
        for (int band = 0; band < NumBands && band * 2 < termWidth - 2; band++)
        {
            if (band % labelInterval == 0 && band / labelInterval < allLabels.Length)
            {
                string label = allLabels[Math.Min(band / labelInterval, allLabels.Length - 1)];
                Console.Write(label.PadRight(Math.Min(4, labelInterval * 2)).Substring(0, Math.Min(label.Length + 1, 2)));
            }
            else
            {
                Console.Write("  ");
            }
        }
        Console.WriteLine();
        Console.WriteLine("\n Frequency (Hz)".PadRight(termWidth));
    }
}
