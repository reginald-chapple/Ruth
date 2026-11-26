using Microsoft.AspNetCore.Components;
using Ruth.Modules.CheckIn.Contracts.Configurations;
using Ruth.Modules.CheckIn.Contracts.Sessions;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Host.Components.Pages.CheckIn;

/// <summary>Tablet-friendly live Room and Facility Manager screen with occupancy thresholds, toggles, and overrides.</summary>
public partial class RoomFacilityManager : ComponentBase
{
    private List<CheckInConfigurationSummaryResponse> _configurations = [];
    private Guid _selectedConfigId;
    private bool _includeCheckedOut;
    private LiveRoomRosterResponse? _roster;

    private LiveRoomParticipantDto? _movingParticipant;
    private Guid _moveSourceAreaId;
    private Guid _moveTargetLocationId;
    private string _moveOverrideReason = "Room manager reassignment";

    private string? _statusMessage;
    private bool _isError;

    [Inject]
    private IQueryDispatcher QueryDispatcher { get; set; } = default!;

    [Inject]
    private ICommandDispatcher CommandDispatcher { get; set; } = default!;

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        var configs = await QueryDispatcher.QueryAsync<
            GetCheckInConfigurationsQuery,
            IReadOnlyList<CheckInConfigurationSummaryResponse>
        >(new GetCheckInConfigurationsQuery(ActiveOnly: true));
        if (configs.IsSuccess && configs.Value.Count > 0)
        {
            _configurations = [.. configs.Value];
            _selectedConfigId = _configurations[0].Id;
            await RefreshRosterAsync();
        }
    }

    private async Task OnConfigurationChangedAsync(Guid configId)
    {
        _selectedConfigId = configId;
        await RefreshRosterAsync();
    }

    private async Task RefreshRosterAsync()
    {
        if (_selectedConfigId == Guid.Empty)
        {
            return;
        }

        var result = await QueryDispatcher.QueryAsync<
            GetLiveRoomRosterQuery,
            LiveRoomRosterResponse
        >(new GetLiveRoomRosterQuery(_selectedConfigId, IncludeCheckedOut: _includeCheckedOut));
        if (result.IsSuccess)
        {
            _roster = result.Value;
            _isError = false;
            return;
        }

        _isError = true;
        _statusMessage = result.PrimaryError.Description;
    }

    private async Task ToggleRoomAvailabilityAsync(LiveRoomSummaryDto room)
    {
        var result = await CommandDispatcher.SendAsync<
            SetRoomCheckInAvailabilityCommand,
            CheckInConfigurationDetailResponse
        >(
            new SetRoomCheckInAvailabilityCommand(
                _selectedConfigId,
                room.AreaId,
                room.LocationId,
                !room.IsOpenForCheckIn,
                room.SoftCapacity,
                room.FirmCapacity
            )
        );
        if (result.IsSuccess)
        {
            _statusMessage =
                $"{(room.IsOpenForCheckIn ? "Closed" : "Opened")} '{room.LocationName}'.";
            _isError = false;
            await RefreshRosterAsync();
            return;
        }

        _isError = true;
        _statusMessage = result.PrimaryError.Description;
    }

    private async Task StaffCheckOutAsync(Guid entryId)
    {
        var result = await CommandDispatcher.SendAsync<
            CheckOutAttendanceCommand,
            CheckOutResultResponse
        >(
            new CheckOutAttendanceCommand(
                _selectedConfigId,
                EntryIds: [entryId],
                IsStaffOverride: true,
                OverrideReason: "Checked out via Room Manager tablet"
            )
        );
        if (result.IsSuccess)
        {
            _statusMessage = "Participant checked out.";
            _isError = false;
            await RefreshRosterAsync();
            return;
        }

        _isError = true;
        _statusMessage = result.PrimaryError.Description;
    }

    private void BeginMoveParticipant(LiveRoomParticipantDto participant, Guid currentAreaId)
    {
        _movingParticipant = participant;
        _moveSourceAreaId = currentAreaId;
        _moveTargetLocationId = _roster is { Rooms.Count: > 0 }
            ? _roster.Rooms[0].LocationId
            : Guid.Empty;
    }

    private void CancelMove()
    {
        _movingParticipant = null;
    }

    private async Task ConfirmMoveParticipantAsync()
    {
        if (_movingParticipant is null || _roster is null)
        {
            return;
        }

        var targetRoom = _roster.Rooms.FirstOrDefault(r => r.LocationId == _moveTargetLocationId);
        var targetAreaId = targetRoom?.AreaId ?? _moveSourceAreaId;

        var result = await CommandDispatcher.SendAsync<
            OverrideCheckInRoomAssignmentCommand,
            CheckedInEntryReceiptDto
        >(
            new OverrideCheckInRoomAssignmentCommand(
                _movingParticipant.SessionId,
                _movingParticipant.EntryId,
                targetAreaId,
                _moveTargetLocationId,
                Reason: _moveOverrideReason,
                AllowCapacityOverride: true
            )
        );
        if (result.IsSuccess)
        {
            _statusMessage = $"Moved {result.Value.PersonFullName} to {result.Value.LocationName}.";
            _isError = false;
            _movingParticipant = null;
            await RefreshRosterAsync();
            return;
        }

        _isError = true;
        _statusMessage = result.PrimaryError.Description;
    }
}
