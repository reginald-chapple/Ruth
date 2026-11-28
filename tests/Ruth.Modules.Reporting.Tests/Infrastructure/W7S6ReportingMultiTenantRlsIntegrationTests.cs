using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Reporting.Contracts.Analytics;
using Ruth.Modules.Reporting.Contracts.DataViews;
using Ruth.Modules.Reporting.Contracts.Metrics;
using Ruth.Modules.Reporting.Tests.Fixtures;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Tenancy;
using Xunit;

namespace Ruth.Modules.Reporting.Tests.Infrastructure;

[Collection(ReportingFixtures.Name)]
public sealed class W7S6ReportingMultiTenantRlsIntegrationTests(
    ReportingContainersFixture containers
)
{
    [Fact]
    public async Task ForceRls_TenantDataViews_StarSchemaProjections_AndSecurityInvokerBiViews_IsolateTenants()
    {
        var dbConn = await containers.CreateIsolatedDatabaseAsync();
        var tenantA = TenantId.Create();
        var tenantB = TenantId.Create();
        var personA = Guid.CreateVersion7();
        var personB = Guid.CreateVersion7();

        var tenantAwareProvider = new TenantAwarePersonSourceProvider(
            new Dictionary<TenantId, IReadOnlyList<DataViewEntityRecord>>
            {
                [tenantA] =
                [
                    new DataViewEntityRecord(
                        personA,
                        "Ruth.Crm.Person",
                        personA,
                        new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                        {
                            ["FirstName"] = "Alice",
                            ["RecordStatus"] = "Active",
                        }
                    ),
                ],
                [tenantB] =
                [
                    new DataViewEntityRecord(
                        personB,
                        "Ruth.Crm.Person",
                        personB,
                        new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                        {
                            ["FirstName"] = "Bob",
                            ["RecordStatus"] = "Active",
                        }
                    ),
                ],
            }
        );

        await using var provider = await ReportingTestHostFactory.CreateAsync(
            dbConn,
            tenantAwareProvider
        );
        await W7S6ReportingRlsTestHelper.AssertAllReportingTablesHaveForceRlsAndFailClosedAsync(
            dbConn,
            provider
        );
        W7S6ReportingRlsTestHelper.VerifyTenantScopedAstCompilation(tenantA);

        await SeedAndRefreshTenantReportingAsync(provider, tenantA, "alpha-rep", personA, 10m);
        await SeedAndRefreshTenantReportingAsync(provider, tenantB, "beta-rep", personB, 25m);

        await W7S6ReportingRlsTestHelper.VerifyBiViewsEnforceAppRoleRlsAndGucPredicateAsync(
            dbConn,
            provider,
            tenantA,
            tenantB
        );
    }

    private static async Task SeedAndRefreshTenantReportingAsync(
        ServiceProvider provider,
        TenantId tenantId,
        string slug,
        Guid expectedPersonId,
        decimal manualBonusValue
    )
    {
        using (AmbientTenantContext.BeginScope(tenantId, slug, TenantLifecycleStage.Live))
        {
            await using var scope = provider.CreateAsyncScope();
            var commands = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();

            var dv = (
                await commands.SendAsync<CreateDataViewCommand, DataViewResponse>(
                    new CreateDataViewCommand(
                        "Active Constituents",
                        "Ruth.Crm.Person",
                        new DataViewFilterNodeDto(
                            DataViewFilterExpressionType.Rule,
                            DataViewFilterRuleKind.EntityProperty,
                            "RecordStatus",
                            DataViewFilterOperator.Equals,
                            "Active"
                        ),
                        PersistedScheduleIntervalMinutes: 30
                    )
                )
            ).Value;

            var mat = (
                await commands.SendAsync<MaterializeDataViewCommand, DataViewExecutionResponse>(
                    new MaterializeDataViewCommand(dv.Id)
                )
            ).Value;
            var singleId = Assert.Single(mat.EntityIds);
            Assert.Equal(expectedPersonId, singleId);
            Assert.Contains(
                "tenant_id = @tenantId",
                mat.CompiledSqlPreview,
                StringComparison.Ordinal
            );

            var metric = (
                await commands.SendAsync<CreateMetricCommand, MetricResponse>(
                    new CreateMetricCommand(
                        "Impact Metric",
                        "kpi.impact-score",
                        MetricSourceType.Manual,
                        UnitLabel: "Points"
                    )
                )
            ).Value;

            _ = await commands.SendAsync<
                RecordMetricValuesBatchCommand,
                IReadOnlyList<MetricValueResponse>
            >(
                new RecordMetricValuesBatchCommand([
                    new MetricValueEntryInput(
                        metric.Id,
                        MetricValueType.Measure,
                        manualBonusValue,
                        DateTimeOffset.UtcNow
                    ),
                ])
            );

            var star = await commands.SendAsync<
                RefreshStarSchemaProjectionsCommand,
                StarSchemaRefreshResponse
            >(new RefreshStarSchemaProjectionsCommand());
            Assert.True(star.IsSuccess);
            Assert.Equal(1, star.Value.FactMetricDailyRows);
        }
    }

    private sealed class TenantAwarePersonSourceProvider(
        IReadOnlyDictionary<TenantId, IReadOnlyList<DataViewEntityRecord>> byTenant
    ) : IDataViewSourceProvider
    {
        public string EntityTypeKey => "Ruth.Crm.Person";

        public string DisplayName => "Tenant-Aware People";

        public IReadOnlyList<string> AvailableFields => ["FirstName", "RecordStatus"];

        public Task<IReadOnlyList<DataViewEntityRecord>> LoadRecordsAsync(
            int maxRows,
            CancellationToken cancellationToken = default
        )
        {
            var tid = AmbientTenantContext.CurrentTenantId;
            return Task.FromResult(
                !tid.IsEmpty && byTenant.TryGetValue(tid, out var rows) ? rows : []
            );
        }
    }
}
