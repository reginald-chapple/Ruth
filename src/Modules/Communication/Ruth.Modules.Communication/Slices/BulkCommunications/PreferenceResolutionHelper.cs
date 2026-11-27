using Microsoft.Extensions.Options;
using Ruth.Modules.Communication.Contracts.BulkCommunications;
using Ruth.Modules.Communication.Contracts.Transports;
using Ruth.Modules.Communication.Domain.BulkCommunications;
using Ruth.Modules.Communication.Infrastructure;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Tenancy;

namespace Ruth.Modules.Communication.Slices.BulkCommunications;

internal sealed class PreferenceResolutionHelper(IOptions<CommunicationBulkOptions> bulkOptions)
{
    private readonly CommunicationBulkOptions _options = bulkOptions.Value;

    public Result<(
        CommunicationChannel Channel,
        string NormalizedDestination,
        Guid? PersonId
    )> ResolveTarget(
        string? preferenceToken,
        string? explicitDestination,
        CommunicationChannel explicitChannel,
        Guid? explicitPersonId
    )
    {
        if (!string.IsNullOrWhiteSpace(preferenceToken))
        {
            return PreferenceTokenSigner.Verify(
                preferenceToken,
                _options.PreferenceSigningSecret,
                AmbientTenantContext.CurrentTenantId
            );
        }

        var normalized = CommunicationRecipient.NormalizeDestination(
            explicitChannel,
            explicitDestination
        );
        return normalized.IsFailure
            ? Result.Failure<(CommunicationChannel, string, Guid?)>(normalized.Errors)
            : (explicitChannel, normalized.Value.Normalized, explicitPersonId);
    }

    public CommunicationPreferenceResponse ToResponse(
        CommunicationChannel channel,
        string normalizedDestination,
        Guid? personId,
        CommunicationSubscriptionPreference? entity
    )
    {
        var token = PreferenceTokenSigner.Sign(
            channel,
            normalizedDestination,
            entity?.PersonId ?? personId,
            _options.PreferenceSigningSecret,
            AmbientTenantContext.CurrentTenantId
        );
        return new CommunicationPreferenceResponse(
            normalizedDestination,
            entity?.PersonId ?? personId,
            channel,
            entity?.GlobalOptOut ?? false,
            entity is null ? [] : [.. entity.OptedOutTopics.Order(StringComparer.Ordinal)],
            CommunicationSubscriptionPreference.DefaultAvailableTopics,
            token,
            entity?.ModifiedOnUtc ?? entity?.CreatedOnUtc
        );
    }
}
