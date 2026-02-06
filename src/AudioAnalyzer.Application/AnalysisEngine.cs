using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Application.Fft;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Application;

public sealed class AnalysisEngine
{
    private const int FftLength = 8192;
    private const int WaveformSize = 512;
    private const int UpdateIntervalMs = 50;
    private const double SmoothingFactor = 0.7;
    private const int PeakHoldFrames = 20;
    private const double PeakFallRate = 0.08;
    private const int EnergyHistorySize = 20;
    private const int MinBeatInterval = 250;
    private const int BPMHistorySize = 8;
    private static readonly int FftLog2N = (int)Math.Log2(FftLength);

    private readonly ComplexFloat[] _fftBuffer = new ComplexFloat[FftLength];
    private int _bufferPosition;
    private readonly float[] _waveformBuffer = new float[WaveformSize];
    private int _waveformPosition;
    private readonly float[] _displayWaveform = new float[WaveformSize];

    private DateTime _lastUpdate = DateTime.Now;
    private int _lastTerminalWidth;
    private int _lastTerminalHeight;
    private int _displayStartRow = 6;
    private Action? _onRedrawHeader;

    private VisualizationMode _currentMode = VisualizationMode.SpectrumBars;
    private int _numBands;
    private double[] _bandMagnitudes = Array.Empty<double>();
    private double[] _smoothedMagnitudes = Array.Empty<double>();
    private double[] _peakHold = Array.Empty<double>();
    private int[] _peakHoldTime = Array.Empty<int>();

    private double _maxMagnitudeEver = 0.001;
    private double _targetMaxMagnitude = 0.001;

    private float _leftChannel;
    private float _rightChannel;
    private float _leftPeak;
    private float _rightPeak;
    private float _leftPeakHold;
    private float _rightPeakHold;
    private int _leftPeakHoldTime;
    private int _rightPeakHoldTime;

    private readonly Queue<double> _energyHistory = new();
    private readonly Queue<DateTime> _beatTimes = new();
    private double _beatThreshold = 1.3;
    private DateTime _lastBeatTime = DateTime.MinValue;
    private double _currentBpm;
    private double _instantEnergy;
    private int _beatFlashFrames;

    private double _geissPhase;
    private double _geissColorPhase;
    private double _geissBassIntensity;
    private double _geissTrebleIntensity;
    private readonly List<BeatCircle> _beatCircles = new();
    private bool _showBeatCircles = true;

    private readonly IVisualizationRenderer _renderer;
    private readonly IDisplayDimensions _displayDimensions;
    private readonly VisualizationFrame _frame = new();

    public AnalysisEngine(IVisualizationRenderer renderer, IDisplayDimensions displayDimensions)
    {
        _renderer = renderer;
        _displayDimensions = displayDimensions;
        UpdateDisplayDimensions();
    }

    public VisualizationMode CurrentMode => _currentMode;
    public double BeatSensitivity { get => _beatThreshold; set => _beatThreshold = Math.Clamp(value, 0.5, 3.0); }
    public bool ShowBeatCircles { get => _showBeatCircles; set => _showBeatCircles = value; }

    public void SetHeaderCallback(Action? redrawHeader, int startRow)
    {
        _onRedrawHeader = redrawHeader;
        _displayStartRow = startRow;
    }

    public void NextVisualizationMode()
    {
        _currentMode = (VisualizationMode)(((int)_currentMode + 1) % Enum.GetValues<VisualizationMode>().Length);
    }

    public void SetVisualizationMode(VisualizationMode mode)
    {
        _currentMode = mode;
    }

    public string GetModeName()
    {
        return _currentMode switch
        {
            VisualizationMode.SpectrumBars => "Spectrum Analyzer",
            VisualizationMode.Oscilloscope => "Oscilloscope",
            VisualizationMode.VuMeter => "VU Meter",
            VisualizationMode.WinampBars => "Winamp Style",
            VisualizationMode.Geiss => "Geiss",
            _ => "Unknown"
        };
    }

