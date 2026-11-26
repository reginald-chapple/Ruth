using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Engagement.Contracts.Touchpoints;
using Ruth.Modules.Engagement.Domain.Touchpoints;
using Ruth.Modules.Engagement.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Engagement.Slices.Touchpoints;

internal sealed class GeneratePortfolioCadenceTouchpointsCommandHandler(
    EngagementDbContext dbContext,
    IClock clock
) : ICommandHandler<GeneratePortfolioCadenceTouchpointsCommand, TouchpointCadenceRunResponse>
{
    public async Task<Result<TouchpointCadenceRunResponse>> HandleAsync(
        GeneratePortfolioCadenceTouchpointsCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);
        var today = DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);
        var horizon = today.AddDays(Math.Clamp(command.HorizonDays, 0, 90));

        var contactQuery = dbContext.ConstituentContacts.Where(static c => c.IsActive);
        if (command.OwnerPersonId is { } oid && oid != Guid.Empty)
        {
            contactQuery = contactQuery.Where(c => c.OwnerPersonId == oid);
        }

        var contacts = await contactQuery.ToListAsync(cancellationToken);
        if (contacts.Count == 0)
        {
            return new TouchpointCadenceRunResponse(0, 0);
        }

        var contactIds = contacts.Select(static c => c.Id.Value).ToHashSet();
        var openTouchpoints = await dbContext
            .Touchpoints.AsNoTracking()
            .Where(t =>
                contactIds.Contains(t.ConstituentContactId)
                && (t.Status == TouchpointStatus.Pending || t.Status == TouchpointStatus.Snoozed)
            )
            .ToListAsync(cancellationToken);

        var generated = 0;
        foreach (var contact in contacts)
        {
            contact.RefreshScheduleAndHealth(today);
            generated += EnsureCadenceItems(contact, openTouchpoints, today, horizon);
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return new TouchpointCadenceRunResponse(generated, contacts.Count);
    }

    private int EnsureCadenceItems(
        ConstituentContact contact,
        List<Touchpoint> openTouchpoints,
        DateOnly today,
        DateOnly horizon
    )
    {
        var count = 0;
        if (
            contact.NextScheduledTouchpointDate is { } due
            && due <= horizon
            && !openTouchpoints.Exists(t =>
                t.ConstituentContactId == contact.Id.Value
                && t.Kind == TouchpointKind.ScheduledCadence
            )
        )
        {
            var tp = Touchpoint.Schedule(
                contact,
                TouchpointKind.ScheduledCadence,
                contact.PreferredChannel,
                due,
                $"Scheduled {contact.PortfolioRole} outreach"
            );
            if (tp.IsSuccess)
            {
                _ = dbContext.Touchpoints.Add(tp.Value);
                count++;
            }
        }

        var baseline =
            contact.CreatedOnUtc == default
                ? today
                : DateOnly.FromDateTime(contact.CreatedOnUtc.UtcDateTime);
        var noteDue = TouchpointCadenceScheduler.ComputeNextConnectionNoteDate(
            contact.ConnectionNoteCadenceDays,
            contact.LastConnectionNoteAtUtc,
            baseline
        );
        if (
            noteDue is { } nd
            && nd <= horizon
            && !openTouchpoints.Exists(t =>
                t.ConstituentContactId == contact.Id.Value
                && t.Kind == TouchpointKind.ConnectionNote
            )
        )
        {
            var noteTp = Touchpoint.Schedule(
                contact,
                TouchpointKind.ConnectionNote,
                TouchpointChannel.HandwrittenNote,
                nd,
                "Periodic personal connection note"
            );
            if (noteTp.IsSuccess)
            {
                _ = dbContext.Touchpoints.Add(noteTp.Value);
                count++;
            }
        }

        return count;
    }
}
