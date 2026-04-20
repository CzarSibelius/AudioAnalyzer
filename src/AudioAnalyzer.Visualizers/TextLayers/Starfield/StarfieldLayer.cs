using AudioAnalyzer.Application;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Visualizers;

/// <summary>Pseudo-3D starfield: perspective projection toward the viewer (ADR-0082).</summary>
public sealed class StarfieldLayer : TextLayerRendererBase, ITextLayerRenderer<StarfieldLayerState>
{
    /// <summary>Hard cap on <see cref="StarfieldSettings.StarCount"/> regardless of hand-edited JSON (ADR-0030).</summary>
    public const int MaxStarHardCap = 1000;

    private const string DefaultGlyphFallback = " .'`+*";

    private readonly ITextLayerStateStore<StarfieldLayerState> _stateStore;
    private readonly CharsetResolver _charsetResolver;

    /// <summary>Creates the layer with state store and charset resolution.</summary>
    public StarfieldLayer(ITextLayerStateStore<StarfieldLayerState> stateStore, CharsetResolver charsetResolver)
    {
        _stateStore = stateStore ?? throw new ArgumentNullException(nameof(stateStore));
        _charsetResolver = charsetResolver ?? throw new ArgumentNullException(nameof(charsetResolver));
    }

    /// <inheritdoc />
    public override TextLayerType LayerType => TextLayerType.Starfield;

    /// <inheritdoc />
    public override (double Offset, int SnippetIndex) Draw(
        TextLayerSettings layer,
        ref (double Offset, int SnippetIndex) state,
        TextLayerDrawContext ctx)
    {
        int w = ctx.Width;
        int h = ctx.Height;
        if (w <= 0 || h <= 0)
        {
            return state;
        }

        var s = layer.GetCustom<StarfieldSettings>() ?? new StarfieldSettings();
        int activeCount = ClampStarCount(s.StarCount);
        NormalizeGeometry(s, out double zNear, out double zFar, out double focal, out double spreadX, out double spreadY, out double cellAspect);

        var st = _stateStore.GetState(ctx.LayerIndex);
        st.ActiveCount = activeCount;

        bool fullReinit = st.LastWidth != w
            || st.LastHeight != h
            || st.LastStarCount != activeCount
            || st.LastSpawnFixedSeed != s.FixedRandomSeed;

        if (fullReinit)
        {
            st.RecreateFixedRandom(s.FixedRandomSeed);
            st.DriftAccumX = 0;
            st.DriftAccumY = 0;
            st.LastWidth = w;
            st.LastHeight = h;
            st.LastStarCount = activeCount;
            st.LastSpawnFixedSeed = s.FixedRandomSeed;
        }

        Random? fixedRng = st.GetFixedRng(s.FixedRandomSeed);
        Random spawnRng = fixedRng ?? Random.Shared;

        string ramp = _charsetResolver.ResolveByIdOrDefault(s.CharsetId, CharsetIds.DensitySoft, DefaultGlyphFallback);
        if (ramp.Length == 0)
        {
            ramp = DefaultGlyphFallback;
        }

        if (fullReinit)
        {
            for (int i = 0; i < activeCount; i++)
            {
                st.Stars[i] = CreateSpawn(spawnRng, spreadX, spreadY, zNear, zFar, ramp.Length);
            }
        }

        double dtScale = DisplayAnimationTiming.ScaleForReference60(ctx.FrameDeltaSeconds);
        bool beat = ctx.Analysis.BeatFlashActive;
        double beatSpeed = s.BeatReaction == StarfieldBeatReaction.SpeedBurst && beat ? 2.0 : 1.0;
        double speed = s.BaseSpeed * layer.SpeedMultiplier * ctx.SpeedBurst * beatSpeed;

        st.DriftAccumX += s.CenterDriftX * dtScale;
        st.DriftAccumY += s.CenterDriftY * dtScale;

        double tumbleDelta = s.TumbleRadiansPerSecond * dtScale * layer.SpeedMultiplier;
        double cosT = Math.Cos(tumbleDelta);
        double sinT = Math.Sin(tumbleDelta);

        double zSpan = zFar - zNear;
        double travelSec = Math.Clamp(s.TravelSeconds, 4.0, 180.0);
        // Depth units per wall second at BaseSpeed 1; multiply by dt in seconds (dtScale/60) for per-frame step.
        double zVelocity = zSpan / travelSec;
        double zStep = zVelocity * speed * (dtScale / DisplayAnimationTiming.ReferenceHz);
        if (zStep < 1e-9)
        {
            zStep = 1e-9;
        }

        for (int i = 0; i < activeCount; i++)
        {
            StarfieldStar star = st.Stars[i];
            double xr = star.X * cosT - star.Y * sinT;
            double yr = star.X * sinT + star.Y * cosT;
            double newZ = star.Z - zStep;
            if (newZ <= zNear)
            {
                st.Stars[i] = CreateSpawn(spawnRng, spreadX, spreadY, zNear, zFar, ramp.Length);
            }
            else
            {
                st.Stars[i] = star with { X = xr, Y = yr, Z = newZ };
            }
        }

        double cx = w * 0.5 + st.DriftAccumX + s.ViewCenterOffsetX;
        double cy = h * 0.5 + st.DriftAccumY + s.ViewCenterOffsetY;

        int[] order = st.SortScratch;
        for (int i = 0; i < activeCount; i++)
        {
            order[i] = i;
        }

        Array.Sort(order, 0, activeCount, Comparer<int>.Create((ia, ib) => st.Stars[ib].Z.CompareTo(st.Stars[ia].Z)));

        int paletteCount = ctx.Palette.Count;
        if (paletteCount <= 0)
        {
            return state;
        }

        int flashOff = s.BeatReaction == StarfieldBeatReaction.Flash && beat ? 1 : 0;

        for (int k = 0; k < activeCount; k++)
        {
            StarfieldStar star = st.Stars[order[k]];
            var (sx, sy) = StarfieldProjection.Project(
                star.X,
                star.Y,
                star.Z,
                focal,
                cellAspect,
                cx,
                cy);

            int ix = (int)Math.Floor(sx + 0.5);
            int iy = (int)Math.Floor(sy + 0.5);
            if ((uint)ix >= (uint)w || (uint)iy >= (uint)h)
            {
                continue;
            }

            char ch = ramp[star.GlyphIndex % ramp.Length];
            PaletteColor color = PickColor(s, layer, ctx.Palette, star.Z, zNear, zFar, flashOff);
            ctx.SetLocal(ix, iy, ch, color);
        }

        return state;
    }

