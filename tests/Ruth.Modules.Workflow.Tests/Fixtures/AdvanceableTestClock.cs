using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Workflow.Tests.Fixtures;

internal sealed class AdvanceableTestClock(DateTimeOffset initialUtc) : IClock
{
    public DateTimeOffset UtcNow { get; private set; } = initialUtc;
    public DateTimeOffset LocalNow => UtcNow;
    public DateOnly TodayUtc => DateOnly.FromDateTime(UtcNow.UtcDateTime);
    public DateOnly TodayLocal => TodayUtc;
    public TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    public TimeProvider TimeProvider => TimeProvider.System;

    public void Advance(TimeSpan delta)
    {
        UtcNow = UtcNow.Add(delta);
    }

    public DateTimeOffset ConvertToLocal(DateTimeOffset utcTimestamp)
    {
        return utcTimestamp.ToUniversalTime();
    }

    public long GetTimestamp()
    {
        return TimeProvider.System.GetTimestamp();
    }

    public TimeSpan GetElapsedTime(long startingTimestamp)
    {
        return TimeProvider.System.GetElapsedTime(startingTimestamp);
    }
}
