using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Communication.Contracts.BulkCommunications;
using Ruth.Modules.Communication.Contracts.Transports;
using Ruth.Modules.Communication.Domain.BulkCommunications;
using Ruth.Modules.Communication.Infrastructure;
using Ruth.Modules.Crm.Contracts.People;
using Ruth.Modules.Groups.Contracts.Members;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Communication.Slices.BulkCommunications;

internal sealed class BulkRecipientResolver(
    CommunicationDbContext dbContext,
    IQueryHandler<GetPersonByIdQuery, PersonDetailResponse>? personQueryHandler = null,
    IQueryHandler<GetGroupMembersQuery, IReadOnlyList<GroupMemberResponse>>? groupMembersHandler =
        null
)
{
    public async Task<Result<List<CommunicationRecipient>>> BuildRecipientsAsync(
        CommunicationId communicationId,
        CommunicationChannel channel,
        IReadOnlyList<CommunicationRecipientInputDto>? explicitRecipients,
        Guid? targetGroupId,
        CancellationToken cancellationToken
    )
    {
        var list = new List<CommunicationRecipient>();
        var seenDestinations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (explicitRecipients is not null)
        {
            var explicitResult = await AppendExplicitRecipientsAsync(
                communicationId,
                channel,
                explicitRecipients,
                list,
                seenDestinations,
                cancellationToken
            );
            if (explicitResult.IsFailure)
            {
                return explicitResult;
            }
        }

        if (
            targetGroupId.HasValue
            && targetGroupId.Value != Guid.Empty
            && groupMembersHandler is not null
        )
        {
            await AppendGroupRecipientsAsync(
                communicationId,
                channel,
                targetGroupId.Value,
                list,
                seenDestinations,
                cancellationToken
            );
        }

        return list;
    }

    private async Task<Result<List<CommunicationRecipient>>> AppendExplicitRecipientsAsync(
        CommunicationId communicationId,
        CommunicationChannel channel,
        IReadOnlyList<CommunicationRecipientInputDto> inputs,
        List<CommunicationRecipient> output,
        HashSet<string> seenDestinations,
        CancellationToken cancellationToken
    )
    {
        foreach (var item in inputs)
        {
            var resolved = await ResolveCandidateAsync(channel, item, cancellationToken);
            if (resolved is null)
            {
                if (!string.IsNullOrWhiteSpace(item.DestinationAddress))
                {
                    return Result.Failure<List<CommunicationRecipient>>(
                        BulkCommunicationErrors.RecipientDestinationInvalid
                    );
                }

                continue;
            }

            var recipient = CommunicationRecipient.Create(
                communicationId,
                channel,
                resolved.Value.Destination,
                item.PersonId,
                resolved.Value.DisplayName,
                item.MergeFields
            );
            if (recipient.IsFailure)
            {
                return Result.Failure<List<CommunicationRecipient>>(recipient.Errors);
            }

            if (seenDestinations.Add(recipient.Value.NormalizedDestination))
            {
                output.Add(recipient.Value);
            }
        }

        return output;
    }

    private async Task AppendGroupRecipientsAsync(
        CommunicationId communicationId,
        CommunicationChannel channel,
        Guid groupId,
        List<CommunicationRecipient> output,
        HashSet<string> seenDestinations,
        CancellationToken cancellationToken
    )
    {
        var membersResult = await groupMembersHandler!.HandleAsync(
            new GetGroupMembersQuery(groupId, GroupMemberStatus.Active),
            cancellationToken
        );
        if (membersResult.IsFailure)
        {
            return;
        }

        foreach (var member in membersResult.Value)
        {
            var resolved = await ResolveCandidateAsync(
                channel,
                new CommunicationRecipientInputDto(PersonId: member.PersonId),
                cancellationToken
            );
            if (resolved is null)
            {
                continue;
            }

            var created = CommunicationRecipient.Create(
                communicationId,
                channel,
                resolved.Value.Destination,
                member.PersonId,
                resolved.Value.DisplayName
            );
            if (created.IsSuccess && seenDestinations.Add(created.Value.NormalizedDestination))
            {
                output.Add(created.Value);
            }
        }
    }

    private async Task<(string Destination, string? DisplayName)?> ResolveCandidateAsync(
        CommunicationChannel channel,
        CommunicationRecipientInputDto input,
        CancellationToken cancellationToken
    )
    {
        if (!string.IsNullOrWhiteSpace(input.DestinationAddress))
        {
            return (input.DestinationAddress.Trim(), input.DisplayName);
        }

        if (!input.PersonId.HasValue || input.PersonId.Value == Guid.Empty)
        {
            return null;
        }

        if (channel == CommunicationChannel.Push)
        {
            var token = await dbContext
                .PersonalDevices.AsNoTracking()
                .Where(d =>
                    d.PersonId == input.PersonId.Value && d.IsActive && d.NotificationsEnabled
                )
                .Select(static d => d.PushRegistrationToken)
                .FirstOrDefaultAsync(cancellationToken);
            return string.IsNullOrWhiteSpace(token) ? null : (token, input.DisplayName);
        }

        if (personQueryHandler is null)
        {
            return null;
        }

        var personResult = await personQueryHandler.HandleAsync(
            new GetPersonByIdQuery(input.PersonId.Value),
            cancellationToken
        );
        return personResult.IsFailure
            ? null
            : ExtractFromPerson(channel, personResult.Value, input.DisplayName);
    }

    private static (string Destination, string? DisplayName)? ExtractFromPerson(
        CommunicationChannel channel,
        PersonDetailResponse person,
        string? overrideName
    )
    {
        var name = string.IsNullOrWhiteSpace(overrideName) ? person.FullName : overrideName;
        if (channel == CommunicationChannel.Email)
        {
            return
                person.IsEmailActive
                && person.EmailPreference != EmailPreference.DoNotEmail
                && !string.IsNullOrWhiteSpace(person.Email)
                ? (person.Email, name)
                : null;
        }

        var smsPhone = person.PhoneNumbers.FirstOrDefault(static p =>
            p.IsMessagingEnabled && !p.IsMessagingOptedOut
        );
        return string.IsNullOrWhiteSpace(smsPhone?.E164Number) ? null : (smsPhone.E164Number, name);
    }
}
