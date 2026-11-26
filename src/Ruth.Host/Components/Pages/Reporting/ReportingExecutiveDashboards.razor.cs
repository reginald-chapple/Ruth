using Microsoft.AspNetCore.Components;
using Ruth.Modules.Reporting.Contracts.Analytics;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Host.Components.Pages.Reporting;

public sealed partial class ReportingExecutiveDashboards : ComponentBase
{
    private ExecutiveInsightsResponse? _insights;
    private RevenueAndImpactOverviewResponse? _overview;
    private StarSchemaRefreshResponse? _starSchema;
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
        var ins = await QueryDispatcher.QueryAsync<
            GetExecutiveInsightsQuery,
            ExecutiveInsightsResponse
        >(new GetExecutiveInsightsQuery(WindowDays: 30));
        if (ins.IsSuccess)
        {
            _insights = ins.Value;
        }

        var rev = await QueryDispatcher.QueryAsync<
            GetRevenueAndImpactOverviewQuery,
            RevenueAndImpactOverviewResponse
        >(new GetRevenueAndImpactOverviewQuery());
        if (rev.IsSuccess)
        {
            _overview = rev.Value;
        }

        var st = await QueryDispatcher.QueryAsync<
            GetStarSchemaStatusQuery,
            StarSchemaRefreshResponse
        >(new GetStarSchemaStatusQuery());
        if (st.IsSuccess)
        {
            _starSchema = st.Value;
        }
    }

    private async Task RefreshStarSchemaAsync()
    {
        var res = await CommandDispatcher.SendAsync<
            RefreshStarSchemaProjectionsCommand,
            StarSchemaRefreshResponse
        >(new RefreshStarSchemaProjectionsCommand());
        _isError = res.IsFailure;
        if (res.IsSuccess)
        {
            _starSchema = res.Value;
            _statusMessage = "Refreshed PostgreSQL star-schema projections and BI views.";
            await ReloadAsync();
        }
        else
        {
            _statusMessage = res.PrimaryError.Description;
        }
    }
}
