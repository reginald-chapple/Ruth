namespace Ruth.Persistence.Scheduling;

internal readonly record struct CronScheduleFields(
    CronField Seconds,
    CronField Minutes,
    CronField Hours,
    CronField DaysOfMonth,
    CronField Months,
    CronField DaysOfWeek
);
