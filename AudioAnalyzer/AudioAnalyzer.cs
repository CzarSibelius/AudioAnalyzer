using NAudio.Dsp;
using NAudio.Wave;

/// <summary>
/// Real-time audio spectrum analyzer with frequency visualization and BPM detection.
/// </summary>
public class AudioAnalyzer
{
    // FFT Configuration
    private const int FftLength = 8192;
    private readonly Complex[] fftBuffer = new Complex[FftLength];
    private int bufferPosition = 0;

    // Display Configuration
    private const int UpdateIntervalMs = 50; // Update display every 50ms (20 FPS)
    private DateTime lastUpdate = DateTime.Now;
    private int lastTerminalWidth = 0;
    private int lastTerminalHeight = 0;

    // Frequency Band Configuration
    private int NumBands; // Dynamic, 8-60 based on terminal width
    private double[] bandMagnitudes = Array.Empty<double>();
    private double[] smoothedMagnitudes = Array.Empty<double>();
    private const double SmoothingFactor = 0.7;

    // Peak Hold Configuration
    private double[] peakHold = Array.Empty<double>();
    private int[] peakHoldTime = Array.Empty<int>();
    private const int PeakHoldFrames = 20; // Hold peak for ~1 second
    private const double PeakFallRate = 0.08;

    // Auto-Gain Configuration
    private double maxMagnitudeEver = 0.001;
    private double targetMaxMagnitude = 0.001;

    // BPM Detection Configuration
    private readonly Queue<double> energyHistory = new Queue<double>();
    private readonly Queue<DateTime> beatTimes = new Queue<DateTime>();
    private const int EnergyHistorySize = 20;
    private const double BeatThreshold = 1.3;
    private DateTime lastBeatTime = DateTime.MinValue;
    private const int MinBeatInterval = 250;
    private double currentBPM = 0;
    private const int BPMHistorySize = 8;
    private double instantEnergy = 0;

    public AudioAnalyzer()
    {
        UpdateDisplayDimensions();
    }

    private void UpdateDisplayDimensions()
    {
        // Minimum terminal size check
        if (Console.WindowWidth < 30 || Console.WindowHeight < 15)
        {
            // Terminal too small, keep previous dimensions or use minimum
            if (NumBands == 0)
                NumBands = 8;
            return;
        }

        // Calculate number of bands based on terminal width
        // Account for left axis labels (5 chars) and margins
        NumBands = Math.Max(8, Math.Min(60, (Console.WindowWidth - 8) / 2));

        // Resize arrays if needed
        if (bandMagnitudes == null || bandMagnitudes.Length != NumBands)
        {
            bandMagnitudes = new double[NumBands];
            smoothedMagnitudes = new double[NumBands];
            peakHold = new double[NumBands];
            peakHoldTime = new int[NumBands];
        }

        lastTerminalWidth = Console.WindowWidth;
        lastTerminalHeight = Console.WindowHeight;
    }

    public void ProcessAudio(byte[] buffer, int bytesRecorded, WaveFormat format)
    {
        int bytesPerSample = format.BitsPerSample / 8;
        int samplesRecorded = bytesRecorded / bytesPerSample;
        float maxVolume = 0;

        for (int i = 0; i < samplesRecorded; i++)
        {
            float sample = format.BitsPerSample switch
            {
                16 => BitConverter.ToInt16(buffer, i * bytesPerSample) / 32768f,
                32 => BitConverter.ToSingle(buffer, i * bytesPerSample),
                _ => 0
            };

            maxVolume = Math.Max(maxVolume, Math.Abs(sample));
            instantEnergy += sample * sample;

            if (bufferPosition < FftLength)
            {
                fftBuffer[bufferPosition].X = sample;
                fftBuffer[bufferPosition].Y = 0;
                bufferPosition++;
            }
        }

        if (bufferPosition >= FftLength)
        {
            AnalyzeFrequencies(format.SampleRate);
            bufferPosition = 0;
        }

        double avgEnergy = samplesRecorded > 0 ? Math.Sqrt(instantEnergy / samplesRecorded) : 0;
        DetectBeat(avgEnergy);
        instantEnergy = 0;

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

        // Create logarithmic frequency bands
        var bandRanges = CreateFrequencyBands(sampleRate);

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

            // Update peak hold
            UpdatePeakHold(b);

            // Track maximum for auto-gain
            if (smoothedMagnitudes[b] > maxMagnitudeEver)
                maxMagnitudeEver = smoothedMagnitudes[b];
        }

