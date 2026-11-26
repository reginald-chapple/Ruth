using System.Globalization;
using Microsoft.AspNetCore.Components;
using Ruth.Modules.Finance.Contracts.Accounts;
using Ruth.Modules.Finance.Contracts.Invoices;
using Ruth.Modules.Finance.Contracts.Transactions;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Host.Components.Pages.Finance;

/// <summary>Staff workspace for issuing and settling membership dues, fee, and sponsorship invoices.</summary>
public sealed partial class InvoicesAndBillings : ComponentBase
{
    private List<FinancialAccountResponse> _accounts = [];
    private List<InvoiceResponse> _invoices = [];

    private string _personIdInput = Guid.CreateVersion7().ToString("D");
    private Guid _accountId;
    private string _description = "Annual Membership Dues";
    private string _unitAmountInput = "150.00";
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
        var accRes = await QueryDispatcher.QueryAsync<
            GetFinancialAccountsQuery,
            IReadOnlyList<FinancialAccountResponse>
        >(new GetFinancialAccountsQuery(ActiveOnly: true));
        if (accRes.IsSuccess)
        {
            _accounts = [.. accRes.Value];
            if (_accountId == Guid.Empty && _accounts.Count > 0)
            {
                _accountId = _accounts[0].Id;
            }
        }

        var invRes = await QueryDispatcher.QueryAsync<
            GetInvoicesQuery,
            IReadOnlyList<InvoiceResponse>
        >(new GetInvoicesQuery());
        if (invRes.IsSuccess)
        {
            _invoices = [.. invRes.Value];
        }
    }

    private async Task CreateInvoiceAsync()
    {
        if (
            !Guid.TryParse(_personIdInput, out var personId)
            || !decimal.TryParse(
                _unitAmountInput,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out var amount
            )
        )
        {
            _isError = true;
            _statusMessage = "Provide a valid Person GUID and amount.";
            return;
        }

        var today = DateOnly.FromDateTime(DateTimeOffset.UtcNow.UtcDateTime);
        var res = await CommandDispatcher.SendAsync<CreateInvoiceCommand, InvoiceResponse>(
            new CreateInvoiceCommand(
                personId,
                today,
                today.AddDays(30),
                "USD",
                [new InvoiceLineItemSpec(_accountId, _description, 1m, amount)],
                IssueImmediately: true
            )
        );
        if (res.IsFailure)
        {
            _isError = true;
            _statusMessage = res.PrimaryError.Description;
            return;
        }

        _isError = false;
        _statusMessage = $"Issued invoice {res.Value.InvoiceNumber}.";
        await ReloadAsync();
    }

    private async Task SettleFullBalanceAsync(InvoiceResponse invoice)
    {
        var targetAccount =
            invoice.LineItems.Count > 0 ? invoice.LineItems[0].AccountId : _accountId;
        var res = await CommandDispatcher.SendAsync<
            RecordFinancialTransactionCommand,
            FinancialTransactionResponse
        >(
            new RecordFinancialTransactionCommand(
                invoice.PersonId,
                invoice.CurrencyCode,
                FinancialTransactionSourceType.StaffEntry,
                FinancialTenderType.Check,
                [
                    new FinancialTransactionSplitSpec(
                        targetAccount,
                        invoice.BalanceDue,
                        invoice.Id,
                        $"Settlement for {invoice.InvoiceNumber}"
                    ),
                ]
            )
        );
        if (res.IsFailure)
        {
            _isError = true;
            _statusMessage = res.PrimaryError.Description;
            return;
        }

        _isError = false;
        _statusMessage = $"Settled invoice {invoice.InvoiceNumber}.";
        await ReloadAsync();
    }
}
