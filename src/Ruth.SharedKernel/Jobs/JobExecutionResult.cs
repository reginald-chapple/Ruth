namespace Ruth.SharedKernel.Jobs;

/// <summary>Outcome returned by an <see cref="IScheduledJob" /> execution.</summary>
/// <param name="Status">The execution status.</param>
/// <param name="Message">Optional diagnostic summary.</param>
public sealed record JobExecutionResult(JobExecutionStatus Status, string? Message = null)
{
    /// <summary>Creates a successful execution result.</summary>
    /// <param name="message">Optional summary message.</param>
    public static JobExecutionResult Success(string? message = null)
    {
        return new JobExecutionResult(JobExecutionStatus.Success, message);
    }

    /// <summary>Creates a warning execution result.</summary>
    /// <param name="message">Diagnostic warning message.</param>
    public static JobExecutionResult Warning(string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        return new JobExecutionResult(JobExecutionStatus.Warning, message);
    }

    /// <summary>Creates a failed execution result.</summary>
    /// <param name="message">Diagnostic failure message.</param>
    public static JobExecutionResult Failed(string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        return new JobExecutionResult(JobExecutionStatus.Failed, message);
    }
}
