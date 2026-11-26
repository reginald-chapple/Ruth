using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Engagement.Contracts.Touchpoints;
using Ruth.Modules.Engagement.Domain.Touchpoints;
using Ruth.Modules.Engagement.Infrastructure;
using Ruth.Modules.Engagement.Slices.Pipelines;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Engagement.Slices.Touchpoints;

internal sealed class UpsertConstituentContactCommandHandler(
    EngagementDbContext dbContext,
    IQueryDispatcher queryDispatcher,
    IClock clock
) : ICommandHandler<UpsertConstituentContactCommand, ConstituentContactResponse>
{
    public async Task<Result<ConstituentContactResponse>> HandleAsync(
        UpsertConstituentContactCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);
        var today = DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);

        var contactRes = await CreateOrUpdateContactAsync(command.Spec, today, cancellationToken);
        if (contactRes.IsFailure)
        {
            return Result.Failure<ConstituentContactResponse>(contactRes.Errors);
        }

        var contact = contactRes.Value;
        await EnsureInitialCadenceTouchpointAsync(contact, cancellationToken);

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        var people = await PipelineProjectionMapper.LoadPeopleAsync(
            queryDispatcher,
            [contact.ConstituentPersonId],
            cancellationToken
        );
        return TouchpointProjectionMapper.ToResponse(contact, people);
    }

    private async Task<Result<ConstituentContact>> CreateOrUpdateContactAsync(
        ConstituentContactSpec spec,
        DateOnly today,
        CancellationToken cancellationToken
    )
    {
        var existing = await dbContext.ConstituentContacts.FirstOrDefaultAsync(
            c =>
                c.OwnerPersonId == spec.OwnerPersonId
                && c.ConstituentPersonId == spec.ConstituentPersonId,
            cancellationToken
        );
        if (existing is null)
        {
            var created = ConstituentContact.Create(spec, today);
            if (created.IsSuccess)
            {
                _ = dbContext.ConstituentContacts.Add(created.Value);
            }

            return created;
        }

        var res = existing.ApplySpec(spec, today);
        return res.IsFailure ? Result.Failure<ConstituentContact>(res.Errors) : existing;
    }

    private async Task EnsureInitialScheduledTouchpointAsync(
        ConstituentContact contact,
        CancellationToken cancellationToken
    )
    {
        var hasOpen = await dbContext.Touchpoints.AnyAsync(
            t =>
                t.ConstituentContactId == contact.Id.Value
                && t.Kind == TouchpointKind.ScheduledCadence
                && (t.Status == TouchpointStatus.Pending || t.Status == TouchpointStatus.Snoozed),
            cancellationToken
        );
        if (!hasOpen && contact.NextScheduledTouchpointDate.HasValue)
        {
            var tp = Touchpoint.Schedule(
                contact,
                TouchpointKind.ScheduledCadence,
                contact.PreferredChannel,
                contact.NextScheduledTouchpointDate.Value,
                $"Scheduled {contact.PortfolioRole} outreach"
            );
            if (tp.IsSuccess)
            {
                _ = dbContext.Touchpoints.Add(tp.Value);
            }
        }
    }

    private Task EnsureInitialCadenceTouchpointAsync(
        ConstituentContact contact,
        CancellationToken cancellationToken
    )
    {
        return contact.IsActive && contact.NextScheduledTouchpointDate.HasValue
            ? EnsureInitialScheduledTouchpointAsync(contact, cancellationToken)
            : Task.CompletedTask;
    }
}
