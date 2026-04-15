using System.Globalization;
using AudioAnalyzer.Application;
using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Visualizers;

/// <summary>Renders mono waveform history as a horizontal strip using the full layer height; prefers decimated overview when present (ADR-0077).</summary>
public sealed class WaveformStripLayer : TextLayerRendererBase, ITextLayerRenderer<NoLayerState>
{
    private readonly ITextLayerStateStore<WaveformStripLayerState>? _columnBucketCache;

    /// <summary>Constructs the layer. When <paramref name="columnBucketCache"/> is provided (DI), column→overview-bucket indices are cached per layer slot while geometry and visible chronology are unchanged; envelope values still come from each frame’s snapshot.</summary>
    public WaveformStripLayer(ITextLayerStateStore<WaveformStripLayerState>? columnBucketCache = null)
    {
        _columnBucketCache = columnBucketCache;
    }

    public override TextLayerType LayerType => TextLayerType.WaveformStrip;

    public override (double Offset, int SnippetIndex) Draw(
        TextLayerSettings layer,
        ref (double Offset, int SnippetIndex) state,
        TextLayerDrawContext ctx)
    {
        int w = ctx.Width;
        int h = ctx.Height;
        var snapshot = ctx.Analysis;
        if (w < 1 || h < 1)
        {
            return state;
        }

        var s = layer.GetCustom<WaveformStripSettings>() ?? new WaveformStripSettings();
        ComputeWaveformBandLayout(h, s.ShowTimeLabel, s.StereoLayout == WaveformStripStereoLayout.StereoStacked, out int bandTop, out int bandHeight, out int timeLabelRow, out bool stereoStackedActive);
        double gain = Math.Clamp(s.Gain > 0 ? s.Gain : 2.5, 1.0, 10.0);
        bool filled = s.Filled;

        if (stereoStackedActive
            && TryDrawStereoStackedOverview(layer, ref state, ctx, s, w, bandTop, bandHeight, timeLabelRow, gain, filled, snapshot))
        {
            return state;
        }

        int centerY = bandTop + bandHeight / 2;
        if (TryDrawOverviewBand(ref state, ctx, s, w, bandTop, bandHeight, centerY, gain, filled, snapshot, useRightChannel: false))
        {
            DrawOverlays(layer, ctx, s, w, bandTop, bandHeight, timeLabelRow, snapshot, drawTimeLabel: true);
            return state;
        }

        if (snapshot.Waveform is not { Length: > 0 } || snapshot.WaveformSize <= 0)
        {
            return state;
        }

        int prevY = centerY;
        int width = Math.Min(w, snapshot.WaveformSize);
        int step = Math.Max(1, snapshot.WaveformSize / width);
        for (int x = 0; x < width; x++)
        {
            int sampleIndex = (snapshot.WaveformPosition + x * step) % snapshot.WaveformSize;
            float sample = snapshot.Waveform[sampleIndex];
            DrawColumnFromSample(ctx, s, x, sample, gain, filled, ref prevY, bandTop, bandHeight, centerY, snapshot, -1, useRightChannel: false);
        }

        DrawOverlays(layer, ctx, s, w, bandTop, bandHeight, timeLabelRow, snapshot, drawTimeLabel: true);
        return state;
    }

    /// <summary>
    /// Maps a global mono history sample index to a column using the same overview bucket partition as the engine rebuild
    /// and the same column→bucket mapping as <see cref="TryDrawOverviewBand"/> (<see cref="OverviewBucketIndexForColumn"/>).
    /// Pass built newest/oldest and <paramref name="vis"/> from the same snapshot/settings as drawing.
    /// </summary>
    internal static int ColumnForGlobalMonoSample(
        long markGlobal,
        int w,
        long oldestGlobal,
        long newestGlobal,
        int useLen,
        WaveformStripVisibleChronology vis)
    {
        int n = vis.RingSampleCount;
        if (w < 1 || useLen < 2 || n < 2)
        {
            return markGlobal >= oldestGlobal && markGlobal <= newestGlobal ? 0 : -1;
        }

        if (markGlobal < oldestGlobal || markGlobal > newestGlobal)
        {
            return -1;
        }

        long chLong = markGlobal - oldestGlobal;
        int chFull = (int)Math.Clamp(chLong, 0, (long)n - 1);
        if (chFull < vis.OldestVisibleChronologicalIndex)
        {
            return -1;
        }

        int wid = Math.Max(2, vis.WindowSampleCount);
        int chVis = chFull - vis.OldestVisibleChronologicalIndex;
        if (chVis < 0 || chVis > wid - 1)
        {
            return -1;
        }

        int b = WaveformOverviewBucketIndex.FromChronologicalIndex(chFull, n, useLen);
        if (w == 1)
        {
            return 0;
        }

        int xIdeal = (int)((chVis * (long)(w - 1) + (wid - 1) / 2) / (wid - 1));
        xIdeal = Math.Clamp(xIdeal, 0, w - 1);
        if (OverviewBucketIndexForColumn(xIdeal, w, useLen, vis) == b)
        {
            return xIdeal;
        }

        return ColumnForOverviewBucket(b, w, useLen, vis);
    }

