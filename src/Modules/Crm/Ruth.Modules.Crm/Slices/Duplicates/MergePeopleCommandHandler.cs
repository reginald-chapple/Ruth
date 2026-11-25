using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Crm.Contracts.Duplicates;
using Ruth.Modules.Crm.Contracts.Events;
using Ruth.Modules.Crm.Domain;
using Ruth.Modules.Crm.Domain.Duplicates;
using Ruth.Modules.Crm.Domain.People;
using Ruth.Modules.Crm.Infrastructure;
using Ruth.Modules.Crm.Infrastructure.Graph;
using Ruth.Modules.Crm.Slices.Households;
using Ruth.Modules.Crm.Slices.People;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Crm.Slices.Duplicates;

internal sealed class MergePeopleCommandHandler(
    CrmDbContext dbContext,
    ICrmGraphService graphService,
    IClock clock
) : ICommandHandler<MergePeopleCommand, PersonMergeResponse>
{
    public async Task<Result<PersonMergeResponse>> HandleAsync(
        MergePeopleCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var survivingId = PersonId.From(command.SurvivingPersonId);
        var retiredId = PersonId.From(command.RetiredPersonId);

        var surviving = await LoadFullPersonAsync(survivingId, cancellationToken);
        var retired = await LoadFullPersonAsync(retiredId, cancellationToken);
        if (surviving is null || retired is null)
        {
            return CrmErrors.PersonNotFound;
        }

        var eligibility = PersonMergeFieldResolver.ValidateMergeEligibility(
            surviving,
            retired,
            command.HasElevatedMergeAuthorization
        );
        return eligibility.IsFailure
            ? Result.Failure<PersonMergeResponse>(eligibility.Errors)
            : await ExecuteMergeAsync(surviving, retired, command, cancellationToken);
    }

    private async Task<Result<PersonMergeResponse>> ExecuteMergeAsync(
        Person surviving,
        Person retired,
        MergePeopleCommand command,
        CancellationToken cancellationToken
    )
    {
        var nowUtc = clock.UtcNow;
        var hadMultipleEmails = HasMultipleDistinctEmails(surviving.Email, retired.Email);

        var mergedInput = PersonMergeFieldResolver.BuildMergedInput(
            surviving,
            retired,
            command.FieldSelections ?? new PersonMergeFieldSelections()
        );
        var updateResult = surviving.Update(mergedInput, nowUtc);
        if (updateResult.IsFailure)
        {
            return Result.Failure<PersonMergeResponse>(updateResult.Errors);
        }

        surviving.PreserveEarliestCreatedOnUtc(retired.CreatedOnUtc);
        var (phones, households, relationships) = await TransferChildDataAsync(
            surviving,
            retired,
            nowUtc,
            cancellationToken
        );

        var (mergeBatchId, redirectId) = await FinalizeRedirectAndOutboxAsync(
            surviving,
            retired,
            command,
            hadMultipleEmails,
            nowUtc,
            cancellationToken
        );

        await SyncPostMergeStateAsync(surviving, retired, nowUtc, cancellationToken);

        return Result.Success(
            new PersonMergeResponse(
                mergeBatchId,
                surviving.Id.Value,
                retired.Id.Value,
                redirectId,
                phones,
                households,
                relationships,
                hadMultipleEmails,
                nowUtc
            )
        );
    }

    private async Task<(int Phones, int Households, int Relationships)> TransferChildDataAsync(
        Person surviving,
        Person retired,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken
    )
    {
        var phones = PersonMergeTransferHelper.TransferPhonesAndIdentityChildren(
            surviving,
            retired
        );
        var households = await PersonMergeTransferHelper.TransferHouseholdsAsync(
            dbContext,
            surviving.Id,
            retired.Id,
            cancellationToken
        );
        var relationships = await PersonMergeTransferHelper.TransferRelationshipsAsync(
            dbContext,
            surviving.Id,
            retired.Id,
            cancellationToken
        );
        await PersonMergeTransferHelper.TransferW2S4RecordsAsync(
            dbContext,
            surviving,
            retired.Id,
            nowUtc,
            cancellationToken
        );
        return (phones, households, relationships);
    }

    private async Task<(Guid MergeBatchId, Guid RedirectId)> FinalizeRedirectAndOutboxAsync(
        Person surviving,
        Person retired,
        MergePeopleCommand command,
        bool hadMultipleEmails,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken
    )
    {
        retired.MarkMergedInto(surviving.Id, nowUtc);
        var mergeBatchId = Guid.CreateVersion7();
        var redirect = PersonRedirect.Create(
            retired.Id,
            surviving.Id,
            mergeBatchId,
            nowUtc,
            command.LegacyPersonAliasGuid,
            command.MergedByUserId
        );
        _ = dbContext.PersonRedirects.Add(redirect);

        await RepointChainedRedirectsAndCleanupDuplicatesAsync(
            surviving.Id,
            retired.Id,
            cancellationToken
        );
        await MetaphoneIndexSynchronizer.EnsureIndexedAsync(
            dbContext,
            surviving,
            cancellationToken
        );

        Guid[] legacyAliases = command.LegacyPersonAliasGuid.HasValue
            ? [command.LegacyPersonAliasGuid.Value]
            : [];

        dbContext.EnqueueIntegrationEvent(
            new PersonMergedIntegrationEvent(
                Guid.CreateVersion7(),
                nowUtc,
                surviving.Id.Value,
                retired.Id.Value,
                legacyAliases,
                hadMultipleEmails
            )
        );
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return (mergeBatchId, redirect.Id.Value);
    }

    private async Task SyncPostMergeStateAsync(
        Person surviving,
        Person retired,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken
    )
    {
        await PersonHouseholdSyncHelper.SyncPersonDerivedFieldsAsync(
            dbContext,
            surviving.Id,
            DateOnly.FromDateTime(nowUtc.UtcDateTime),
            cancellationToken
        );
        _ = await dbContext.SaveChangesAsync(cancellationToken);

        await graphService.UpsertPersonVertexAsync(
            surviving.Id.Value,
            surviving.FullName,
            cancellationToken
        );
        await graphService.RemovePersonVertexAsync(retired.Id.Value, cancellationToken);
    }

    private Task<Person?> LoadFullPersonAsync(PersonId id, CancellationToken cancellationToken)
    {
        return dbContext
            .People.AsSplitQuery()
            .Include(static p => p.PhoneNumbers)
            .Include(static p => p.PreviousNames)
            .Include(static p => p.SearchKeys)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    private async Task RepointChainedRedirectsAndCleanupDuplicatesAsync(
        PersonId survivingId,
        PersonId retiredId,
        CancellationToken cancellationToken
    )
    {
        var priorRedirects = await dbContext
            .PersonRedirects.Where(r => r.TargetPersonId == retiredId)
            .ToListAsync(cancellationToken);
        foreach (var prior in priorRedirects)
        {
            prior.RepointTarget(survivingId);
        }

        var obsoleteDuplicates = await dbContext
            .PersonDuplicates.Where(d => d.Person1Id == retiredId || d.Person2Id == retiredId)
            .ToListAsync(cancellationToken);
        if (obsoleteDuplicates.Count > 0)
        {
            dbContext.PersonDuplicates.RemoveRange(obsoleteDuplicates);
        }
    }

    private static bool HasMultipleDistinctEmails(string? email1, string? email2)
    {
        return !string.IsNullOrWhiteSpace(email1)
            && !string.IsNullOrWhiteSpace(email2)
            && !string.Equals(email1.Trim(), email2.Trim(), StringComparison.OrdinalIgnoreCase);
    }
}
