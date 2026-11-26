using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Ruth.Modules.Reporting.Contracts.DataViews;
using Ruth.Modules.Reporting.Contracts.MergeTemplates;
using Ruth.Modules.Reporting.Contracts.Reports;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Reporting.Slices;

internal static class W6S5ReportingEndpoints
{
    public static IEndpointRouteBuilder MapReportingDataViewsAndReportsEndpoints(
        this IEndpointRouteBuilder endpoints
    )
    {
        var group = endpoints
            .MapGroup("/api/reporting")
            .WithTags("Reporting DataViews & Reports")
            .RequireAuthorization();
        MapDataViewCrud(group);
        MapDataViewExecution(group);
        MapReportCrud(group);
        MapReportExecution(group);
        MapMergeTemplates(group);
        return endpoints;
    }

    private static void MapDataViewCrud(RouteGroupBuilder group)
    {
        _ = group
            .MapGet(
                "/dataviews",
                static async (
                    string? entityTypeKey,
                    bool? includeInactive,
                    IQueryDispatcher q,
                    CancellationToken ct
                ) =>
                    ReportingHttpResultMapper.ToHttpResult(
                        await q.QueryAsync<GetDataViewsQuery, IReadOnlyList<DataViewResponse>>(
                            new GetDataViewsQuery(entityTypeKey, includeInactive == true),
                            ct
                        )
                    )
            )
            .WithName("GetReportingDataViews");

        _ = group
            .MapPost(
                "/dataviews",
                static async (
                    CreateDataViewCommand cmd,
                    ICommandDispatcher c,
                    CancellationToken ct
                ) =>
                {
                    var res = await c.SendAsync<CreateDataViewCommand, DataViewResponse>(cmd, ct);
                    return res.IsSuccess
                        ? Results.Created($"/api/reporting/dataviews/{res.Value.Id:D}", res.Value)
                        : ReportingHttpResultMapper.ToProblem(res.PrimaryError);
                }
            )
            .WithName("CreateReportingDataView");

        _ = group
            .MapPut(
                "/dataviews/{dataViewId:guid}",
                static async (
                    Guid dataViewId,
                    UpdateDataViewCommand body,
                    ICommandDispatcher c,
                    CancellationToken ct
                ) =>
                    ReportingHttpResultMapper.ToHttpResult(
                        await c.SendAsync<UpdateDataViewCommand, DataViewResponse>(
                            body with
                            {
                                DataViewId = dataViewId,
                            },
                            ct
                        )
                    )
            )
            .WithName("UpdateReportingDataView");
    }

    private static void MapDataViewExecution(RouteGroupBuilder group)
    {
        _ = group
            .MapGet(
                "/dataviews/{dataViewId:guid}/execute",
                static async (
                    Guid dataViewId,
                    int? maxRows,
                    bool? usePersisted,
                    IQueryDispatcher q,
                    CancellationToken ct
                ) =>
                    ReportingHttpResultMapper.ToHttpResult(
                        await q.QueryAsync<ExecuteDataViewQuery, DataViewExecutionResponse>(
                            new ExecuteDataViewQuery(
                                dataViewId,
                                maxRows ?? 250,
                                usePersisted != false
                            ),
                            ct
                        )
                    )
            )
            .WithName("ExecuteReportingDataView");

        _ = group
            .MapPost(
                "/dataviews/{dataViewId:guid}/materialize",
                static async (Guid dataViewId, ICommandDispatcher c, CancellationToken ct) =>
                    ReportingHttpResultMapper.ToHttpResult(
                        await c.SendAsync<MaterializeDataViewCommand, DataViewExecutionResponse>(
                            new MaterializeDataViewCommand(dataViewId),
                            ct
                        )
                    )
            )
            .WithName("MaterializeReportingDataView");
    }

