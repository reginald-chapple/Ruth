namespace Ruth.Persistence.Scheduling;

/// <summary>Canonical persisted status strings for scheduled jobs and run histories.</summary>
public static class ScheduledJobStatus
{
    /// <summary>The job execution is currently in progress.</summary>
    public const string Running = "Running";

    /// <summary>The job execution completed normally.</summary>
    public const string Success = "Success";

    /// <summary>The job execution completed with non-fatal warnings.</summary>
    public const string Warning = "Warning";

    /// <summary>The job execution failed.</summary>
    public const string Failed = "Failed";
}
