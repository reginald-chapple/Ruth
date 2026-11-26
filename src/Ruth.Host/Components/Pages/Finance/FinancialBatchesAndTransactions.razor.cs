using System.Globalization;
using Microsoft.AspNetCore.Components;
using Ruth.Modules.Finance.Contracts.Accounts;
using Ruth.Modules.Finance.Contracts.Batches;
using Ruth.Modules.Finance.Contracts.Transactions;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Host.Components.Pages.Finance;

/// <summary>Staff workspace for batch reconciliation, multi-account transactions, and refunds.</summary>
public sealed partial class FinancialBatchesAndTransactions : ComponentBase
{
    private List<FinancialAccountResponse> _accounts = [];
    private List<FinancialBatchResponse> _batches = [];
    private List<FinancialBatchResponse> _openBatches = [];
    private List<FinancialTransactionResponse> _transactions = [];

    private string _batchName = "Teller Deposit Batch";
    private string _controlAmountInput = "250.00";
    private string _controlCountInput = "1";

    private Guid _selectedBatchId;
    private Guid _selectedAccountId;
    private string _txPersonIdInput = Guid.CreateVersion7().ToString("D");
    private string _txAmountInput = "250.00";
    private string _txCode = "CHK-1042";

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
            if (_selectedAccountId == Guid.Empty && _accounts.Count > 0)
            {
                _selectedAccountId = _accounts[0].Id;
            }
        }

        var batchRes = await QueryDispatcher.QueryAsync<
            GetFinancialBatchesQuery,
            IReadOnlyList<FinancialBatchResponse>
        >(new GetFinancialBatchesQuery());
        if (batchRes.IsSuccess)
        {
            _batches = [.. batchRes.Value];
            _openBatches = [.. _batches.Where(static b => b.Status == FinancialBatchStatus.Open)];
            if (_selectedBatchId == Guid.Empty && _openBatches.Count > 0)
            {
                _selectedBatchId = _openBatches[0].Id;
            }
        }

        var txRes = await QueryDispatcher.QueryAsync<
            GetFinancialTransactionsQuery,
            IReadOnlyList<FinancialTransactionResponse>
        >(new GetFinancialTransactionsQuery(MaxResults: 30));
        if (txRes.IsSuccess)
        {
            _transactions = [.. txRes.Value];
        }
    }

    private async Task CreateAndOpenBatchAsync()
    {
        _ = decimal.TryParse(
            _controlAmountInput,
            NumberStyles.Number,
            CultureInfo.InvariantCulture,
            out var ctrlAmt
        );
        _ = int.TryParse(
            _controlCountInput,
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out var ctrlCnt
        );

        var res = await CommandDispatcher.SendAsync<
            CreateFinancialBatchCommand,
            FinancialBatchResponse
        >(
            new CreateFinancialBatchCommand(
                _batchName,
                "USD",
                ctrlAmt,
                ctrlCnt,
                OpenImmediately: true
            )
        );
        if (res.IsFailure)
        {
            _isError = true;
            _statusMessage = res.PrimaryError.Description;
            return;
        }

        _selectedBatchId = res.Value.Id;
        _isError = false;
        _statusMessage = $"Opened batch {res.Value.Name}.";
        await ReloadAsync();
    }

    private async Task PostTransactionAsync()
    {
        if (
            !decimal.TryParse(
                _txAmountInput,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out var amount
            )
        )
        {
            _isError = true;
            _statusMessage = "Enter a valid transaction amount.";
            return;
        }

        Guid? personId = Guid.TryParse(_txPersonIdInput, out var parsedPid) ? parsedPid : null;
        Guid? batchId = _selectedBatchId == Guid.Empty ? null : _selectedBatchId;

        var res = await CommandDispatcher.SendAsync<
            RecordFinancialTransactionCommand,
            FinancialTransactionResponse
        >(
            new RecordFinancialTransactionCommand(
                personId,
                "USD",
                FinancialTransactionSourceType.StaffEntry,
                FinancialTenderType.Check,
                [new FinancialTransactionSplitSpec(_selectedAccountId, amount)],
                BatchId: batchId,
                TransactionCode: _txCode
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
            $"Posted transaction {res.Value.TotalAmount:F2} USD."
        );
        await ReloadAsync();
    }

    private async Task CloseBatchAsync(Guid batchId)
    {
        var res = await CommandDispatcher.SendAsync<
            CloseFinancialBatchCommand,
            FinancialBatchResponse
        >(new CloseFinancialBatchCommand(batchId, AuditNote: "Closed from Staff UI"));
        if (res.IsFailure)
        {
            _isError = true;
            _statusMessage = res.PrimaryError.Description;
            return;
        }

        _isError = false;
        _statusMessage = $"Reconciled and audit-locked batch {res.Value.Name}.";
        await ReloadAsync();
    }

    private async Task RefundFullAsync(FinancialTransactionResponse tx)
    {
        var res = await CommandDispatcher.SendAsync<
            RefundFinancialTransactionCommand,
            FinancialTransactionResponse
        >(
            new RefundFinancialTransactionCommand(
                tx.Id,
                tx.NetAmount,
                "Staff UI refund",
                ExecuteGatewayRefund: true
            )
        );
        if (res.IsFailure)
        {
            _isError = true;
            _statusMessage = res.PrimaryError.Description;
            return;
        }

        _isError = false;
        _statusMessage = "Processed transaction refund.";
        await ReloadAsync();
    }
}
