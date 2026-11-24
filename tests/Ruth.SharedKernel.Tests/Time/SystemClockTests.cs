using Ruth.SharedKernel.Tests.TestDoubles;
using Ruth.SharedKernel.Time;
using Xunit;

namespace Ruth.SharedKernel.Tests.Time;

public sealed class SystemClockTests
{
    [Fact]
    public void SystemClock_Default_Constructor_Uses_SystemTimeProvider()
    {
        var clock = new SystemClock();

        Assert.Same(TimeProvider.System, clock.TimeProvider);
        Assert.Equal(TimeProvider.System.LocalTimeZone, clock.LocalTimeZone);
        Assert.True(clock.UtcNow <= DateTimeOffset.UtcNow.AddSeconds(1));
    }

    [Fact]
    public void SystemClock_Converts_Utc_To_Configured_LocalTimeZone_And_Measures_Elapsed()
    {
        var mountainZone = TimeZoneInfo.CreateCustomTimeZone(
            "TestMountain",
            TimeSpan.FromHours(-7),
            "Test Mountain",
            "Test Mountain"
        );
        var utcInstant = new DateTimeOffset(2026, 3, 15, 2, 30, 0, TimeSpan.Zero);
        var fakeProvider = new FakeTimeProvider(utcInstant, TimeZoneInfo.Utc);
        var clock = new SystemClock(fakeProvider, mountainZone);

        Assert.Equal(utcInstant, clock.UtcNow);
        Assert.Equal(new DateOnly(2026, 3, 15), clock.TodayUtc);
        Assert.Equal(
            new DateTimeOffset(2026, 3, 14, 19, 30, 0, TimeSpan.FromHours(-7)),
            clock.LocalNow
        );
        Assert.Equal(new DateOnly(2026, 3, 14), clock.TodayLocal);
        Assert.Equal(
            new DateTimeOffset(2026, 3, 14, 19, 30, 0, TimeSpan.FromHours(-7)),
            clock.ConvertToLocal(utcInstant)
        );

        var start = clock.GetTimestamp();
        fakeProvider.Advance(TimeSpan.FromMilliseconds(125));
        Assert.Equal(TimeSpan.FromMilliseconds(125), clock.GetElapsedTime(start));
    }

    [Fact]
    public void SystemClock_Rejects_Null_TimeProvider()
    {
        _ = Assert.Throws<ArgumentNullException>(() => new SystemClock(null!));
    }
}
