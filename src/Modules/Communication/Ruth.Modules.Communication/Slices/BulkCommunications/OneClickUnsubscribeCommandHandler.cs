using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Communication.Contracts.BulkCommunications;
using Ruth.Modules.Communication.Contracts.Transports;
using Ruth.Modules.Communication.Domain.BulkCommunications;
using Ruth.Modules.Communication.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Communication.Slices.BulkCommunications;

internal sealed class OneClickUnsubscribeCommandHandler(
    CommunicationDbContext dbContext,
    PreferenceResolutionHelper helper
) : ICommandHandler<OneClickUnsubscribeCommand, CommunicationPreferenceResponse>
{
    public async Task<Result<CommunicationPreferenceResponse>> HandleAsync(
        OneClickUnsubscribeCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);
        if (string.IsNullOrWhiteSpace(command.PreferenceToken))
        {
            return Result.Failure<CommunicationPreferenceResponse>(
                BulkCommunicationErrors.PreferenceTokenInvalid
            );
        }

        var target = helper.ResolveTarget(
            command.PreferenceToken,
            explicitDestination: null,
            CommunicationChannel.Email,
            explicitPersonId: null
        );
        if (target.IsFailure)
        {
            return Result.Failure<CommunicationPreferenceResponse>(target.Errors);
        }

        var (channel, normalizedDest, personId) = target.Value;
        var entity = await dbContext.CommunicationSubscriptionPreferences.FirstOrDefaultAsync(
            p => p.Channel == channel && p.NormalizedDestination == normalizedDest,
            cancellationToken
        );

        if (entity is null)
        {
            var created = CommunicationSubscriptionPreference.Create(
                channel,
                normalizedDest,
                personId
            );
            if (created.IsFailure)
            {
                return Result.Failure<CommunicationPreferenceResponse>(created.Errors);
            }

            entity = created.Value;
            _ = dbContext.CommunicationSubscriptionPreferences.Add(entity);
        }

        entity.ApplyOneClickOptOut(command.TopicKey, "OneClickUnsubscribe");
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return helper.ToResponse(channel, normalizedDest, personId, entity);
    }
}
