using Ruth.Modules.Engagement.Contracts.Pipelines;

namespace Ruth.Modules.Engagement.Domain.Pipelines;

/// <summary>Clean-room stage progression and SLA health evaluator for Connector Boards.</summary>
public static class PipelineBoardEvaluator
{
    /// <summary>Evaluates whether transitioning between two stages is permitted under pipeline progression rules.</summary>
    public static bool IsStageMoveAllowed(
        PipelineType pipeline,
        PipelineStage? currentStage,
        PipelineStage targetStage
    )
    {
        ArgumentNullException.ThrowIfNull(pipeline);
        ArgumentNullException.ThrowIfNull(targetStage);

        if (
            !pipeline.IsSequentialStageProgression
            || currentStage is null
            || targetStage.IsTerminalInactive
            || currentStage.Id == targetStage.Id
        )
        {
            return true;
        }

        var ordered = pipeline.Stages.OrderBy(static s => s.Order).ToList();
        var currentIndex = ordered.FindIndex(s => s.Id == currentStage.Id);
        var targetIndex = ordered.FindIndex(s => s.Id == targetStage.Id);
        return currentIndex >= 0 && targetIndex == currentIndex + 1;
    }

    /// <summary>Computes elapsed idle days and the SLA health status for an EngagementRequest.</summary>
    public static (int DaysIdle, EngagementRequestHealth Health) EvaluateHealth(
        EngagementRequest request,
        PipelineType pipeline,
        DateOnly today,
        DateTimeOffset nowUtc
    )
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(pipeline);

        var elapsedDays = Math.Max(
            0,
            (int)Math.Floor((nowUtc.ToUniversalTime() - request.LastActivityAtUtc).TotalDays)
        );

        if (request.RequestState == EngagementRequestState.Connected)
        {
            return (elapsedDays, EngagementRequestHealth.Completed);
        }

        if (request.RequestState == EngagementRequestState.Inactive)
        {
            return (elapsedDays, EngagementRequestHealth.Inactive);
        }

        return (elapsedDays, EvaluateOpenHealth(request, pipeline, today, elapsedDays));
    }

    private static EngagementRequestHealth EvaluateOpenHealth(
        EngagementRequest request,
        PipelineType pipeline,
        DateOnly today,
        int elapsedDays
    )
    {
        if (request.FollowUpDate.HasValue)
        {
            return EvaluateFollowUpHealth(request.FollowUpDate.Value, today);
        }

        var stage = pipeline.FindStage(request.CurrentStageId);
        var idleThreshold = stage?.IdleThresholdDaysOverride ?? pipeline.DaysUntilRequestIdle;
        return elapsedDays >= idleThreshold ? EngagementRequestHealth.Idle
            : elapsedDays >= Math.Max(1, idleThreshold - 2) ? EngagementRequestHealth.DueSoon
            : EngagementRequestHealth.OnTrack;
    }

    private static EngagementRequestHealth EvaluateFollowUpHealth(
        DateOnly followUpDate,
        DateOnly today
    )
    {
        return followUpDate <= today ? EngagementRequestHealth.OverdueFollowUp
            : (followUpDate.DayNumber - today.DayNumber) <= 2 ? EngagementRequestHealth.DueSoon
            : EngagementRequestHealth.OnTrack;
    }
}
