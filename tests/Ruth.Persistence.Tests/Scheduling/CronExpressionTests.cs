using Ruth.Persistence.Scheduling;
using Xunit;

namespace Ruth.Persistence.Tests.Scheduling;

public sealed class CronExpressionTests
{
    [Theory]
    [InlineData("*/5 * * * *")]
    [InlineData("30 2 15 * MON-FRI")]
    [InlineData("0,15,30,45 * * * * *")]
    [InlineData("0 0 1 JAN,JUN,DEC SUN")]
    [InlineData("0 12 ? * 7")]
    [InlineData("0 0 * * 5-7")]
    [InlineData("0 0 * * 1-7")]
    [InlineData("0 0 * * 0-7")]
    [InlineData("0 0 * * FRI-7")]
    [InlineData("0 0 * * 7/2")]
    [InlineData("0 0 * * 1/2")]
    [InlineData("@hourly")]
    [InlineData("@daily")]
    [InlineData("@midnight")]
    [InlineData("@weekly")]
    [InlineData("@monthly")]
    [InlineData("@yearly")]
    [InlineData("@annually")]
    [InlineData("@never")]
    [InlineData("@manual")]
    public void TryParse_Accepts_Valid_Expressions_And_Macros(string expression)
    {
        Assert.True(CronExpression.IsValid(expression));
        Assert.True(CronExpression.TryParse(expression, out var parsed));
        Assert.NotNull(parsed);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("* * * *")]
    [InlineData("* * * * * * *")]
    [InlineData("60 * * * * *")]
    [InlineData("60 * * * *")]
    [InlineData("* 24 * * *")]
    [InlineData("* * 0 * *")]
    [InlineData("* * 32 * *")]
    [InlineData("* * * 13 *")]
    [InlineData("* * * * 8")]
    [InlineData("0 0 * * 7-5")]
    [InlineData("0 0 * * 0-8")]
    [InlineData("0 0 * * bad/2")]
    [InlineData("5-2 * * * *")]
    [InlineData("*/0 * * * *")]
    [InlineData("invalid * * * *")]
    public void TryParse_Rejects_Invalid_Expressions(string? expression)
    {
        Assert.False(CronExpression.IsValid(expression));
        Assert.False(CronExpression.TryParse(expression, out _));
        if (expression is not null)
        {
            _ = Assert.Throws<FormatException>(() => CronExpression.Parse(expression));
        }
    }

    [Fact]
    public void Never_Sentinels_Return_Null_NextOccurrence()
    {
        var now = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        var never = CronExpression.Parse(CronExpression.NeverExpression);
        var manual = CronExpression.Parse(CronExpression.ManualExpression);

        Assert.True(never.IsNever);
        Assert.True(manual.IsNever);
        Assert.Null(never.GetNextOccurrence(now, TimeZoneInfo.Utc));
        Assert.Null(manual.GetNextOccurrence(now, TimeZoneInfo.Utc));
    }

    [Fact]
    public void FiveField_And_SixField_Schedules_Compute_Expected_Next_Utc()
    {
        var fiveField = CronExpression.Parse("*/15 * * * *");
        var after = new DateTimeOffset(2026, 5, 10, 14, 15, 0, TimeSpan.Zero);
        Assert.Equal(
            new DateTimeOffset(2026, 5, 10, 14, 30, 0, TimeSpan.Zero),
            fiveField.GetNextOccurrence(after, TimeZoneInfo.Utc)
        );

        var sixField = CronExpression.Parse("0,30 * * * * *");
        var afterSix = new DateTimeOffset(2026, 5, 10, 14, 15, 10, TimeSpan.Zero);
        Assert.Equal(
            new DateTimeOffset(2026, 5, 10, 14, 15, 30, TimeSpan.Zero),
            sixField.GetNextOccurrence(afterSix, TimeZoneInfo.Utc)
        );
    }

    [Fact]
    public void Leap_Year_Feb_29_Advances_From_2024_To_2028()
    {
        var cron = CronExpression.Parse("0 0 29 2 *");
        var after = new DateTimeOffset(2024, 2, 29, 0, 0, 1, TimeSpan.Zero);

        var next = cron.GetNextOccurrence(after, TimeZoneInfo.Utc);

        Assert.Equal(new DateTimeOffset(2028, 2, 29, 0, 0, 0, TimeSpan.Zero), next);
    }

