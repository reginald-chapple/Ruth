using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Security;

namespace Ruth.Modules.VirtualRooms.Contracts.Rooms;

/// <summary>Evaluates Ruth ACL permissions on a VirtualRoom, ensures a Live session exists, and mints a short-lived HMAC-SHA256 LiveKit room JWT.</summary>
/// <param name="RoomIdOrSlug">Target VirtualRoom UUID or slug.</param>
/// <param name="DisplayName">Participant display name.</param>
/// <param name="PersonId">Optional CRM Person identifier.</param>
/// <param name="Subject">Caller SecuritySubject used for ACL View/Edit/Administrate evaluation.</param>
/// <param name="RequestPresenterRole">Optional presenter role hint when authorized.</param>
/// <param name="TtlMinutes">Token validity minutes (clamped 1..120).</param>
public sealed record MintVirtualRoomJoinTokenCommand(
    string RoomIdOrSlug,
    string DisplayName,
    Guid? PersonId = null,
    SecuritySubject? Subject = null,
    bool RequestPresenterRole = false,
    int TtlMinutes = 30
) : ICommand<VirtualRoomJoinSessionResponse>;
