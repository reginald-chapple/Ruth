namespace Ruth.SharedKernel.Tests.TestDoubles;

internal sealed class FakeTimeProvider(
    DateTimeOffset initialUtc,
    TimeZoneInfo? localTimeZone = null
) : TimeProvider
{
    private DateTimeOffset _utcNow = initialUtc.ToUniversalTime();
    private long _timestampTicks = initialUtc.UtcTicks;

    public override TimeZoneInfo LocalTimeZone { get; } = localTimeZone ?? TimeZoneInfo.Utc;

    public override long TimestampFrequency => TimeSpan.TicksPerSecond;

    public override DateTimeOffset GetUtcNow()
    {
        return _utcNow;
    }

    public override long GetTimestamp()
    {
        return _timestampTicks;
    }

    public void Advance(TimeSpan delta)
    {
        _utcNow = _utcNow.Add(delta);
        _timestampTicks += delta.Ticks;
    }
}
