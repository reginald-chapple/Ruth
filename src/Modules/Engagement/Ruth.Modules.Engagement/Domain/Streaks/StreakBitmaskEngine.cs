using System.Buffers.Binary;
using System.Numerics;
using Ruth.Modules.Engagement.Contracts.Streaks;

namespace Ruth.Modules.Engagement.Domain.Streaks;

/// <summary>High-density safe Span&lt;byte&gt; participation cadence engine using hardware PopCount and exclusion forgiveness.</summary>
public static class StreakBitmaskEngine
{
    /// <summary>Aligns a calendar date to the end of its 7-day reporting week when cadence is Weekly.</summary>
    public static DateOnly AlignToBucketDate(
        DateOnly date,
        StreakCadence cadence,
        DayOfWeek weekEndDay = DayOfWeek.Sunday
    )
    {
        if (cadence == StreakCadence.Daily)
        {
            return date;
        }

        var daysUntilEnd = ((int)weekEndDay - (int)date.DayOfWeek + 7) % 7;
        return date.AddDays(daysUntilEnd);
    }

    /// <summary>Computes zero-based bitmask bucket index relative to the aligned anchor date.</summary>
    public static int ComputeBucketIndex(
        DateOnly anchorStartDate,
        DateOnly targetDate,
        StreakCadence cadence,
        DayOfWeek weekEndDay = DayOfWeek.Sunday
    )
    {
        var alignedAnchor = AlignToBucketDate(anchorStartDate, cadence, weekEndDay);
        var alignedTarget = AlignToBucketDate(targetDate, cadence, weekEndDay);
        var deltaDays = alignedTarget.DayNumber - alignedAnchor.DayNumber;
        return cadence == StreakCadence.Weekly ? deltaDays / 7 : deltaDays;
    }

    /// <summary>Converts a zero-based bucket index back into its calendar bucket date.</summary>
    public static DateOnly BucketIndexToDate(
        DateOnly anchorStartDate,
        int bucketIndex,
        StreakCadence cadence,
        DayOfWeek weekEndDay = DayOfWeek.Sunday
    )
    {
        var alignedAnchor = AlignToBucketDate(anchorStartDate, cadence, weekEndDay);
        return cadence == StreakCadence.Weekly
            ? alignedAnchor.AddDays(bucketIndex * 7)
            : alignedAnchor.AddDays(bucketIndex);
    }

    /// <summary>Ensures buffer capacity and sets the bit corresponding to <paramref name="bucketIndex" />.</summary>
    public static byte[] EnsureAndSetBit(byte[] currentMap, int bucketIndex)
    {
        ArgumentNullException.ThrowIfNull(currentMap);
        ArgumentOutOfRangeException.ThrowIfNegative(bucketIndex);

        var requiredBytes = (bucketIndex >> 3) + 1;
        var target =
            currentMap.Length >= requiredBytes
                ? currentMap
                : ResizePreserving(currentMap, requiredBytes);

        SetBit(target.AsSpan(), bucketIndex);
        return target;
    }

    /// <summary>Sets a single bucket bit in a mutable <see cref="Span{T}" />.</summary>
    public static void SetBit(Span<byte> map, int bucketIndex)
    {
        var byteIndex = bucketIndex >> 3;
        var bitOffset = bucketIndex & 7;
        map[byteIndex] |= (byte)(1 << bitOffset);
    }

    /// <summary>Tests whether a bucket bit is set in a <see cref="ReadOnlySpan{T}" />.</summary>
    public static bool IsBitSet(ReadOnlySpan<byte> map, int bucketIndex)
    {
        if (bucketIndex < 0)
        {
            return false;
        }

        var byteIndex = bucketIndex >> 3;
        return byteIndex < map.Length && (map[byteIndex] & (1 << (bucketIndex & 7))) != 0;
    }

    /// <summary>Counts total set bits across a bitmask using 64-bit <see cref="BitOperations.PopCount(ulong)" />.</summary>
    public static int CountSetBits(ReadOnlySpan<byte> map)
    {
        var count = 0;
        var span = map;
        while (span.Length >= sizeof(ulong))
        {
            count += BitOperations.PopCount(BinaryPrimitives.ReadUInt64LittleEndian(span));
            span = span[sizeof(ulong)..];
        }

        foreach (var b in span)
        {
            count += BitOperations.PopCount((uint)b);
        }

        return count;
    }

