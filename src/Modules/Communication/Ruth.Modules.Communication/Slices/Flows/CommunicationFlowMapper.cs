using Ruth.Modules.Communication.Contracts.Flows;
using Ruth.Modules.Communication.Domain.Flows;

namespace Ruth.Modules.Communication.Slices.Flows;

internal static class CommunicationFlowMapper
{
    public static CommunicationFlowResponse ToResponse(CommunicationFlow entity)
    {
        return new CommunicationFlowResponse(
            entity.Id.Value,
            entity.Name,
            entity.Key,
            entity.Description,
            entity.Category,
            entity.TopicKey,
            entity.TriggerMode,
            entity.ExitCondition,
            entity.ConversionGoalKind,
            entity.ConversionTargetGroupId,
            entity.ConversionGoalUrlPrefix,
            entity.ConversionWindowDays,
            [.. entity.Steps.OrderBy(static s => s.Order).Select(static s => s.ToDto())],
            entity.IsSystem,
            entity.IsActive,
            entity.CreatedOnUtc,
            entity.ModifiedOnUtc
        );
    }

    public static CommunicationFlowInstanceRecipientResponse ToResponse(
        CommunicationFlowInstanceRecipient entity
    )
    {
        return new CommunicationFlowInstanceRecipientResponse(
            entity.Id.Value,
            entity.FlowId.Value,
            entity.FlowInstanceId.Value,
            entity.PersonId,
            entity.EmailAddress,
            entity.PhoneNumber,
            entity.DisplayName,
            entity.Status,
            entity.CurrentStepIndex,
            entity.CompletedStepsCount,
            entity.OpenedStepsCount,
            entity.ClickedStepsCount,
            entity.EnrolledAtUtc,
            entity.LastStepSentAtUtc,
            entity.NextStepDueAtUtc,
            entity.ExitedAtUtc,
            entity.ExitReason
        );
    }

    public static CommunicationFlowInstanceResponse ToResponse(
        CommunicationFlowInstance instance,
        IReadOnlyList<CommunicationFlowInstanceRecipient> recipients
    )
    {
        return new CommunicationFlowInstanceResponse(
            instance.Id.Value,
            instance.FlowId.Value,
            instance.CohortLabel,
            instance.Status,
            instance.StartDateUtc,
            instance.CompletedAtUtc,
            [.. recipients.Select(ToResponse)]
        );
    }

    public static CommunicationFlowConversionResponse ToResponse(CommunicationFlowConversion entity)
    {
        return new CommunicationFlowConversionResponse(
            entity.Id.Value,
            entity.FlowId.Value,
            entity.FlowInstanceId.Value,
            entity.FlowInstanceRecipientId.Value,
            entity.PersonId,
            entity.GoalKind,
            entity.StepOrderAtConversion,
            entity.ConversionValue,
            entity.ReferenceKey,
            entity.OccurredAtUtc,
            entity.Notes
        );
    }
}
