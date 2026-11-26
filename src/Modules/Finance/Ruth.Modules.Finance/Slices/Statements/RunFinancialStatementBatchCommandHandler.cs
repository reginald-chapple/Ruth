using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Finance.Contracts.Statements;
using Ruth.Modules.Finance.Domain;
using Ruth.Modules.Finance.Domain.Statements;
using Ruth.Modules.Finance.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Finance.Slices.Statements;

internal sealed class RunFinancialStatementBatchCommandHandler(
    FinanceDbContext dbContext,
    StatementRecipientResolver recipientResolver,
    StatementLedgerQueryService ledgerQueryService,
    StatementGenerationCollaborator collaborator
) : ICommandHandler<RunFinancialStatementBatchCommand, FinancialStatementBatchRunResponse>
{
    public async Task<Result<FinancialStatementBatchRunResponse>> HandleAsync(
        RunFinancialStatementBatchCommand command,
        CancellationToken cancellationToken = default
    )
    {
        if (command.PeriodEndDate < command.PeriodStartDate)
        {
            return FinanceErrors.StatementPeriodInvalid;
        }

        var templateId = FinancialStatementTemplateId.From(command.TemplateId);
        var template = await dbContext
            .StatementTemplates.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == templateId && t.IsActive, cancellationToken);
        if (template is null)
        {
            return FinanceErrors.StatementTemplateNotFound;
        }

        var candidates = await DiscoverRecipientsAsync(command, cancellationToken);
        var batchRunId = Guid.CreateVersion7();
        var generated = await GenerateQualifyingStatementsAsync(
            template,
            command,
            candidates,
            batchRunId,
            cancellationToken
        );

        return new FinancialStatementBatchRunResponse(
            batchRunId,
            template.Id.Value,
            command.PeriodStartDate,
            command.PeriodEndDate,
            generated.Count,
            generated.Sum(static s => s.TaxDeductibleTotal),
            generated.Sum(static s => s.NonDeductiblePaidTotal),
            generated
        );
    }

    private async Task<List<GeneratedFinancialStatementResponse>> GenerateQualifyingStatementsAsync(
        FinancialStatementTemplate template,
        RunFinancialStatementBatchCommand command,
        List<(
            FinancialStatementRecipientScope Scope,
            Guid? PersonId,
            Guid? HouseholdId
        )> candidates,
        Guid batchRunId,
        CancellationToken cancellationToken
    )
    {
        var generated = new List<GeneratedFinancialStatementResponse>();
        foreach (var (scope, personId, householdId) in candidates)
        {
            var memberIds = await ResolveCandidateMemberIdsAsync(
                scope,
                personId,
                householdId,
                cancellationToken
            );
            var preview = await ledgerQueryService.BuildStatementLedgerAsync(
                template,
                memberIds,
                householdId,
                command.PeriodStartDate,
                command.PeriodEndDate,
                cancellationToken
            );
            var total = preview.RelevantActivityTotal(template.StatementKind);
            if (total <= 0m || total < template.MinimumTotalThreshold)
            {
                continue;
            }

            var rendered = await collaborator.GenerateAndPersistAsync(
                template,
                scope,
                personId,
                householdId,
                command.PeriodStartDate,
                command.PeriodEndDate,
                command.PersistArchiveToStorage,
                batchRunId,
                cancellationToken
            );
            if (rendered.IsSuccess)
            {
                generated.Add(rendered.Value.Statement);
            }
        }

        return generated;
    }

    private async Task<
        List<(FinancialStatementRecipientScope Scope, Guid? PersonId, Guid? HouseholdId)>
    > DiscoverRecipientsAsync(
        RunFinancialStatementBatchCommand command,
        CancellationToken cancellationToken
    )
    {
        var combined = await LoadPeriodTargetPairsAsync(
            command.PeriodStartDate,
            command.PeriodEndDate,
            cancellationToken
        );
        var results =
            new List<(FinancialStatementRecipientScope Scope, Guid? PersonId, Guid? HouseholdId)>();
        var coveredPersons = new HashSet<Guid>();
        var seenHouseholds = new HashSet<Guid>();

        if (command.ConsolidateHouseholds)
        {
            var householdIds = combined
                .Where(static x => x.HouseholdId.HasValue)
                .Select(static x => x.HouseholdId!.Value)
                .Distinct();
            foreach (var hhId in householdIds)
            {
                _ = seenHouseholds.Add(hhId);
                results.Add((FinancialStatementRecipientScope.Household, null, hhId));
                foreach (
                    var mId in await recipientResolver.ResolveHouseholdMemberIdsAsync(
                        hhId,
                        cancellationToken
                    )
                )
                {
                    _ = coveredPersons.Add(mId);
                }
            }
        }

        var personIds = combined
            .Where(static x => x.AuthorizedPersonId.HasValue)
            .Select(static x => x.AuthorizedPersonId!.Value)
            .Distinct();
        foreach (var personId in personIds)
        {
            if (!coveredPersons.Contains(personId))
            {
                await AppendClassifiedPersonCandidateAsync(
                    personId,
                    command,
                    seenHouseholds,
                    coveredPersons,
                    results,
                    cancellationToken
                );
            }
        }

        return results;
    }

    private async Task<
        List<(Guid? AuthorizedPersonId, Guid? HouseholdId)>
    > LoadPeriodTargetPairsAsync(
        DateOnly periodStartDate,
        DateOnly periodEndDate,
        CancellationToken cancellationToken
    )
    {
        var startUtc = new DateTimeOffset(
            periodStartDate.ToDateTime(TimeOnly.MinValue),
            TimeSpan.Zero
        );
        var endExclusiveUtc = new DateTimeOffset(
            periodEndDate.AddDays(1).ToDateTime(TimeOnly.MinValue),
            TimeSpan.Zero
        );

        var txTargets = await dbContext
            .FinancialTransactions.AsNoTracking()
            .Where(t =>
                t.TransactionDateTimeUtc >= startUtc && t.TransactionDateTimeUtc < endExclusiveUtc
            )
            .Select(static t => new ValueTuple<Guid?, Guid?>(t.AuthorizedPersonId, t.HouseholdId))
            .ToListAsync(cancellationToken);

        var invTargets = await dbContext
            .Invoices.AsNoTracking()
            .Where(i => i.IssueDate <= periodEndDate)
            .Select(static i => new ValueTuple<Guid?, Guid?>(i.PersonId, i.HouseholdId))
            .ToListAsync(cancellationToken);

        return [.. txTargets, .. invTargets];
    }

    private async Task AppendClassifiedPersonCandidateAsync(
        Guid personId,
        RunFinancialStatementBatchCommand command,
        HashSet<Guid> seenHouseholds,
        HashSet<Guid> coveredPersons,
        List<(FinancialStatementRecipientScope, Guid?, Guid?)> results,
        CancellationToken cancellationToken
    )
    {
        var (isBusiness, combinedHh) = await recipientResolver.InspectPersonForBatchAsync(
            personId,
            command.ConsolidateHouseholds,
            cancellationToken
        );
        if (isBusiness)
        {
            if (command.IncludeOrganizations)
            {
                results.Add((FinancialStatementRecipientScope.OrganizationSponsor, personId, null));
            }

            return;
        }

        if (combinedHh is not null)
        {
            if (seenHouseholds.Add(combinedHh.Id))
            {
                results.Add((FinancialStatementRecipientScope.Household, null, combinedHh.Id));
            }

            foreach (var m in combinedHh.Members)
            {
                _ = coveredPersons.Add(m.PersonId);
            }

            return;
        }

        results.Add((FinancialStatementRecipientScope.Individual, personId, null));
    }

    private async Task<HashSet<Guid>> ResolveCandidateMemberIdsAsync(
        FinancialStatementRecipientScope scope,
        Guid? personId,
        Guid? householdId,
        CancellationToken cancellationToken
    )
    {
        return scope == FinancialStatementRecipientScope.Household && householdId.HasValue
                ? await recipientResolver.ResolveHouseholdMemberIdsAsync(
                    householdId.Value,
                    cancellationToken
                )
            : personId.HasValue && personId.Value != Guid.Empty ? [personId.Value]
            : [];
    }
}
