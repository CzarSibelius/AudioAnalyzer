namespace AudioAnalyzer.Visualizers;

/// <summary>Runtime state for <see cref="StarfieldLayer"/> — star field, sort scratch, drift, and RNG for fixed seeds.</summary>
public sealed class StarfieldLayerState
{
    /// <summary>Backing storage for stars (only the first <see cref="ActiveCount"/> entries are used).</summary>
    public StarfieldStar[] Stars { get; } = new StarfieldStar[StarfieldLayer.MaxStarHardCap];

    /// <summary>Indices 0..ActiveCount-1 sorted by Z descending for painter's order.</summary>
    public int[] SortScratch { get; } = new int[StarfieldLayer.MaxStarHardCap];

    /// <summary>Number of active stars (clamped count).</summary>
    public int ActiveCount { get; set; }

    /// <summary>Accumulated view-center drift in cells (see <see cref="StarfieldSettings.CenterDriftX"/>).</summary>
    public double DriftAccumX { get; set; }

    /// <summary>Accumulated view-center drift in cells.</summary>
    public double DriftAccumY { get; set; }

    /// <summary>Last layer-local width used to detect resize.</summary>
    public int LastWidth { get; set; } = -1;

    /// <summary>Last layer-local height used to detect resize.</summary>
    public int LastHeight { get; set; } = -1;

    /// <summary>Last applied star count.</summary>
    public int LastStarCount { get; set; } = -1;

    /// <summary>Last <see cref="StarfieldSettings.FixedRandomSeed"/> used when stars were (re)initialized.</summary>
    public int LastSpawnFixedSeed { get; set; } = int.MinValue;

    /// <summary>Last <see cref="StarfieldSettings.FixedRandomSeed"/> applied to <see cref="_fixedRng"/>.</summary>
    public int LastFixedRandomSeed { get; set; } = int.MinValue;

    private Random? _fixedRng;

    /// <summary>Replaces the fixed-seed RNG so a new field spawn sequence matches the first initialization for the same <paramref name="seed"/>.</summary>
    public void RecreateFixedRandom(int seed)
    {
        if (seed < 0)
        {
            _fixedRng = null;
            LastFixedRandomSeed = int.MinValue;
            return;
        }

        _fixedRng = new Random(seed);
        LastFixedRandomSeed = seed;
    }

    /// <summary>Returns a dedicated RNG when <paramref name="fixedRandomSeed"/> is non-negative; otherwise null (use <see cref="Random.Shared"/>).</summary>
    public Random? GetFixedRng(int fixedRandomSeed)
    {
        if (fixedRandomSeed < 0)
        {
            _fixedRng = null;
            LastFixedRandomSeed = int.MinValue;
            return null;
        }

        if (_fixedRng == null || LastFixedRandomSeed != fixedRandomSeed)
        {
            _fixedRng = new Random(fixedRandomSeed);
            LastFixedRandomSeed = fixedRandomSeed;
        }

        return _fixedRng;
    }
}