    /// <summary>
    /// Column for partition bucket <paramref name="bucketIndex"/> using the same column→chronology→bucket path as
    /// <see cref="OverviewBucketIndexForColumn"/>: prefers exact <c>f(x)==b</c>, else minimum <c>abs(f(x)-b)</c> (tie-break lower <paramref name="x"/>).
    /// </summary>
    internal static int ColumnForOverviewBucket(int bucketIndex, int w, int useLen, WaveformStripVisibleChronology vis)
    {
        int n = vis.RingSampleCount;
        if (w < 1 || useLen < 2 || n < 2 || vis.WindowSampleCount < 2)
        {
            return 0;
        }

        if (w == 1)
        {
            return 0;
        }

        int b = Math.Clamp(bucketIndex, 0, useLen - 1);
        for (int x = 0; x < w; x++)
        {
            if (OverviewBucketIndexForColumn(x, w, useLen, vis) == b)
            {
                return x;
            }
        }

        int bestX = 0;
        int bestDist = int.MaxValue;
        for (int x = 0; x < w; x++)
        {
            int bi = OverviewBucketIndexForColumn(x, w, useLen, vis);
            int d = Math.Abs(bi - b);
            if (d < bestDist || (d == bestDist && x < bestX))
            {
                bestDist = d;
                bestX = x;
            }
        }

        return bestX;
    }

    /// <summary>Maps overview bucket index to column via rounding (legacy; prefer <see cref="ColumnForOverviewBucket"/> for strip alignment).</summary>
    internal static int BucketToColumnX(int bucketIndex, int w, int useLen)
    {
        if (w < 2 || useLen < 2)
        {
            return 0;
        }

        return (int)Math.Round(bucketIndex * (w - 1.0) / (useLen - 1));
    }

    /// <summary>Reserves the top row for the time span label when requested and <paramref name="viewportHeight"/> is at least 2.</summary>
    private static void ComputeWaveformBandLayout(
        int viewportHeight,
        bool showTimeLabel,
        bool wantStereoStacked,
        out int bandTop,
        out int bandHeight,
        out int timeLabelRow,
        out bool stereoStackedActive)
    {
        timeLabelRow = -1;
        int innerTop = 0;
        int innerH = viewportHeight;
        if (showTimeLabel && viewportHeight >= 2)
        {
            timeLabelRow = 0;
            innerTop = 1;
            innerH = viewportHeight - 1;
        }

        stereoStackedActive = wantStereoStacked && innerH >= 4;
        bandTop = innerTop;
        bandHeight = innerH;
    }

    /// <summary>Builds the chronological window used to map columns to overview buckets (trailing <see cref="WaveformStripSettings.FixedVisibleSeconds"/> within the built partition).</summary>
    private static WaveformStripVisibleChronology ComputeVisibleChronology(
        int ringSampleCount,
        double overviewSpanSeconds,
        WaveformStripSettings settings)
    {
        int n = Math.Max(ringSampleCount, 0);
        if (n < 2)
        {
            return new WaveformStripVisibleChronology(2, 2, 0);
        }

        if (!double.IsFinite(overviewSpanSeconds) || overviewSpanSeconds <= 0)
        {
            return new WaveformStripVisibleChronology(n, n, 0);
        }

        double seconds = Math.Clamp(settings.FixedVisibleSeconds, 1.0, 120.0);
        double sampleRate = n / overviewSpanSeconds;
        long win = (long)Math.Round(seconds * sampleRate);
        int windowSamples = (int)Math.Clamp(win, 2, n);
        if (windowSamples >= n)
        {
            return new WaveformStripVisibleChronology(n, n, 0);
        }

        return new WaveformStripVisibleChronology(n, windowSamples, n - windowSamples);
    }

