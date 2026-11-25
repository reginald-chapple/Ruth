using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Crm.Contracts.People;
using Ruth.Modules.Crm.Domain.People;
using Ruth.Modules.Crm.Domain.Signals;
using Ruth.Modules.Crm.Infrastructure;

namespace Ruth.Modules.Crm.Slices.Duplicates;

internal static class PersonMergeTransferHelper
{
    public static int TransferPhonesAndIdentityChildren(Person surviving, Person retired)
    {
        var phonesTransferred = TransferPhones(surviving, retired);
        TransferNamesAndSearchKeys(surviving, retired);
        MergeMissingAttributes(surviving, retired);
        return phonesTransferred;
    }

    private static int TransferPhones(Person surviving, Person retired)
    {
        var phonesTransferred = 0;

        foreach (var retiredPhone in retired.PhoneNumbers.ToList())
        {
            var matching = surviving.PhoneNumbers.FirstOrDefault(p =>
                p.NumberType == retiredPhone.NumberType
                && string.Equals(p.E164Number, retiredPhone.E164Number, StringComparison.Ordinal)
            );
            if (matching is not null)
            {
                if (retiredPhone.IsMessagingEnabled)
                {
                    matching.EnableMessagingConsent();
                }

                continue;
            }

            var added = surviving.AddPhoneNumber(
                new PhoneNumberInputDto(
                    retiredPhone.NumberType,
                    retiredPhone.Number,
                    retiredPhone.CountryCode,
                    retiredPhone.Extension,
                    retiredPhone.IsMessagingEnabled,
                    retiredPhone.IsUnlisted,
                    retiredPhone.Description,
                    retiredPhone.NumberTypeValueId
                )
            );
            if (added.IsSuccess)
            {
                phonesTransferred++;
            }
        }

        return phonesTransferred;
    }

    private static void TransferNamesAndSearchKeys(Person surviving, Person retired)
    {
        if (
            !string.IsNullOrWhiteSpace(retired.LastName)
            && !string.Equals(
                surviving.LastName,
                retired.LastName,
                StringComparison.OrdinalIgnoreCase
            )
        )
        {
            _ = surviving.AddPreviousName(retired.LastName);
        }

        foreach (var prev in retired.PreviousNames)
        {
            _ = surviving.AddPreviousName(prev.LastName);
        }

        if (
            !string.IsNullOrWhiteSpace(retired.Email)
            && !string.Equals(surviving.Email, retired.Email, StringComparison.OrdinalIgnoreCase)
        )
        {
            _ = surviving.AddSearchKey(PersonSearchKeyType.Email, retired.Email);
        }

        foreach (var key in retired.SearchKeys)
        {
            _ = surviving.AddSearchKey(
                key.KeyType,
                key.SearchValue,
                key.IsValuePrivate,
                key.SearchTypeValueId
            );
        }
    }

    public static async Task<int> TransferHouseholdsAsync(
        CrmDbContext dbContext,
        PersonId survivingId,
        PersonId retiredId,
        CancellationToken cancellationToken
    )
    {
        var retiredHouseholds = await dbContext
            .Households.Include(static h => h.Members)
            .Where(h => h.Members.Any(m => m.PersonId == retiredId))
            .ToListAsync(cancellationToken);

        var count = 0;
        foreach (var household in retiredHouseholds)
        {
            var retiredMember = household.Members.FirstOrDefault(m => m.PersonId == retiredId);
            if (retiredMember is null)
            {
                continue;
            }

            var survivorAlreadyPresent = household.Members.Any(m => m.PersonId == survivingId);
            if (!survivorAlreadyPresent)
            {
                retiredMember.RepointPerson(survivingId);
            }
            else
            {
                _ = household.RemoveMember(retiredId);
            }

            if (household.Members.Count == 0)
            {
                household.Archive();
            }

            count++;
        }

        return count;
    }

