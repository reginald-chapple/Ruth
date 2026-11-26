using System.Globalization;
using Microsoft.AspNetCore.Components;
using Ruth.Modules.Reporting.Contracts.DataViews;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Host.Components.Pages.Reporting;

public sealed partial class ReportingDataViewsStudio : ComponentBase
{
    private IReadOnlyList<DataViewResponse> _dataViews = [];
    private DataViewExecutionResponse? _lastExecution;
    private string _name = "Active Constituents";
    private string _entityTypeKey = "Ruth.Crm.Person";
    private string _fieldKey = "RecordStatus";
    private string _filterValue = "Active";
    private string? _statusMessage;
    private bool _isError;

    [Inject]
    private IQueryDispatcher QueryDispatcher { get; set; } = default!;

    [Inject]
    private ICommandDispatcher CommandDispatcher { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        await ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        var res = await QueryDispatcher.QueryAsync<
            GetDataViewsQuery,
            IReadOnlyList<DataViewResponse>
        >(new GetDataViewsQuery(IncludeInactive: true));
        if (res.IsSuccess)
        {
            _dataViews = res.Value;
        }
    }

    private async Task CreateDataViewAsync()
    {
        var ast = new DataViewFilterNodeDto(
            DataViewFilterExpressionType.GroupAll,
            Children:
            [
                new DataViewFilterNodeDto(
                    DataViewFilterExpressionType.Rule,
                    DataViewFilterRuleKind.EntityProperty,
                    _fieldKey,
                    DataViewFilterOperator.Equals,
                    _filterValue
                ),
            ]
        );
        var res = await CommandDispatcher.SendAsync<CreateDataViewCommand, DataViewResponse>(
            new CreateDataViewCommand(
                _name,
                _entityTypeKey,
                ast,
                PersistedScheduleIntervalMinutes: 60
            )
        );
        _isError = res.IsFailure;
        _statusMessage = res.IsSuccess
            ? $"Created DataView '{res.Value.Name}'."
            : res.PrimaryError.Description;
        if (res.IsSuccess)
        {
            await ReloadAsync();
        }
    }

    private async Task ExecuteAsync(Guid dataViewId)
    {
        var res = await QueryDispatcher.QueryAsync<ExecuteDataViewQuery, DataViewExecutionResponse>(
            new ExecuteDataViewQuery(dataViewId)
        );
        if (res.IsSuccess)
        {
            _lastExecution = res.Value;
            _isError = false;
            _statusMessage = string.Create(
                CultureInfo.InvariantCulture,
                $"Executed DataView: {res.Value.TotalMatchedCount} match(es)."
            );
        }
    }

    private async Task MaterializeAsync(Guid dataViewId)
    {
        var res = await CommandDispatcher.SendAsync<
            MaterializeDataViewCommand,
            DataViewExecutionResponse
        >(new MaterializeDataViewCommand(dataViewId));
        if (res.IsSuccess)
        {
            _lastExecution = res.Value;
            _isError = false;
            _statusMessage = string.Create(
                CultureInfo.InvariantCulture,
                $"Materialized {res.Value.TotalMatchedCount} persisted entity ID(s)."
            );
            await ReloadAsync();
        }
    }
}
