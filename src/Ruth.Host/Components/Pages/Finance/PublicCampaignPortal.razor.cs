using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Ruth.Modules.Finance.Contracts.Campaigns;
using Ruth.Modules.Finance.Contracts.Transactions;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Host.Components.Pages.Finance;

/// <summary>Public peer-to-peer and project contribution portal honoring donor privacy and participant credit.</summary>
[AllowAnonymous]
public sealed partial class PublicCampaignPortal : ComponentBase
{
    private static readonly CampaignDonorPrivacy[] _privacyOptions =
        Enum.GetValues<CampaignDonorPrivacy>();

    private PublicCampaignPortalResponse? _portal;
    private string _donorName = "Community Supporter";
    private string _amountInput = "100.00";
    private CampaignDonorPrivacy _selectedPrivacy = CampaignDonorPrivacy.PublicNameAndAmount;
    private Guid _selectedParticipantId;
    private string _comment = string.Empty;
    private string? _statusMessage;
    private bool _isError;

    /// <summary>Gets or sets the campaign URL slug from the route.</summary>
    [Parameter]
    public string Slug { get; set; } = string.Empty;

    [Inject]
    private IQueryDispatcher QueryDispatcher { get; set; } = default!;

    [Inject]
    private ICommandDispatcher CommandDispatcher { get; set; } = default!;

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        await ReloadPortalAsync();
    }

    private async Task ReloadPortalAsync()
    {
        var res = await QueryDispatcher.QueryAsync<
            GetPublicCampaignPortalQuery,
            PublicCampaignPortalResponse
        >(new GetPublicCampaignPortalQuery(Slug));
        _portal = res.IsSuccess ? res.Value : null;
    }

    private async Task SubmitContributionAsync()
    {
        if (
            _portal is null
            || !decimal.TryParse(
                _amountInput,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out var amount
            )
        )
        {
            _isError = true;
            _statusMessage = "Enter a valid contribution amount.";
            return;
        }

        Guid? creditedId = _selectedParticipantId == Guid.Empty ? null : _selectedParticipantId;
        var res = await CommandDispatcher.SendAsync<
            SubmitCampaignContributionCommand,
            CampaignContributionReceiptResponse
        >(
            new SubmitCampaignContributionCommand(
                Slug,
                amount,
                _portal.CurrencyCode,
                _donorName,
                _selectedPrivacy,
                CreditedParticipantId: creditedId,
                OneTimeHostedToken: _portal.DefaultFinancialGatewayId.HasValue
                    ? "tok_portal_card_4242"
                    : null,
                TenderType: FinancialTenderType.CreditCard,
                DonorComment: _comment
            )
        );
        if (res.IsFailure)
        {
            _isError = true;
            _statusMessage = res.PrimaryError.Description;
            return;
        }

        _isError = false;
        _statusMessage = string.Create(
            CultureInfo.InvariantCulture,
            $"Thank you! Recorded {res.Value.Amount:F2} {res.Value.CurrencyCode}."
        );
        await ReloadPortalAsync();
    }
}
