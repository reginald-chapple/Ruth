using System.Globalization;
using Microsoft.AspNetCore.Components;
using Ruth.Modules.Finance.Contracts.Accounts;
using Ruth.Modules.Finance.Contracts.Pledges;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Host.Components.Pages.Finance;

/// <summary>Staff workspace for managing hierarchical financial accounts, funds, revenue categories, and pledges.</summary>
public sealed partial class FinancialAccountsAndPledges : ComponentBase
{
    private static readonly FinancialAccountKind[] _accountKinds =
        Enum.GetValues<FinancialAccountKind>();
    private static readonly PledgeFrequency[] _frequencies = Enum.GetValues<PledgeFrequency>();

    private List<FinancialAccountResponse> _accounts = [];
    private List<FinancialPledgeResponse> _pledges = [];
    private PledgeAnalyticsSummaryResponse? _analytics;

    private string _accountName = string.Empty;
    private string _accountCode = string.Empty;
    private FinancialAccountKind _accountKind = FinancialAccountKind.CharitableFund;
    private string _glCode = string.Empty;
    private string _costCenter = string.Empty;
    private bool _isTaxDeductible = true;

    private Guid _pledgeAccountId;
    private string _pledgePersonIdInput = Guid.CreateVersion7().ToString("D");
    private string _pledgeAmountInput = "1200.00";
    private PledgeFrequency _pledgeFrequency = PledgeFrequency.Monthly;

    private string? _statusMessage;
    private bool _isError;

    [Inject]
    private IQueryDispatcher QueryDispatcher { get; set; } = default!;

    [Inject]
    private ICommandDispatcher CommandDispatcher { get; set; } = default!;

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        await ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        var accountsRes = await QueryDispatcher.QueryAsync<
            GetFinancialAccountsQuery,
            IReadOnlyList<FinancialAccountResponse>
        >(new GetFinancialAccountsQuery());
        if (accountsRes.IsSuccess)
        {
            _accounts = [.. accountsRes.Value];
            if (_pledgeAccountId == Guid.Empty && _accounts.Count > 0)
            {
                _pledgeAccountId = _accounts[0].Id;
            }
        }

        var pledgesRes = await QueryDispatcher.QueryAsync<
            GetFinancialPledgesQuery,
            IReadOnlyList<FinancialPledgeResponse>
        >(new GetFinancialPledgesQuery());
        if (pledgesRes.IsSuccess)
        {
            _pledges = [.. pledgesRes.Value];
        }

        var analyticsRes = await QueryDispatcher.QueryAsync<
            GetPledgeAnalyticsQuery,
            PledgeAnalyticsSummaryResponse
        >(new GetPledgeAnalyticsQuery());
        if (analyticsRes.IsSuccess)
        {
            _analytics = analyticsRes.Value;
        }
    }

    private async Task CreateAccountAsync()
    {
        var result = await CommandDispatcher.SendAsync<
            CreateFinancialAccountCommand,
            FinancialAccountResponse
        >(
            new CreateFinancialAccountCommand(
                new FinancialAccountSpec(
                    _accountName,
                    _accountName,
                    _accountCode,
                    _accountKind,
                    _isTaxDeductible,
                    GlCode: _glCode,
                    CostCenterCode: _costCenter
                )
            )
        );
        if (result.IsFailure)
        {
            _isError = true;
            _statusMessage = result.PrimaryError.Description;
            return;
        }

        _accountName = string.Empty;
        _accountCode = string.Empty;
        _isError = false;
        _statusMessage = $"Created financial account {result.Value.Name} ({result.Value.Code}).";
        await ReloadAsync();
    }

    private async Task CreatePledgeAsync()
    {
        if (
            !Guid.TryParse(_pledgePersonIdInput, out var personId)
            || !decimal.TryParse(
                _pledgeAmountInput,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out var amount
            )
        )
        {
            _isError = true;
            _statusMessage = "Enter a valid Person GUID and positive decimal pledge amount.";
            return;
        }

        var start = DateOnly.FromDateTime(DateTimeOffset.UtcNow.UtcDateTime);
        var result = await CommandDispatcher.SendAsync<
            CreateFinancialPledgeCommand,
            FinancialPledgeResponse
        >(
            new CreateFinancialPledgeCommand(
                personId,
                _pledgeAccountId,
                amount,
                "USD",
                _pledgeFrequency,
                start,
                start.AddMonths(12)
            )
        );
        if (result.IsFailure)
        {
            _isError = true;
            _statusMessage = result.PrimaryError.Description;
            return;
        }

        _isError = false;
        _statusMessage = string.Create(
            CultureInfo.InvariantCulture,
            $"Recorded pledge of {result.Value.TotalAmount:F2} {result.Value.CurrencyCode}."
        );
        await ReloadAsync();
    }
}
