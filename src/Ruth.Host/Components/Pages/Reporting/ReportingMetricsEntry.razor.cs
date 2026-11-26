using Microsoft.AspNetCore.Components;
using Ruth.Modules.Reporting.Contracts.Analytics;
using Ruth.Modules.Reporting.Contracts.Metrics;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Time;

namespace Ruth.Host.Components.Pages.Reporting;

public sealed partial class ReportingMetricsEntry : ComponentBase
{
    private IReadOnlyList<MetricResponse> _metrics = [];
    private DynamicChartResponse? _chart;
    private string _metricTitle = "Weekly Volunteer Shifts Filled";
    private string _metricKey = "ops.volunteer-shifts";
    private string _unitLabel = "Shifts";
    private readonly decimal _entryValue = 42m;
    private string? _statusMessage;
    private bool _isError;

    [Inject]
    private IQueryDispatcher QueryDispatcher { get; set; } = default!;

    [Inject]
    private ICommandDispatcher CommandDispatcher { get; set; } = default!;

    [Inject]
    private IClock Clock { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        await ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        var res = await QueryDispatcher.QueryAsync<GetMetricsQuery, IReadOnlyList<MetricResponse>>(
            new GetMetricsQuery()
        );
        if (res.IsSuccess)
        {
            _metrics = res.Value;
        }
    }

    private async Task CreateMetricAsync()
    {
        var res = await CommandDispatcher.SendAsync<CreateMetricCommand, MetricResponse>(
            new CreateMetricCommand(
                _metricTitle,
                _metricKey,
                MetricSourceType.Manual,
                UnitLabel: _unitLabel
            )
        );
        _isError = res.IsFailure;
        _statusMessage = res.IsSuccess
            ? $"Created metric '{res.Value.Title}'."
            : res.PrimaryError.Description;
        if (res.IsSuccess)
        {
            await ReloadAsync();
        }
    }

    private async Task RecordSampleAsync(Guid metricId)
    {
        var res = await CommandDispatcher.SendAsync<
            RecordMetricValuesBatchCommand,
            IReadOnlyList<MetricValueResponse>
        >(
            new RecordMetricValuesBatchCommand([
                new MetricValueEntryInput(
                    metricId,
                    MetricValueType.Measure,
                    _entryValue,
                    Clock.UtcNow,
                    Note: "Entered via OperationalMetricsEntry"
                ),
            ])
        );
        _isError = res.IsFailure;
        _statusMessage = res.IsSuccess
            ? "Recorded operational metric value."
            : res.PrimaryError.Description;
        if (res.IsSuccess)
        {
            await LoadChartAsync(metricId);
        }
    }

    private async Task LoadChartAsync(Guid metricId)
    {
        var res = await QueryDispatcher.QueryAsync<GetDynamicChartQuery, DynamicChartResponse>(
            new GetDynamicChartQuery(metricId, Granularity: ChartRollupGranularity.Month)
        );
        if (res.IsSuccess)
        {
            _chart = res.Value;
        }
    }
}
