using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Crm.Contracts.Events;
using Ruth.Modules.Finance.Infrastructure;
using Ruth.SharedKernel.Events;

namespace Ruth.Modules.Finance.Slices.Events;

internal sealed class FinancePersonMergedConsumer(FinanceDbContext dbContext)
    : IIntegrationEventConsumer<PersonMergedIntegrationEvent>
{
    public async Task ConsumeAsync(
        PersonMergedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var retiredId = integrationEvent.RetiredPersonId;
        var survivingId = integrationEvent.SurvivingPersonId;
        if (retiredId == Guid.Empty || survivingId == Guid.Empty || retiredId == survivingId)
        {
            return;
        }

        var changed = false;
        changed |= await RepointPledgesAndPaymentMethodsAsync(
            retiredId,
            survivingId,
            cancellationToken
        );
        changed |= await RepointInvoicesAndTransactionsAsync(
            retiredId,
            survivingId,
            cancellationToken
        );
        changed |= await RepointRecurringAndAlertsAsync(retiredId, survivingId, cancellationToken);
        changed |= await RepointStatementsAndAssistanceAsync(
            retiredId,
            survivingId,
            cancellationToken
        );
        changed |= await RepointCampaignsAsync(retiredId, survivingId, cancellationToken);

        if (changed)
        {
            _ = await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task<bool> RepointPledgesAndPaymentMethodsAsync(
        Guid retiredId,
        Guid survivingId,
        CancellationToken cancellationToken
    )
    {
        var modified = false;

        var pledges = await dbContext
            .FinancialPledges.Where(p => p.PersonId == retiredId)
            .ToListAsync(cancellationToken);
        foreach (var pledge in pledges)
        {
            modified |= pledge.RepointPerson(retiredId, survivingId);
        }

        var savedMethods = await dbContext
            .SavedPaymentMethods.Where(m => m.PersonId == retiredId)
            .ToListAsync(cancellationToken);
        foreach (var method in savedMethods)
        {
            modified |= method.RepointPerson(retiredId, survivingId);
        }

        var bankIdentifiers = await dbContext
            .BankAccountIdentifiers.Where(b => b.PersonId == retiredId)
            .ToListAsync(cancellationToken);
        foreach (var bank in bankIdentifiers)
        {
            modified |= bank.RepointPerson(retiredId, survivingId);
        }

        return modified;
    }

    private async Task<bool> RepointInvoicesAndTransactionsAsync(
        Guid retiredId,
        Guid survivingId,
        CancellationToken cancellationToken
    )
    {
        var modified = false;

        var invoices = await dbContext
            .Invoices.Where(i => i.PersonId == retiredId)
            .ToListAsync(cancellationToken);
        foreach (var invoice in invoices)
        {
            modified |= invoice.RepointPerson(retiredId, survivingId);
        }

        var transactions = await dbContext
            .FinancialTransactions.Where(t => t.AuthorizedPersonId == retiredId)
            .ToListAsync(cancellationToken);
        foreach (var tx in transactions)
        {
            modified |= tx.RepointPerson(retiredId, survivingId);
        }

        return modified;
    }

    private async Task<bool> RepointRecurringAndAlertsAsync(
        Guid retiredId,
        Guid survivingId,
        CancellationToken cancellationToken
    )
    {
        var modified = false;

        var schedules = await dbContext
            .RecurringSchedules.Where(s => s.PersonId == retiredId)
            .ToListAsync(cancellationToken);
        foreach (var schedule in schedules)
        {
            modified |= schedule.RepointPerson(retiredId, survivingId);
        }

        var alerts = await dbContext
            .CadenceAlerts.Where(a => a.PersonId == retiredId)
            .ToListAsync(cancellationToken);
        foreach (var alert in alerts)
        {
            modified |= alert.RepointPerson(retiredId, survivingId);
        }

        return modified;
    }

    private async Task<bool> RepointStatementsAndAssistanceAsync(
        Guid retiredId,
        Guid survivingId,
        CancellationToken cancellationToken
    )
    {
        var modified = false;

        var statements = await dbContext
            .GeneratedStatements.Where(s => s.TargetPersonId == retiredId)
            .ToListAsync(cancellationToken);
        foreach (var statement in statements)
        {
            modified |= statement.RepointPerson(retiredId, survivingId);
        }

        var cases = await dbContext
            .AssistanceCases.Where(c =>
                c.ApplicantPersonId == retiredId || c.AssignedCaseworkerPersonId == retiredId
            )
            .ToListAsync(cancellationToken);
        foreach (var item in cases)
        {
            modified |= item.RepointPerson(retiredId, survivingId);
        }

        return modified;
    }

    private async Task<bool> RepointCampaignsAsync(
        Guid retiredId,
        Guid survivingId,
        CancellationToken cancellationToken
    )
    {
        var modified = false;

        var participants = await dbContext
            .CampaignParticipants.Where(p => p.PersonId == retiredId)
            .ToListAsync(cancellationToken);
        foreach (var p in participants)
        {
            if (
                !await dbContext.CampaignParticipants.AnyAsync(
                    existing =>
                        existing.CampaignId == p.CampaignId && existing.PersonId == survivingId,
                    cancellationToken
                )
            )
            {
                modified |= p.RepointPerson(retiredId, survivingId);
            }
        }

        var credits = await dbContext
            .CampaignContributionCredits.Where(c => c.DonorPersonId == retiredId)
            .ToListAsync(cancellationToken);
        foreach (var c in credits)
        {
            modified |= c.RepointDonorPerson(retiredId, survivingId);
        }

        return modified;
    }
}
