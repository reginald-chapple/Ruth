using Ruth.Modules.CheckIn.Contracts.Configurations;
using Ruth.SharedKernel.Messaging;
using Xunit;

namespace Ruth.Modules.CheckIn.Tests.Infrastructure;

internal static class W3S3CheckInProfileSeedHelper
{
    public static async Task<(Guid ConfigId, Guid AreaId)> ConfigureCheckInProfileAsync(
        ICommandDispatcher commands,
        Guid linkedGroupId,
        Guid scheduleId,
        Guid roomAId,
        Guid roomBId
    )
    {
        var config = await commands.SendAsync<
            CreateCheckInConfigurationCommand,
            CheckInConfigurationDetailResponse
        >(
            new CreateCheckInConfigurationCommand(
                "Youth & Makerspace Kiosk",
                ProfileKind: CheckInProfileKind.YouthAndChildcare,
                SearchMode: CheckInSearchMode.PhoneAndName
            )
        );
        Assert.True(config.IsSuccess);

        var updated = await commands.SendAsync<
            UpsertCheckInAreaCommand,
            CheckInConfigurationDetailResponse
        >(
            new UpsertCheckInAreaCommand(
                config.Value.Id,
                "Elementary STEM Lab",
                LinkedGroupId: linkedGroupId,
                BalancingStrategy: RoomBalancingStrategy.OrderedPriority,
                IssueSecurityPickupCode: true,
                Eligibility: new CheckInEligibilityRuleDto(
                    MinAgeYears: 6m,
                    MaxAgeYears: 12m,
                    AllowedMembershipTiers: ["Family"]
                ),
                Rooms:
                [
                    new CheckInRoomMatrixCellInputDto(
                        roomAId,
                        "STEM Room A",
                        [scheduleId],
                        SoftCapacity: 10,
                        FirmCapacity: 15,
                        PriorityOrder: 0
                    ),
                    new CheckInRoomMatrixCellInputDto(
                        roomBId,
                        "STEM Room B",
                        [scheduleId],
                        SoftCapacity: 10,
                        FirmCapacity: 15,
                        PriorityOrder: 1
                    ),
                ]
            )
        );
        Assert.True(updated.IsSuccess);
        return (config.Value.Id, Assert.Single(updated.Value.Areas).Id);
    }
}
