using AudioAnalyzer.Application;
using Xunit;

namespace AudioAnalyzer.Tests.Application;

public sealed class WaveformOverviewBucketIndexTests
{
    [Theory]
    [InlineData(10, 4, 0, 0)]
    [InlineData(10, 4, 4, 1)]
    [InlineData(10, 4, 5, 2)]
    [InlineData(10, 4, 9, 3)]
    public void FromChronologicalIndex_matches_partition_ranges(int validSampleCount, int bucketCount, int ch, int expectedBucket)
    {
        int b = WaveformOverviewBucketIndex.FromChronologicalIndex(ch, validSampleCount, bucketCount);
        Assert.Equal(expectedBucket, b);
    }

    [Fact]
    public void GetBucketSampleRange_partitions_full_range_without_gaps_or_overlap()
    {
        const int n = 17;
        const int bCount = 5;
        var covered = new bool[n];
        for (int b = 0; b < bCount; b++)
        {
            WaveformOverviewBucketIndex.GetBucketSampleRange(b, n, bCount, out int t0, out int t1);
            Assert.InRange(t0, 0, n);
            Assert.InRange(t1, t0 + 1, n + 1);
            for (int i = t0; i < t1; i++)
            {
                Assert.False(covered[i]);
                covered[i] = true;
            }
        }

        Assert.All(covered, Assert.True);
    }

    [Fact]
    public void FromChronologicalIndex_matches_brute_force_partition_scan_for_small_parameters()
    {
        for (int n = 2; n <= 40; n++)
        {
            for (int bCount = 2; bCount <= 25; bCount++)
            {
                for (int ch = 0; ch < n; ch++)
                {
                    int expected = BruteBucket(ch, n, bCount);
                    int actual = WaveformOverviewBucketIndex.FromChronologicalIndex(ch, n, bCount);
                    Assert.Equal(expected, actual);
                }
            }
        }
    }

    private static int BruteBucket(int chronologicalIndex, int validSampleCount, int bucketCount)
    {
        for (int b = 0; b < bucketCount; b++)
        {
            WaveformOverviewBucketIndex.GetBucketSampleRange(b, validSampleCount, bucketCount, out int t0, out int t1);
            if (chronologicalIndex >= t0 && chronologicalIndex < t1)
            {
                return b;
            }
        }

        return bucketCount - 1;
    }

    [Fact]
    public void FromChronologicalIndex_covers_each_index_once_across_buckets()
    {
        const int n = 100;
        const int bCount = 8192;
        var seen = new int[bCount];
        for (int ch = 0; ch < n; ch++)
        {
            int b = WaveformOverviewBucketIndex.FromChronologicalIndex(ch, n, bCount);
            Assert.InRange(b, 0, bCount - 1);
            seen[b]++;
        }

        Assert.Equal(n, seen.Sum());
    }
}
