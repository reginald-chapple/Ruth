namespace Ruth.Modules.Reporting.Domain.Analytics;

/// <summary>Star-schema calendar date dimension table (reporting.dim_date).</summary>
public sealed class DimDate
{
    public int DateKey { get; set; }
    public DateOnly CalendarDate { get; set; }
    public int Year { get; set; }
    public int Quarter { get; set; }
    public int Month { get; set; }
    public string MonthLabel { get; set; } = string.Empty;
    public int IsoWeek { get; set; }
    public int DayOfWeek { get; set; }
}
