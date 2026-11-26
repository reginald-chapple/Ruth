using System.Globalization;
using Microsoft.AspNetCore.Components;
using Ruth.Modules.Finance.Contracts.CadenceAlerts;
using Ruth.Modules.Finance.Contracts.Recurring;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Host.Components.Pages.Finance;

/// <summary>Staff and constituent self-service recurring billing management and cadence alert review page.</summary>
public sealed partial class RecurringSchedulesAndAlerts : ComponentBase
{
    private List<RecurringScheduleResponse> _schedules = [];
    private List<TransactionCadenceAlertResponse> _alerts = [];
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
        var schedRes = await QueryDispatcher.QueryAsync<
            GetRecurringSchedulesQuery,
            IReadOnlyList<RecurringScheduleResponse>
        >(new GetRecurringSchedulesQuery());
        if (schedRes.IsSuccess)
        {
            _schedules = [.. schedRes.Value];
        }

        var alertRes = await QueryDispatcher.QueryAsync<
            GetTransactionCadenceAlertsQuery,
            IReadOnlyList<TransactionCadenceAlertResponse>
        >(new GetTransactionCadenceAlertsQuery());
        if (alertRes.IsSuccess)
        {
            _alerts = [.. alertRes.Value];
        }
    }

    private async Task ProcessDueSettlementsAsync()
    {
        var res = await CommandDispatcher.SendAsync<
            ProcessDueRecurringSchedulesCommand,
            RecurringSettlementRunResponse
        >(new ProcessDueRecurringSchedulesCommand());
        if (res.IsFailure)
        {
            _isError = true;
            _statusMessage = res.PrimaryError.Description;
            return;
        }

        _isError = false;
        _statusMessage = string.Create(
            CultureInfo.InvariantCulture,
            $"Processed {res.Value.EvaluatedCount} due schedules ({res.Value.SettledCount} settled, {res.Value.FailedCount} failed)."
        );
        await ReloadAsync();
    }

    private async Task RunCadenceScanAsync()
    {
        var res = await CommandDispatcher.SendAsync<
            RunTransactionCadenceScanCommand,
            CadenceScanSummaryResponse
        >(new RunTransactionCadenceScanCommand());
        if (res.IsFailure)
        {
            _isError = true;
            _statusMessage = res.PrimaryError.Description;
            return;
        }

        _isError = false;
        _statusMessage = string.Create(
            CultureInfo.InvariantCulture,
            $"Analyzed {res.Value.EvaluatedConstituentCount} constituents; emitted {res.Value.AlertsCreatedCount} alerts."
        );
        await ReloadAsync();
    }

    private async Task PauseAsync(Guid scheduleId)
    {
        var res = await CommandDispatcher.SendAsync<
            PauseRecurringScheduleCommand,
            RecurringScheduleResponse
        >(new PauseRecurringScheduleCommand(scheduleId));
        _isError = res.IsFailure;
        _statusMessage = res.IsSuccess ? "Paused schedule." : res.PrimaryError.Description;
        await ReloadAsync();
    }

    private async Task ResumeAsync(Guid scheduleId)
    {
        var res = await CommandDispatcher.SendAsync<
            ResumeRecurringScheduleCommand,
            RecurringScheduleResponse
        >(new ResumeRecurringScheduleCommand(scheduleId));
        _isError = res.IsFailure;
        _statusMessage = res.IsSuccess ? "Resumed schedule." : res.PrimaryError.Description;
        await ReloadAsync();
    }

    private async Task ResolveAlertAsync(Guid alertId)
    {
        var res = await CommandDispatcher.SendAsync<
            UpdateCadenceAlertStatusCommand,
            TransactionCadenceAlertResponse
        >(
            new UpdateCadenceAlertStatusCommand(
                alertId,
                CadenceAlertStatus.Resolved,
                "Followed up from Staff UI"
            )
        );
        _isError = res.IsFailure;
        _statusMessage = res.IsSuccess ? "Resolved cadence alert." : res.PrimaryError.Description;
        await ReloadAsync();
    }
}
