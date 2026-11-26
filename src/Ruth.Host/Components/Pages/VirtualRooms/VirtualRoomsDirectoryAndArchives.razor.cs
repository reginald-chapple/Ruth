using System.Globalization;
using Microsoft.AspNetCore.Components;
using Ruth.Modules.VirtualRooms.Contracts.Rooms;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Time;

namespace Ruth.Host.Components.Pages.VirtualRooms;

/// <summary>Staff W5-S7 VirtualRooms directory, hybrid session dwell monitor, and RustFS recording archive browser.</summary>
public sealed partial class VirtualRoomsDirectoryAndArchives : ComponentBase
{
    private IReadOnlyList<VirtualRoomResponse> _rooms = [];
    private IReadOnlyList<VirtualRoomSessionResponse> _sessions = [];

    private string _roomName = "Community Cohort Hybrid Room";
    private string _roomSlug = "community-cohort-room";
    private string _dwellSecondsInput = "180";

    private string? _statusMessage;
    private bool _isError;

    [Inject]
    private IQueryDispatcher QueryDispatcher { get; set; } = default!;

    [Inject]
    private ICommandDispatcher CommandDispatcher { get; set; } = default!;

    [Inject]
    private IClock Clock { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        await ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        var roomsRes = await QueryDispatcher.QueryAsync<
            GetVirtualRoomsQuery,
            IReadOnlyList<VirtualRoomResponse>
        >(new GetVirtualRoomsQuery(IncludeInactive: true));
        _rooms = roomsRes.IsSuccess ? roomsRes.Value : [];

        var sessRes = await QueryDispatcher.QueryAsync<
            GetVirtualRoomSessionsQuery,
            IReadOnlyList<VirtualRoomSessionResponse>
        >(new GetVirtualRoomSessionsQuery());
        _sessions = sessRes.IsSuccess ? sessRes.Value : [];
    }

    private async Task CreateRoomAsync()
    {
        if (
            !int.TryParse(_dwellSecondsInput, CultureInfo.InvariantCulture, out var dwell)
            || dwell < 10
        )
        {
            dwell = 180;
        }

        var res = await CommandDispatcher.SendAsync<CreateVirtualRoomCommand, VirtualRoomResponse>(
            new CreateVirtualRoomCommand(
                new VirtualRoomSpec(
                    _roomName,
                    _roomSlug,
                    VirtualRoomContextKind.GroupMeeting,
                    MinimumAttendanceDwellSeconds: dwell
                )
            )
        );
        _isError = res.IsFailure;
        _statusMessage = res.IsSuccess
            ? $"Created VirtualRoom '{res.Value.Name}' ({res.Value.LiveKitRoomName})."
            : res.PrimaryError.Description;
        await ReloadAsync();
    }

    private async Task StartSessionNowAsync(Guid roomId, string roomName)
    {
        var now = Clock.UtcNow;
        var res = await CommandDispatcher.SendAsync<
            CreateVirtualRoomSessionCommand,
            VirtualRoomSessionResponse
        >(
            new CreateVirtualRoomSessionCommand(
                roomId,
                $"{roomName} Live Session",
                now,
                now.AddHours(1),
                StartLiveNow: true
            )
        );
        _isError = res.IsFailure;
        _statusMessage = res.IsSuccess
            ? $"Started session '{res.Value.Title}' ({res.Value.Status})."
            : res.PrimaryError.Description;
        await ReloadAsync();
    }
}
