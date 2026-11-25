using Ruth.Modules.Core.Domain.Schedules;
using Xunit;

namespace Ruth.Modules.Core.Tests.Domain;

public sealed class CoreScheduleRecurrenceTests
{
    [Fact]
    public void Weekly_Recurrence_Evaluates_Across_SpringForward_Dst_Without_Throwing()
    {
        // America/Denver springs forward on Sunday 2026-03-08 at 02:00 -> 03:00.
        // Schedule at 02:30 local wall time is an invalid wall time on 2026-03-08.
        var ical = ICalendarParser.BuildSimpleICalendar(
            new DateTime(2026, 3, 1, 2, 30, 0),
            TimeSpan.FromHours(1),
            rrule: "FREQ=WEEKLY;BYDAY=SU;COUNT=3",
            timeZoneId: "America/Denver"
        );
        var schedule = Schedule
            .Create(
                new ScheduleInput(
                    "Early Setup",
                    AbbreviatedName: null,
                    Description: null,
                    ical,
                    CheckInStartOffsetMinutes: 30,
                    CheckInEndOffsetMinutes: 15,
                    WeeklyDayOfWeek: null,
                    WeeklyTimeOfDay: null,
                    CategoryId: null,
                    AutoInactivateWhenComplete: false,
                    IsActive: true,
                    IsPublic: true,
                    0
                )
            )
            .Value;

        var occurrences = schedule.GetOccurrences(
            new DateTimeOffset(2026, 2, 28, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 3, 20, 0, 0, 0, TimeSpan.Zero),
            "America/Denver"
        );

        Assert.Equal(3, occurrences.Count);
        Assert.Equal(new DateTime(2026, 3, 1, 2, 30, 0), occurrences[0].LocalStart);
        Assert.Equal(
            new DateTimeOffset(2026, 3, 1, 9, 30, 0, TimeSpan.Zero),
            occurrences[0].StartOnUtc
        );
        // Spring-forward adjusts 02:30 -> 03:30 MDT (UTC-6 = 09:30Z)
        Assert.Equal(new DateTime(2026, 3, 8, 3, 30, 0), occurrences[1].LocalStart);
        Assert.Equal(
            new DateTimeOffset(2026, 3, 8, 9, 30, 0, TimeSpan.Zero),
            occurrences[1].StartOnUtc
        );
        // Next Sunday is back to 02:30 MDT (UTC-6 = 08:30Z)
        Assert.Equal(new DateTime(2026, 3, 15, 2, 30, 0), occurrences[2].LocalStart);
        Assert.Equal(
            new DateTimeOffset(2026, 3, 15, 8, 30, 0, TimeSpan.Zero),
            occurrences[2].StartOnUtc
        );
    }

    [Fact]
    public void Weekly_Recurrence_Handles_FallBack_Ambiguous_Dst_Deterministically()
    {
        // America/Denver falls back on Sunday 2026-11-01 at 02:00 -> 01:00.
        // 01:30 occurs twice (first in MDT UTC-6, then in MST UTC-7).
        var ical = ICalendarParser.BuildSimpleICalendar(
            new DateTime(2026, 10, 25, 1, 30, 0),
            TimeSpan.FromHours(1),
            rrule: "FREQ=WEEKLY;BYDAY=SU;COUNT=3",
            timeZoneId: "America/Denver"
        );
        var schedule = Schedule
            .Create(
                new ScheduleInput(
                    "Night Watch",
                    AbbreviatedName: null,
                    Description: null,
                    ical,
                    CheckInStartOffsetMinutes: null,
                    CheckInEndOffsetMinutes: null,
                    WeeklyDayOfWeek: null,
                    WeeklyTimeOfDay: null,
                    CategoryId: null,
                    AutoInactivateWhenComplete: false,
                    IsActive: true,
                    IsPublic: true,
                    0
                )
            )
            .Value;

        var occurrences = schedule.GetOccurrences(
            new DateTimeOffset(2026, 10, 24, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 11, 10, 0, 0, 0, TimeSpan.Zero),
            "America/Denver"
        );

        Assert.Equal(3, occurrences.Count);
        // Oct 25 MDT (UTC-6): 07:30Z
        Assert.Equal(
            new DateTimeOffset(2026, 10, 25, 7, 30, 0, TimeSpan.Zero),
            occurrences[0].StartOnUtc
        );
        // Nov 1 ambiguous 01:30 picks first (daylight, UTC-6): 07:30Z
        Assert.Equal(
            new DateTimeOffset(2026, 11, 1, 7, 30, 0, TimeSpan.Zero),
            occurrences[1].StartOnUtc
        );
        // Nov 8 standard MST (UTC-7): 08:30Z
        Assert.Equal(
            new DateTimeOffset(2026, 11, 8, 8, 30, 0, TimeSpan.Zero),
            occurrences[2].StartOnUtc
        );
    }

