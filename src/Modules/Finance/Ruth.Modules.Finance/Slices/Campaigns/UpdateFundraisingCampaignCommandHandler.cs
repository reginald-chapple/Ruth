using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Finance.Contracts.Campaigns;
using Ruth.Modules.Finance.Domain;
using Ruth.Modules.Finance.Domain.Accounts;
using Ruth.Modules.Finance.Domain.Campaigns;
using Ruth.Modules.Finance.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Finance.Slices.Campaigns;

internal sealed class UpdateFundraisingCampaignCommandHandler(
    FinanceDbContext dbContext,
    CampaignLedgerLoader ledgerLoader
) : ICommandHandler<UpdateFundraisingCampaignCommand, FundraisingCampaignResponse>
{
    public async Task<Result<FundraisingCampaignResponse>> HandleAsync(
        UpdateFundraisingCampaignCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var id = FundraisingCampaignId.From(command.CampaignId);
        var campaign = await dbContext
            .FundraisingCampaigns.Include(static c => c.Participants)
            .Include(static c => c.Credits)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (campaign is null)
        {
            return FinanceCampaignErrors.CampaignNotFound;
        }

        var accountId = FinancialAccountId.From(command.Spec.FinancialAccountId);
        var accountExists = await dbContext
            .FinancialAccounts.AsNoTracking()
            .AnyAsync(a => a.Id == accountId, cancellationToken);
        if (!accountExists)
        {
            return FinanceErrors.AccountNotFound;
        }

        var normalizedSlug = command.Spec.Slug?.Trim().ToLowerInvariant() ?? string.Empty;
        var duplicateSlug = await dbContext
            .FundraisingCampaigns.AsNoTracking()
            .AnyAsync(c => c.Slug == normalizedSlug && c.Id != id, cancellationToken);
        if (duplicateSlug)
        {
            return FinanceCampaignErrors.CampaignSlugDuplicate;
        }

        var updated = campaign.Update(command.Spec);
        if (updated.IsFailure)
        {
            return Result.Failure<FundraisingCampaignResponse>(updated.Errors);
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        var netMap = await ledgerLoader.LoadNetTransactionAmountsAsync(
            campaign.Credits,
            cancellationToken
        );
        return CampaignProgressCalculator.ProjectCampaignSummary(campaign, netMap);
    }
}
