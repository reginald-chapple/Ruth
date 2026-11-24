namespace Ruth.SharedKernel.Jobs;

/// <summary>Represents the terminal outcome of a scheduled job execution.</summary>
public enum JobExecutionStatus
{
    /// <summary>The job completed normally.</summary>
    Success = 0,

    /// <summary>The job completed with non-fatal warnings.</summary>
    Warning = 1,

    /// <summary>The job failed.</summary>
    Failed = 2,
}
