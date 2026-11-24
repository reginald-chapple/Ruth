using System.Numerics;

namespace Ruth.Persistence.Scheduling;

internal readonly record struct CronField(ulong Mask, bool IsWildcard)
{
    public static CronField Single(int value)
    {
        return new CronField(1UL << value, IsWildcard: false);
    }

    public bool Contains(int value)
    {
        return (Mask & (1UL << value)) != 0;
    }

    public int? NextOrEqual(int value, int max)
    {
        if (value > max)
        {
            return null;
        }

        var shifted = Mask & (~0UL << value);
        if (shifted == 0)
        {
            return null;
        }

        var next = BitOperations.TrailingZeroCount(shifted);
        return next <= max ? next : null;
    }

    public static bool TryParse(
        string token,
        int min,
        int max,
        CronFieldKind kind,
        out CronField field
    )
    {
        return CronFieldParser.TryParse(token, min, max, kind, out field);
    }
}
