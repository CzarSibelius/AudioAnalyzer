using NAudio.Dsp;
using NAudio.Wave;

public enum VisualizationMode
{
    SpectrumBars,
    Oscilloscope,
    VuMeter,
    WinampBars,
    Geiss
}

/// <summary>
/// Real-time audio spectrum analyzer with multiple Winamp-style visualizations.
/// </summary>
public class AudioAnalyzer
{
    // FFT Configuration
    private const int FftLength = 8192;
    private readonly Complex[] fftBuffer = new Complex[FftLength];
    private int bufferPosition = 0;

    // Waveform buffer for oscilloscope
    private const int WaveformSize = 512;
    private readonly float[] waveformBuffer = new float[WaveformSize];
    private int waveformPosition = 0;
    private readonly float[] displayWaveform = new float[WaveformSize];

    // Display Configuration
    private const int UpdateIntervalMs = 50;
    private DateTime lastUpdate = DateTime.Now;
    private int lastTerminalWidth = 0;
    private int lastTerminalHeight = 0;
    private int displayStartRow = 6;
    private Action? onRedrawHeader;

    // Visualization mode
    private VisualizationMode currentMode = VisualizationMode.SpectrumBars;
    public VisualizationMode CurrentMode => currentMode;

    // Frequency Band Configuration
    private int NumBands;
    private double[] bandMagnitudes = Array.Empty<double>();
    private double[] smoothedMagnitudes = Array.Empty<double>();
    private const double SmoothingFactor = 0.7;

    // Peak Hold Configuration
    private double[] peakHold = Array.Empty<double>();
    private int[] peakHoldTime = Array.Empty<int>();
    private const int PeakHoldFrames = 20;
    private const double PeakFallRate = 0.08;

    // Auto-Gain Configuration
    private double maxMagnitudeEver = 0.001;
    private double targetMaxMagnitude = 0.001;

    // VU Meter
    private float leftChannel = 0;
    private float rightChannel = 0;
    private float leftPeak = 0;
    private float rightPeak = 0;
    private float leftPeakHold = 0;
    private float rightPeakHold = 0;
    private int leftPeakHoldTime = 0;
    private int rightPeakHoldTime = 0;

    // BPM Detection
    private readonly Queue<double> energyHistory = new();
    private readonly Queue<DateTime> beatTimes = new();
    private const int EnergyHistorySize = 20;
    private double beatThreshold = 1.3;
    private DateTime lastBeatTime = DateTime.MinValue;
    private const int MinBeatInterval = 250;
    private double currentBPM = 0;
    private const int BPMHistorySize = 8;
    private double instantEnergy = 0;

    // Beat sensitivity (public accessor)
    public double BeatSensitivity
    {
        get => beatThreshold;
        set => beatThreshold = Math.Clamp(value, 0.5, 3.0);
    }

    // Beat flash effect
    private int beatFlashFrames = 0;

    // Geiss visualization state
    private double geissPhase = 0;
    private double geissColorPhase = 0;
    private double geissBassIntensity = 0;
    private double geissTrebleIntensity = 0;

    // Beat circles for Geiss mode
    private readonly List<(double radius, double maxRadius, int age, ConsoleColor color)> beatCircles = new();
    private bool showBeatCircles = true;
    public bool ShowBeatCircles
    {
        get => showBeatCircles;
        set => showBeatCircles = value;
    }

    public AudioAnalyzer()
    {
        UpdateDisplayDimensions();
    }

    public void SetHeaderCallback(Action redrawHeader, int startRow)
    {
        onRedrawHeader = redrawHeader;
        displayStartRow = startRow;
    }

    public void NextVisualizationMode()
    {
        currentMode = (VisualizationMode)(((int)currentMode + 1) % Enum.GetValues<VisualizationMode>().Length);
    }

    public void SetVisualizationMode(VisualizationMode mode)
    {
        currentMode = mode;
    }

    public string GetModeName()
    {
        return currentMode switch
        {
            VisualizationMode.SpectrumBars => "Spectrum Analyzer",
            VisualizationMode.Oscilloscope => "Oscilloscope",
            VisualizationMode.VuMeter => "VU Meter",
            VisualizationMode.WinampBars => "Winamp Style",
            VisualizationMode.Geiss => "Geiss",
            _ => "Unknown"
        };
    }