    /// <summary>
    /// Fills or reuses <see cref="WaveformStripLayerState"/> column bucket array when width, bucket count, and visible chronology
    /// match the last draw for this layer slot.
    /// </summary>
    private bool TryGetCachedOverviewColumnBuckets(
        int layerIndex,
        int w,
        int useLen,
        WaveformStripVisibleChronology vis,
        out int[]? columnBuckets)
    {
        columnBuckets = null;
        if (_columnBucketCache is null || w < 1)
        {
            return false;
        }

        WaveformStripLayerState slot = _columnBucketCache.GetState(layerIndex);
        bool match =
            slot.OverviewColumnToBucket is { Length: int len }
            && len == w
            && slot.CachedUseLen == useLen
            && slot.CachedRingSampleCount == vis.RingSampleCount
            && slot.CachedWindowSampleCount == vis.WindowSampleCount
            && slot.CachedOldestVisibleChronologicalIndex == vis.OldestVisibleChronologicalIndex;

        if (!match)
        {
            if (slot.OverviewColumnToBucket is null || slot.OverviewColumnToBucket.Length != w)
            {
                slot.OverviewColumnToBucket = new int[w];
            }

            for (int x = 0; x < w; x++)
            {
                slot.OverviewColumnToBucket[x] = OverviewBucketIndexForColumn(x, w, useLen, vis);
            }

            slot.CachedUseLen = useLen;
            slot.CachedRingSampleCount = vis.RingSampleCount;
            slot.CachedWindowSampleCount = vis.WindowSampleCount;
            slot.CachedOldestVisibleChronologicalIndex = vis.OldestVisibleChronologicalIndex;
        }

        columnBuckets = slot.OverviewColumnToBucket;
        return true;
    }

    private bool TryDrawStereoStackedOverview(
        TextLayerSettings layer,
        ref (double Offset, int SnippetIndex) state,
        TextLayerDrawContext ctx,
        WaveformStripSettings s,
        int w,
        int bandTop,
        int bandHeight,
        int timeLabelRow,
        double gain,
        bool filled,
        AudioAnalysisSnapshot snapshot)
    {
        int ovLen = snapshot.WaveformOverviewLength;
        if (ovLen <= 0
            || snapshot.WaveformOverviewMin is not { Length: > 0 } minL
            || snapshot.WaveformOverviewMax is not { Length: > 0 } maxL
            || snapshot.WaveformOverviewMinRight is not { Length: > 0 } minR
            || snapshot.WaveformOverviewMaxRight is not { Length: > 0 } maxR)
        {
            return false;
        }

        int useLen = Math.Min(ovLen, Math.Min(Math.Min(minL.Length, maxL.Length), Math.Min(minR.Length, maxR.Length)));
        if (useLen < 2)
        {
            return false;
        }

        int hL = bandHeight / 2;
        int hR = bandHeight - hL;
        int centerL = bandTop + hL / 2;
        int centerR = bandTop + hL + hR / 2;
        if (!TryDrawOverviewBand(ref state, ctx, s, w, bandTop, hL, centerL, gain, filled, snapshot, useRightChannel: false))
        {
            return false;
        }

        if (!TryDrawOverviewBand(ref state, ctx, s, w, bandTop + hL, hR, centerR, gain, filled, snapshot, useRightChannel: true))
        {
            return false;
        }

        DrawOverlays(layer, ctx, s, w, bandTop, hL, timeLabelRow, snapshot, drawTimeLabel: true, overlayBandTopOverride: bandTop, overlayBandHeightOverride: bandHeight);
        return true;
    }