    public void ProcessAudio(byte[] buffer, int bytesRecorded, AudioFormat format)
    {
        int bytesPerFrame = format.BytesPerFrame;
        int framesRecorded = bytesRecorded / bytesPerFrame;
        int bytesPerSample = format.BytesPerSample;
        int channels = format.Channels;

        float maxVolume = 0;
        float maxLeft = 0, maxRight = 0;

        for (int frame = 0; frame < framesRecorded; frame++)
        {
            int frameOffset = frame * bytesPerFrame;
            float left = format.BitsPerSample switch
            {
                16 => BitConverter.ToInt16(buffer, frameOffset) / 32768f,
                32 => BitConverter.ToSingle(buffer, frameOffset),
                _ => 0
            };
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
            _instantEnergy += mono * mono;

            _waveformBuffer[_waveformPosition] = mono;
            _waveformPosition = (_waveformPosition + 1) % WaveformSize;

            if (_bufferPosition < FftLength)
            {
                _fftBuffer[_bufferPosition].X = mono;
                _fftBuffer[_bufferPosition].Y = 0;
                _bufferPosition++;
            }
        }

        _leftChannel = _leftChannel * 0.7f + maxLeft * 0.3f;
        _rightChannel = _rightChannel * 0.7f + maxRight * 0.3f;
        if (maxLeft > _leftPeak) _leftPeak = maxLeft;
        else _leftPeak *= 0.95f;
        if (maxRight > _rightPeak) _rightPeak = maxRight;
        else _rightPeak *= 0.95f;
        UpdateVuPeakHold(ref _leftPeakHold, ref _leftPeakHoldTime, maxLeft);
        UpdateVuPeakHold(ref _rightPeakHold, ref _rightPeakHoldTime, maxRight);

        if (_bufferPosition >= FftLength)
        {
            AnalyzeFrequencies(format.SampleRate);
            _bufferPosition = 0;
        }

        double avgEnergy = framesRecorded > 0 ? Math.Sqrt(_instantEnergy / framesRecorded) : 0;
        DetectBeat(avgEnergy);
        _instantEnergy = 0;

        if ((DateTime.Now - _lastUpdate).TotalMilliseconds >= UpdateIntervalMs)
        {
            Array.Copy(_waveformBuffer, _displayWaveform, WaveformSize);
            int w = _displayDimensions.Width;
            int h = _displayDimensions.Height;
            if (w >= 30 && h >= 15)
            {
                if (w != _lastTerminalWidth || h != _lastTerminalHeight)
                {
                    UpdateDisplayDimensions();
                    _onRedrawHeader?.Invoke();
                }
                if (_currentMode == VisualizationMode.Geiss)
                {
                    UpdateBeatCircles();
                    _geissPhase += 0.15;
                    _geissColorPhase += 0.08;
                    if (_smoothedMagnitudes.Length > 0)
                    {
                        double gain = _targetMaxMagnitude > 0.0001 ? Math.Min(1000, 1.0 / _targetMaxMagnitude) : 1000;
                        int bassEnd = Math.Max(1, _smoothedMagnitudes.Length / 4);
                        double bassSum = 0;
                        for (int i = 0; i < bassEnd; i++) bassSum += _smoothedMagnitudes[i] * gain;
                        _geissBassIntensity = _geissBassIntensity * 0.7 + (bassSum / bassEnd) * 0.3;
                        int trebleStart = _smoothedMagnitudes.Length * 3 / 4;
                        double trebleSum = 0;
                        for (int i = trebleStart; i < _smoothedMagnitudes.Length; i++) trebleSum += _smoothedMagnitudes[i] * gain;
                        _geissTrebleIntensity = _geissTrebleIntensity * 0.7 + (trebleSum / (_smoothedMagnitudes.Length - trebleStart)) * 0.3;
                    }
                }
                FillFrame(maxVolume, w, h);
                try { _renderer.Render(_frame); } catch { }
            }
            _lastUpdate = DateTime.Now;
            if (_beatFlashFrames > 0) _beatFlashFrames--;
        }
    }

