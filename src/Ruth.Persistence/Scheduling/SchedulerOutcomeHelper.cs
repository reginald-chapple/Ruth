using Microsoft.Extensions.Logging;
using Ruth.SharedKernel.Jobs;

namespace Ruth.Persistence.Scheduling;

internal static class SchedulerOutcomeHelper
{
    public static void RecordMetricsAndLog(
        ILogger logger,
        string jobKey,
        JobExecutionResult outcome
    )
    {
        SchedulerDiagnostics.JobsExecuted.Add(1);

        if (outcome.Status == JobExecutionStatus.Warning)
        {
            SchedulerLog.JobWarning(logger, jobKey, outcome.Message);
            return;
        }

        if (outcome.Status == JobExecutionStatus.Failed)
        {
            SchedulerDiagnostics.JobsFailed.Add(1);
            SchedulerLog.JobFailed(logger, exception: null, jobKey, outcome.Message);
        }
    }

    public static string FormatExceptionMessage(Exception exception)
    {
        if (exception is AggregateException aggregate)
        {
            var flattened = aggregate.Flatten();
            if (flattened.InnerExceptions.Count == 1)
            {
                var inner = flattened.InnerExceptions[0];
                return $"{inner.GetType().Name}: {inner.Message}";
            }

            if (flattened.InnerExceptions.Count > 1)
            {
                var first = flattened.InnerExceptions[0];
                return $"One or more exceptions occurred. First Exception: {first.GetType().Name}: {first.Message}";
            }
        }

        return $"{exception.GetType().Name}: {exception.Message}";
    }
}