    private bool TryDrawOverviewBand(
        ref (double Offset, int SnippetIndex) state,
        TextLayerDrawContext ctx,
        WaveformStripSettings s,
        int w,
        int bandTop,
        int bandHeight,
        int centerY,
        double gain,
        bool filled,
        AudioAnalysisSnapshot snapshot,
        bool useRightChannel)
    {
        int ovLen = snapshot.WaveformOverviewLength;
        float[] minArr = useRightChannel ? snapshot.WaveformOverviewMinRight : snapshot.WaveformOverviewMin;
        float[] maxArr = useRightChannel ? snapshot.WaveformOverviewMaxRight : snapshot.WaveformOverviewMax;
        if (ovLen <= 0 || minArr is not { Length: > 0 } || maxArr is not { Length: > 0 })
        {
            return false;
        }

        int useLen = Math.Min(ovLen, Math.Min(minArr.Length, maxArr.Length));
        if (useLen < 2)
        {
            return false;
        }

        int validForMap = snapshot.WaveformOverviewBuiltValidSampleCount >= 2
            ? snapshot.WaveformOverviewBuiltValidSampleCount
            : snapshot.WaveformOverviewValidSampleCount;
        if (validForMap < 2)
        {
            return false;
        }

        WaveformStripVisibleChronology vis = ComputeVisibleChronology(
            validForMap,
            snapshot.WaveformOverviewSpanSeconds,
            s);

        bool useCachedBuckets = TryGetCachedOverviewColumnBuckets(ctx.LayerIndex, w, useLen, vis, out int[]? columnBuckets);

        int prevY = centerY;
        int prevBucketIndex = -1;
        for (int x = 0; x < w; x++)
        {
            int bi = useCachedBuckets && columnBuckets is not null
                ? columnBuckets[x]
                : OverviewBucketIndexForColumn(x, w, useLen, vis);
            float minV = minArr[bi];
            float maxV = maxArr[bi];
            float midSample = (minV + maxV) * 0.5f;
            DrawColumnFromOverviewSample(
                ctx,
                s,
                x,
                minV,
                maxV,
                midSample,
                gain,
                filled,
                ref prevY,
                ref prevBucketIndex,
                bandTop,
                bandHeight,
                centerY,
                bi,
                snapshot,
                useRightChannel);
        }

        return true;
    }

    /// <summary>
    /// Maps column <paramref name="x"/> to an overview bucket by resampling chronological index onto the strip, then applying the same
    /// partition as <see cref="WaveformOverviewBucketIndex"/> (matches beat placement and engine rebuild).
    /// </summary>
    internal static int OverviewBucketIndexForColumn(int x, int w, int useLen, WaveformStripVisibleChronology vis)
    {
        int n = vis.RingSampleCount;
        if (useLen < 2 || w < 1 || n < 2 || vis.WindowSampleCount < 2)
        {
            return 0;
        }

        if (w == 1)
        {
            return WaveformOverviewBucketIndex.FromChronologicalIndex(
                Math.Max(0, n - 1),
                n,
                useLen);
        }

        int wid = vis.WindowSampleCount;
        int ch = vis.OldestVisibleChronologicalIndex + x * (wid - 1) / (w - 1);
        ch = Math.Clamp(ch, 0, n - 1);
        return WaveformOverviewBucketIndex.FromChronologicalIndex(ch, n, useLen);
    }

    private static void DrawColumnFromOverviewSample(
        TextLayerDrawContext ctx,
        WaveformStripSettings s,
        int x,
        float minV,
        float maxV,
        float midSample,
        double gain,
        bool filled,
        ref int prevY,
        ref int prevBucketIndex,
        int bandTop,
        int bandHeight,
        int centerY,
        int bucketIndex,
        AudioAnalysisSnapshot snapshot,
        bool useRightChannel)
    {
        if (s.Filled)
        {
            int yHi = AmplitudeToRow(maxV, gain, bandTop, bandHeight);
            int yLo = AmplitudeToRow(minV, gain, bandTop, bandHeight);
            int topEnd = Math.Min(yHi, yLo);
            int botEnd = Math.Max(yHi, yLo);
            for (int lineY = topEnd; lineY <= botEnd; lineY++)
            {
                var color = ResolveColor(s, lineY, centerY, bandHeight, ctx.Palette, snapshot, bucketIndex, useRightChannel);
                ctx.SetLocal(x, lineY, '█', color);
            }

            prevBucketIndex = bucketIndex;
        }
        else
        {
            int y = AmplitudeToRow(midSample, gain, bandTop, bandHeight);
            bool connect =
                prevBucketIndex >= 0
                && Math.Abs(bucketIndex - prevBucketIndex) <= 1;
            if (!connect)
            {
                var dotColor = ResolveColor(s, y, centerY, bandHeight, ctx.Palette, snapshot, bucketIndex, useRightChannel);
                ctx.SetLocal(x, y, '█', dotColor);
            }
            else
            {
                int minY = Math.Min(prevY, y);
                int maxY = Math.Max(prevY, y);
                minY = Math.Clamp(minY, bandTop, bandTop + bandHeight - 1);
                maxY = Math.Clamp(maxY, bandTop, bandTop + bandHeight - 1);
                for (int lineY = minY; lineY <= maxY; lineY++)
                {
                    var color = ResolveColor(s, lineY, centerY, bandHeight, ctx.Palette, snapshot, bucketIndex, useRightChannel);
                    ctx.SetLocal(x, lineY, '█', color);
                }
            }

            prevY = y;
            prevBucketIndex = bucketIndex;
        }
    }

