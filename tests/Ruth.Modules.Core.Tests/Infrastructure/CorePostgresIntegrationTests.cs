using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Ruth.Modules.Core.Contracts.DefinedTypes;
using Ruth.Modules.Core.Contracts.EntityTypes;
using Ruth.Modules.Core.Contracts.FieldTypes;
using Ruth.Modules.Core.Domain;
using Ruth.Modules.Core.Tests.Fixtures;
using Ruth.SharedKernel.Messaging;
using Xunit;

namespace Ruth.Modules.Core.Tests.Infrastructure;

[Collection(CorePostgresFixtures.Name)]
public sealed class CorePostgresIntegrationTests(CorePostgresContainerFixture postgres)
{
    [Fact]
    public async Task BootstrapMigration_CreatesCoreSchemaGinIndexAndSyncsEntityTypes()
    {
        var cs = await postgres.CreateIsolatedDatabaseAsync();
        await using var provider = await CoreTestHostFactory.BuildAndMigrateAsync(cs);

        await using (var conn = new NpgsqlConnection(cs))
        {
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText =
                "SELECT indexdef FROM pg_indexes WHERE schemaname = 'core' AND tablename = 'defined_values' AND indexdef ILIKE '%USING gin%';";
            var indexDef = await cmd.ExecuteScalarAsync() as string;
            Assert.NotNull(indexDef);
            Assert.Contains("attributes", indexDef, StringComparison.OrdinalIgnoreCase);
        }

        await using var scope = provider.CreateAsyncScope();
        var queries = scope.ServiceProvider.GetRequiredService<IQueryDispatcher>();

        var entityTypes = await queries.QueryAsync<
            GetEntityTypesQuery,
            IReadOnlyList<EntityTypeResponse>
        >(new GetEntityTypesQuery());
        Assert.True(entityTypes.IsSuccess);
        Assert.True(entityTypes.Value.Count >= 6);

        var definedValueType = await queries.QueryAsync<
            GetEntityTypeByKeyQuery,
            EntityTypeResponse
        >(new GetEntityTypeByKeyQuery(DefinedValue.EntityTypeKeyName));
        Assert.True(definedValueType.IsSuccess);
        Assert.True(definedValueType.Value.SupportsAttributes);

        var fieldTypes = await queries.QueryAsync<
            GetFieldTypesQuery,
            IReadOnlyList<FieldTypeResponse>
        >(new GetFieldTypesQuery());
        Assert.True(fieldTypes.IsSuccess);
        Assert.Equal(25, fieldTypes.Value.Count);
    }

    [Fact]
    public async Task DefinedTypesAndValues_CrudReorderAndJsonbGinContainmentQuery_WorkAgainstPostgres18()
    {
        var cs = await postgres.CreateIsolatedDatabaseAsync();
        await using var provider = await CoreTestHostFactory.BuildAndMigrateAsync(cs);
        await using var scope = provider.CreateAsyncScope();

        var commands = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();
        var queries = scope.ServiceProvider.GetRequiredService<IQueryDispatcher>();

        var createdType = await commands.SendAsync<
            CreateDefinedTypeCommand,
            DefinedTypeDetailResponse
        >(new CreateDefinedTypeCommand("Marital Status", Description: "CRM statuses", Order: 1));
        Assert.True(createdType.IsSuccess);

        var married = await commands.SendAsync<AddDefinedValueCommand, DefinedValueResponse>(
            new AddDefinedValueCommand(
                createdType.Value.Id,
                "Married",
                Description: "Married individual",
                Order: 0,
                Attributes: new Dictionary<string, string?>(StringComparer.Ordinal)
                {
                    ["badgeColor"] = "#107C10",
                    ["tier"] = "family",
                }
            )
        );
        var single = await commands.SendAsync<AddDefinedValueCommand, DefinedValueResponse>(
            new AddDefinedValueCommand(
                createdType.Value.Id,
                "Single",
                Description: "Single individual",
                Order: 1,
                Attributes: new Dictionary<string, string?>(StringComparer.Ordinal)
                {
                    ["badgeColor"] = "#0078D4",
                    ["tier"] = "individual",
                }
            )
        );
        Assert.True(married.IsSuccess);
        Assert.True(single.IsSuccess);

        var reorder = await commands.SendAsync<
            ReorderDefinedValuesCommand,
            IReadOnlyList<DefinedValueResponse>
        >(
            new ReorderDefinedValuesCommand(
                createdType.Value.Id,
                [single.Value.Id, married.Value.Id]
            )
        );
        Assert.True(reorder.IsSuccess);

        var detail = await queries.QueryAsync<GetDefinedTypeDetailQuery, DefinedTypeDetailResponse>(
            new GetDefinedTypeDetailQuery(createdType.Value.Id)
        );
        Assert.True(detail.IsSuccess);
        Assert.Equal("Single", detail.Value.Values[0].Value);
        Assert.Equal("Married", detail.Value.Values[1].Value);

        var ginMatches = await queries.QueryAsync<
            QueryDefinedValuesByAttributeQuery,
            IReadOnlyList<DefinedValueResponse>
        >(new QueryDefinedValuesByAttributeQuery(createdType.Value.Id, "tier", "family"));
        Assert.True(ginMatches.IsSuccess);
        var match = Assert.Single(ginMatches.Value);
        Assert.Equal("Married", match.Value);
    }
}