    [Fact]
    public void Both_DayOfMonth_And_DayOfWeek_Restricted_Use_Vixie_Or_Semantics()
    {
        // 15th of month OR Monday: 2026-06-15 is Monday, 2026-06-08 is Monday
        var cron = CronExpression.Parse("0 9 15 * MON");
        var after = new DateTimeOffset(2026, 6, 7, 10, 0, 0, TimeSpan.Zero);

        var first = cron.GetNextOccurrence(after, TimeZoneInfo.Utc);
        Assert.Equal(new DateTimeOffset(2026, 6, 8, 9, 0, 0, TimeSpan.Zero), first);

        // Next match after 2026-06-14 (Sunday) -> 2026-06-15 (Monday & 15th);
        // Let's also check a month where 15th is NOT Monday: 2026-07-15 is Wednesday!
        var julyAfter = new DateTimeOffset(2026, 7, 14, 10, 0, 0, TimeSpan.Zero);
        var julyNext = cron.GetNextOccurrence(julyAfter, TimeZoneInfo.Utc);
        Assert.Equal(new DateTimeOffset(2026, 7, 15, 9, 0, 0, TimeSpan.Zero), julyNext);
    }

    [Fact]
    public void Dst_Spring_Forward_Skips_Invalid_Local_Time()
    {
        var eastern = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
        var cron = CronExpression.Parse("30 2 * * *");

        // 2025-03-09 02:00 jumps to 03:00 in America/New_York; 02:30 does not exist.
        var beforeJump = new DateTimeOffset(2025, 3, 9, 6, 0, 0, TimeSpan.Zero); // 01:00 EST
        var next = cron.GetNextOccurrence(beforeJump, eastern);

        Assert.Equal(new DateTimeOffset(2025, 3, 10, 6, 30, 0, TimeSpan.Zero), next); // 02:30 EDT next day
    }

    [Fact]
    public void Dst_Fall_Back_Fires_Earliest_Then_Second_Pass_Of_Ambiguous_Hour()
    {
        var eastern = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
        var cron = CronExpression.Parse("30 1 * * *");

        // 2025-11-02 01:15 EDT = 05:15 UTC -> next is 01:30 EDT = 05:30 UTC
        var beforeFirst = new DateTimeOffset(2025, 11, 2, 5, 15, 0, TimeSpan.Zero);
        var firstPass = cron.GetNextOccurrence(beforeFirst, eastern);
        Assert.Equal(new DateTimeOffset(2025, 11, 2, 5, 30, 0, TimeSpan.Zero), firstPass);

        // 2025-11-02 01:45 EDT = 05:45 UTC -> next is 01:30 EST = 06:30 UTC
        var betweenPasses = new DateTimeOffset(2025, 11, 2, 5, 45, 0, TimeSpan.Zero);
        var secondPass = cron.GetNextOccurrence(betweenPasses, eastern);
        Assert.Equal(new DateTimeOffset(2025, 11, 2, 6, 30, 0, TimeSpan.Zero), secondPass);
    }

    [Theory]
    [InlineData("* * * * *", "2025-11-02T05:30:00Z", "2025-11-02T05:31:00Z")]
    [InlineData("0,45 1 * * *", "2025-11-02T05:30:00Z", "2025-11-02T05:45:00Z")]
    [InlineData("*/30 * * * * *", "2025-11-02T05:30:00Z", "2025-11-02T05:30:30Z")]
    public void Dst_Fall_Back_Ambiguous_Hour_Returns_Earliest_Remaining_First_Pass_Occurrence(
        string expression,
        string afterIso,
        string expectedIso
    )
    {
        var eastern = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
        var cron = CronExpression.Parse(expression);
        var afterUtc = DateTimeOffset.Parse(
            afterIso,
            System.Globalization.CultureInfo.InvariantCulture
        );
        var expectedUtc = DateTimeOffset.Parse(
            expectedIso,
            System.Globalization.CultureInfo.InvariantCulture
        );

        Assert.Equal(expectedUtc, cron.GetNextOccurrence(afterUtc, eastern));
    }

    [Theory]
    [InlineData("0 0 * * 5-7")]
    [InlineData("0 0 * * 1-7")]
    [InlineData("0 0 * * 0-7")]
    [InlineData("0 0 * * FRI-7")]
    public void DayOfWeek_Ranges_Ending_In_Seven_Match_Sunday(string expression)
    {
        var cron = CronExpression.Parse(expression);
        // 2026-05-09 is Saturday; next Sunday is 2026-05-10 00:00:00Z.
        var saturdayNight = new DateTimeOffset(2026, 5, 9, 23, 0, 0, TimeSpan.Zero);

        var next = cron.GetNextOccurrence(saturdayNight, TimeZoneInfo.Utc);

        Assert.Equal(new DateTimeOffset(2026, 5, 10, 0, 0, 0, TimeSpan.Zero), next);
    }
}