        // Smooth the target max magnitude
        targetMaxMagnitude = targetMaxMagnitude * 0.95 + maxMagnitudeEver * 0.05;
    }

    private List<(int start, int end, string label)> CreateFrequencyBands(int sampleRate)
    {
        var bandRanges = new List<(int start, int end, string label)>();
        const double minFreq = 20;
        const double maxFreq = 20000;
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

        return bandRanges;
    }

    private void UpdatePeakHold(int bandIndex)
    {
        if (smoothedMagnitudes[bandIndex] > peakHold[bandIndex])
        {
            peakHold[bandIndex] = smoothedMagnitudes[bandIndex];
            peakHoldTime[bandIndex] = 0;
        }
        else
        {
            peakHoldTime[bandIndex]++;
            if (peakHoldTime[bandIndex] > PeakHoldFrames)
            {
                peakHold[bandIndex] = Math.Max(0, peakHold[bandIndex] - peakHold[bandIndex] * PeakFallRate);
            }
        }
    }

    private void DetectBeat(double energy)
    {
        energyHistory.Enqueue(energy);
        if (energyHistory.Count > EnergyHistorySize)
            energyHistory.Dequeue();

        if (energyHistory.Count < EnergyHistorySize / 2)
            return;

        double avgEnergy = energyHistory.Take(energyHistory.Count - 1).Average();
        DateTime now = DateTime.Now;

        if (energy > avgEnergy * BeatThreshold &&
            energy > 0.01 &&
            (now - lastBeatTime).TotalMilliseconds > MinBeatInterval)
        {
            beatTimes.Enqueue(now);
            lastBeatTime = now;

            while (beatTimes.Count > 0 && (now - beatTimes.Peek()).TotalSeconds > 8)
                beatTimes.Dequeue();

            CalculateBPM();
        }
    }

    private void CalculateBPM()
    {
        if (beatTimes.Count < 2)
            return;

        var recentBeats = beatTimes.TakeLast(Math.Min(BPMHistorySize + 1, beatTimes.Count)).ToList();
        if (recentBeats.Count < 2)
            return;

        var intervals = new List<double>();
        for (int i = 1; i < recentBeats.Count; i++)
        {
            double intervalMs = (recentBeats[i] - recentBeats[i - 1]).TotalMilliseconds;
            if (intervalMs >= 250 && intervalMs <= 2000)
                intervals.Add(intervalMs);
        }

        if (intervals.Count > 0)
        {
            double avgInterval = intervals.Average();
            double newBPM = 60000.0 / avgInterval;

            currentBPM = currentBPM == 0 ? newBPM : currentBPM * 0.8 + newBPM * 0.2;
        }
    }

    private void DisplaySpectrum(float volume)
    {
        try
        {
            // Skip display if terminal is too small
            if (Console.WindowWidth < 30 || Console.WindowHeight < 15)
                return;

            if (Console.WindowWidth != lastTerminalWidth || Console.WindowHeight != lastTerminalHeight)
            {
                UpdateDisplayDimensions();
                RedrawHeader();
            }

        Console.SetCursorPosition(0, 5);
        int termWidth = Console.WindowWidth;

            DisplayVolumeInfo(volume, termWidth);
            DisplayVolumeBar(volume, termWidth);
            DisplayFrequencyBars(termWidth);
            DisplayFrequencyLabels(termWidth);
        }
        catch (Exception)
        {
            // Silently ignore display errors (e.g., terminal size issues)
        }
    }

    private void RedrawHeader()
    {
        Console.Clear();
        Console.SetCursorPosition(0, 0);

        int width = Console.WindowWidth;
        string title = " AUDIO ANALYZER - Real-time Frequency Spectrum ";
        int padding = Math.Max(0, (width - title.Length - 2) / 2);
        Console.WriteLine("╔" + new string('═', width - 2) + "╗");
        Console.WriteLine("║" + new string(' ', padding) + title + new string(' ', width - padding - title.Length - 2) + "║");
        Console.WriteLine("╚" + new string('═', width - 2) + "╝");
        Console.WriteLine("\nPress ESC to stop...\n");
    }

    private void DisplayVolumeInfo(float volume, int termWidth)
    {
        double db = 20 * Math.Log10(Math.Max(volume, 0.00001));
        string bpmDisplay = currentBPM > 0 ? $" | BPM: {currentBPM:F0}" : "";
        Console.WriteLine($"Volume: {volume * 100:F1}% ({db:F1} dB){bpmDisplay}".PadRight(termWidth));
    }

    private void DisplayVolumeBar(float volume, int termWidth)
    {
        int availableWidth = Math.Max(20, termWidth - 10);
        int volBarLength = (int)(volume * availableWidth);
        Console.Write("[");

        for (int i = 0; i < availableWidth; i++)
        {
            if (i < volBarLength)
            {
                SetColorByPosition((double)i / availableWidth);
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
    }

    private void DisplayFrequencyBars(int termWidth)
    {
        int barHeight = Math.Max(10, Math.Min(30, Console.WindowHeight - 15));

        for (int row = barHeight; row > 0; row--)
        {
            DisplayAmplitudeLabel(row, barHeight);
            Console.Write(" ");

            for (int band = 0; band < NumBands; band++)
            {
                DisplayBandCell(band, row, barHeight);
            }

            Console.WriteLine();
        }

        // Draw baseline
        Console.Write("     ");
        Console.WriteLine(new string('─', Math.Min(NumBands * 2, termWidth - 6)));
    }

    private void DisplayAmplitudeLabel(int row, int barHeight)
    {
        if (row == barHeight)
            Console.Write("100%");
        else if (row == (int)(barHeight * 0.75) && barHeight >= 16)
            Console.Write(" 75%");
        else if (row == (int)(barHeight * 0.5) && barHeight >= 12)
            Console.Write(" 50%");
        else if (row == (int)(barHeight * 0.25) && barHeight >= 16)
            Console.Write(" 25%");
        else if (row == 1)
            Console.Write("  0%");
        else
            Console.Write("    ");
    }

    private void DisplayBandCell(int band, int row, int barHeight)
    {
        double gain = targetMaxMagnitude > 0.0001 ? 1.0 / targetMaxMagnitude : 1000;
        gain = Math.Min(gain, 1000);
        
        double normalizedMag = Math.Min(smoothedMagnitudes[band] * gain * 0.8, 1.0);
        int height = (int)(normalizedMag * barHeight);

        double normalizedPeak = Math.Min(peakHold[band] * gain * 0.8, 1.0);
        int peakHeight = (int)(normalizedPeak * barHeight);

        if (row == peakHeight && peakHeight > 0)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("══");
            Console.ResetColor();
        }
        else if (height >= row)
        {
            SetColorByRow(row, barHeight);
            Console.Write("██");
            Console.ResetColor();
        }
        else
        {
            Console.Write("  ");
        }
    }

    private void DisplayFrequencyLabels(int termWidth)
    {
        Console.Write("     ");
        var allLabels = new[] { "20", "30", "50", "80", "100", "150", "200", "300", "500", "800", 
                                "1k", "1.5k", "2k", "3k", "5k", "8k", "10k", "15k", "20k" };

        int maxLabels = Math.Max(4, Math.Min(allLabels.Length, NumBands / 3));
        int labelInterval = Math.Max(1, NumBands / maxLabels);

        for (int band = 0; band < NumBands && band * 2 < termWidth - 6; band++)
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

    private void SetColorByPosition(double position)
    {
        Console.ForegroundColor = position switch
        {
            >= 0.85 => ConsoleColor.Red,
            >= 0.7 => ConsoleColor.Magenta,
            >= 0.55 => ConsoleColor.Yellow,
            >= 0.4 => ConsoleColor.Green,
            >= 0.25 => ConsoleColor.Cyan,
            _ => ConsoleColor.Blue
        };
    }

    private void SetColorByRow(int row, int barHeight)
    {
        if (row <= barHeight * 0.25)
            Console.ForegroundColor = ConsoleColor.Red;
        else if (row <= barHeight * 0.4)
            Console.ForegroundColor = ConsoleColor.Magenta;
        else if (row <= barHeight * 0.55)
            Console.ForegroundColor = ConsoleColor.Yellow;
        else if (row <= barHeight * 0.7)
            Console.ForegroundColor = ConsoleColor.Green;
        else if (row <= barHeight * 0.85)
            Console.ForegroundColor = ConsoleColor.Cyan;
        else
            Console.ForegroundColor = ConsoleColor.Blue;
    }
}
