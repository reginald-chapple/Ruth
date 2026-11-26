using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Communication.Contracts.BulkCommunications;
using Ruth.Modules.Communication.Domain.BulkCommunications;
using Ruth.Modules.Communication.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Communication.Slices.BulkCommunications;

internal sealed class UpdateRecipientPreferencesCommandHandler(
    CommunicationDbContext dbContext,
    PreferenceResolutionHelper helper
) : ICommandHandler<UpdateRecipientPreferencesCommand, CommunicationPreferenceResponse>
{
    public async Task<Result<CommunicationPreferenceResponse>> HandleAsync(
        UpdateRecipientPreferencesCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var target = helper.ResolveTarget(
            command.PreferenceToken,
            command.DestinationAddress,
            command.Channel,
            command.PersonId
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

        entity.ApplyPreferences(
            command.GlobalOptOut,
            command.OptedOutTopics,
            command.Reason ?? "PreferenceCenterUpdate",
            personId
        );
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return helper.ToResponse(channel, normalizedDest, personId, entity);
    }
}