    private void UpdateDisplayDimensions()
    {
        if (Console.WindowWidth < 30 || Console.WindowHeight < 15)
        {
            if (NumBands == 0) NumBands = 8;
            return;
        }

        NumBands = Math.Max(8, Math.Min(60, (Console.WindowWidth - 8) / 2));

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
        int channels = format.Channels;
        int bytesPerFrame = bytesPerSample * channels;
        int framesRecorded = bytesRecorded / bytesPerFrame;

        float maxVolume = 0;
        float maxLeft = 0, maxRight = 0;

        for (int frame = 0; frame < framesRecorded; frame++)
        {
            int frameOffset = frame * bytesPerFrame;

            // Read left channel
            float left = format.BitsPerSample switch
            {
                16 => BitConverter.ToInt16(buffer, frameOffset) / 32768f,
                32 => BitConverter.ToSingle(buffer, frameOffset),
                _ => 0
            };

            // Read right channel (or duplicate mono)
            float right = channels >= 2
                ? format.BitsPerSample switch
                {
                    16 => BitConverter.ToInt16(buffer, frameOffset + bytesPerSample) / 32768f,
                    32 => BitConverter.ToSingle(buffer, frameOffset + bytesPerSample),
                    _ => 0
                }
                : left;

            float mono = (left + right) / 2;

            maxVolume = Math.Max(maxVolume, Math.Abs(mono));
            maxLeft = Math.Max(maxLeft, Math.Abs(left));
            maxRight = Math.Max(maxRight, Math.Abs(right));
            instantEnergy += mono * mono;

            // Store waveform for oscilloscope
            waveformBuffer[waveformPosition] = mono;
            waveformPosition = (waveformPosition + 1) % WaveformSize;

            // FFT buffer
            if (bufferPosition < FftLength)
            {
                fftBuffer[bufferPosition].X = mono;
                fftBuffer[bufferPosition].Y = 0;
                bufferPosition++;
            }
        }

        // Update VU meters with smoothing
        leftChannel = leftChannel * 0.7f + maxLeft * 0.3f;
        rightChannel = rightChannel * 0.7f + maxRight * 0.3f;

        // Update VU peaks
        if (maxLeft > leftPeak) leftPeak = maxLeft;
        else leftPeak *= 0.95f;

        if (maxRight > rightPeak) rightPeak = maxRight;
        else rightPeak *= 0.95f;

        // Peak hold for VU
        UpdateVuPeakHold(ref leftPeakHold, ref leftPeakHoldTime, maxLeft);
        UpdateVuPeakHold(ref rightPeakHold, ref rightPeakHoldTime, maxRight);

        if (bufferPosition >= FftLength)
        {
            AnalyzeFrequencies(format.SampleRate);
            bufferPosition = 0;
        }

        double avgEnergy = framesRecorded > 0 ? Math.Sqrt(instantEnergy / framesRecorded) : 0;
        DetectBeat(avgEnergy);
        instantEnergy = 0;

        if ((DateTime.Now - lastUpdate).TotalMilliseconds >= UpdateIntervalMs)
        {
            // Copy waveform for display
            Array.Copy(waveformBuffer, displayWaveform, WaveformSize);

            DisplayVisualization(maxVolume);
            lastUpdate = DateTime.Now;

            if (beatFlashFrames > 0) beatFlashFrames--;
        }
    }

    private void UpdateVuPeakHold(ref float peakHold, ref int holdTime, float current)
    {
        if (current > peakHold)
        {
            peakHold = current;
            holdTime = 0;
        }
        else
        {
            holdTime++;
            if (holdTime > 30) // Hold for ~1.5 seconds
                peakHold = Math.Max(0, peakHold - 0.02f);
        }
    }

