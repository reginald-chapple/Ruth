using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.CheckIn.Contracts.Configurations;
using Ruth.Modules.CheckIn.Tests.Fixtures;
using Ruth.SharedKernel.Messaging;
using Xunit;

namespace Ruth.Modules.CheckIn.Tests.Infrastructure;

[Collection(CheckInPostgresFixtures.Name)]
public sealed class W3S2CheckInPersistenceIntegrationTests(CheckInPostgresContainerFixture postgres)
{
    [Fact]
    public async Task CheckInConfigurations_AreaMatrix_LiveToggle_AndEvaluation_PersistEndToEnd()
    {
        var connectionString = await postgres.CreateIsolatedDatabaseAsync();
        await using var provider = await CheckInTestHostFactory.BuildAndMigrateAsync(
            connectionString
        );
        await using var scope = provider.CreateAsyncScope();
        var commands = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();
        var queries = scope.ServiceProvider.GetRequiredService<IQueryDispatcher>();

        var branchId = Guid.CreateVersion7();
        var scheduleId = Guid.CreateVersion7();
        var room1Id = Guid.CreateVersion7();
        var room2Id = Guid.CreateVersion7();

        var created = await commands.SendAsync<
            CreateCheckInConfigurationCommand,
            CheckInConfigurationDetailResponse
        >(
            new CreateCheckInConfigurationCommand(
                "Downtown Community Center Check-In",
                Description: "Youth studio and makerspace check-in",
                ProfileKind: CheckInProfileKind.YouthAndChildcare,
                BranchId: branchId
            )
        );
        Assert.True(created.IsSuccess);
        var configId = created.Value.Id;

        var dupAttempt = await commands.SendAsync<
            CreateCheckInConfigurationCommand,
            CheckInConfigurationDetailResponse
        >(new CreateCheckInConfigurationCommand("downtown community center check-in"));
        Assert.True(dupAttempt.IsFailure);

        var withArea = await UpsertAndUpdateAreaMatrixAsync(
            commands,
            configId,
            scheduleId,
            room1Id,
            room2Id
        );
        var areaId = Assert.Single(withArea.Areas).Id;

        await VerifyRoomToggleAndEvaluationAsync(
            commands,
            queries,
            configId,
            areaId,
            scheduleId,
            room1Id,
            room2Id
        );

        var listed = await queries.QueryAsync<
            GetCheckInConfigurationsQuery,
            IReadOnlyList<CheckInConfigurationSummaryResponse>
        >(new GetCheckInConfigurationsQuery(BranchId: branchId, ActiveOnly: true));
        _ = Assert.Single(listed.Value);

        var removed = await commands.SendAsync<
            RemoveCheckInAreaCommand,
            CheckInConfigurationDetailResponse
        >(new RemoveCheckInAreaCommand(configId, areaId));
        Assert.True(removed.IsSuccess);
        Assert.Empty(removed.Value.Areas);
    }