    private static void MapReportCrud(RouteGroupBuilder group)
    {
        _ = group
            .MapGet(
                "/reports",
                static async (
                    string? entityTypeKey,
                    bool? includeInactive,
                    IQueryDispatcher q,
                    CancellationToken ct
                ) =>
                    ReportingHttpResultMapper.ToHttpResult(
                        await q.QueryAsync<GetReportsQuery, IReadOnlyList<ReportResponse>>(
                            new GetReportsQuery(entityTypeKey, includeInactive == true),
                            ct
                        )
                    )
            )
            .WithName("GetReportingReports");

        _ = group
            .MapPost(
                "/reports",
                static async (
                    CreateReportCommand cmd,
                    ICommandDispatcher c,
                    CancellationToken ct
                ) =>
                {
                    var res = await c.SendAsync<CreateReportCommand, ReportResponse>(cmd, ct);
                    return res.IsSuccess
                        ? Results.Created($"/api/reporting/reports/{res.Value.Id:D}", res.Value)
                        : ReportingHttpResultMapper.ToProblem(res.PrimaryError);
                }
            )
            .WithName("CreateReportingReport");
    }

    private static void MapReportExecution(RouteGroupBuilder group)
    {
        _ = group
            .MapGet(
                "/reports/{reportId:guid}/execute",
                static async (
                    Guid reportId,
                    int? maxRows,
                    IQueryDispatcher q,
                    CancellationToken ct
                ) =>
                    ReportingHttpResultMapper.ToHttpResult(
                        await q.QueryAsync<ExecuteReportQuery, ReportExecutionResponse>(
                            new ExecuteReportQuery(reportId, maxRows),
                            ct
                        )
                    )
            )
            .WithName("ExecuteReportingReport");

        _ = group
            .MapGet(
                "/reports/{reportId:guid}/export",
                static async (
                    Guid reportId,
                    ReportExportFormat? format,
                    int? maxRows,
                    IQueryDispatcher q,
                    CancellationToken ct
                ) =>
                {
                    var res = await q.QueryAsync<ExportReportQuery, ReportingFileExportResponse>(
                        new ExportReportQuery(reportId, format ?? ReportExportFormat.Csv, maxRows),
                        ct
                    );
                    return res.IsSuccess
                        ? Results.File(res.Value.Content, res.Value.ContentType, res.Value.FileName)
                        : ReportingHttpResultMapper.ToProblem(res.PrimaryError);
                }
            )
            .WithName("ExportReportingReport");
    }

    private static void MapMergeTemplates(RouteGroupBuilder group)
    {
        _ = group
            .MapGet(
                "/merge-templates",
                static async (bool? includeInactive, IQueryDispatcher q, CancellationToken ct) =>
                    ReportingHttpResultMapper.ToHttpResult(
                        await q.QueryAsync<
                            GetMergeTemplatesQuery,
                            IReadOnlyList<MergeTemplateResponse>
                        >(new GetMergeTemplatesQuery(includeInactive == true), ct)
                    )
            )
            .WithName("GetReportingMergeTemplates");

        _ = group
            .MapPost(
                "/merge-templates",
                static async (
                    CreateMergeTemplateCommand cmd,
                    ICommandDispatcher c,
                    CancellationToken ct
                ) =>
                {
                    var res = await c.SendAsync<CreateMergeTemplateCommand, MergeTemplateResponse>(
                        cmd,
                        ct
                    );
                    return res.IsSuccess
                        ? Results.Created(
                            $"/api/reporting/merge-templates/{res.Value.Id:D}",
                            res.Value
                        )
                        : ReportingHttpResultMapper.ToProblem(res.PrimaryError);
                }
            )
            .WithName("CreateReportingMergeTemplate");

        _ = group
            .MapPost(
                "/merge-templates/{mergeTemplateId:guid}/generate",
                static async (
                    Guid mergeTemplateId,
                    GenerateMergeDocumentCommand body,
                    ICommandDispatcher c,
                    CancellationToken ct
                ) =>
                {
                    var res = await c.SendAsync<
                        GenerateMergeDocumentCommand,
                        ReportingFileExportResponse
                    >(body with { MergeTemplateId = mergeTemplateId }, ct);
                    return res.IsSuccess
                        ? Results.File(res.Value.Content, res.Value.ContentType, res.Value.FileName)
                        : ReportingHttpResultMapper.ToProblem(res.PrimaryError);
                }
            )
            .WithName("GenerateReportingMergeDocument");
    }
}