    /// <summary>Clamps requested star count for tests and documentation.</summary>
    public static int ClampStarCount(int requested) => Math.Clamp(requested, 1, MaxStarHardCap);

    private static void NormalizeGeometry(StarfieldSettings s, out double zNear, out double zFar, out double focal, out double spreadX, out double spreadY, out double cellAspect)
    {
        zFar = Math.Max(0.25, s.ZFar);
        zNear = Math.Clamp(s.ZNear, 0.04, zFar - 0.05);
        focal = Math.Clamp(s.FocalLength, 1.0, 400.0);
        spreadX = Math.Clamp(s.SpreadX, 0.1, 50.0);
        spreadY = Math.Clamp(s.SpreadY, 0.1, 50.0);
        cellAspect = Math.Clamp(s.CellAspect, 0.5, 4.0);
    }

    /// <summary>Spawns a star with random XY in the slab and random Z across the depth range so the field has near and far points (continuous travel).</summary>
    private static StarfieldStar CreateSpawn(Random r, double spreadX, double spreadY, double zNear, double zFar, int glyphCount)
    {
        double x = (r.NextDouble() * 2.0 - 1.0) * spreadX;
        double y = (r.NextDouble() * 2.0 - 1.0) * spreadY;
        double zSpan = zFar - zNear;
        double zLo = zNear + Math.Max(1e-6, zSpan * 1e-5);
        double z = zLo + r.NextDouble() * (zFar - zLo);
        int g = glyphCount > 0 ? r.Next(0, glyphCount) : 0;
        return new StarfieldStar(x, y, z, g);
    }

    private static PaletteColor PickColor(
        StarfieldSettings s,
        TextLayerSettings layer,
        IReadOnlyList<PaletteColor> palette,
        double z,
        double zNear,
        double zFar,
        int flashOff)
    {
        int paletteCount = palette.Count;
        int idx;
        if (s.DepthShading == StarfieldDepthShading.Flat)
        {
            idx = layer.ColorIndex;
        }
        else
        {
            double span = zFar - zNear;
            double t = span > 1e-9 ? (z - zNear) / span : 0.5;
            if (t < 0)
            {
                t = 0;
            }

            if (t > 1)
            {
                t = 1;
            }

            int spread = Math.Max(1, paletteCount - 1);
            int off = (int)Math.Round((1.0 - t) * spread);
            idx = layer.ColorIndex + off;
        }

        idx += flashOff;
        idx = ((idx % paletteCount) + paletteCount) % paletteCount;
        return palette[idx];
    }
}
