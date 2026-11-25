using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Core.Contracts.Branches;
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
        var branchId = (
            await commands.SendAsync<CreateBranchCommand, BranchResponse>(
                new CreateBranchCommand("West Branch", LeaderPersonId: retiredId)
            )
        )
            .Value
            .Id;

        await RunDuplicateJobAndVerifyPairAsync(provider, queries);
        await MergeAndVerifyRedirectsAsync(commands, queries, survivorId, retiredId);
        await DispatchOutboxAndVerifyCoreBranchRepointedAsync(
            provider,
            queries,
            branchId,
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
        var externalRecordGuid = Guid.CreateVersion7();
        var mergeResult = await commands.SendAsync<MergePeopleCommand, PersonMergeResponse>(
            new MergePeopleCommand(survivorId, retiredId, ExternalRecordGuid: externalRecordGuid)
        );
        Assert.True(mergeResult.IsSuccess);

        var loadedViaRetired = (
            await queries.QueryAsync<GetPersonByIdQuery, PersonDetailResponse>(
                new GetPersonByIdQuery(retiredId, FollowRedirects: true)
            )
        ).Value;
        Assert.Equal(survivorId, loadedViaRetired.Id);
        Assert.True(loadedViaRetired.PhoneNumbers[0].IsMessagingEnabled);

        var resolvedExternal = (
            await queries.QueryAsync<
                ResolveCanonicalPersonIdQuery,
                CanonicalPersonResolutionResponse
            >(new ResolveCanonicalPersonIdQuery(externalRecordGuid))
        ).Value;
        Assert.True(resolvedExternal.WasRedirected);
        Assert.Equal(survivorId, resolvedExternal.CanonicalPersonId);
    }

    private static async Task DispatchOutboxAndVerifyCoreBranchRepointedAsync(
        ServiceProvider provider,
        IQueryDispatcher queries,
        Guid branchId,
        Guid survivorId
    )
    {
        var totalDispatched = 0;
        foreach (var processor in provider.GetServices<IOutboxModuleProcessor>())
        {
            totalDispatched += await processor.ProcessBatchAsync();
        }

        Assert.True(totalDispatched >= 1);

        var updatedBranch = (
            await queries.QueryAsync<GetBranchByIdQuery, BranchResponse>(
                new GetBranchByIdQuery(branchId)
            )
        ).Value;
        Assert.Equal(survivorId, updatedBranch.LeaderPersonId);
    }
}
