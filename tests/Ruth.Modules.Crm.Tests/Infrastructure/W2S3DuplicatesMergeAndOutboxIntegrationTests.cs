using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Core.Contracts.Campuses;
using Ruth.Modules.Crm.Contracts.Duplicates;
using Ruth.Modules.Crm.Contracts.People;
using Ruth.Modules.Crm.Slices.Duplicates;
using Ruth.Modules.Crm.Tests.Fixtures;
using Ruth.Persistence.Outbox;
using Ruth.SharedKernel.Jobs;
using Ruth.SharedKernel.Messaging;
using Xunit;

namespace Ruth.Modules.Crm.Tests.Infrastructure;

[Collection(CrmPostgresFixtures.Name)]
public sealed class W2S3DuplicatesMergeAndOutboxIntegrationTests(
    CrmPostgresContainerFixture fixture
)
{
    [Fact]
    public async Task DuplicateScannerJob_Merge_ChainedRedirect_AndCoreOutboxConsumer_WorkEndToEnd()
    {
        var conn = await fixture.CreateIsolatedDatabaseAsync();
        await using var provider = await CrmTestHostFactory.BuildAndMigrateAsync(
            conn,
            includeCoreModule: true
        );
        await using var scope = provider.CreateAsyncScope();
        var commands = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();
        var queries = scope.ServiceProvider.GetRequiredService<IQueryDispatcher>();

        var (survivorId, retiredId) = await SeedDuplicatePairAsync(commands);
        var campusId = (
            await commands.SendAsync<CreateCampusCommand, CampusResponse>(
                new CreateCampusCommand("West Campus", LeaderPersonId: retiredId)
            )
        )
            .Value
            .Id;

        await RunDuplicateJobAndVerifyPairAsync(provider, queries);
        await MergeAndVerifyRedirectsAsync(commands, queries, survivorId, retiredId);
        await DispatchOutboxAndVerifyCoreCampusRepointedAsync(
            provider,
            queries,
            campusId,
            survivorId
        );
    }

    private static async Task<(Guid SurvivorId, Guid RetiredId)> SeedDuplicatePairAsync(
        ICommandDispatcher commands
    )
    {
        var survivor = (
            await commands.SendAsync<CreatePersonCommand, PersonDetailResponse>(
                new CreatePersonCommand(
                    "Robert",
                    "Miller",
                    Email: "bob.miller@example.org",
                    Gender: Gender.Male,
                    BirthYear: 1980,
                    BirthMonth: 2,
                    BirthDay: 14,
                    PhoneNumbers:
                    [
                        new PhoneNumberInputDto(
                            PhoneNumberType.Mobile,
                            "6025550111",
                            IsMessagingEnabled: false
                        ),
                    ]
                )
            )
        )
            .Value
            .Id;

        var retired = (
            await commands.SendAsync<CreatePersonCommand, PersonDetailResponse>(
                new CreatePersonCommand(
                    "Bob",
                    "Miller",
                    Email: "bob.miller@example.org",
                    Gender: Gender.Male,
                    BirthYear: 1980,
                    BirthMonth: 2,
                    BirthDay: 14,
                    PhoneNumbers:
                    [
                        new PhoneNumberInputDto(
                            PhoneNumberType.Mobile,
                            "6025550111",
                            IsMessagingEnabled: true
                        ),
                    ]
                )
            )
        )
            .Value
            .Id;

        return (survivor, retired);
    }

    private static async Task RunDuplicateJobAndVerifyPairAsync(
        ServiceProvider provider,
        IQueryDispatcher queries
    )
    {
        var dupJob = provider
            .GetServices<IScheduledJob>()
            .OfType<PersonDuplicateScannerJob>()
            .Single();
        var jobResult = await dupJob.ExecuteAsync();
        Assert.Equal(JobExecutionStatus.Success, jobResult.Status);

        var duplicates = (
            await queries.QueryAsync<
                GetPersonDuplicatesQuery,
                IReadOnlyList<PersonDuplicateResponse>
            >(new GetPersonDuplicatesQuery(MinConfidenceScore: 40m))
        ).Value;
        var singlePair = Assert.Single(duplicates);
        Assert.True(singlePair.ConfidenceScore >= 60m);
    }

    private static async Task MergeAndVerifyRedirectsAsync(
        ICommandDispatcher commands,
        IQueryDispatcher queries,
        Guid survivorId,
        Guid retiredId
    )
    {
        var legacyAliasGuid = Guid.CreateVersion7();
        var mergeResult = await commands.SendAsync<MergePeopleCommand, PersonMergeResponse>(
            new MergePeopleCommand(survivorId, retiredId, LegacyPersonAliasGuid: legacyAliasGuid)
        );
        Assert.True(mergeResult.IsSuccess);

        var loadedViaRetired = (
            await queries.QueryAsync<GetPersonByIdQuery, PersonDetailResponse>(
                new GetPersonByIdQuery(retiredId, FollowRedirects: true)
            )
        ).Value;
        Assert.Equal(survivorId, loadedViaRetired.Id);
        Assert.True(loadedViaRetired.PhoneNumbers[0].IsMessagingEnabled);

        var resolvedLegacy = (
            await queries.QueryAsync<
                ResolveCanonicalPersonIdQuery,
                CanonicalPersonResolutionResponse
            >(new ResolveCanonicalPersonIdQuery(legacyAliasGuid))
        ).Value;
        Assert.True(resolvedLegacy.WasRedirected);
        Assert.Equal(survivorId, resolvedLegacy.CanonicalPersonId);
    }

    private static async Task DispatchOutboxAndVerifyCoreCampusRepointedAsync(
        ServiceProvider provider,
        IQueryDispatcher queries,
        Guid campusId,
        Guid survivorId
    )
    {
        var totalDispatched = 0;
        foreach (var processor in provider.GetServices<IOutboxModuleProcessor>())
        {
            totalDispatched += await processor.ProcessBatchAsync();
        }

        Assert.True(totalDispatched >= 1);

        var updatedCampus = (
            await queries.QueryAsync<GetCampusByIdQuery, CampusResponse>(
                new GetCampusByIdQuery(campusId)
            )
        ).Value;
        Assert.Equal(survivorId, updatedCampus.LeaderPersonId);
    }
}
