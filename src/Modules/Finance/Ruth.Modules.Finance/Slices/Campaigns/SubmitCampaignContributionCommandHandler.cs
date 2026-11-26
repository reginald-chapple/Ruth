using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Finance.Contracts.Campaigns;
using Ruth.Modules.Finance.Contracts.Events;
using Ruth.Modules.Finance.Contracts.Transactions;
using Ruth.Modules.Finance.Domain.Campaigns;
using Ruth.Modules.Finance.Domain.Transactions;
using Ruth.Modules.Finance.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Finance.Slices.Campaigns;

internal sealed class SubmitCampaignContributionCommandHandler(
    FinanceDbContext dbContext,
    ICommandDispatcher commandDispatcher,
    CampaignLedgerLoader ledgerLoader,
    IClock clock
) : ICommandHandler<SubmitCampaignContributionCommand, CampaignContributionReceiptResponse>
{
    public async Task<Result<CampaignContributionReceiptResponse>> HandleAsync(
        SubmitCampaignContributionCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var campaign = await ResolveCampaignAsync(command.CampaignIdOrSlug, cancellationToken);
        if (campaign is null)
        {
            return FinanceCampaignErrors.CampaignNotFound;
        }

        var nowUtc = clock.UtcNow;
        if (!campaign.IsOpenForPublicContributions(DateOnly.FromDateTime(nowUtc.UtcDateTime)))
        {
            return FinanceCampaignErrors.CampaignClosedForContributions;
        }

        CampaignParticipantId? creditedParticipantId =
            command.CreditedParticipantId is { } cp && cp != Guid.Empty
                ? CampaignParticipantId.From(cp)
                : null;
        if (
            creditedParticipantId.HasValue
            && !campaign.Participants.Any(p =>
                p.Id == creditedParticipantId.Value && p.IsFundraisingEnabled
            )
        )
        {
            return FinanceCampaignErrors.ParticipantNotFound;
        }

        var txResult = await RecordContributionTransactionAsync(
            campaign,
            command,
            nowUtc,
            cancellationToken
        );
        return txResult.IsFailure
            ? Result.Failure<CampaignContributionReceiptResponse>(txResult.Errors)
            : await AttachCreditAndPersistAsync(
                campaign,
                command,
                txResult.Value,
                creditedParticipantId,
                nowUtc,
                cancellationToken
            );
    }

    private async Task<FundraisingCampaign?> ResolveCampaignAsync(
        string idOrSlug,
        CancellationToken cancellationToken
    )
    {
        if (string.IsNullOrWhiteSpace(idOrSlug))
        {
            return null;
        }

        if (Guid.TryParse(idOrSlug, out var guid))
        {
            var id = FundraisingCampaignId.From(guid);
            return await dbContext
                .FundraisingCampaigns.Include(static c => c.Participants)
                .Include(static c => c.Credits)
                .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        }

        var slug = idOrSlug.Trim().ToLowerInvariant();
        return await dbContext
            .FundraisingCampaigns.Include(static c => c.Participants)
            .Include(static c => c.Credits)
            .FirstOrDefaultAsync(c => c.Slug == slug, cancellationToken);
    }

    private async Task<Result<FinancialTransactionResponse>> RecordContributionTransactionAsync(
        FundraisingCampaign campaign,
        SubmitCampaignContributionCommand command,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken
    )
    {
        var gatewayId = command.FinancialGatewayId ?? campaign.DefaultFinancialGatewayId?.Value;
        var summary = $"Campaign: {campaign.Name}";
        return await commandDispatcher.SendAsync<
            RecordFinancialTransactionCommand,
            FinancialTransactionResponse
        >(
            new RecordFinancialTransactionCommand(
                command.DonorPersonId,
                command.CurrencyCode,
                FinancialTransactionSourceType.Website,
                command.TenderType,
                [
                    new FinancialTransactionSplitSpec(
                        campaign.FinancialAccountId.Value,
                        command.Amount,
                        Summary: summary
                    ),
                ],
                FinancialGatewayId: gatewayId,
                SavedPaymentMethodId: command.SavedPaymentMethodId,
                OneTimeHostedToken: command.OneTimeHostedToken,
                TransactionDateTimeUtc: nowUtc,
                Summary: summary
            ),
            cancellationToken
        );
    }

    private async Task<Result<CampaignContributionReceiptResponse>> AttachCreditAndPersistAsync(
        FundraisingCampaign campaign,
        SubmitCampaignContributionCommand command,
        FinancialTransactionResponse transaction,
        CampaignParticipantId? creditedParticipantId,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken
    )
    {
        var creditResult = campaign.RecordContributionCredit(
            FinancialTransactionId.From(transaction.Id),
            command.Amount,
            command.DonorDisplayName,
            command.DonorPrivacy,
            nowUtc,
            creditedParticipantId,
            command.DonorPersonId,
            command.DonorComment
        );
        if (creditResult.IsFailure)
        {
            return Result.Failure<CampaignContributionReceiptResponse>(creditResult.Errors);
        }

        _ = dbContext.CampaignContributionCredits.Add(creditResult.Value);
        dbContext.EnqueueIntegrationEvent(
            new CampaignContributionRecordedIntegrationEvent(
                Guid.CreateVersion7(),
                nowUtc,
                campaign.Id.Value,
                creditResult.Value.Id.Value,
                transaction.Id,
                creditResult.Value.CreditedParticipantId?.Value,
                creditResult.Value.DonorPersonId,
                creditResult.Value.Amount,
                creditResult.Value.CurrencyCode,
                creditResult.Value.DonorPrivacy
            )
        );
        _ = await dbContext.SaveChangesAsync(cancellationToken);

        var netMap = await ledgerLoader.LoadNetTransactionAmountsAsync(
            campaign.Credits,
            cancellationToken
        );
        var summary = CampaignProgressCalculator.ProjectCampaignSummary(campaign, netMap);
        return new CampaignContributionReceiptResponse(
            creditResult.Value.Id.Value,
            transaction.Id,
            campaign.Id.Value,
            creditResult.Value.CreditedParticipantId?.Value,
            creditResult.Value.Amount,
            creditResult.Value.CurrencyCode,
            creditResult.Value.DonorPrivacy,
            summary.TotalRaisedAmount,
            summary.ProgressPercent
        );
    }
}
