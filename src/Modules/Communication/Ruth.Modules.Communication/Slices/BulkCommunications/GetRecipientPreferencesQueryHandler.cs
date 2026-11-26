using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Communication.Contracts.BulkCommunications;
using Ruth.Modules.Communication.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Communication.Slices.BulkCommunications;

internal sealed class GetRecipientPreferencesQueryHandler(
    CommunicationDbContext dbContext,
    PreferenceResolutionHelper helper
) : IQueryHandler<GetRecipientPreferencesQuery, CommunicationPreferenceResponse>
{
    public async Task<Result<CommunicationPreferenceResponse>> HandleAsync(
        GetRecipientPreferencesQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);

        var target = helper.ResolveTarget(
            query.PreferenceToken,
            query.DestinationAddress,
            query.Channel,
            query.PersonId
        );
        if (target.IsFailure)
        {
            return Result.Failure<CommunicationPreferenceResponse>(target.Errors);
        }

        var (channel, normalizedDest, personId) = target.Value;
        var entity = await dbContext
            .CommunicationSubscriptionPreferences.AsNoTracking()
            .FirstOrDefaultAsync(
                p => p.Channel == channel && p.NormalizedDestination == normalizedDest,
                cancellationToken
            );

        return helper.ToResponse(channel, normalizedDest, personId, entity);
    }
}
