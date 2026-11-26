using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Ruth.Modules.VirtualRooms.Contracts.Rooms;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Host.Components.Pages.VirtualRooms;

/// <summary>Embedded LiveKit WebComponent Blazor island page minting short-lived ACL room JWTs.</summary>
public sealed partial class VirtualRoomStagePage : ComponentBase, IAsyncDisposable
{
    private readonly string _stageElementId = $"stage-{Guid.NewGuid():N}";
    private IJSObjectReference? _jsModule;
    private VirtualRoomJoinSessionResponse? _joinSession;

    private string _targetRoomSlug = "community-cohort-room";
    private string _displayName = "Coordinator Host";
    private string? _statusMessage;
    private bool _isError;

    [Parameter]
    public string? RoomSlug { get; set; }

    [Inject]
    private ICommandDispatcher CommandDispatcher { get; set; } = default!;

    [Inject]
    private IJSRuntime JsRuntime { get; set; } = default!;

    protected override void OnParametersSet()
    {
        if (!string.IsNullOrWhiteSpace(RoomSlug))
        {
            _targetRoomSlug = RoomSlug;
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _jsModule = await JsRuntime.InvokeAsync<IJSObjectReference>(
                "import",
                "/ruth-livekit-room.js"
            );
            _ = await _jsModule.InvokeAsync<object>("ensureLiveKitWebComponentRegistered");
        }
    }

    private async Task ConnectStageAsync()
    {
        var res = await CommandDispatcher.SendAsync<
            MintVirtualRoomJoinTokenCommand,
            VirtualRoomJoinSessionResponse
        >(new MintVirtualRoomJoinTokenCommand(_targetRoomSlug, _displayName, TtlMinutes: 30));
        if (res.IsFailure)
        {
            _isError = true;
            _statusMessage = res.PrimaryError.Description;
            return;
        }

        _joinSession = res.Value;
        _isError = false;
        _statusMessage = string.Create(
            CultureInfo.InvariantCulture,
            $"Joined '{res.Value.RoomDisplayName}' as {res.Value.GrantedRole} (JWT expires {res.Value.ExpiresAtUtc:HH:mm:ss} UTC)."
        );

        if (_jsModule is not null)
        {
            _ = await _jsModule.InvokeAsync<object>(
                "connectLiveKitStage",
                _stageElementId,
                res.Value.WebSocketUrl,
                res.Value.AccessToken,
                _displayName,
                res.Value.GrantedRole.ToString()
            );
        }
    }

    private async Task ToggleTrackAsync(string trackKind)
    {
        if (_jsModule is not null)
        {
            _ = await _jsModule.InvokeAsync<object>("toggleStageTrack", _stageElementId, trackKind);
        }
    }

    private async Task StartRecordingAsync()
    {
        if (_joinSession is null)
        {
            return;
        }

        var res = await CommandDispatcher.SendAsync<
            StartVirtualRoomRecordingCommand,
            VirtualRoomRecordingArchiveResponse
        >(new StartVirtualRoomRecordingCommand(_joinSession.SessionId));
        _isError = res.IsFailure;
        _statusMessage = res.IsSuccess
            ? $"Started RustFS egress {res.Value.EgressId} -> {res.Value.StorageObjectKey}."
            : res.PrimaryError.Description;
    }

    private async Task DisconnectStageAsync()
    {
        if (_jsModule is not null)
        {
            _ = await _jsModule.InvokeAsync<object>("disconnectLiveKitStage", _stageElementId);
        }

        _joinSession = null;
        _statusMessage = "Disconnected from LiveKit stage.";
    }

    public async ValueTask DisposeAsync()
    {
        if (_jsModule is not null)
        {
            try
            {
                _ = await _jsModule.InvokeAsync<object>("disconnectLiveKitStage", _stageElementId);
                await _jsModule.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
                // Circuit already disconnected.
            }
        }
    }
}
