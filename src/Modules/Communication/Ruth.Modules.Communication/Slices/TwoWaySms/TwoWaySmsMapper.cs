using Ruth.Modules.Communication.Contracts.TwoWaySms;
using Ruth.Modules.Communication.Domain.TwoWaySms;

namespace Ruth.Modules.Communication.Slices.TwoWaySms;

internal static class TwoWaySmsMapper
{
    public static SmsPipelineResponse ToResponse(SmsPipeline entity)
    {
        return new SmsPipelineResponse(
            entity.Id.Value,
            entity.Name,
            entity.Description,
            entity.SystemPhoneNumberId,
            entity.PriorityOrder,
            entity.IsSystem,
            entity.IsActive,
            [
                .. entity
                    .Actions.OrderBy(static a => a.Order)
                    .Select(static a => new SmsActionResponse(
                        a.Id.Value,
                        a.Name,
                        a.Order,
                        a.MatchMode,
                        a.Keyword,
                        a.ActionKind,
                        a.ReplyTemplate,
                        a.TargetSystemCommunicationKey,
                        a.ContinueProcessing,
                        a.IsActive
                    )),
            ],
            entity.CreatedOnUtc
        );
    }

    public static SmsConversationMessageResponse ToResponse(CommunicationResponse entity)
    {
        return new SmsConversationMessageResponse(
            entity.Id.Value,
            entity.SystemPhoneNumberId,
            entity.SystemPhoneNumber,
            entity.ParticipantPhoneNumber,
            entity.ParticipantPersonId,
            entity.ParticipantDisplayName,
            entity.Direction,
            entity.MessageBody,
            entity.MatchedKeyword,
            entity.IsRead,
            entity.RepliedByPersonId,
            entity.OccurredAtUtc
        );
    }
}