    [Fact]
    public void Yearly_LeapDay_Feb29_Skips_NonLeap_Years_And_Hits_2024_2028_2032()
    {
        var ical = ICalendarParser.BuildSimpleICalendar(
            new DateTime(2024, 2, 29, 9, 0, 0),
            TimeSpan.FromHours(1),
            rrule: "FREQ=YEARLY;BYMONTH=2;BYMONTHDAY=29"
        );
        var schedule = Schedule
            .Create(
                new ScheduleInput(
                    "Leap Day Celebration",
                    AbbreviatedName: null,
                    Description: null,
                    ical,
                    CheckInStartOffsetMinutes: null,
                    CheckInEndOffsetMinutes: null,
                    WeeklyDayOfWeek: null,
                    WeeklyTimeOfDay: null,
                    CategoryId: null,
                    AutoInactivateWhenComplete: false,
                    IsActive: true,
                    IsPublic: true,
                    0
                )
            )
            .Value;

        var occurrences = schedule.GetOccurrences(
            new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2033, 1, 1, 0, 0, 0, TimeSpan.Zero),
            "UTC"
        );

        Assert.Equal(3, occurrences.Count);
        Assert.Equal(20240229, occurrences[0].StartDateKey);
        Assert.Equal(20280229, occurrences[1].StartDateKey);
        Assert.Equal(20320229, occurrences[2].StartDateKey);
        Assert.Contains(
            "Yearly on February 29",
            schedule.ToFriendlyText(),
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void Monthly_ByDay_Ordinals_BySetPos_And_Negative_MonthDay_Work_Accurately()
    {
        // Last weekday of each month for Q1 2026 (Jan 30, Feb 27, Mar 31)
        var icalLastWeekday = ICalendarParser.BuildSimpleICalendar(
            new DateTime(2026, 1, 1, 17, 0, 0),
            TimeSpan.FromHours(1),
            rrule: "FREQ=MONTHLY;BYDAY=MO,TU,WE,TH,FR;BYSETPOS=-1;COUNT=3"
        );
        var s1 = Schedule
            .Create(
                new ScheduleInput(
                    "Month End Close",
                    AbbreviatedName: null,
                    Description: null,
                    icalLastWeekday,
                    CheckInStartOffsetMinutes: null,
                    CheckInEndOffsetMinutes: null,
                    WeeklyDayOfWeek: null,
                    WeeklyTimeOfDay: null,
                    CategoryId: null,
                    AutoInactivateWhenComplete: false,
                    IsActive: true,
                    IsPublic: true,
                    0
                )
            )
            .Value;
        var occ1 = s1.GetOccurrences(
            new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 4, 1, 0, 0, 0, TimeSpan.Zero),
            "UTC"
        );
        Assert.Equal([20260130, 20260227, 20260331], [.. occ1.Select(static o => o.StartDateKey)]);

        // 1st and 3rd Sunday of the month
        var icalSundays = ICalendarParser.BuildSimpleICalendar(
            new DateTime(2026, 1, 1, 10, 30, 0),
            TimeSpan.FromMinutes(75),
            rrule: "FREQ=MONTHLY;BYDAY=1SU,3SU;UNTIL=20260228T235959Z"
        );
        var s2 = Schedule
            .Create(
                new ScheduleInput(
                    "1st & 3rd Sunday",
                    AbbreviatedName: null,
                    Description: null,
                    icalSundays,
                    CheckInStartOffsetMinutes: null,
                    CheckInEndOffsetMinutes: null,
                    WeeklyDayOfWeek: null,
                    WeeklyTimeOfDay: null,
                    CategoryId: null,
                    AutoInactivateWhenComplete: false,
                    IsActive: true,
                    IsPublic: true,
                    0
                )
            )
            .Value;
        var occ2 = s2.GetOccurrences(
            new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero),
            "UTC"
        );
        Assert.Equal(
            [20260104, 20260118, 20260201, 20260215],
            [.. occ2.Select(static o => o.StartDateKey)]
        );

