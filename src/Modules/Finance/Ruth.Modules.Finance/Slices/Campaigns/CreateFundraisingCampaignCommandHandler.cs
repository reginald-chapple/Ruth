using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Finance.Contracts.Campaigns;
using Ruth.Modules.Finance.Domain;
using Ruth.Modules.Finance.Domain.Accounts;
using Ruth.Modules.Finance.Domain.Campaigns;
using Ruth.Modules.Finance.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Finance.Slices.Campaigns;

internal sealed class CreateFundraisingCampaignCommandHandler(FinanceDbContext dbContext)
    : ICommandHandler<CreateFundraisingCampaignCommand, FundraisingCampaignResponse>
{
    public async Task<Result<FundraisingCampaignResponse>> HandleAsync(
        CreateFundraisingCampaignCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var accountId = FinancialAccountId.From(command.Spec.FinancialAccountId);
        var accountExists = await dbContext
            .FinancialAccounts.AsNoTracking()
            .AnyAsync(a => a.Id == accountId, cancellationToken);
        if (!accountExists)
        {
            return FinanceErrors.AccountNotFound;
        }

        var created = FundraisingCampaign.Create(command.Spec);
        if (created.IsFailure)
        {
            return Result.Failure<FundraisingCampaignResponse>(created.Errors);
        }

        var duplicateSlug = await dbContext
            .FundraisingCampaigns.AsNoTracking()
            .AnyAsync(c => c.Slug == created.Value.Slug, cancellationToken);
        if (duplicateSlug)
        {
            return FinanceCampaignErrors.CampaignSlugDuplicate;
        }

        _ = dbContext.FundraisingCampaigns.Add(created.Value);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return CampaignProgressCalculator.ProjectCampaignSummary(
            created.Value,
            new Dictionary<Guid, decimal>()
        );
    }
}