    public static async Task<int> TransferRelationshipsAsync(
        CrmDbContext dbContext,
        PersonId survivingId,
        PersonId retiredId,
        CancellationToken cancellationToken
    )
    {
        var rels = await dbContext
            .KnownRelationships.Where(r =>
                r.OwnerPersonId == retiredId || r.RelatedPersonId == retiredId
            )
            .ToListAsync(cancellationToken);

        var existingSurvivorKeys = await dbContext
            .KnownRelationships.AsNoTracking()
            .Where(r => r.OwnerPersonId == survivingId || r.RelatedPersonId == survivingId)
            .Select(static r => new
            {
                r.OwnerPersonId,
                r.RelatedPersonId,
                r.RelationshipType,
            })
            .ToListAsync(cancellationToken);

        var seen = existingSurvivorKeys
            .Select(static k => (k.OwnerPersonId, k.RelatedPersonId, k.RelationshipType))
            .ToHashSet();

        var count = 0;
        foreach (var rel in rels)
        {
            var newOwner = rel.OwnerPersonId == retiredId ? survivingId : rel.OwnerPersonId;
            var newRelated = rel.RelatedPersonId == retiredId ? survivingId : rel.RelatedPersonId;

            if (newOwner == newRelated || !seen.Add((newOwner, newRelated, rel.RelationshipType)))
            {
                _ = dbContext.KnownRelationships.Remove(rel);
                continue;
            }

            rel.RepointOwner(newOwner);
            rel.RepointRelated(newRelated);
            count++;
        }

        await TransferPeerNetworkEdgesAsync(dbContext, survivingId, retiredId, cancellationToken);
        return count;
    }

    public static async Task TransferW2S4RecordsAsync(
        CrmDbContext dbContext,
        Person surviving,
        PersonId retiredId,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken
    )
    {
        var signals = await dbContext
            .PersonSignals.Where(s => s.PersonId == retiredId)
            .ToListAsync(cancellationToken);
        foreach (var signal in signals)
        {
            signal.RepointPerson(surviving.Id);
        }

        var assessments = await dbContext
            .Assessments.Where(a => a.PersonId == retiredId)
            .ToListAsync(cancellationToken);
        foreach (var assessment in assessments)
        {
            assessment.RepointPerson(surviving.Id);
        }

        var checks = await dbContext
            .BackgroundChecks.Where(b => b.PersonId == retiredId)
            .ToListAsync(cancellationToken);
        foreach (var check in checks)
        {
            check.RepointPerson(surviving.Id);
        }

        var views = await dbContext
            .PersonViewedEntries.Where(v => v.TargetPersonId == retiredId)
            .ToListAsync(cancellationToken);
        foreach (var view in views)
        {
            view.RepointTarget(surviving.Id);
        }

        if (views.Count > 0)
        {
            surviving.IncrementViewedCount(views.Count);
        }

        if (signals.Count > 0)
        {
            var allSignals = await dbContext
                .PersonSignals.Where(s => s.PersonId == surviving.Id || s.PersonId == retiredId)
                .ToListAsync(cancellationToken);
            var types = await dbContext
                .SignalTypes.AsNoTracking()
                .ToDictionaryAsync(static t => t.Id, cancellationToken);
            TopSignalCalculator.Recalculate(surviving, allSignals, types, nowUtc);
        }
    }

    private static async Task TransferPeerNetworkEdgesAsync(
        CrmDbContext dbContext,
        PersonId survivingId,
        PersonId retiredId,
        CancellationToken cancellationToken
    )
    {
        var edges = await dbContext
            .PeerNetworkEdges.Where(e =>
                e.SourcePersonId == retiredId || e.TargetPersonId == retiredId
            )
            .ToListAsync(cancellationToken);

        foreach (var edge in edges)
        {
            var newSrc = edge.SourcePersonId == retiredId ? survivingId : edge.SourcePersonId;
            var newDst = edge.TargetPersonId == retiredId ? survivingId : edge.TargetPersonId;
            if (newSrc == newDst)
            {
                _ = dbContext.PeerNetworkEdges.Remove(edge);
            }
            else
            {
                edge.RepointSource(newSrc);
                edge.RepointTarget(newDst);
            }
        }
    }

    private static void MergeMissingAttributes(Person surviving, Person retired)
    {
        foreach (var pair in retired.Attributes)
        {
            if (
                !surviving.Attributes.ContainsKey(pair.Key)
                && !string.IsNullOrWhiteSpace(pair.Value)
            )
            {
                surviving.SetAttributeValue(pair.Key, pair.Value);
            }
        }
    }
}