    private static void DrawColumnFromSample(
        TextLayerDrawContext ctx,
        WaveformStripSettings s,
        int x,
        float sample,
        double gain,
        bool filled,
        ref int prevY,
        int bandTop,
        int bandHeight,
        int centerY,
        AudioAnalysisSnapshot snapshot,
        int bucketIndex,
        bool useRightChannel)
    {
        int y = AmplitudeToRow(sample, gain, bandTop, bandHeight);
        int minY = filled ? Math.Min(centerY, y) : Math.Min(prevY, y);
        int maxY = filled ? Math.Max(centerY, y) : Math.Max(prevY, y);
        minY = Math.Clamp(minY, bandTop, bandTop + bandHeight - 1);
        maxY = Math.Clamp(maxY, bandTop, bandTop + bandHeight - 1);
        for (int lineY = minY; lineY <= maxY; lineY++)
        {
            var color = ResolveColor(s, lineY, centerY, bandHeight, ctx.Palette, snapshot, bucketIndex, useRightChannel);
            ctx.SetLocal(x, lineY, '█', color);
        }

        prevY = y;
    }

    private static PaletteColor ResolveColor(
        WaveformStripSettings s,
        int lineY,
        int centerY,
        int bandHeight,
        IReadOnlyList<PaletteColor>? palette,
        AudioAnalysisSnapshot snapshot,
        int bucketIndex,
        bool useRightChannel)
    {
        if (bucketIndex >= 0)
        {
            if (s.ColorMode == WaveformStripColorMode.SpectralGoertzel)
            {
                float[] lo = useRightChannel ? snapshot.WaveformOverviewBandLowGoertzelRight : snapshot.WaveformOverviewBandLowGoertzel;
                float[] mid = useRightChannel ? snapshot.WaveformOverviewBandMidGoertzelRight : snapshot.WaveformOverviewBandMidGoertzel;
                float[] hi = useRightChannel ? snapshot.WaveformOverviewBandHighGoertzelRight : snapshot.WaveformOverviewBandHighGoertzel;
                if (lo is { Length: > 0 } && mid is { Length: > 0 } && hi is { Length: > 0 }
                    && bucketIndex < lo.Length && bucketIndex < mid.Length && bucketIndex < hi.Length)
                {
                    return RgbFromThreeBands(lo[bucketIndex], mid[bucketIndex], hi[bucketIndex]);
                }
            }
            else if (s.ColorMode == WaveformStripColorMode.SpectralApprox)
            {
                float[] lo = useRightChannel ? snapshot.WaveformOverviewBandLowRight : snapshot.WaveformOverviewBandLow;
                float[] mid = useRightChannel ? snapshot.WaveformOverviewBandMidRight : snapshot.WaveformOverviewBandMid;
                float[] hi = useRightChannel ? snapshot.WaveformOverviewBandHighRight : snapshot.WaveformOverviewBandHigh;
                if (lo is { Length: > 0 } && mid is { Length: > 0 } && hi is { Length: > 0 }
                    && bucketIndex < lo.Length && bucketIndex < mid.Length && bucketIndex < hi.Length)
                {
                    return RgbFromThreeBands(lo[bucketIndex], mid[bucketIndex], hi[bucketIndex]);
                }
            }
        }

        return GetColorFromPalette(lineY, centerY, bandHeight, palette);
    }

    private static PaletteColor RgbFromThreeBands(float nLo, float nMid, float nHi)
    {
        float m = Math.Max(1e-6f, Math.Max(nLo, Math.Max(nMid, nHi)));
        byte r = (byte)(255f * Math.Clamp(nHi / m, 0f, 1f));
        byte g = (byte)(255f * Math.Clamp(nMid / m, 0f, 1f));
        byte b = (byte)(255f * Math.Clamp(nLo / m, 0f, 1f));
        return PaletteColor.FromRgb(r, g, b);
    }

