using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Communication.Contracts.Flows;
using Ruth.Modules.Communication.Domain.Flows;
using Ruth.Modules.Communication.Infrastructure;
using Ruth.Modules.Crm.Contracts.People;
using Ruth.Modules.Groups.Contracts.Members;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Communication.Slices.Flows;

internal sealed class EnrollCommunicationFlowRecipientsCommandHandler(
    CommunicationDbContext dbContext,
    IClock clock,
    IQueryHandler<GetPersonByIdQuery, PersonDetailResponse>? personQueryHandler = null,
    IQueryHandler<GetGroupMembersQuery, IReadOnlyList<GroupMemberResponse>>? groupMembersHandler =
        null
) : ICommandHandler<EnrollCommunicationFlowRecipientsCommand, CommunicationFlowInstanceResponse>
{
    public async Task<Result<CommunicationFlowInstanceResponse>> HandleAsync(
        EnrollCommunicationFlowRecipientsCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var flowId = CommunicationFlowId.From(command.FlowId);
        var flow = await dbContext
            .CommunicationFlows.AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == flowId, cancellationToken);
        if (flow is null)
        {
            return Result.Failure<CommunicationFlowInstanceResponse>(
                CommunicationFlowErrors.FlowNotFound
            );
        }

        if (!flow.IsActive)
        {
            return Result.Failure<CommunicationFlowInstanceResponse>(
                CommunicationFlowErrors.FlowInactive
            );
        }

        var nowUtc = clock.UtcNow.ToUniversalTime();
        var instanceResult = await ResolveOrCreateInstanceAsync(
            flow,
            command,
            nowUtc,
            cancellationToken
        );
        if (instanceResult.IsFailure)
        {
            return Result.Failure<CommunicationFlowInstanceResponse>(instanceResult.Errors);
        }

        var instance = instanceResult.Value;
        var candidates = await ExpandCandidatesAsync(command, cancellationToken);
        var enrolled = await BuildDeduplicatedEnrollmentsAsync(
            flow,
            instance.Id,
            candidates,
            nowUtc,
            cancellationToken
        );

        if (enrolled.Count == 0)
        {
            return Result.Failure<CommunicationFlowInstanceResponse>(
                CommunicationFlowErrors.FlowEnrollmentEmpty
            );
        }

        dbContext.CommunicationFlowInstanceRecipients.AddRange(enrolled);
        _ = await dbContext.SaveChangesAsync(cancellationToken);

        var allRecipients = await dbContext
            .CommunicationFlowInstanceRecipients.AsNoTracking()
            .Where(r => r.FlowInstanceId == instance.Id)
            .ToListAsync(cancellationToken);
        return CommunicationFlowMapper.ToResponse(instance, allRecipients);
    }

    private async Task<Result<CommunicationFlowInstance>> ResolveOrCreateInstanceAsync(
        CommunicationFlow flow,
        EnrollCommunicationFlowRecipientsCommand command,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken
    )
    {
        if (command.ExistingInstanceId.HasValue && command.ExistingInstanceId.Value != Guid.Empty)
        {
            var instId = CommunicationFlowInstanceId.From(command.ExistingInstanceId.Value);
            var existing = await dbContext.CommunicationFlowInstances.FirstOrDefaultAsync(
                i => i.Id == instId && i.FlowId == flow.Id,
                cancellationToken
            );
            return existing is null
                ? Result.Failure<CommunicationFlowInstance>(
                    CommunicationFlowErrors.FlowInstanceNotFound
                )
                : existing;
        }

        var created = CommunicationFlowInstance.Create(flow.Id, command.CohortLabel, nowUtc);
        _ = dbContext.CommunicationFlowInstances.Add(created);
        return created;
    }

    private async Task<List<FlowRecipientEnrollmentInputDto>> ExpandCandidatesAsync(
        EnrollCommunicationFlowRecipientsCommand command,
        CancellationToken cancellationToken
    )
    {
        var list = new List<FlowRecipientEnrollmentInputDto>();
        if (command.Recipients is not null)
        {
            list.AddRange(command.Recipients);
        }

        if (
            command.TargetGroupId.HasValue
            && command.TargetGroupId.Value != Guid.Empty
            && groupMembersHandler is not null
        )
        {
            var members = await groupMembersHandler.HandleAsync(
                new GetGroupMembersQuery(command.TargetGroupId.Value, GroupMemberStatus.Active),
                cancellationToken
            );
            if (members.IsSuccess)
            {
                list.AddRange(
                    members.Value.Select(static m => new FlowRecipientEnrollmentInputDto(
                        PersonId: m.PersonId
                    ))
                );
            }
        }

        return list;
    }

    private async Task<List<CommunicationFlowInstanceRecipient>> BuildDeduplicatedEnrollmentsAsync(
        CommunicationFlow flow,
        CommunicationFlowInstanceId instanceId,
        List<FlowRecipientEnrollmentInputDto> candidates,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken
    )
    {
        var existingKeys = await LoadActiveIdentityKeysAsync(flow.Id, cancellationToken);
        var added = new List<CommunicationFlowInstanceRecipient>();

        foreach (var raw in candidates)
        {
            var hydrated = await HydratePersonContactAsync(raw, cancellationToken);
            var created = CommunicationFlowInstanceRecipient.Enroll(
                flow,
                instanceId,
                hydrated.PersonId,
                hydrated.EmailAddress,
                hydrated.PhoneNumber,
                hydrated.DisplayName,
                hydrated.MergeFields,
                nowUtc
            );
            if (created.IsFailure)
            {
                continue;
            }

            var dedupeKey = created.Value.NormalizedEmail
                ?? created.Value.PhoneNumber
                ?? created.Value.Id.ToString();
            if (existingKeys.Add(dedupeKey))
            {
                added.Add(created.Value);
            }
        }

        return added;
    }

    private async Task<HashSet<string>> LoadActiveIdentityKeysAsync(
        CommunicationFlowId flowId,
        CancellationToken cancellationToken
    )
    {
        var active = await dbContext
            .CommunicationFlowInstanceRecipients.AsNoTracking()
            .Where(r => r.FlowId == flowId && r.Status == FlowRecipientStatus.Active)
            .Select(static r => new { r.NormalizedEmail, r.PhoneNumber })
            .ToListAsync(cancellationToken);

        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var a in active)
        {
            if (a.NormalizedEmail is not null)
            {
                _ = set.Add(a.NormalizedEmail);
            }

            if (a.PhoneNumber is not null)
            {
                _ = set.Add(a.PhoneNumber);
            }
        }

        return set;
    }

    private async Task<FlowRecipientEnrollmentInputDto> HydratePersonContactAsync(
        FlowRecipientEnrollmentInputDto input,
        CancellationToken cancellationToken
    )
    {
        if (
            !string.IsNullOrWhiteSpace(input.EmailAddress)
            || !string.IsNullOrWhiteSpace(input.PhoneNumber)
            || !input.PersonId.HasValue
            || input.PersonId.Value == Guid.Empty
            || personQueryHandler is null
        )
        {
            return input;
        }

        var res = await personQueryHandler.HandleAsync(
            new GetPersonByIdQuery(input.PersonId.Value),
            cancellationToken
        );
        if (res.IsFailure)
        {
            return input;
        }

        var p = res.Value;
        var sms = p
            .PhoneNumbers.FirstOrDefault(static ph =>
                ph.IsMessagingEnabled && !ph.IsMessagingOptedOut
            )
            ?.E164Number;
        return input with
        {
            EmailAddress = p.IsEmailActive ? p.Email : null,
            PhoneNumber = sms,
            DisplayName = input.DisplayName ?? p.FullName,
        };
    }
}
