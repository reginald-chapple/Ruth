using Ruth.Modules.CheckIn.Contracts.Configurations;
using Ruth.Modules.CheckIn.Domain.Configurations;
using Xunit;

namespace Ruth.Modules.CheckIn.Tests.Domain;

public sealed class W3S2RoomBalancingDomainTests
{
    [Fact]
    public void RoomBalancing_StagesOverflowRooms_EnforcesFirmCap_AndSupportsAllThreeStrategies()
    {
        var scheduleId = Guid.CreateVersion7();
        var otherScheduleId = Guid.CreateVersion7();
        var roomA = Guid.CreateVersion7();
        var roomB = Guid.CreateVersion7();
        var overflowRoom = Guid.CreateVersion7();
        var wrongScheduleRoom = Guid.CreateVersion7();

        var rooms = new[]
        {
            new CheckInRoomMatrixCellInputDto(
                roomA,
                "Room A (Small)",
                [scheduleId],
                SoftCapacity: 10,
                FirmCapacity: 12,
                PriorityOrder: 1
            ),
            new CheckInRoomMatrixCellInputDto(
                roomB,
                "Room B (Large)",
                [scheduleId],
                SoftCapacity: 30,
                FirmCapacity: 35,
                PriorityOrder: 0
            ),
            new CheckInRoomMatrixCellInputDto(
                overflowRoom,
                "Overflow Studio",
                [scheduleId],
                SoftCapacity: 15,
                FirmCapacity: 20,
                PriorityOrder: 2,
                IsOverflowRoom: true
            ),
            new CheckInRoomMatrixCellInputDto(
                wrongScheduleRoom,
                "Evening Only Room",
                [otherScheduleId],
                SoftCapacity: 10,
                FirmCapacity: 15,
                PriorityOrder: 0
            ),
        };

        var config = CreateBaseConfiguration();

        var ratioArea = config
            .UpsertArea(
                CreateAreaSpec("Ratio Area", RoomBalancingStrategy.LowestCapacityRatio, rooms)
            )
            .Value;
        var (ratioRecId, _, _) = RoomBalancingDomainService.EvaluateAreaRooms(
            ratioArea,
            scheduleId,
            new Dictionary<Guid, int> { [roomA] = 4, [roomB] = 6 }
        );
        Assert.Equal(roomB, ratioRecId);

        var countArea = config
            .UpsertArea(
                CreateAreaSpec("Count Area", RoomBalancingStrategy.LeastOccupancyCount, rooms)
            )
            .Value;
        var (countRecId, _, _) = RoomBalancingDomainService.EvaluateAreaRooms(
            countArea,
            scheduleId,
            new Dictionary<Guid, int> { [roomA] = 4, [roomB] = 6 }
        );
        Assert.Equal(roomA, countRecId);

        var priorityArea = config
            .UpsertArea(
                CreateAreaSpec("Priority Area", RoomBalancingStrategy.OrderedPriority, rooms)
            )
            .Value;
        var (prioRecId, _, _) = RoomBalancingDomainService.EvaluateAreaRooms(
            priorityArea,
            scheduleId,
            new Dictionary<Guid, int> { [roomA] = 1, [roomB] = 8 }
        );
        Assert.Equal(roomB, prioRecId);

        var (overflowRecId, _, _) = RoomBalancingDomainService.EvaluateAreaRooms(
            ratioArea,
            scheduleId,
            new Dictionary<Guid, int>
            {
                [roomA] = 10,
                [roomB] = 30,
                [overflowRoom] = 2,
            }
        );
        Assert.Equal(overflowRoom, overflowRecId);

        var (softFallbackRecId, _, _) = RoomBalancingDomainService.EvaluateAreaRooms(
            ratioArea,
            scheduleId,
            new Dictionary<Guid, int>
            {
                [roomA] = 12,
                [roomB] = 32,
                [overflowRoom] = 20,
            }
        );
        Assert.Equal(roomB, softFallbackRecId);

        var (fullRecId, _, _) = RoomBalancingDomainService.EvaluateAreaRooms(
            ratioArea,
            scheduleId,
            new Dictionary<Guid, int>
            {
                [roomA] = 12,
                [roomB] = 35,
                [overflowRoom] = 20,
            }
        );
        Assert.Null(fullRecId);
    }

    private static CheckInConfiguration CreateBaseConfiguration()
    {
        return CheckInConfiguration
            .Create(
                new CheckInConfigurationInput(
                    "Balancing Test Config",
                    Description: null,
                    CheckInProfileKind.MemberFacilityAccess,
                    BranchId: null,
                    CheckInSearchMode.PhoneAndName,
                    30,
                    30,
                    2,
                    2,
                    PreventDuplicateCheckIn: true,
                    AllowCheckout: true,
                    60,
                    IsActive: true
                )
            )
            .Value;
    }

    private static CheckInAreaSpec CreateAreaSpec(
        string name,
        RoomBalancingStrategy strategy,
        IReadOnlyList<CheckInRoomMatrixCellInputDto> rooms
    )
    {
        return new CheckInAreaSpec(
            name,
            Description: null,
            LinkedGroupId: null,
            strategy,
            Order: 0,
            IsActive: true,
            PrintBadge: true,
            IssueSecurityPickupCode: false,
            Eligibility: null,
            rooms
        );
    }
}