    private static void DrawOverlays(
        TextLayerSettings layer,
        TextLayerDrawContext ctx,
        WaveformStripSettings s,
        int w,
        int bandTop,
        int bandHeight,
        int timeLabelRow,
        AudioAnalysisSnapshot snapshot,
        bool drawTimeLabel,
        int? overlayBandTopOverride = null,
        int? overlayBandHeightOverride = null)
    {
        int gridTop = overlayBandTopOverride ?? bandTop;
        int gridHeight = overlayBandHeightOverride ?? bandHeight;
        int useLen = snapshot.WaveformOverviewLength;
        int validSamples = snapshot.WaveformOverviewValidSampleCount;
        long oldestG = snapshot.WaveformOverviewOldestMonoSampleIndex;
        long newestG = snapshot.WaveformOverviewNewestMonoSampleIndex;
        bool useBuiltAnchors = useLen >= 2 && snapshot.WaveformOverviewBuiltValidSampleCount >= 2;
        int mapValid = useBuiltAnchors ? snapshot.WaveformOverviewBuiltValidSampleCount : validSamples;
        long mapOldest = useBuiltAnchors ? snapshot.WaveformOverviewBuiltOldestMonoSampleIndex : oldestG;
        long mapNewest = useBuiltAnchors ? snapshot.WaveformOverviewBuiltNewestMonoSampleIndex : newestG;
        WaveformStripVisibleChronology vis = useLen >= 2 && mapValid >= 2
            ? ComputeVisibleChronology(mapValid, snapshot.WaveformOverviewSpanSeconds, s)
            : new WaveformStripVisibleChronology(Math.Max(mapValid, 2), Math.Max(mapValid, 2), 0);
        double spanSeconds = snapshot.WaveformOverviewSpanSeconds;
        double gridSpanSeconds = vis.RingSampleCount > 0
            ? spanSeconds * vis.WindowSampleCount / vis.RingSampleCount
            : spanSeconds;
        bool beatTimingOk = snapshot.CurrentBpm is > 40 and < 400 && gridSpanSeconds > 0.01;
        int step = 1;
        if (beatTimingOk)
        {
            double beatSeconds = 60.0 / snapshot.CurrentBpm * 4.0 / 4.0;
            double colsPerBeat = w * beatSeconds / gridSpanSeconds;
            step = Math.Max(1, (int)Math.Round(colsPerBeat));
        }

        var dim = ctx.Palette is { Count: > 0 }
            ? ctx.Palette[Math.Min(ctx.Palette.Count - 1, 0)]
            : PaletteColor.FromConsoleColor(ConsoleColor.DarkGray);

        long[]? markSamples = snapshot.WaveformBeatMarkMonoSampleIndex;
        int[]? markOrdinals = snapshot.WaveformBeatMarkBeatOrdinal;
        bool hasBeatMarks = snapshot.WaveformBeatMarkLength > 0
            && markSamples is { Length: > 0 }
            && markOrdinals is { Length: > 0 }
            && useLen >= 2
            && mapValid >= 2;

        if (s.ShowBeatGrid)
        {
            if (hasBeatMarks)
            {
                int n = Math.Min(snapshot.WaveformBeatMarkLength, Math.Min(markSamples!.Length, markOrdinals!.Length));
                for (int i = 0; i < n; i++)
                {
                    int x = ColumnForGlobalMonoSample(markSamples[i], w, mapOldest, mapNewest, useLen, vis);
                    if (x < 0)
                    {
                        continue;
                    }

                    x = Math.Clamp(x + s.BeatGridOffsetColumns, 0, w - 1);
                    DrawBeatColumn(ctx, x, gridTop, gridHeight, dim);
                }
            }
            else if (beatTimingOk)
            {
                for (int x = 0; x < w; x += step)
                {
                    int xx = Math.Clamp(x + s.BeatGridOffsetColumns, 0, w - 1);
                    DrawBeatColumn(ctx, xx, gridTop, gridHeight, dim);
                }
            }
        }

        if (s.ShowBeatMarkers && w >= 1)
        {
            var baseOrange = PaletteColor.FromRgb(200, 100, 0);
            var pulseOrange = PaletteColor.FromRgb(255, 140, 0);
            int xr = w - 1;
            if (hasBeatMarks)
            {
                int n = Math.Min(snapshot.WaveformBeatMarkLength, Math.Min(markSamples!.Length, markOrdinals!.Length));
                int beatsPerBar = Math.Clamp(s.BeatsPerBar, 1, 16);
                for (int i = 0; i < n; i++)
                {
                    int ord = markOrdinals[i];
                    if (ord < 1 || ((ord - 1) % beatsPerBar) != 0)
                    {
                        continue;
                    }

                    int x = ColumnForGlobalMonoSample(markSamples[i], w, mapOldest, mapNewest, useLen, vis);
                    if (x < 0 || x == xr)
                    {
                        continue;
                    }

                    x = Math.Clamp(x + s.BeatGridOffsetColumns, 0, w - 1);
                    ctx.SetLocal(x, gridTop, '^', baseOrange);
                    ctx.SetLocal(x, gridTop + gridHeight - 1, 'v', baseOrange);
                }
            }
            else if (beatTimingOk)
            {
                int barStep = Math.Max(1, step * Math.Clamp(s.BeatsPerBar, 1, 16));
                for (int x = 0; x < w; x += barStep)
                {
                    int xx = Math.Clamp(x + s.BeatGridOffsetColumns, 0, w - 1);
                    if (xx == xr)
                    {
                        continue;
                    }

                    ctx.SetLocal(xx, gridTop, '^', baseOrange);
                    ctx.SetLocal(xx, gridTop + gridHeight - 1, 'v', baseOrange);
                }
            }

            var rightAccent = snapshot.BeatFlashActive ? pulseOrange : baseOrange;
            ctx.SetLocal(xr, gridTop, '^', rightAccent);
            ctx.SetLocal(xr, gridTop + gridHeight - 1, 'v', rightAccent);
        }

        if (drawTimeLabel && s.ShowTimeLabel && timeLabelRow >= 0 && gridSpanSeconds > 0)
        {
            string label = gridSpanSeconds.ToString("F1", CultureInfo.InvariantCulture) + "s";
            var fg = ctx.Palette is { Count: > 0 } ? ctx.Palette[0] : PaletteColor.FromConsoleColor(ConsoleColor.DarkGray);
            for (int i = 0; i < label.Length && i < w; i++)
            {
                ctx.SetLocal(i, timeLabelRow, label[i], fg);
            }
        }

        _ = layer;
    }