    private void AnalyzeFrequencies(int sampleRate)
    {
        for (int i = 0; i < FftLength; i++)
        {
            double window = 0.54 - 0.46 * Math.Cos(2 * Math.PI * i / (FftLength - 1));
            fftBuffer[i].X *= (float)window;
        }

        FastFourierTransform.FFT(true, (int)Math.Log(FftLength, 2), fftBuffer);

        var bandRanges = CreateFrequencyBands(sampleRate);

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
            smoothedMagnitudes[b] = smoothedMagnitudes[b] * SmoothingFactor + bandMagnitudes[b] * (1 - SmoothingFactor);
            UpdatePeakHold(b);

            if (smoothedMagnitudes[b] > maxMagnitudeEver)
                maxMagnitudeEver = smoothedMagnitudes[b];
        }

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
                peakHold[bandIndex] = Math.Max(0, peakHold[bandIndex] - peakHold[bandIndex] * PeakFallRate);
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

        if (energy > avgEnergy * beatThreshold &&
            energy > 0.01 &&
            (now - lastBeatTime).TotalMilliseconds > MinBeatInterval)
        {
            beatTimes.Enqueue(now);
            lastBeatTime = now;
            beatFlashFrames = 3;

            // Spawn a beat circle if enabled
            if (showBeatCircles && currentMode == VisualizationMode.Geiss)
            {
                SpawnBeatCircle();
            }

            while (beatTimes.Count > 0 && (now - beatTimes.Peek()).TotalSeconds > 8)
                beatTimes.Dequeue();

            CalculateBPM();
        }
    }

    private void CalculateBPM()
    {
        if (beatTimes.Count < 2) return;

        var recentBeats = beatTimes.TakeLast(Math.Min(BPMHistorySize + 1, beatTimes.Count)).ToList();
        if (recentBeats.Count < 2) return;

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

    private void SpawnBeatCircle()
    {
        // Pick a random color for the circle
        ConsoleColor[] colors = [ConsoleColor.Cyan, ConsoleColor.Magenta, ConsoleColor.Yellow,
                                  ConsoleColor.Green, ConsoleColor.Red, ConsoleColor.Blue];
        var color = colors[Random.Shared.Next(colors.Length)];

        // Calculate max radius based on bass intensity
        double maxRadius = 0.3 + geissBassIntensity * 0.4;
        maxRadius = Math.Clamp(maxRadius, 0.3, 0.7);

        beatCircles.Add((radius: 0.02, maxRadius: maxRadius, age: 0, color: color));

        // Limit number of active circles
        while (beatCircles.Count > 5)
            beatCircles.RemoveAt(0);
    }

    private void UpdateBeatCircles()
    {
        // Update and age all circles
        for (int i = beatCircles.Count - 1; i >= 0; i--)
        {
            var circle = beatCircles[i];
            double newRadius = circle.radius + 0.03; // Expansion speed
            int newAge = circle.age + 1;

            if (newRadius > circle.maxRadius || newAge > 30)
            {
                beatCircles.RemoveAt(i);
            }
            else
            {
                beatCircles[i] = (newRadius, circle.maxRadius, newAge, circle.color);
            }
        }
    }

    private void DisplayVisualization(float volume)
    {
        try
        {
            if (Console.WindowWidth < 30 || Console.WindowHeight < 15) return;

            if (Console.WindowWidth != lastTerminalWidth || Console.WindowHeight != lastTerminalHeight)
            {
                UpdateDisplayDimensions();
                RedrawHeader();
            }

            Console.SetCursorPosition(0, displayStartRow);
            int termWidth = Console.WindowWidth;

            DisplayVolumeInfo(volume, termWidth);

            switch (currentMode)
            {
                case VisualizationMode.SpectrumBars:
                    DisplayVolumeBar(volume, termWidth);
                    DisplayFrequencyBars(termWidth);
                    DisplayFrequencyLabels(termWidth);
                    break;
                case VisualizationMode.Oscilloscope:
                    DisplayOscilloscope(termWidth);
                    break;
                case VisualizationMode.VuMeter:
                    DisplayVuMeter(termWidth);
                    break;
                case VisualizationMode.WinampBars:
                    DisplayWinampBars(termWidth);
                    break;
                case VisualizationMode.Geiss:
                    DisplayGeiss(termWidth);
                    break;
            }
        }
        catch (Exception)
        {
            // Silently ignore display errors
        }
    }

    private void RedrawHeader()
    {
        Console.Clear();

        if (onRedrawHeader != null)
        {
            onRedrawHeader();
        }
        else
        {
            Console.SetCursorPosition(0, 0);
            int width = Console.WindowWidth;
            string title = " AUDIO ANALYZER - Real-time Frequency Spectrum ";
            int padding = Math.Max(0, (width - title.Length - 2) / 2);
            Console.WriteLine("╔" + new string('═', width - 2) + "╗");
            Console.WriteLine("║" + new string(' ', padding) + title + new string(' ', width - padding - title.Length - 2) + "║");
            Console.WriteLine("╚" + new string('═', width - 2) + "╝");
            Console.WriteLine("\nPress H for help, D to change device, ESC to quit\n");
        }
    }

    private void DisplayVolumeInfo(float volume, int termWidth)
    {
        double db = 20 * Math.Log10(Math.Max(volume, 0.00001));
        string bpmDisplay = currentBPM > 0 ? $" | BPM: {currentBPM:F0}" : "";
        string sensitivityDisplay = $" | Beat: {beatThreshold:F1} (+/-)";

        // Beat indicator
        string beatIndicator = beatFlashFrames > 0 ? " *BEAT*" : "";
        if (beatFlashFrames > 0)
            Console.ForegroundColor = ConsoleColor.Red;

        Console.WriteLine($"Volume: {volume * 100:F1}% ({db:F1} dB){bpmDisplay}{sensitivityDisplay}{beatIndicator}".PadRight(termWidth));
        Console.ResetColor();

        // Second line with mode info
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"Mode: {GetModeName()} (V) | S=Save | H=Help".PadRight(termWidth));
        Console.ResetColor();
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

    // ==================== OSCILLOSCOPE ====================
    private void DisplayOscilloscope(int termWidth)
    {
        int height = Math.Max(10, Math.Min(25, Console.WindowHeight - 12));
        int width = Math.Min(termWidth - 4, WaveformSize);
        int centerY = height / 2;

        Console.WriteLine();

        // Draw oscilloscope frame
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("  ┌" + new string('─', width) + "┐");
        Console.ResetColor();

        char[,] screen = new char[height, width];
        ConsoleColor[,] colors = new ConsoleColor[height, width];

        // Initialize with spaces
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                screen[y, x] = ' ';
                colors[y, x] = ConsoleColor.Gray;
            }

        // Draw center line
        for (int x = 0; x < width; x++)
        {
            screen[centerY, x] = '·';
            colors[centerY, x] = ConsoleColor.DarkGray;
        }

        // Draw waveform
        int step = Math.Max(1, WaveformSize / width);
        int prevY = centerY;

        for (int x = 0; x < width; x++)
        {
            int sampleIndex = (waveformPosition + x * step) % WaveformSize;
            float sample = displayWaveform[sampleIndex];

            int y = centerY - (int)(sample * (height / 2 - 1));
            y = Math.Clamp(y, 0, height - 1);

            // Draw line between previous and current point
            int minY = Math.Min(prevY, y);
            int maxY = Math.Max(prevY, y);

            for (int lineY = minY; lineY <= maxY; lineY++)
            {
                screen[lineY, x] = '█';
                colors[lineY, x] = GetOscilloscopeColor(lineY, centerY, height);
            }

            prevY = y;
        }

        // Render screen
        for (int y = 0; y < height; y++)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("  │");
            for (int x = 0; x < width; x++)
            {
                Console.ForegroundColor = colors[y, x];
                Console.Write(screen[y, x]);
            }
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("│");
        }

        Console.WriteLine("  └" + new string('─', width) + "┘");
        Console.ResetColor();
        Console.WriteLine("\n  Waveform Display - Shows audio amplitude over time".PadRight(termWidth));
    }

    private ConsoleColor GetOscilloscopeColor(int y, int centerY, int height)
    {
        double distance = Math.Abs(y - centerY) / (double)(height / 2);
        return distance switch
        {
            >= 0.8 => ConsoleColor.Red,
            >= 0.6 => ConsoleColor.Yellow,
            >= 0.4 => ConsoleColor.Green,
            _ => ConsoleColor.Cyan
        };
    }

    // ==================== VU METER ====================
    private void DisplayVuMeter(int termWidth)
    {
        int meterWidth = Math.Min(60, termWidth - 20);
        Console.WriteLine();
        Console.WriteLine();

        // Left channel
        DrawVuMeterChannel("  L ", leftChannel, leftPeakHold, meterWidth);
        Console.WriteLine();

        // Right channel
        DrawVuMeterChannel("  R ", rightChannel, rightPeakHold, meterWidth);
        Console.WriteLine();
        Console.WriteLine();

        // Draw scale
        Console.Write("    ");
        for (int i = 0; i <= 10; i++)
        {
            int pos = (int)(i * meterWidth / 10.0);
            string label = (i * 10).ToString();
            Console.Write(label.PadRight(meterWidth / 10));
        }
        Console.WriteLine();

        // Draw dB scale
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write("    ");
        string[] dbLabels = { "-∞", "-40", "-30", "-20", "-10", "-6", "-3", "0" };
        int labelSpacing = meterWidth / (dbLabels.Length - 1);
        for (int i = 0; i < dbLabels.Length; i++)
        {
            Console.Write(dbLabels[i].PadRight(labelSpacing));
        }
        Console.ResetColor();
        Console.WriteLine();

        // Stereo balance indicator
        Console.WriteLine();
        float balance = (rightChannel - leftChannel) / Math.Max(0.001f, leftChannel + rightChannel);
        int balancePos = (int)((balance + 1) / 2 * meterWidth);
        Console.Write("  BAL ");
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write(new string('─', balancePos));
        Console.ForegroundColor = ConsoleColor.White;
        Console.Write("●");
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine(new string('─', meterWidth - balancePos - 1));
        Console.Write("      L");
        Console.Write(new string(' ', meterWidth / 2 - 2));
        Console.Write("C");
        Console.Write(new string(' ', meterWidth / 2 - 2));
        Console.WriteLine("R");
        Console.ResetColor();

        Console.WriteLine("\n  Classic VU Meter - Shows channel levels".PadRight(termWidth));
    }

    private void DrawVuMeterChannel(string label, float level, float peakHold, int width)
    {
        Console.Write(label);
        Console.Write("[");

        int barLength = (int)(level * width);
        int peakPos = (int)(peakHold * width);

        for (int i = 0; i < width; i++)
        {
            if (i == peakPos && peakPos > 0)
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("│");
            }
            else if (i < barLength)
            {
                SetVuColor((double)i / width);
                Console.Write("█");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write("░");
            }
            Console.ResetColor();
        }

        Console.Write("]");

        // Show dB value
        double db = 20 * Math.Log10(Math.Max(level, 0.00001));
        Console.Write($" {db:F1} dB");
    }

    private void SetVuColor(double position)
    {
        Console.ForegroundColor = position switch
        {
            >= 0.9 => ConsoleColor.Red,
            >= 0.75 => ConsoleColor.Yellow,
            _ => ConsoleColor.Green
        };
    }

    // ==================== WINAMP STYLE BARS ====================
    private void DisplayWinampBars(int termWidth)
    {
        int barHeight = Math.Max(10, Math.Min(20, Console.WindowHeight - 14));
        int numBars = Math.Min(NumBands, (termWidth - 4) / 3);

        Console.WriteLine();

        // Winamp uses thin bars with gaps
        string[] barChars = { " ", "▁", "▂", "▃", "▄", "▅", "▆", "▇", "█" };

        for (int row = barHeight; row >= 1; row--)
        {
            Console.Write("  ");

            for (int band = 0; band < numBars; band++)
            {
                double gain = targetMaxMagnitude > 0.0001 ? 1.0 / targetMaxMagnitude : 1000;
                gain = Math.Min(gain, 1000);

                double normalizedMag = Math.Min(smoothedMagnitudes[band] * gain * 0.8, 1.0);
                int height = (int)(normalizedMag * barHeight);

                double normalizedPeak = Math.Min(peakHold[band] * gain * 0.8, 1.0);
                int peakH = (int)(normalizedPeak * barHeight);

                if (row == peakH && peakH > 0)
                {
                    // Peak dot
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write("▀▀");
                }
                else if (height >= row)
                {
                    // Colored bar - Winamp gradient (green -> yellow -> red from bottom to top)
                    SetWinampColor(row, barHeight);
                    Console.Write("██");
                }
                else
                {
                    Console.Write("  ");
                }

                Console.ResetColor();
                Console.Write(" "); // Gap between bars
            }

            Console.WriteLine();
        }

        // Draw baseline
        Console.Write("  ");
        Console.ForegroundColor = ConsoleColor.DarkGray;
        for (int band = 0; band < numBars; band++)
        {
            Console.Write("══ ");
        }
        Console.ResetColor();
        Console.WriteLine();

        Console.WriteLine("\n  Winamp Style - Classic music player visualization".PadRight(termWidth));
    }

    private void SetWinampColor(int row, int barHeight)
    {
        // Winamp uses green at bottom, yellow in middle, red at top
        double position = (double)row / barHeight;
        Console.ForegroundColor = position switch
        {
            >= 0.85 => ConsoleColor.Red,
            >= 0.7 => ConsoleColor.DarkYellow,
            >= 0.5 => ConsoleColor.Yellow,
            >= 0.3 => ConsoleColor.Green,
            _ => ConsoleColor.DarkGreen
        };
    }

    // ==================== GEISS (Psychedelic) ====================
    private void DisplayGeiss(int termWidth)
    {
        int height = Math.Max(12, Math.Min(25, Console.WindowHeight - 12));
        int width = Math.Min(termWidth - 4, 100);

        // Update beat circles
        UpdateBeatCircles();

        // Update Geiss animation state
        geissPhase += 0.15;
        geissColorPhase += 0.08;

        // Calculate bass and treble intensity from frequency bands
        if (smoothedMagnitudes.Length > 0)
        {
            double gain = targetMaxMagnitude > 0.0001 ? 1.0 / targetMaxMagnitude : 1000;
            gain = Math.Min(gain, 1000);

            // Bass = lower 1/4 of bands
            int bassEnd = Math.Max(1, smoothedMagnitudes.Length / 4);
            double bassSum = 0;
            for (int i = 0; i < bassEnd; i++)
                bassSum += smoothedMagnitudes[i] * gain;
            geissBassIntensity = geissBassIntensity * 0.7 + (bassSum / bassEnd) * 0.3;

            // Treble = upper 1/4 of bands
            int trebleStart = smoothedMagnitudes.Length * 3 / 4;
            double trebleSum = 0;
            for (int i = trebleStart; i < smoothedMagnitudes.Length; i++)
                trebleSum += smoothedMagnitudes[i] * gain;
            geissTrebleIntensity = geissTrebleIntensity * 0.7 + (trebleSum / (smoothedMagnitudes.Length - trebleStart)) * 0.3;
        }

        // Plasma characters from sparse to dense
        char[] plasmaChars = [' ', '·', ':', ';', '+', '*', '#', '@', '█'];

        // Pre-calculate plasma values and colors for the frame
        var lineChars = new char[width];
        var lineColors = new ConsoleColor[width];

        Console.Write(new string(' ', termWidth - 1));
        Console.WriteLine();

        for (int y = 0; y < height; y++)
        {
            // Build the line
            for (int x = 0; x < width; x++)
            {
                double nx = (double)x / width;
                double ny = (double)y / height;

                // Check if this point is on a beat circle outline
                bool onCircle = false;
                ConsoleColor circleColor = ConsoleColor.White;

                if (showBeatCircles)
                {
                    // Adjust for aspect ratio (console chars are taller than wide)
                    double aspectRatio = 2.0;
                    double distFromCenter = Math.Sqrt((nx - 0.5) * (nx - 0.5) + ((ny - 0.5) / aspectRatio) * ((ny - 0.5) / aspectRatio));

                    foreach (var circle in beatCircles)
                    {
                        double thickness = 0.02 + (1.0 - (double)circle.age / 30) * 0.01;
                        if (Math.Abs(distFromCenter - circle.radius) < thickness)
                        {
                            onCircle = true;
                            circleColor = circle.color;
                            break;
                        }
                    }
                }

                if (onCircle)
                {
                    lineChars[x] = '○';
                    lineColors[x] = circleColor;
                }
                else
                {
                    // Base plasma waves
                    double v1 = Math.Sin(nx * 10 + geissPhase);
                    double v2 = Math.Sin(ny * 8 + geissPhase * 0.7);
                    double v3 = Math.Sin((nx + ny) * 6 + geissPhase * 1.3);
                    double v4 = Math.Sin(Math.Sqrt((nx - 0.5) * (nx - 0.5) + (ny - 0.5) * (ny - 0.5)) * 12 + geissPhase);

                    double plasma = (v1 + v2 + v3 + v4) / 4.0;

                    // Bass ripple
                    double distFromCenterPlasma = Math.Sqrt((nx - 0.5) * (nx - 0.5) + (ny - 0.5) * (ny - 0.5));
                    plasma += Math.Sin(distFromCenterPlasma * 20 - geissPhase * 2) * geissBassIntensity * 0.5;

                    // Treble shimmer
                    plasma += Math.Sin(nx * 30 + ny * 30 + geissPhase * 3) * geissTrebleIntensity * 0.3;

                    // Beat pulse
                    if (beatFlashFrames > 0)
                        plasma += 0.3;

                    // Normalize
                    plasma = Math.Clamp((plasma + 1.5) / 3.0, 0, 1);

                    lineChars[x] = plasmaChars[(int)(plasma * (plasmaChars.Length - 1))];

                    double hue = ((nx + ny + geissColorPhase) + plasma * 0.3) % 1.0;
                    lineColors[x] = GetGeissColor(hue, plasma);
                }
            }

            // Write line with padding
            Console.Write("  ");
            for (int x = 0; x < width; x++)
            {
                Console.ForegroundColor = lineColors[x];
                Console.Write(lineChars[x]);
            }
            Console.ResetColor();

            // Pad to end of line to clear any artifacts
            int remaining = termWidth - width - 3;
            if (remaining > 0)
                Console.Write(new string(' ', remaining));
            Console.WriteLine();
        }

        // Status line
        string status = $"  Geiss - Psychedelic | Bass: {geissBassIntensity:F2} | Treble: {geissTrebleIntensity:F2}";
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write(status.PadRight(termWidth - 1));
        Console.ResetColor();
        Console.WriteLine();
    }

    private static ConsoleColor GetGeissColor(double hue, double intensity)
    {
        // More saturated colors at higher intensity
        if (intensity < 0.2)
            return ConsoleColor.DarkBlue;

        // Map hue to console colors in a rainbow pattern
        int colorIndex = (int)(hue * 12) % 12;
        return colorIndex switch
        {
            0 => ConsoleColor.Red,
            1 => ConsoleColor.DarkYellow,
            2 => ConsoleColor.Yellow,
            3 => ConsoleColor.Green,
            4 => ConsoleColor.Cyan,
            5 => ConsoleColor.Blue,
            6 => ConsoleColor.DarkBlue,
            7 => ConsoleColor.Magenta,
            8 => ConsoleColor.DarkMagenta,
            9 => ConsoleColor.Red,
            10 => ConsoleColor.DarkRed,
            _ => ConsoleColor.White
        };
    }

    // ==================== SPECTRUM BARS (Original) ====================
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
                Console.Write(label.PadRight(Math.Min(4, labelInterval * 2))[..Math.Min(label.Length + 1, 2)]);
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
        double position = (double)row / barHeight;
        Console.ForegroundColor = position switch
        {
            <= 0.25 => ConsoleColor.Red,
            <= 0.4 => ConsoleColor.Magenta,
            <= 0.55 => ConsoleColor.Yellow,
            <= 0.7 => ConsoleColor.Green,
            <= 0.85 => ConsoleColor.Cyan,
            _ => ConsoleColor.Blue
        };
    }
}
