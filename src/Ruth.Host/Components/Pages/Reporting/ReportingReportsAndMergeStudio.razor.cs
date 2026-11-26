using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Ruth.Modules.Reporting.Contracts.MergeTemplates;
using Ruth.Modules.Reporting.Contracts.Reports;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Host.Components.Pages.Reporting;

public sealed partial class ReportingReportsAndMergeStudio : ComponentBase
{
    private IReadOnlyList<ReportResponse> _reports = [];
    private IReadOnlyList<MergeTemplateResponse> _templates = [];
    private ReportExecutionResponse? _preview;
    private string _reportName = "Constituent Directory Export";
    private string _reportEntityType = "Ruth.Crm.Person";
    private string _templateName = "Welcome Letter";
    private string _templateBody = "Dear {{ FirstName }} {{ LastName }}, welcome to our community.";
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
        var rRes = await QueryDispatcher.QueryAsync<GetReportsQuery, IReadOnlyList<ReportResponse>>(
            new GetReportsQuery()
        );
        if (rRes.IsSuccess)
        {
            _reports = rRes.Value;
        }

        var tRes = await QueryDispatcher.QueryAsync<
            GetMergeTemplatesQuery,
            IReadOnlyList<MergeTemplateResponse>
        >(new GetMergeTemplatesQuery());
        if (tRes.IsSuccess)
        {
            _templates = tRes.Value;
        }
    }

    private async Task CreateReportAsync()
    {
        var fields = new List<ReportFieldSpec>
        {
            new(
                "reporting.field.composite",
                "Full Name",
                JsonSerializer.Serialize(new { template = "{FirstName} {LastName}" }),
                SortOrder: 1,
                SortDirection: ReportSortDirection.Ascending
            ),
            new(
                "reporting.field.property",
                "Email",
                JsonSerializer.Serialize(new { property = "Email" })
            ),
            new(
                "reporting.field.property",
                "Status",
                JsonSerializer.Serialize(new { property = "RecordStatus" })
            ),
        };
        var res = await CommandDispatcher.SendAsync<CreateReportCommand, ReportResponse>(
            new CreateReportCommand(_reportName, _reportEntityType, fields)
        );
        _isError = res.IsFailure;
        _statusMessage = res.IsSuccess
            ? $"Created report '{res.Value.Name}'."
            : res.PrimaryError.Description;
        if (res.IsSuccess)
        {
            await ReloadAsync();
        }
    }

    private async Task RunReportAsync(Guid reportId)
    {
        var res = await QueryDispatcher.QueryAsync<ExecuteReportQuery, ReportExecutionResponse>(
            new ExecuteReportQuery(reportId)
        );
        if (res.IsSuccess)
        {
            _preview = res.Value;
            _isError = false;
            _statusMessage = string.Create(
                CultureInfo.InvariantCulture,
                $"Loaded {res.Value.TotalRows} row(s)."
            );
        }
    }

    private async Task CreateTemplateAsync()
    {
        var res = await CommandDispatcher.SendAsync<
            CreateMergeTemplateCommand,
            MergeTemplateResponse
        >(new CreateMergeTemplateCommand(_templateName, MergeTemplateFormat.Html, _templateBody));
        _isError = res.IsFailure;
        _statusMessage = res.IsSuccess
            ? $"Created merge template '{res.Value.Name}'."
            : res.PrimaryError.Description;
        if (res.IsSuccess)
        {
            await ReloadAsync();
        }
    }
}
