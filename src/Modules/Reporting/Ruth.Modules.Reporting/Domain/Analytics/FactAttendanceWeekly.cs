namespace Ruth.Modules.Reporting.Domain.Analytics;

/// <summary>Star-schema weekly attendance fact table (reporting.fact_attendance_weekly).</summary>
public sealed class FactAttendanceWeekly
{
    public Guid Id { get; set; }
    public int WeekEndDateKey { get; set; }
    public DateOnly WeekEndDate { get; set; }
    public Guid? BranchId { get; set; }
    public int OccurrenceCount { get; set; }
    public int AttendedCount { get; set; }
    public DateTimeOffset RefreshedAtUtc { get; set; }
}
