namespace Ruth.SharedKernel.Time;

/// <summary>
/// Testable clock abstraction backed by <see cref="TimeProvider" /> for deterministic UTC and local time evaluation.
/// </summary>
public interface IClock
{
    /// <summary>Gets the current UTC instant.</summary>
    DateTimeOffset UtcNow { get; }

    /// <summary>Gets the current instant converted to <see cref="LocalTimeZone" />.</summary>
    DateTimeOffset LocalNow { get; }

    /// <summary>Gets the current UTC calendar date.</summary>
    DateOnly TodayUtc { get; }

    /// <summary>Gets the current calendar date in <see cref="LocalTimeZone" />.</summary>
    DateOnly TodayLocal { get; }

    /// <summary>Gets the configured organization or local <see cref="TimeZoneInfo" />.</summary>
    TimeZoneInfo LocalTimeZone { get; }

    /// <summary>Gets the underlying <see cref="TimeProvider" />.</summary>
    TimeProvider TimeProvider { get; }

    /// <summary>Converts a UTC timestamp into <see cref="LocalTimeZone" />.</summary>
    DateTimeOffset ConvertToLocal(DateTimeOffset utcTimestamp);

    /// <summary>Gets a monotonic high-resolution timestamp for elapsed time measurements.</summary>
    long GetTimestamp();

    /// <summary>Calculates elapsed time since <paramref name="startingTimestamp" />.</summary>
    TimeSpan GetElapsedTime(long startingTimestamp);
}
