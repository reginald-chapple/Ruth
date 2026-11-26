using Ruth.Modules.Engagement.Contracts.Pipelines;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Engagement.Domain.Pipelines;

internal static class EngagementRequestValidator
{
    public static Result ValidateCreation(
        PipelineType pipeline,
        EngagementOpportunity opportunity,
        EngagementRequestCreationSpec spec,
        DateOnly today
    )
    {
        return !HasValidTarget(pipeline, opportunity, spec.RequesterPersonId)
                ? (Result)EngagementErrors.EngagementRequestInvalid
            : spec.FollowUpDate.HasValue
            && (!pipeline.EnableFutureFollowUp || spec.FollowUpDate.Value <= today)
                ? EngagementErrors.FutureFollowUpNotAllowed
            : Result.Success();
    }

    public static int? FindAutoFollowUpDays(PipelineStage stage)
    {
        foreach (var automation in stage.Automations)
        {
            if (automation.AutoSetFollowUpDays is > 0)
            {
                return automation.AutoSetFollowUpDays;
            }
        }

        return null;
    }

    public static Guid? FindStagePlacementGroup(PipelineStage stage)
    {
        foreach (var automation in stage.Automations)
        {
            if (automation.AutomationKind == StageAutomationKind.AddToPlacementGroup)
            {
                return automation.TargetEntityId;
            }
        }

        return null;
    }

    private static bool HasValidTarget(
        PipelineType pipeline,
        EngagementOpportunity opportunity,
        Guid requesterPersonId
    )
    {
        return requesterPersonId != Guid.Empty
            && opportunity.PipelineTypeId == pipeline.Id.Value
            && pipeline.Stages.Count > 0;
    }
}
