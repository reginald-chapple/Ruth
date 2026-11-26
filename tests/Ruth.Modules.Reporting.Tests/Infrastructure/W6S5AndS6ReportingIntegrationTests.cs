using System.IO.Compression;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Reporting.Contracts.DataViews;
using Ruth.Modules.Reporting.Contracts.MergeTemplates;
using Ruth.Modules.Reporting.Contracts.Reports;
using Ruth.Modules.Reporting.Tests.Fixtures;
using Ruth.SharedKernel.Messaging;
using Xunit;

namespace Ruth.Modules.Reporting.Tests.Infrastructure;

[Collection(ReportingFixtures.Name)]
public sealed class W6S5AndS6ReportingIntegrationTests(ReportingContainersFixture containers)
{
    [Fact]
    public async Task DataViews_Reports_MergeTemplates_And_PersonMerge_WorkEndToEnd()
    {
        var conn = await containers.CreateIsolatedDatabaseAsync();
        var aliceId = Guid.CreateVersion7();
        var bobId = Guid.CreateVersion7();

        await using var sp = await ReportingTestHostFactory.CreateAsync(
            conn,
            StubPersonSourceProvider.CreateDefault(aliceId, bobId)
        );
        await using var scope = sp.CreateAsyncScope();
        var commands = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();
        var queries = scope.ServiceProvider.GetRequiredService<IQueryDispatcher>();

        var dataViewId = await CreateAndMaterializeDataViewAsync(commands, queries, aliceId);
        var reportId = await CreateAndExportReportAsync(commands, queries, dataViewId);
        await VerifyMergeTemplateAndPersonMergeAsync(
            scope.ServiceProvider,
            commands,
            reportId,
            aliceId
        );
    }

    private static async Task<Guid> CreateAndMaterializeDataViewAsync(
        ICommandDispatcher commands,
        IQueryDispatcher queries,
        Guid aliceId
    )
    {
        var dvRes = await commands.SendAsync<CreateDataViewCommand, DataViewResponse>(
            new CreateDataViewCommand(
                "Active Gold Members",
                "Ruth.Crm.Person",
                new DataViewFilterNodeDto(
                    DataViewFilterExpressionType.GroupAll,
                    Children:
                    [
                        new DataViewFilterNodeDto(
                            DataViewFilterExpressionType.Rule,
                            DataViewFilterRuleKind.EntityProperty,
                            "RecordStatus",
                            DataViewFilterOperator.Equals,
                            "Active"
                        ),
                        new DataViewFilterNodeDto(
                            DataViewFilterExpressionType.Rule,
                            DataViewFilterRuleKind.JsonbAttribute,
                            "tier",
                            DataViewFilterOperator.Equals,
                            "Gold"
                        ),
                    ]
                ),
                PersistedScheduleIntervalMinutes: 60
            )
        );
        Assert.True(dvRes.IsSuccess);

        var matRes = await commands.SendAsync<
            MaterializeDataViewCommand,
            DataViewExecutionResponse
        >(new MaterializeDataViewCommand(dvRes.Value.Id));
        Assert.True(matRes.IsSuccess);
        var matchedId = Assert.Single(matRes.Value.EntityIds);
        Assert.Equal(aliceId, matchedId);

        var cachedExec = await queries.QueryAsync<ExecuteDataViewQuery, DataViewExecutionResponse>(
            new ExecuteDataViewQuery(dvRes.Value.Id, UsePersistedIfFresh: true)
        );
        Assert.True(cachedExec.IsSuccess);
        Assert.True(cachedExec.Value.UsedPersistedValues);
        _ = Assert.Single(cachedExec.Value.EntityIds);
        return dvRes.Value.Id;
    }

    private static async Task<Guid> CreateAndExportReportAsync(
        ICommandDispatcher commands,
        IQueryDispatcher queries,
        Guid dataViewId
    )
    {
        var repRes = await commands.SendAsync<CreateReportCommand, ReportResponse>(
            new CreateReportCommand(
                "Gold Directory",
                "Ruth.Crm.Person",
                [
                    new ReportFieldSpec(
                        "reporting.field.composite",
                        "Member Name",
                        JsonSerializer.Serialize(new { template = "{FirstName} {LastName}" })
                    ),
                    new ReportFieldSpec(
                        "reporting.field.attribute",
                        "Tier",
                        JsonSerializer.Serialize(new { key = "tier" })
                    ),
                ],
                DataViewId: dataViewId
            )
        );
        Assert.True(repRes.IsSuccess);

        var csvRes = await queries.QueryAsync<ExportReportQuery, ReportingFileExportResponse>(
            new ExportReportQuery(repRes.Value.Id, ReportExportFormat.Csv)
        );
        Assert.True(csvRes.IsSuccess);
        Assert.Contains(
            "Alice Rivera",
            Encoding.UTF8.GetString(csvRes.Value.Content),
            StringComparison.Ordinal
        );

        var xlsxRes = await queries.QueryAsync<ExportReportQuery, ReportingFileExportResponse>(
            new ExportReportQuery(repRes.Value.Id, ReportExportFormat.Excel)
        );
        Assert.True(xlsxRes.IsSuccess);
        await using var stream = new MemoryStream(xlsxRes.Value.Content);
        await using var zip = new ZipArchive(stream, ZipArchiveMode.Read);
        Assert.NotNull(zip.GetEntry("xl/worksheets/sheet1.xml"));
        return repRes.Value.Id;
    }

    private static async Task VerifyMergeTemplateAndPersonMergeAsync(
        IServiceProvider sp,
        ICommandDispatcher commands,
        Guid reportId,
        Guid aliceId
    )
    {
        var tplRes = await commands.SendAsync<CreateMergeTemplateCommand, MergeTemplateResponse>(
            new CreateMergeTemplateCommand(
                "Gold Welcome Packet",
                MergeTemplateFormat.Html,
                "<p>Hello {{ MemberName }} ({{ Tier }})</p>",
                OwnerPersonId: aliceId
            )
        );
        Assert.True(tplRes.IsSuccess);

        var htmlDoc = await commands.SendAsync<
            GenerateMergeDocumentCommand,
            ReportingFileExportResponse
        >(new GenerateMergeDocumentCommand(tplRes.Value.Id, ReportId: reportId));
        Assert.True(htmlDoc.IsSuccess);
        Assert.Contains(
            "Hello Alice Rivera (Gold)",
            Encoding.UTF8.GetString(htmlDoc.Value.Content),
            StringComparison.Ordinal
        );

        var docxDoc = await commands.SendAsync<
            GenerateMergeDocumentCommand,
            ReportingFileExportResponse
        >(
            new GenerateMergeDocumentCommand(
                tplRes.Value.Id,
                ReportId: reportId,
                FormatOverride: MergeTemplateFormat.Docx
            )
        );
        Assert.True(docxDoc.IsSuccess);
        Assert.EndsWith(".docx", docxDoc.Value.FileName, StringComparison.Ordinal);

        await ReportingTestHostFactory.VerifyPersonMergeRepointsAsync(sp, aliceId);
    }
}
