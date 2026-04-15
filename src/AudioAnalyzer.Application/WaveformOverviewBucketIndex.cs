namespace AudioAnalyzer.Application;

/// <summary>
/// Maps chronological mono history indices to decimated overview bucket indices using the same partition as
/// <see cref="AnalysisEngine"/> overview rebuild.
/// </summary>
public static class WaveformOverviewBucketIndex
{
    /// <summary>
    /// Half-open sample index range <c>[t0, t1)</c> aggregated into overview bucket <paramref name="bucketIndex"/>.
    /// </summary>
    public static void GetBucketSampleRange(int bucketIndex, int validSampleCount, int bucketCount, out int t0, out int t1)
    {
        long n = validSampleCount;
        t0 = (int)(bucketIndex * n / bucketCount);
        t1 = (int)((bucketIndex + 1L) * n / bucketCount);
        if (t1 <= t0)
        {
            t1 = Math.Min(t0 + 1, validSampleCount);
        }
    }

    /// <summary>
    /// Returns the overview bucket that contains chronological sample index <paramref name="chronologicalIndex"/>
    /// (0 = oldest sample in the valid window). <c>O(log bucketCount)</c> via binary search on partition end indices.
    /// </summary>
    public static int FromChronologicalIndex(int chronologicalIndex, int validSampleCount, int bucketCount)
    {
        if (bucketCount < 1 || validSampleCount < 1)
        {
            return 0;
        }

        if (chronologicalIndex <= 0)
        {
            return 0;
        }

        if (chronologicalIndex >= validSampleCount)
        {
            return bucketCount - 1;
        }

        int lo = 0;
        int hi = bucketCount - 1;
        int ans = bucketCount - 1;
        while (lo <= hi)
        {
            int mid = lo + ((hi - lo) >> 1);
            GetBucketSampleRange(mid, validSampleCount, bucketCount, out _, out int t1);
            if (t1 > chronologicalIndex)
            {
                ans = mid;
                hi = mid - 1;
            }
            else
            {
                lo = mid + 1;
            }
        }

        return ans;
    }
}
