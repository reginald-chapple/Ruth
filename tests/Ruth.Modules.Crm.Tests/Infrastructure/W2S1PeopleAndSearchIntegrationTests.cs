using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Crm.Contracts.People;
using Ruth.Modules.Crm.Tests.Fixtures;
using Ruth.SharedKernel.Messaging;
using Xunit;

namespace Ruth.Modules.Crm.Tests.Infrastructure;

[Collection(CrmPostgresFixtures.Name)]
public sealed class W2S1PeopleAndSearchIntegrationTests(CrmPostgresContainerFixture fixture)
{
    [Fact]
    public async Task Boot_SeedsReferenceLookupsWithZeroSamplePeople_AndSupportsMultiStrategySearch()
    {
        var conn = await fixture.CreateIsolatedDatabaseAsync();
        await using var provider = await CrmTestHostFactory.BuildAndMigrateAsync(conn);
        await using var scope = provider.CreateAsyncScope();
        var commands = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();
        var queries = scope.ServiceProvider.GetRequiredService<IQueryDispatcher>();

        await VerifyCleanBootAndMetaInsightsAsync(queries);
        var rob = await CreatePersonWithBarcodeAsync(commands);
        await VerifyAllSearchStrategiesAsync(queries, rob.Id);
    }

    private static async Task VerifyCleanBootAndMetaInsightsAsync(IQueryDispatcher queries)
    {
        var initialDir = await queries.QueryAsync<
            SearchPeopleQuery,
            IReadOnlyList<PersonSummaryResponse>
        >(new SearchPeopleQuery());
        Assert.True(initialDir.IsSuccess);
        Assert.Empty(initialDir.Value);

        var insights = await queries.QueryAsync<GetMetaNameInsightsQuery, MetaNameInsightsResponse>(
            new GetMetaNameInsightsQuery("Robert")
        );
        Assert.True(insights.IsSuccess);
        Assert.Equal(Gender.Male, insights.Value.PredictedGender);
        Assert.Contains("Bob", insights.Value.RelatedNickNames, StringComparer.OrdinalIgnoreCase);
    }

    private static async Task<PersonDetailResponse> CreatePersonWithBarcodeAsync(
        ICommandDispatcher commands
    )
    {
        var robResult = await commands.SendAsync<CreatePersonCommand, PersonDetailResponse>(
            new CreatePersonCommand(
                "Robert",
                "Smith",
                Email: "robert.smith@example.org",
                Gender: Gender.Male,
                BirthYear: 1985,
                BirthMonth: 4,
                BirthDay: 12,
                ConnectionStatus: PersonConnectionStatus.Member,
                PhoneNumbers:
                [
                    new PhoneNumberInputDto(
                        PhoneNumberType.Mobile,
                        "(602) 555-0199",
                        IsMessagingEnabled: true
                    ),
                ],
                Attributes: new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["shirtSize"] = "XL",
                }
            )
        );
        Assert.True(robResult.IsSuccess);
        var rob = robResult.Value;
        Assert.Equal("XL", rob.Attributes["shirtSize"]);
        var singlePhone = Assert.Single(rob.PhoneNumbers);
        Assert.Equal("+16025550199", singlePhone.E164Number);

        var keyResult = await commands.SendAsync<AddPersonSearchKeyCommand, PersonDetailResponse>(
            new AddPersonSearchKeyCommand(rob.Id, PersonSearchKeyType.Barcode, "BARCODE-7741")
        );
        Assert.True(keyResult.IsSuccess);
        return rob;
    }

    private static async Task VerifyAllSearchStrategiesAsync(IQueryDispatcher queries, Guid robId)
    {
        var byNick = await queries.QueryAsync<
            SearchPeopleQuery,
            IReadOnlyList<PersonSummaryResponse>
        >(new SearchPeopleQuery("Bob"));
        Assert.Contains(byNick.Value, p => p.Id == robId);

        var byPhonetic = await queries.QueryAsync<
            SearchPeopleQuery,
            IReadOnlyList<PersonSummaryResponse>
        >(new SearchPeopleQuery("Smyth"));
        Assert.Contains(byPhonetic.Value, p => p.Id == robId);

        var byPhone = await queries.QueryAsync<
            SearchPeopleQuery,
            IReadOnlyList<PersonSummaryResponse>
        >(new SearchPeopleQuery("0199"));
        Assert.Contains(byPhone.Value, p => p.Id == robId);

        var byBarcode = await queries.QueryAsync<
            SearchPeopleQuery,
            IReadOnlyList<PersonSummaryResponse>
        >(new SearchPeopleQuery("BARCODE-7741"));
        Assert.Contains(byBarcode.Value, p =>
            p.Id == robId && string.Equals(p.MatchReason, "SearchKey", StringComparison.Ordinal)
        );
    }
}