    /// <summary>Bitwise-OR merges two participation bitmasks into a newly sized array (used on Person Merge).</summary>
    public static byte[] MergeBitwiseOr(ReadOnlySpan<byte> primary, ReadOnlySpan<byte> secondary)
    {
        var maxLen = Math.Max(primary.Length, secondary.Length);
        if (maxLen == 0)
        {
            return [];
        }

        var merged = new byte[maxLen];
        var dst = merged.AsSpan();
        primary.CopyTo(dst);
        for (var i = 0; i < secondary.Length; i++)
        {
            dst[i] |= secondary[i];
        }

        return merged;
    }

    /// <summary>Builds an exclusion bitmask over <c>[0..maxBucketIndex]</c> for a branch or org-wide exclusions.</summary>
    public static byte[] BuildExclusionMap(
        DateOnly anchorStartDate,
        StreakCadence cadence,
        DayOfWeek weekEndDay,
        IEnumerable<StreakExclusion> exclusions,
        Guid? branchId,
        int maxBucketIndex
    )
    {
        if (maxBucketIndex < 0)
        {
            return [];
        }

        var map = new byte[(maxBucketIndex >> 3) + 1];
        var span = map.AsSpan();
        foreach (var ex in exclusions)
        {
            if (ex.BranchId.HasValue && branchId.HasValue && ex.BranchId != branchId)
            {
                continue;
            }

            if (ex.BranchId.HasValue && !branchId.HasValue)
            {
                continue;
            }

            var idx = ComputeBucketIndex(anchorStartDate, ex.ExclusionDate, cadence, weekEndDay);
            if (idx >= 0 && idx <= maxBucketIndex)
            {
                SetBit(span, idx);
            }
        }

        return map;
    }

    /// <summary>Computes current streak, longest streak, and total engagement counts up to <paramref name="asOfBucketIndex" />.</summary>
    public static StreakComputedMetrics ComputeMetrics(
        ReadOnlySpan<byte> engagementMap,
        ReadOnlySpan<byte> exclusionMap,
        DateOnly anchorStartDate,
        StreakCadence cadence,
        DayOfWeek weekEndDay,
        int asOfBucketIndex,
        bool allowUnengagedCurrentBucketGrace = true
    )
    {
        if (asOfBucketIndex < 0 || engagementMap.IsEmpty)
        {
            return default;
        }

        int totalCount = 0, runCount = 0, runStart = -1;
        int longestCount = 0, longestStart = -1, longestEnd = -1, mostRecent = -1;

        for (var b = 0; b <= asOfBucketIndex; b++)
        {
            if (IsBitSet(engagementMap, b))
            {
                totalCount++;
                mostRecent = b;
                if (runCount == 0)
                {
                    runStart = b;
                }

                runCount++;
                if (runCount >= longestCount)
                {
                    longestCount = runCount;
                    longestStart = runStart;
                    longestEnd = b;
                }
            }
            else if (!IsBitSet(exclusionMap, b) && (b < asOfBucketIndex || !allowUnengagedCurrentBucketGrace))
            {
                runCount = 0;
                runStart = -1;
            }
        }

        return new StreakComputedMetrics(
            totalCount,
            runCount,
            runCount > 0 ? BucketIndexToDate(anchorStartDate, runStart, cadence, weekEndDay) : null,
            longestCount,
            longestCount > 0 ? BucketIndexToDate(anchorStartDate, longestStart, cadence, weekEndDay) : null,
            longestCount > 0 ? BucketIndexToDate(anchorStartDate, longestEnd, cadence, weekEndDay) : null,
            mostRecent >= 0 ? BucketIndexToDate(anchorStartDate, mostRecent, cadence, weekEndDay) : null
        );
    }

    /// <summary>Formats the trailing <paramref name="windowBuckets" /> buckets into a compact sparkline string ('1', 'E', '0').</summary>
    public static string FormatRecentBuckets(
        ReadOnlySpan<byte> engagementMap,
        ReadOnlySpan<byte> exclusionMap,
        int asOfBucketIndex,
        int windowBuckets = 24
    )
    {
        if (asOfBucketIndex < 0 || windowBuckets <= 0)
        {
            return string.Empty;
        }

        var start = Math.Max(0, asOfBucketIndex - windowBuckets + 1);
        var len = asOfBucketIndex - start + 1;
        Span<char> chars = len <= 128 ? stackalloc char[len] : new char[len];

        for (var i = 0; i < len; i++)
        {
            var b = start + i;
            chars[i] = IsBitSet(engagementMap, b)
                ? '1'
                : IsBitSet(exclusionMap, b)
                    ? 'E'
                    : '0';
        }

        return new string(chars);
    }

    private static byte[] ResizePreserving(byte[] existing, int requiredLength)
    {
        var grown = new byte[requiredLength];
        existing.AsSpan().CopyTo(grown);
        return grown;
    }
}
