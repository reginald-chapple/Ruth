using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Crm.Contracts.Events;
using Ruth.Modules.Engagement.Infrastructure;
using Ruth.SharedKernel.Events;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Engagement.Slices.Events;

/// <summary>Idempotently repoints and merges PersonId references across W5-S1..W5-S4 upon PersonMergedIntegrationEvent.</summary>
internal sealed class EngagementPersonMergedConsumer(EngagementDbContext dbContext, IClock clock)
    : IIntegrationEventConsumer<PersonMergedIntegrationEvent>
{
    public async Task ConsumeAsync(
        PersonMergedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);
        var retired = integrationEvent.RetiredPersonId;
        var surviving = integrationEvent.SurvivingPersonId;
        if (retired == Guid.Empty || surviving == Guid.Empty || retired == surviving)
        {
            return;
        }

        var changed = false;
        changed |= await RepointPipelinesAndJourneysAsync(retired, surviving, cancellationToken);
        changed |= await MergeStreaksAndAchievementsAsync(retired, surviving, cancellationToken);
        changed |= await RepointSupportRemindersFollowingAndPortfolioAsync(
            retired,
            surviving,
            cancellationToken
        );

        if (changed)
        {
            _ = await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task<bool> RepointPipelinesAndJourneysAsync(
        Guid retired,
        Guid surviving,
        CancellationToken ct
    )
    {
        var modified = false;
        foreach (var opp in await dbContext.EngagementOpportunities.ToListAsync(ct))
        {
            modified |= opp.RepointPerson(retired, surviving);
        }

        var reqs = await dbContext
            .EngagementRequests.Include(static r => r.Activities)
            .Where(r =>
                r.RequesterPersonId == retired
                || r.AssignedConnectorPersonId == retired
                || r.Activities.Any(a => a.ConnectorPersonId == retired)
            )
            .ToListAsync(ct);
        foreach (var r in reqs)
        {
            modified |= r.RepointPerson(retired, surviving);
        }

        foreach (
            var pm in await dbContext
                .PersonMilestones.Where(m => m.PersonId == retired)
                .ToListAsync(ct)
        )
        {
            modified |= pm.RepointPerson(retired, surviving);
        }

        var survivingCompletions = await dbContext
            .JourneyCompletions.Where(c => c.PersonId == surviving)
            .Select(static c => c.JourneyProgramId)
            .ToHashSetAsync(ct);
        foreach (
            var jc in await dbContext
                .JourneyCompletions.Where(c => c.PersonId == retired)
                .ToListAsync(ct)
        )
        {
            if (survivingCompletions.Contains(jc.JourneyProgramId))
            {
                _ = dbContext.JourneyCompletions.Remove(jc);
                modified = true;
            }
            else
            {
                modified |= jc.RepointPerson(retired, surviving);
            }
        }

        return modified;
    }

    private async Task<bool> MergeStreaksAndAchievementsAsync(
        Guid retired,
        Guid surviving,
        CancellationToken ct
    )
    {
        var modified = false;
        var today = DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);
        var retiredStreaks = await dbContext
            .Streaks.Where(s => s.PersonId == retired)
            .ToListAsync(ct);
        if (retiredStreaks.Count > 0)
        {
            var streakTypes = await dbContext
                .StreakTypes.AsNoTracking()
                .Include(static t => t.Exclusions)
                .ToDictionaryAsync(static t => t.Id.Value, ct);
            var survivingStreaks = await dbContext
                .Streaks.Where(s => s.PersonId == surviving)
                .ToDictionaryAsync(static s => s.StreakTypeId, ct);

            foreach (var rs in retiredStreaks)
            {
                if (
                    survivingStreaks.TryGetValue(rs.StreakTypeId, out var ss)
                    && streakTypes.TryGetValue(rs.StreakTypeId, out var st)
                )
                {
                    ss.MergeFrom(rs, st, today);
                    _ = dbContext.Streaks.Remove(rs);
                }
                else
                {
                    _ = rs.RepointPerson(retired, surviving);
                }

                modified = true;
            }
        }

        foreach (
            var att in await dbContext
                .AchievementAttempts.Where(a => a.PersonId == retired)
                .ToListAsync(ct)
        )
        {
            modified |= att.RepointPerson(retired, surviving);
        }

        return modified;
    }

    private async Task<bool> RepointSupportRemindersFollowingAndPortfolioAsync(
        Guid retired,
        Guid surviving,
        CancellationToken ct
    )
    {
        var modified = false;
        foreach (
            var cat in await dbContext
                .SupportCategories.Where(c => c.DefaultCoordinatorPersonId == retired)
                .ToListAsync(ct)
        )
        {
            modified |= cat.RepointPerson(retired, surviving);
        }

        var sreqs = await dbContext
            .SupportRequests.Include(static r => r.Comments)
            .Where(r =>
                r.RequesterPersonId == retired
                || r.AssignedCareCoordinatorPersonId == retired
                || r.ApprovedByPersonId == retired
                || r.Comments.Any(c => c.AuthorPersonId == retired)
            )
            .ToListAsync(ct);
        foreach (var sr in sreqs)
        {
            modified |= sr.RepointPerson(retired, surviving);
        }

        foreach (
            var rem in await dbContext
                .Reminders.Where(r => r.AssigneePersonId == retired)
                .ToListAsync(ct)
        )
        {
            modified |= rem.RepointPerson(retired, surviving);
        }

        foreach (
            var n in await dbContext
                .NotificationMessages.Where(n => n.RecipientPersonId == retired)
                .ToListAsync(ct)
        )
        {
            modified |= n.RepointPerson(retired, surviving);
        }

        modified |= await MergeFollowingSubscriptionsAsync(retired, surviving, ct);
        modified |= await MergePortfolioAsync(retired, surviving, ct);
        return modified;
    }

    private async Task<bool> MergeFollowingSubscriptionsAsync(
        Guid retired,
        Guid surviving,
        CancellationToken ct
    )
    {
        var modified = false;
        var survivingList = await dbContext
            .FollowingSubscriptions.Where(f => f.FollowerPersonId == surviving)
            .Select(static f => $"{f.EntityTypeKey}|{f.EntityId:D}|{f.PurposeKey}")
            .ToListAsync(ct);
        var survivingSubs = survivingList.ToHashSet(StringComparer.Ordinal);
        foreach (
            var f in await dbContext
                .FollowingSubscriptions.Where(f => f.FollowerPersonId == retired)
                .ToListAsync(ct)
        )
        {
            var key = $"{f.EntityTypeKey}|{f.EntityId:D}|{f.PurposeKey}";
            if (survivingSubs.Contains(key))
            {
                _ = dbContext.FollowingSubscriptions.Remove(f);
            }
            else
            {
                _ = f.RepointFollower(retired, surviving);
            }

            modified = true;
        }

        return modified;
    }

    private async Task<bool> MergePortfolioAsync(Guid retired, Guid surviving, CancellationToken ct)
    {
        var modified = false;
        var allContacts = await dbContext
            .ConstituentContacts.Where(c =>
                c.OwnerPersonId == retired
                || c.ConstituentPersonId == retired
                || c.OwnerPersonId == surviving
                || c.ConstituentPersonId == surviving
            )
            .ToListAsync(ct);
        var existingPairs = allContacts
            .Where(c => c.OwnerPersonId != retired && c.ConstituentPersonId != retired)
            .Select(static c => (c.OwnerPersonId, c.ConstituentPersonId))
            .ToHashSet();

        foreach (
            var c in allContacts.Where(c =>
                c.OwnerPersonId == retired || c.ConstituentPersonId == retired
            )
        )
        {
            var newOwner = c.OwnerPersonId == retired ? surviving : c.OwnerPersonId;
            var newConstit = c.ConstituentPersonId == retired ? surviving : c.ConstituentPersonId;
            if (newOwner == newConstit || !existingPairs.Add((newOwner, newConstit)))
            {
                _ = dbContext.ConstituentContacts.Remove(c);
            }
            else
            {
                _ = c.RepointPerson(retired, surviving);
            }

            modified = true;
        }

        foreach (
            var t in await dbContext
                .Touchpoints.Where(t =>
                    t.OwnerPersonId == retired || t.ConstituentPersonId == retired
                )
                .ToListAsync(ct)
        )
        {
            modified |= t.RepointPerson(retired, surviving);
        }

        return modified;
    }
}