    private static void DrawBeatColumn(TextLayerDrawContext ctx, int x, int gridTop, int gridHeight, PaletteColor dim)
    {
        for (int row = gridTop; row < gridTop + gridHeight; row++)
        {
            if (((row - gridTop) & 1) == 0)
            {
                ctx.SetLocal(x, row, ':', dim);
            }
        }
    }

    /// <summary>Maps a unit sample with gain to a row so that ±1 uses the top and bottom of the band.</summary>
    private static int AmplitudeToRow(float sample, double gain, int bandTop, int bandHeight)
    {
        double u = Math.Clamp(sample * gain, -1.0, 1.0);
        if (bandHeight < 1)
        {
            return bandTop;
        }

        double center = bandTop + (bandHeight - 1) * 0.5;
        double half = Math.Max((bandHeight - 1) * 0.5, 1e-6);
        int y = (int)Math.Round(center - u * half);
        return Math.Clamp(y, bandTop, bandTop + bandHeight - 1);
    }

    private static PaletteColor GetColorFromPalette(
        int y,
        int centerY,
        int bandHeight,
        IReadOnlyList<PaletteColor>? palette)
    {
        if (palette is { Count: > 0 })
        {
            int half = bandHeight / 2;
            double distance = half <= 0 ? 0 : Math.Abs(y - centerY) / (double)half;
            int idx = Math.Min((int)(distance * palette.Count), Math.Max(0, palette.Count - 1));
            return palette[idx];
        }

        return GetOscilloscopeLikeColor(y, centerY, bandHeight);
    }

    private static PaletteColor GetOscilloscopeLikeColor(int y, int centerY, int bandHeight)
    {
        int half = bandHeight / 2;
        if (half <= 0)
        {
            return PaletteColor.FromConsoleColor(ConsoleColor.Cyan);
        }

        double distance = Math.Abs(y - centerY) / (double)half;
        var cc = distance switch
        {
            >= 0.8 => ConsoleColor.Red,
            >= 0.6 => ConsoleColor.Yellow,
            >= 0.4 => ConsoleColor.Green,
            _ => ConsoleColor.Cyan
        };
        return PaletteColor.FromConsoleColor(cc);
    }
}
