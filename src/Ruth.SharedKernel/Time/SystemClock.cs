namespace Ruth.SharedKernel.Time;

/// <summary>Default <see cref="IClock" /> implementation adapting <see cref="TimeProvider" />.</summary>
public sealed class SystemClock : IClock
{
    /// <summary>Initializes a clock using <see cref="TimeProvider.System" />.</summary>
    public SystemClock()
        : this(TimeProvider.System, localTimeZone: null) { }

    /// <summary>Initializes a clock backed by a specific <see cref="TimeProvider" /> and optional time zone.</summary>
    /// <param name="timeProvider">The underlying time provider.</param>
    /// <param name="localTimeZone">
    /// Optional organization time zone; defaults to <see cref="TimeProvider.LocalTimeZone" />.
    /// </param>
    public SystemClock(TimeProvider timeProvider, TimeZoneInfo? localTimeZone = null)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);

        TimeProvider = timeProvider;
        LocalTimeZone = localTimeZone ?? timeProvider.LocalTimeZone;
    }

    /// <inheritdoc />
    public DateTimeOffset UtcNow => TimeProvider.GetUtcNow();

    /// <inheritdoc />
    public DateTimeOffset LocalNow =>
        TimeZoneInfo.ConvertTime(TimeProvider.GetUtcNow(), LocalTimeZone);

    /// <inheritdoc />
    public DateOnly TodayUtc => DateOnly.FromDateTime(UtcNow.UtcDateTime);

    /// <inheritdoc />
    public DateOnly TodayLocal => DateOnly.FromDateTime(LocalNow.DateTime);

    /// <inheritdoc />
    public TimeZoneInfo LocalTimeZone { get; }

    /// <inheritdoc />
    public TimeProvider TimeProvider { get; }

    /// <inheritdoc />
    public DateTimeOffset ConvertToLocal(DateTimeOffset utcTimestamp)
    {
        return TimeZoneInfo.ConvertTime(utcTimestamp, LocalTimeZone);
    }

    /// <inheritdoc />
    public long GetTimestamp()
    {
        return TimeProvider.GetTimestamp();
    }

    /// <inheritdoc />
    public TimeSpan GetElapsedTime(long startingTimestamp)
    {
        return TimeProvider.GetElapsedTime(startingTimestamp);
    }
}