    private static async Task<CheckInConfigurationDetailResponse> UpsertAndUpdateAreaMatrixAsync(
        ICommandDispatcher commands,
        Guid configId,
        Guid scheduleId,
        Guid room1Id,
        Guid room2Id
    )
    {
        var added = await commands.SendAsync<
            UpsertCheckInAreaCommand,
            CheckInConfigurationDetailResponse
        >(
            new UpsertCheckInAreaCommand(
                configId,
                "Youth Makers Lab",
                BalancingStrategy: RoomBalancingStrategy.LowestCapacityRatio,
                IssueSecurityPickupCode: true,
                Eligibility: new CheckInEligibilityRuleDto(
                    MinAgeYears: 8m,
                    MaxAgeYears: 12m,
                    AllowedMembershipTiers: ["Standard", "Family"]
                ),
                Rooms:
                [
                    new CheckInRoomMatrixCellInputDto(
                        room1Id,
                        "Lab Room 1",
                        [scheduleId],
                        SoftCapacity: 10,
                        FirmCapacity: 12,
                        PriorityOrder: 0
                    ),
                ]
            )
        );
        Assert.True(added.IsSuccess);
        var existingArea = Assert.Single(added.Value.Areas);
        var originalCellId = Assert.Single(existingArea.Rooms).Id;

        // Re-upsert same area adding Room 2 while preserving Room 1 cell ID in-place.
        var updated = await commands.SendAsync<
            UpsertCheckInAreaCommand,
            CheckInConfigurationDetailResponse
        >(
            new UpsertCheckInAreaCommand(
                configId,
                "Youth Makers Lab",
                AreaId: existingArea.Id,
                BalancingStrategy: RoomBalancingStrategy.LowestCapacityRatio,
                IssueSecurityPickupCode: true,
                Eligibility: new CheckInEligibilityRuleDto(
                    MinAgeYears: 8m,
                    MaxAgeYears: 12m,
                    AllowedMembershipTiers: ["Standard", "Family"]
                ),
                Rooms:
                [
                    new CheckInRoomMatrixCellInputDto(
                        room1Id,
                        "Lab Room 1 Updated",
                        [scheduleId],
                        SoftCapacity: 10,
                        FirmCapacity: 12,
                        PriorityOrder: 0
                    ),
                    new CheckInRoomMatrixCellInputDto(
                        room2Id,
                        "Lab Room 2",
                        [scheduleId],
                        SoftCapacity: 20,
                        FirmCapacity: 24,
                        PriorityOrder: 1
                    ),
                ]
            )
        );
        Assert.True(updated.IsSuccess);
        var refreshedArea = Assert.Single(updated.Value.Areas);
        Assert.Equal(2, refreshedArea.Rooms.Count);
        Assert.Equal(originalCellId, refreshedArea.Rooms.Single(r => r.LocationId == room1Id).Id);
        return updated.Value;
    }

    private static async Task VerifyRoomToggleAndEvaluationAsync(
        ICommandDispatcher commands,
        IQueryDispatcher queries,
        Guid configId,
        Guid areaId,
        Guid scheduleId,
        Guid room1Id,
        Guid room2Id
    )
    {
        var evalOpen = await queries.QueryAsync<
            EvaluateCheckInOptionsQuery,
            CheckInEvaluationResponse
        >(
            new EvaluateCheckInOptionsQuery(
                configId,
                scheduleId,
                new CheckInParticipantContextDto(AgeYears: 10m, MembershipTier: "Family"),
                new Dictionary<Guid, int> { [room1Id] = 5, [room2Id] = 2 }
            )
        );
        Assert.True(evalOpen.IsSuccess);
        var areaOption = Assert.Single(evalOpen.Value.AreaOptions);
        Assert.True(areaOption.IsEligible);
        Assert.Equal(room2Id, areaOption.RecommendedLocationId);

        // Close Room 2 live -> evaluation immediately shifts recommendation to Room 1.
        var closedRoom2 = await commands.SendAsync<
            SetRoomCheckInAvailabilityCommand,
            CheckInConfigurationDetailResponse
        >(
            new SetRoomCheckInAvailabilityCommand(
                configId,
                areaId,
                room2Id,
                IsOpenForCheckIn: false,
                SoftCapacity: 20,
                FirmCapacity: 24
            )
        );
        Assert.True(closedRoom2.IsSuccess);

        var evalAfterClose = await queries.QueryAsync<
            EvaluateCheckInOptionsQuery,
            CheckInEvaluationResponse
        >(
            new EvaluateCheckInOptionsQuery(
                configId,
                scheduleId,
                new CheckInParticipantContextDto(AgeYears: 10m, MembershipTier: "Family"),
                new Dictionary<Guid, int> { [room1Id] = 5, [room2Id] = 2 }
            )
        );
        Assert.Equal(
            room1Id,
            Assert.Single(evalAfterClose.Value.AreaOptions).RecommendedLocationId
        );
    }
}
