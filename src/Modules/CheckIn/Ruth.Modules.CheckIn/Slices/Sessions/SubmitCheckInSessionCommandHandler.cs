using Ruth.Modules.CheckIn.Contracts.Configurations;
using Ruth.Modules.CheckIn.Contracts.Sessions;
using Ruth.Modules.CheckIn.Domain;
using Ruth.Modules.CheckIn.Domain.Configurations;
using Ruth.Modules.CheckIn.Domain.Sessions;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.CheckIn.Slices.Sessions;

internal sealed class SubmitCheckInSessionCommandHandler(
    CheckInSessionCollaborator collaborator,
    CheckInExternalContextService externalContext,
    IClock clock
) : ICommandHandler<SubmitCheckInSessionCommand, CheckInSessionReceiptResponse>
{
    public async Task<Result<CheckInSessionReceiptResponse>> HandleAsync(
        SubmitCheckInSessionCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var configResult = await LoadActiveConfigurationAsync(command, cancellationToken);
        if (configResult.IsFailure)
        {
            return Result.Failure<CheckInSessionReceiptResponse>(configResult.Errors);
        }

        var nowUtc = clock.UtcNow.ToUniversalTime();
        var serviceDate = command.ServiceDate ?? DateOnly.FromDateTime(nowUtc.UtcDateTime);
        var specsResult = await PrepareEntrySpecsAsync(
            configResult.Value,
            command,
            serviceDate,
            nowUtc,
            cancellationToken
        );
        if (specsResult.IsFailure)
        {
            return Result.Failure<CheckInSessionReceiptResponse>(specsResult.Errors);
        }

        var persisted = await collaborator.PersistWithDailyCodeAsync(
            configResult.Value,
            command,
            serviceDate,
            nowUtc,
            specsResult.Value.Entries,
            specsResult.Value.RequiresPickupCode,
            cancellationToken
        );
        if (persisted.IsFailure)
        {
            return Result.Failure<CheckInSessionReceiptResponse>(persisted.Errors);
        }

        await externalContext.SynchronizeLinkedGroupAttendanceAsync(
            persisted.Value,
            command.CheckedInByPersonId,
            cancellationToken
        );
        return Result.Success(CheckInSessionMapper.ToReceipt(persisted.Value));
    }

    private async Task<Result<CheckInConfiguration>> LoadActiveConfigurationAsync(
        SubmitCheckInSessionCommand command,
        CancellationToken cancellationToken
    )
    {
        if (command.Items is null || command.Items.Count == 0)
        {
            return Result.Failure<CheckInConfiguration>(CheckInErrors.SessionItemsRequired);
        }

        var config = await collaborator.LoadConfigurationAsync(
            command.ConfigurationId,
            cancellationToken
        );
        return config is null
                ? Result.Failure<CheckInConfiguration>(CheckInErrors.ConfigurationNotFound)
            : !config.IsActive
                ? Result.Failure<CheckInConfiguration>(CheckInErrors.ConfigurationInactive)
            : Result.Success(config);
    }

    private async Task<
        Result<(List<CheckInEntrySpec> Entries, bool RequiresPickupCode)>
    > PrepareEntrySpecsAsync(
        CheckInConfiguration config,
        SubmitCheckInSessionCommand command,
        DateOnly serviceDate,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken
    )
    {
        var occupancyBySchedule = new Dictionary<Guid, Dictionary<Guid, int>>();
        var entrySpecs = new List<CheckInEntrySpec>(command.Items.Count);
        var requiresPickupCode = config.ProfileKind == CheckInProfileKind.YouthAndChildcare;

        foreach (var item in command.Items)
        {
            var singleResult = await BuildSingleEntrySpecAsync(
                config,
                command,
                item,
                serviceDate,
                nowUtc,
                occupancyBySchedule,
                cancellationToken
            );
            if (singleResult.IsFailure)
            {
                return Result.Failure<(List<CheckInEntrySpec>, bool)>(singleResult.Errors);
            }

            requiresPickupCode = requiresPickupCode || singleResult.Value.IssuePickupCode;
            entrySpecs.Add(singleResult.Value.Spec);
        }

        return Result.Success((entrySpecs, requiresPickupCode));
    }

    private async Task<
        Result<(CheckInEntrySpec Spec, bool IssuePickupCode)>
    > BuildSingleEntrySpecAsync(
        CheckInConfiguration config,
        SubmitCheckInSessionCommand command,
        CheckInSelectionItemDto item,
        DateOnly serviceDate,
        DateTimeOffset nowUtc,
        Dictionary<Guid, Dictionary<Guid, int>> occupancyBySchedule,
        CancellationToken cancellationToken
    )
    {
        var itemGuard = ValidateItemAndArea(config, command, item);
        if (itemGuard.IsFailure)
        {
            return Result.Failure<(CheckInEntrySpec, bool)>(itemGuard.Errors);
        }

        var (area, scheduleId) = itemGuard.Value;
        var eligibilityCheck = await VerifyDuplicateAndEligibilityAsync(
            config,
            area,
            item,
            serviceDate,
            scheduleId,
            cancellationToken
        );
        if (eligibilityCheck.IsFailure)
        {
            return Result.Failure<(CheckInEntrySpec, bool)>(eligibilityCheck.Errors);
        }

        var occupancyMap = await collaborator.GetOrLoadScheduleOccupancyAsync(
            config.Id,
            serviceDate,
            scheduleId,
            occupancyBySchedule,
            cancellationToken
        );
        var roomResult = SelectTargetRoom(area, scheduleId, item, occupancyMap);
        if (roomResult.IsFailure)
        {
            return Result.Failure<(CheckInEntrySpec, bool)>(roomResult.Errors);
        }

        occupancyMap[roomResult.Value.LocationId] =
            occupancyMap.GetValueOrDefault(roomResult.Value.LocationId) + 1;

        var spec = CreateSpec(
            area,
            command,
            item,
            eligibilityCheck.Value,
            roomResult.Value,
            scheduleId,
            serviceDate,
            nowUtc
        );
        return Result.Success((spec, area.IssueSecurityPickupCode));
    }

    private async Task<Result<string>> VerifyDuplicateAndEligibilityAsync(
        CheckInConfiguration config,
        CheckInArea area,
        CheckInSelectionItemDto item,
        DateOnly serviceDate,
        Guid scheduleId,
        CancellationToken cancellationToken
    )
    {
        if (
            config.PreventDuplicateCheckIn
            && await collaborator.HasActiveCheckInAsync(
                config.Id,
                item.PersonId,
                serviceDate,
                scheduleId,
                cancellationToken
            )
        )
        {
            return Result.Failure<string>(CheckInErrors.DuplicateActiveCheckIn);
        }

        var (personName, participantContext) = await externalContext.ResolveParticipantAsync(
            item,
            cancellationToken
        );
        var (isEligible, _) = CheckInEligibilityEvaluator.Evaluate(area, participantContext);
        return !isEligible && !item.IsEligibilityOverride
            ? Result.Failure<string>(CheckInErrors.ParticipantNotEligible)
            : Result.Success(personName);
    }

    private static CheckInEntrySpec CreateSpec(
        CheckInArea area,
        SubmitCheckInSessionCommand command,
        CheckInSelectionItemDto item,
        string personName,
        (Guid LocationId, string LocationName, bool UsedOverride) room,
        Guid scheduleId,
        DateOnly serviceDate,
        DateTimeOffset nowUtc
    )
    {
        return new CheckInEntrySpec(
            area.Id,
            area.Name,
            area.LinkedGroupId,
            item.PersonId,
            personName,
            room.LocationId,
            room.LocationName,
            scheduleId,
            serviceDate,
            SecurityPickupCode: null,
            area.PrintBadge,
            room.UsedOverride,
            item.IsEligibilityOverride,
            command.CheckedInByPersonId,
            item.OverrideReason,
            item.AttendanceNote,
            nowUtc
        );
    }

    private static Result<(CheckInArea Area, Guid ScheduleId)> ValidateItemAndArea(
        CheckInConfiguration config,
        SubmitCheckInSessionCommand command,
        CheckInSelectionItemDto item
    )
    {
        if (item.PersonId == Guid.Empty)
        {
            return Result.Failure<(CheckInArea, Guid)>(CheckInErrors.PersonRequired);
        }

        var scheduleId = item.ScheduleId is { } sid && sid != Guid.Empty ? sid : command.ScheduleId;
        if (scheduleId == Guid.Empty)
        {
            return Result.Failure<(CheckInArea, Guid)>(CheckInErrors.ScheduleRequired);
        }

        var areaId = CheckInAreaId.From(item.AreaId);
        var area = config.Areas.FirstOrDefault(a => a.Id == areaId);
        if (area is null)
        {
            return Result.Failure<(CheckInArea, Guid)>(CheckInErrors.AreaNotFound);
        }

        var invalidOverride =
            (item.IsCapacityOverride || item.IsEligibilityOverride)
            && (
                command.Channel != CheckInSessionChannel.StaffOverride
                || string.IsNullOrWhiteSpace(item.OverrideReason)
            );
        return invalidOverride
            ? Result.Failure<(CheckInArea, Guid)>(CheckInErrors.OverrideRequiresStaffAndReason)
            : Result.Success((area, scheduleId));
    }

    private static Result<(
        Guid LocationId,
        string LocationName,
        bool UsedOverride
    )> SelectTargetRoom(
        CheckInArea area,
        Guid scheduleId,
        CheckInSelectionItemDto item,
        IReadOnlyDictionary<Guid, int> occupancyMap
    )
    {
        var (recommendedId, recommendedName, candidateRooms) =
            RoomBalancingDomainService.EvaluateAreaRooms(area, scheduleId, occupancyMap);

        if (item.LocationId is { } explicitId && explicitId != Guid.Empty)
        {
            var matched = candidateRooms.FirstOrDefault(r => r.LocationId == explicitId);
            if (matched is null)
            {
                return Result.Failure<(Guid, string, bool)>(CheckInErrors.RoomNotFound);
            }

            var blocked =
                matched.ThresholdStatus
                is RoomThresholdStatus.Closed
                    or RoomThresholdStatus.AtFirmCapacity;
            return blocked && !item.IsCapacityOverride
                ? Result.Failure<(Guid, string, bool)>(CheckInErrors.NoAvailableRoomForCheckIn)
                : Result.Success((matched.LocationId, matched.LocationName, blocked));
        }

        if (recommendedId.HasValue && !string.IsNullOrWhiteSpace(recommendedName))
        {
            return Result.Success((recommendedId.Value, recommendedName, false));
        }

        if (item.IsCapacityOverride && area.Rooms.Count > 0)
        {
            var fallback = area.Rooms.OrderBy(static r => r.PriorityOrder).First();
            return Result.Success((fallback.LocationId, fallback.LocationName, true));
        }

        return Result.Failure<(Guid, string, bool)>(CheckInErrors.NoAvailableRoomForCheckIn);
    }
}