    private void FillFrame(float volume, int termWidth, int termHeight)
    {
        _frame.Mode = _currentMode;
        _frame.DisplayStartRow = _displayStartRow;
        _frame.TerminalWidth = termWidth;
        _frame.TerminalHeight = termHeight;
        _frame.Volume = volume;
        _frame.CurrentBpm = _currentBpm;
        _frame.BeatSensitivity = _beatThreshold;
        _frame.BeatFlashActive = _beatFlashFrames > 0;
        _frame.ModeName = GetModeName();
        _frame.NumBands = _numBands;
        _frame.SmoothedMagnitudes = _smoothedMagnitudes;
        _frame.PeakHold = _peakHold;
        _frame.TargetMaxMagnitude = _targetMaxMagnitude;
        _frame.Waveform = _displayWaveform;
        _frame.WaveformPosition = _waveformPosition;
        _frame.WaveformSize = WaveformSize;
        _frame.LeftChannel = _leftChannel;
        _frame.RightChannel = _rightChannel;
        _frame.LeftPeakHold = _leftPeakHold;
        _frame.RightPeakHold = _rightPeakHold;
        _frame.GeissPhase = _geissPhase;
        _frame.GeissColorPhase = _geissColorPhase;
        _frame.GeissBassIntensity = _geissBassIntensity;
        _frame.GeissTrebleIntensity = _geissTrebleIntensity;
        _frame.ShowBeatCircles = _showBeatCircles;
        _frame.BeatCircles = _beatCircles.ToList();
    }

    private void UpdateDisplayDimensions()
    {
        int w = _displayDimensions.Width;
        int h = _displayDimensions.Height;
        if (w < 30 || h < 15)
        {
            if (_numBands == 0) _numBands = 8;
            return;
        }
        _numBands = Math.Max(8, Math.Min(60, (w - 8) / 2));
        if (_bandMagnitudes.Length != _numBands)
        {
            _bandMagnitudes = new double[_numBands];
            _smoothedMagnitudes = new double[_numBands];
            _peakHold = new double[_numBands];
            _peakHoldTime = new int[_numBands];
        }
        _lastTerminalWidth = w;
        _lastTerminalHeight = h;
    }

    private static void UpdateVuPeakHold(ref float peakHold, ref int holdTime, float current)
    {
        if (current > peakHold) { peakHold = current; holdTime = 0; }
        else
        {
            holdTime++;
            if (holdTime > 30) peakHold = Math.Max(0, peakHold - 0.02f);
        }
    }

    private void AnalyzeFrequencies(int sampleRate)
    {
        for (int i = 0; i < FftLength; i++)
        {
            float window = 0.54f - 0.46f * MathF.Cos(2 * MathF.PI * i / (FftLength - 1));
            _fftBuffer[i].X *= window;
        }
        FftHelper.Fft(true, FftLog2N, _fftBuffer);

        var bandRanges = CreateFrequencyBands(sampleRate);
        for (int b = 0; b < _numBands; b++)
        {
            double totalMagnitude = 0;
            int count = 0;
            for (int i = bandRanges[b].start; i < bandRanges[b].end && i < FftLength / 2; i++)
            {
                double magnitude = Math.Sqrt(_fftBuffer[i].X * _fftBuffer[i].X + _fftBuffer[i].Y * _fftBuffer[i].Y);
                totalMagnitude += magnitude;
                count++;
            }
            _bandMagnitudes[b] = count > 0 ? totalMagnitude / count : 0;
            _smoothedMagnitudes[b] = _smoothedMagnitudes[b] * SmoothingFactor + _bandMagnitudes[b] * (1 - SmoothingFactor);
            UpdatePeakHold(b);
            if (_smoothedMagnitudes[b] > _maxMagnitudeEver)
                _maxMagnitudeEver = _smoothedMagnitudes[b];
        }
        _targetMaxMagnitude = _targetMaxMagnitude * 0.95 + _maxMagnitudeEver * 0.05;
    }

