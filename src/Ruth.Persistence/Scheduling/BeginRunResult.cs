namespace Ruth.Persistence.Scheduling;

internal readonly record struct BeginRunResult(
    bool ShouldExecute,
    Guid? JobId,
    string? CronExpression,
    int HistoryCount
)
{
    public static BeginRunResult Skip(int historyCount)
    {
        return new BeginRunResult(
            ShouldExecute: false,
            JobId: null,
            CronExpression: null,
            HistoryCount: historyCount
        );
    }

    public static BeginRunResult ProceedWithoutRecord(int historyCount)
    {
        return new BeginRunResult(
            ShouldExecute: true,
            JobId: null,
            CronExpression: null,
            HistoryCount: historyCount
        );
    }
}
