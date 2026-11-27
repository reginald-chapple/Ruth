using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Ruth.Modules.Crm.Contracts.Households;
using Ruth.Modules.Crm.Contracts.People;
using Ruth.Modules.Crm.Contracts.Relationships;
using Ruth.Modules.Crm.Tests.Fixtures;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Tenancy;
using Xunit;

namespace Ruth.Modules.Crm.Tests.Infrastructure;

[Collection(CrmPostgresFixtures.Name)]
public sealed class W7S5CrmMultiTenantRlsIntegrationTests(CrmPostgresContainerFixture fixture)
{
    [Fact]
    public async Task ForceRls_RuntimeRole_People_Metaphones_Households_AndGraph_IsolateTenants()
    {
        var dbConn = await fixture.CreateIsolatedDatabaseAsync();
        await using var provider = await CrmTestHostFactory.BuildAndMigrateAsync(dbConn);
        await AssertAllCrmTablesHaveForceRlsAsync(dbConn);

        var tenantA = TenantId.Create();
        var tenantB = TenantId.Create();

        var personA = await SeedConstituentHouseholdAsync(
            provider,
            tenantA,
            "alpha-crm",
            "Elena",
            "Vasquez",
            "elena@alpha.org"
        );
        var personB = await SeedConstituentHouseholdAsync(
            provider,
            tenantB,
            "beta-crm",
            "Elena",
            "Vasquez",
            "elena@beta.org"
        );

        await AssertTenantCrmIsolationAsync(provider, tenantA, personA, tenantB, personB);
    }

    private static async Task<Guid> SeedConstituentHouseholdAsync(
        ServiceProvider provider,
        TenantId tenantId,
        string slug,
        string firstName,
        string lastName,
        string email
    )
    {
        using (AmbientTenantContext.BeginScope(tenantId, slug, TenantLifecycleStage.Live))
        {
            await using var scope = provider.CreateAsyncScope();
            var commands = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();

            var person = await commands.SendAsync<CreatePersonCommand, PersonDetailResponse>(
                new CreatePersonCommand(firstName, lastName, Email: email, Gender: Gender.Female)
            );
            Assert.True(person.IsSuccess);

            var partner = await commands.SendAsync<CreatePersonCommand, PersonDetailResponse>(
                new CreatePersonCommand("Mateo", lastName, Gender: Gender.Male)
            );
            Assert.True(partner.IsSuccess);

            var hh = await commands.SendAsync<CreateHouseholdCommand, HouseholdResponse>(
                new CreateHouseholdCommand(
                    $"{lastName} Household",
                    Members:
                    [
                        new HouseholdMemberInputDto(
                            person.Value.Id,
                            HouseholdRole.Adult,
                            IsPrimaryContact: true
                        ),
                        new HouseholdMemberInputDto(partner.Value.Id, HouseholdRole.Adult),
                    ]
                )
            );
            Assert.True(hh.IsSuccess);
            return person.Value.Id;
        }
    }

    private static async Task AssertTenantCrmIsolationAsync(
        ServiceProvider provider,
        TenantId tenantA,
        Guid personA,
        TenantId tenantB,
        Guid personB
    )
    {
        using (AmbientTenantContext.BeginScope(tenantB, "beta-crm", TenantLifecycleStage.Live))
        {
            await using var scope = provider.CreateAsyncScope();
            var queries = scope.ServiceProvider.GetRequiredService<IQueryDispatcher>();

            var crossFetch = await queries.QueryAsync<GetPersonByIdQuery, PersonDetailResponse>(
                new GetPersonByIdQuery(personA)
            );
            Assert.True(crossFetch.IsFailure);

            var search = await queries.QueryAsync<
                SearchPeopleQuery,
                IReadOnlyList<PersonSummaryResponse>
            >(new SearchPeopleQuery(Query: "Vasquez"));
            Assert.True(search.IsSuccess);
            Assert.Equal(2, search.Value.Count);
            Assert.Contains(search.Value, p => p.Id == personB);
            Assert.DoesNotContain(search.Value, p => p.Id == personA);

            var graph = await queries.QueryAsync<
                GetPersonRelationshipGraphQuery,
                PersonRelationshipGraphResponse
            >(new GetPersonRelationshipGraphQuery(personB, MaxHops: 2));
            Assert.True(graph.IsSuccess);
            Assert.DoesNotContain(graph.Value.Nodes, n => n.EntityId == personA);
        }

        using (AmbientTenantContext.BeginScope(tenantA, "alpha-crm", TenantLifecycleStage.Live))
        {
            await using var scope = provider.CreateAsyncScope();
            var queries = scope.ServiceProvider.GetRequiredService<IQueryDispatcher>();
            var fetchedA = await queries.QueryAsync<GetPersonByIdQuery, PersonDetailResponse>(
                new GetPersonByIdQuery(personA)
            );
            Assert.True(fetchedA.IsSuccess);
            Assert.Equal("elena@alpha.org", fetchedA.Value.Email);
        }
    }

    private static async Task AssertAllCrmTablesHaveForceRlsAsync(string adminConn)
    {
        await using var conn = new NpgsqlConnection(adminConn);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText =
            "SELECT c.relname, c.relrowsecurity, c.relforcerowsecurity "
            + "FROM pg_class c JOIN pg_namespace n ON n.oid = c.relnamespace "
            + "WHERE n.nspname = 'crm' AND c.relkind = 'r' AND c.relname <> '__EFMigrationsHistory'";
        await using var reader = await cmd.ExecuteReaderAsync();
        var count = 0;
        while (await reader.ReadAsync())
        {
            count++;
            Assert.True(reader.GetBoolean(1), $"Missing RLS on crm.{reader.GetString(0)}");
            Assert.True(reader.GetBoolean(2), $"Missing FORCE RLS on crm.{reader.GetString(0)}");
        }

        Assert.True(count >= 12);
    }
}