    private List<(int start, int end, string label)> CreateFrequencyBands(int sampleRate)
    {
        var bandRanges = new List<(int start, int end, string label)>();
        const double minFreq = 20, maxFreq = 20000;
        double logMin = Math.Log10(minFreq), logMax = Math.Log10(maxFreq);
        double step = (logMax - logMin) / _numBands;
        for (int band = 0; band < _numBands; band++)
        {
            double logStart = logMin + band * step, logEnd = logMin + (band + 1) * step;
            int startFreq = (int)Math.Pow(10, logStart), endFreq = (int)Math.Pow(10, logEnd);
            int startBin = (int)(startFreq * FftLength / (double)sampleRate);
            int endBin = (int)(endFreq * FftLength / (double)sampleRate);
            string label = startFreq < 1000 ? $"{startFreq}" : $"{startFreq / 1000}k";
            bandRanges.Add((startBin, endBin, label));
        }
        return bandRanges;
    }

    private void UpdatePeakHold(int bandIndex)
    {
        if (_smoothedMagnitudes[bandIndex] > _peakHold[bandIndex])
        {
            _peakHold[bandIndex] = _smoothedMagnitudes[bandIndex];
            _peakHoldTime[bandIndex] = 0;
        }
        else
        {
            _peakHoldTime[bandIndex]++;
            if (_peakHoldTime[bandIndex] > PeakHoldFrames)
                _peakHold[bandIndex] = Math.Max(0, _peakHold[bandIndex] - _peakHold[bandIndex] * PeakFallRate);
        }
    }

    private void DetectBeat(double energy)
    {
        _energyHistory.Enqueue(energy);
        if (_energyHistory.Count > EnergyHistorySize) _energyHistory.Dequeue();
        if (_energyHistory.Count < EnergyHistorySize / 2) return;

        double avgEnergy = _energyHistory.Take(_energyHistory.Count - 1).Average();
        DateTime now = DateTime.Now;
        if (energy > avgEnergy * _beatThreshold && energy > 0.01 &&
            (now - _lastBeatTime).TotalMilliseconds > MinBeatInterval)
        {
            _beatTimes.Enqueue(now);
            _lastBeatTime = now;
            _beatFlashFrames = 3;
            if (_showBeatCircles && _currentMode == VisualizationMode.Geiss)
                SpawnBeatCircle();
            while (_beatTimes.Count > 0 && (now - _beatTimes.Peek()).TotalSeconds > 8)
                _beatTimes.Dequeue();
            CalculateBPM();
        }
    }

    private void CalculateBPM()
    {
        if (_beatTimes.Count < 2) return;
        var recentBeats = _beatTimes.TakeLast(Math.Min(BPMHistorySize + 1, _beatTimes.Count)).ToList();
        if (recentBeats.Count < 2) return;
        var intervals = new List<double>();
        for (int i = 1; i < recentBeats.Count; i++)
        {
            double intervalMs = (recentBeats[i] - recentBeats[i - 1]).TotalMilliseconds;
            if (intervalMs >= 250 && intervalMs <= 2000) intervals.Add(intervalMs);
        }
        if (intervals.Count > 0)
        {
            double avgInterval = intervals.Average();
            double newBPM = 60000.0 / avgInterval;
            _currentBpm = _currentBpm == 0 ? newBPM : _currentBpm * 0.8 + newBPM * 0.2;
        }
    }

    private void SpawnBeatCircle()
    {
        int colorIndex = Random.Shared.Next(6);
        double maxRadius = Math.Clamp(0.3 + _geissBassIntensity * 0.4, 0.3, 0.7);
        _beatCircles.Add(new BeatCircle(0.02, maxRadius, 0, colorIndex));
        while (_beatCircles.Count > 5) _beatCircles.RemoveAt(0);
    }

    private void UpdateBeatCircles()
    {
        for (int i = _beatCircles.Count - 1; i >= 0; i--)
        {
            var c = _beatCircles[i];
            double newRadius = c.Radius + 0.03;
            int newAge = c.Age + 1;
            if (newRadius > c.MaxRadius || newAge > 30)
                _beatCircles.RemoveAt(i);
            else
                _beatCircles[i] = new BeatCircle(newRadius, c.MaxRadius, newAge, c.ColorIndex);
        }
    }
}