        // Last day of every month (BYMONTHDAY=-1)
        var icalMonthEnd = ICalendarParser.BuildSimpleICalendar(
            new DateTime(2024, 1, 31, 8, 0, 0),
            TimeSpan.FromMinutes(30),
            rrule: "FREQ=MONTHLY;BYMONTHDAY=-1;COUNT=4"
        );
        var s3 = Schedule
            .Create(
                new ScheduleInput(
                    "Last Day",
                    AbbreviatedName: null,
                    Description: null,
                    icalMonthEnd,
                    CheckInStartOffsetMinutes: null,
                    CheckInEndOffsetMinutes: null,
                    WeeklyDayOfWeek: null,
                    WeeklyTimeOfDay: null,
                    CategoryId: null,
                    AutoInactivateWhenComplete: false,
                    IsActive: true,
                    IsPublic: true,
                    0
                )
            )
            .Value;
        var occ3 = s3.GetOccurrences(
            new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2024, 5, 1, 0, 0, 0, TimeSpan.Zero),
            "UTC"
        );
        Assert.Equal(
            [20240131, 20240229, 20240331, 20240430],
            [.. occ3.Select(static o => o.StartDateKey)]
        );
    }

    [Fact]
    public void CrossMidnight_CheckIn_CheckOut_ExDate_RDate_And_CategoryExclusion_Work()
    {
        // New Year's Eve service: Dec 31 23:00 -> Jan 1 01:00 (2 hours)
        var ical = string.Join(
            '\n',
            "BEGIN:VCALENDAR",
            "VERSION:2.0",
            "BEGIN:VEVENT",
            "DTSTART:20261231T230000",
            "DURATION:PT2H",
            "RDATE:20270107T230000",
            "EXDATE:20270107T230000",
            "END:VEVENT",
            "END:VCALENDAR"
        );

        var schedule = Schedule
            .Create(
                new ScheduleInput(
                    "NYE Watchnight",
                    AbbreviatedName: null,
                    Description: null,
                    ical,
                    CheckInStartOffsetMinutes: 60,
                    CheckInEndOffsetMinutes: 90,
                    WeeklyDayOfWeek: null,
                    WeeklyTimeOfDay: null,
                    CategoryId: null,
                    AutoInactivateWhenComplete: false,
                    IsActive: true,
                    IsPublic: true,
                    0
                )
            )
            .Value;

        Assert.Equal(120, schedule.GetDurationMinutes());
        Assert.Equal(new DateOnly(2026, 12, 31), schedule.EffectiveStartDate);
        Assert.Equal(new DateOnly(2027, 1, 8), schedule.EffectiveEndDate);

        // Check-in opens at 22:00 Dec 31, closes at 00:30 Jan 1; Event ends 01:00 Jan 1
        Assert.True(
            schedule.IsCheckInActiveAt(
                new DateTimeOffset(2026, 12, 31, 22, 15, 0, TimeSpan.Zero),
                "UTC"
            )
        );
        Assert.True(
            schedule.IsCheckInActiveAt(
                new DateTimeOffset(2027, 1, 1, 0, 20, 0, TimeSpan.Zero),
                "UTC"
            )
        );
        Assert.False(
            schedule.IsCheckInActiveAt(
                new DateTimeOffset(2027, 1, 1, 0, 40, 0, TimeSpan.Zero),
                "UTC"
            )
        );
        Assert.True(
            schedule.IsScheduleActiveAt(
                new DateTimeOffset(2027, 1, 1, 0, 45, 0, TimeSpan.Zero),
                "UTC"
            )
        );
        Assert.True(
            schedule.IsActiveForCheckOutAt(
                new DateTimeOffset(2027, 1, 1, 0, 55, 0, TimeSpan.Zero),
                "UTC"
            )
        );

        // Category exclusion spanning Dec 31 blocks occurrence
        var exclusion = ScheduleCategoryExclusion
            .Create(
                Core.Domain.CategoryId.Create(),
                "Winter Storm",
                new DateOnly(2026, 12, 31),
                new DateOnly(2026, 12, 31)
            )
            .Value;
        Assert.False(
            schedule.IsCheckInActiveAt(
                new DateTimeOffset(2026, 12, 31, 22, 15, 0, TimeSpan.Zero),
                "UTC",
                [(exclusion.StartOnUtc, exclusion.EndOnUtc)]
            )
        );
    }

    [Fact]
    public void Simple_Weekly_Schedule_And_Daily_Interval_Format_And_Evaluate()
    {
        var weekly = Schedule
            .Create(
                new ScheduleInput(
                    Name: null,
                    AbbreviatedName: null,
                    Description: null,
                    ICalendarContent: null,
                    CheckInStartOffsetMinutes: null,
                    CheckInEndOffsetMinutes: null,
                    DayOfWeek.Wednesday,
                    new TimeOnly(18, 30),
                    CategoryId: null,
                    AutoInactivateWhenComplete: false,
                    IsActive: true,
                    IsPublic: true,
                    0
                )
            )
            .Value;
        Assert.Equal("Weekly: Wednesday at 6:30 PM", weekly.ToFriendlyText());
        var occ = weekly.GetOccurrences(
            new DateTimeOffset(2026, 11, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 11, 15, 0, 0, 0, TimeSpan.Zero),
            "UTC"
        );
        Assert.Equal(2, occ.Count);

        var dailyIcal = ICalendarParser.BuildSimpleICalendar(
            new DateTime(2026, 5, 1, 8, 0, 0),
            TimeSpan.FromMinutes(45),
            rrule: "FREQ=DAILY;INTERVAL=2;COUNT=3"
        );
        var daily = Schedule
            .Create(
                new ScheduleInput(
                    "Every Other Day",
                    AbbreviatedName: null,
                    Description: null,
                    dailyIcal,
                    CheckInStartOffsetMinutes: null,
                    CheckInEndOffsetMinutes: null,
                    WeeklyDayOfWeek: null,
                    WeeklyTimeOfDay: null,
                    CategoryId: null,
                    AutoInactivateWhenComplete: false,
                    IsActive: true,
                    IsPublic: true,
                    0
                )
            )
            .Value;
        Assert.Equal("Every 2 days at 8:00 AM", daily.ToFriendlyText());
        var dailyOcc = daily.GetOccurrences(
            new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 5, 10, 0, 0, 0, TimeSpan.Zero),
            "UTC"
        );
        Assert.Equal(
            [20260501, 20260503, 20260505],
            [.. dailyOcc.Select(static o => o.StartDateKey)]
        );

        var monthly1 = Schedule
            .Create(
                new ScheduleInput(
                    "M1",
                    AbbreviatedName: null,
                    Description: null,
                    ICalendarParser.BuildSimpleICalendar(
                        new DateTime(2026, 1, 1, 9, 0, 0),
                        TimeSpan.FromHours(1),
                        "FREQ=MONTHLY;BYDAY=1SU,2MO,3TU,-1FR"
                    ),
                    CheckInStartOffsetMinutes: null,
                    CheckInEndOffsetMinutes: null,
                    WeeklyDayOfWeek: null,
                    WeeklyTimeOfDay: null,
                    CategoryId: null,
                    AutoInactivateWhenComplete: false,
                    IsActive: true,
                    IsPublic: true,
                    0
                )
            )
            .Value;
        Assert.Contains(
            "1st Sunday, 2nd Monday, 3rd Tuesday, last Friday",
            monthly1.ToFriendlyText(),
            StringComparison.Ordinal
        );

        var monthly2 = Schedule
            .Create(
                new ScheduleInput(
                    "M2",
                    AbbreviatedName: null,
                    Description: null,
                    ICalendarParser.BuildSimpleICalendar(
                        new DateTime(2026, 1, 11, 9, 0, 0),
                        TimeSpan.FromHours(1),
                        "FREQ=MONTHLY;INTERVAL=2;BYMONTHDAY=11"
                    ),
                    CheckInStartOffsetMinutes: null,
                    CheckInEndOffsetMinutes: null,
                    WeeklyDayOfWeek: null,
                    WeeklyTimeOfDay: null,
                    CategoryId: null,
                    AutoInactivateWhenComplete: false,
                    IsActive: true,
                    IsPublic: true,
                    0
                )
            )
            .Value;
        Assert.Contains(
            "Every 2 months on day 11th",
            monthly2.ToFriendlyText(),
            StringComparison.Ordinal
        );

        var monthly3 = Schedule
            .Create(
                new ScheduleInput(
                    "M3",
                    AbbreviatedName: null,
                    Description: null,
                    ICalendarParser.BuildSimpleICalendar(
                        new DateTime(2026, 1, 12, 9, 0, 0),
                        TimeSpan.FromHours(1),
                        "FREQ=MONTHLY;BYMONTHDAY=12"
                    ),
                    CheckInStartOffsetMinutes: null,
                    CheckInEndOffsetMinutes: null,
                    WeeklyDayOfWeek: null,
                    WeeklyTimeOfDay: null,
                    CategoryId: null,
                    AutoInactivateWhenComplete: false,
                    IsActive: true,
                    IsPublic: true,
                    0
                )
            )
            .Value;
        Assert.Contains("day 12th", monthly3.ToFriendlyText(), StringComparison.Ordinal);

        var monthly4 = Schedule
            .Create(
                new ScheduleInput(
                    "M4",
                    AbbreviatedName: null,
                    Description: null,
                    ICalendarParser.BuildSimpleICalendar(
                        new DateTime(2026, 1, 13, 9, 0, 0),
                        TimeSpan.FromHours(1),
                        "FREQ=MONTHLY;BYMONTHDAY=13"
                    ),
                    CheckInStartOffsetMinutes: null,
                    CheckInEndOffsetMinutes: null,
                    WeeklyDayOfWeek: null,
                    WeeklyTimeOfDay: null,
                    CategoryId: null,
                    AutoInactivateWhenComplete: false,
                    IsActive: true,
                    IsPublic: true,
                    0
                )
            )
            .Value;
        Assert.Contains("day 13th", monthly4.ToFriendlyText(), StringComparison.Ordinal);

        var monthly5 = Schedule
            .Create(
                new ScheduleInput(
                    "M5",
                    AbbreviatedName: null,
                    Description: null,
                    ICalendarParser.BuildSimpleICalendar(
                        new DateTime(2026, 1, 4, 9, 0, 0),
                        TimeSpan.FromHours(1),
                        "FREQ=MONTHLY;BYMONTHDAY=4"
                    ),
                    CheckInStartOffsetMinutes: null,
                    CheckInEndOffsetMinutes: null,
                    WeeklyDayOfWeek: null,
                    WeeklyTimeOfDay: null,
                    CategoryId: null,
                    AutoInactivateWhenComplete: false,
                    IsActive: true,
                    IsPublic: true,
                    0
                )
            )
            .Value;
        Assert.Contains("day 4th", monthly5.ToFriendlyText(), StringComparison.Ordinal);
    }
}
