using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Engagement.Contracts.Events;
using Ruth.Modules.Engagement.Contracts.Touchpoints;
using Ruth.Modules.Engagement.Domain;
using Ruth.Modules.Engagement.Domain.Touchpoints;
using Ruth.Modules.Engagement.Infrastructure;
using Ruth.Modules.Engagement.Slices.Pipelines;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Engagement.Slices.Touchpoints;

internal sealed class CompleteTouchpointCommandHandler(
    EngagementDbContext dbContext,
    RelationshipHealthCrmCollaborator crmCollaborator,
    IQueryDispatcher queryDispatcher,
    IClock clock
) : ICommandHandler<CompleteTouchpointCommand, TouchpointResponse>
{
    public async Task<Result<TouchpointResponse>> HandleAsync(
        CompleteTouchpointCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);
        var touchpoint = await dbContext.Touchpoints.FirstOrDefaultAsync(
            t => t.Id == TouchpointId.From(command.TouchpointId),
            cancellationToken
        );
        if (touchpoint is null)
        {
            return EngagementErrors.TouchpointNotFound;
        }

        var contact = await dbContext.ConstituentContacts.FirstOrDefaultAsync(
            c => c.Id == ConstituentContactId.From(touchpoint.ConstituentContactId),
            cancellationToken
        );
        if (contact is null)
        {
            return EngagementErrors.ConstituentContactNotFound;
        }

        var now = clock.UtcNow;
        var done = touchpoint.Complete(
            command.ChannelUsed,
            command.OutcomeSentiment,
            command.Summary,
            now
        );
        return done.IsFailure
            ? Result.Failure<TouchpointResponse>(done.Errors)
            : await FinalizeCompletionAsync(command, touchpoint, contact, now, cancellationToken);
    }

    private async Task<Result<TouchpointResponse>> FinalizeCompletionAsync(
        CompleteTouchpointCommand command,
        Touchpoint touchpoint,
        ConstituentContact contact,
        DateTimeOffset now,
        CancellationToken cancellationToken
    )
    {
        var today = DateOnly.FromDateTime(now.UtcDateTime);
        contact.RecordCompletedTouchpoint(
            touchpoint.Kind,
            command.ChannelUsed,
            command.OutcomeSentiment,
            command.UpdatedRelationshipStrength,
            now,
            today
        );

        await EnsureNextScheduledCadenceAsync(contact, touchpoint.Id.Value, cancellationToken);
        await crmCollaborator.SyncPeerNetworkEdgeAsync(contact, today, cancellationToken);

        dbContext.EnqueueIntegrationEvent(
            new TouchpointCompletedIntegrationEvent(
                Guid.CreateVersion7(),
                now,
                touchpoint.Id.Value,
                contact.Id.Value,
                contact.OwnerPersonId,
                contact.ConstituentPersonId,
                contact.HealthScore
            )
        );

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        var people = await PipelineProjectionMapper.LoadPeopleAsync(
            queryDispatcher,
            [contact.ConstituentPersonId],
            cancellationToken
        );
        return TouchpointProjectionMapper.ToResponse(touchpoint, contact.PortfolioRole, people);
    }

    private async Task EnsureNextScheduledCadenceAsync(
        ConstituentContact contact,
        Guid completedTouchpointId,
        CancellationToken cancellationToken
    )
    {
        if (!contact.IsActive || !contact.NextScheduledTouchpointDate.HasValue)
        {
            return;
        }

        var hasOpen = await dbContext.Touchpoints.AnyAsync(
            t =>
                t.ConstituentContactId == contact.Id.Value
                && t.Id != TouchpointId.From(completedTouchpointId)
                && t.Kind == TouchpointKind.ScheduledCadence
                && (t.Status == TouchpointStatus.Pending || t.Status == TouchpointStatus.Snoozed),
            cancellationToken
        );
        if (!hasOpen)
        {
            var next = Touchpoint.Schedule(
                contact,
                TouchpointKind.ScheduledCadence,
                contact.PreferredChannel,
                contact.NextScheduledTouchpointDate.Value,
                $"Scheduled {contact.PortfolioRole} outreach"
            );
            if (next.IsSuccess)
            {
                _ = dbContext.Touchpoints.Add(next.Value);
            }
        }
    }
}
