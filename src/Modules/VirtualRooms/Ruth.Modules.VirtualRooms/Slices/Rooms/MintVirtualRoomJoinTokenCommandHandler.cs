using Microsoft.EntityFrameworkCore;
using Ruth.Modules.VirtualRooms.Contracts.Providers;
using Ruth.Modules.VirtualRooms.Contracts.Rooms;
using Ruth.Modules.VirtualRooms.Domain;
using Ruth.Modules.VirtualRooms.Domain.Rooms;
using Ruth.Modules.VirtualRooms.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.VirtualRooms.Slices.Rooms;

internal sealed class MintVirtualRoomJoinTokenCommandHandler(
    VirtualRoomsDbContext dbContext,
    VirtualRoomProviderSelector providerSelector,
    IEntityAuthorizationService authorizationService,
    IClock clock
) : ICommandHandler<MintVirtualRoomJoinTokenCommand, VirtualRoomJoinSessionResponse>
{
    public async Task<Result<VirtualRoomJoinSessionResponse>> HandleAsync(
        MintVirtualRoomJoinTokenCommand command,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(command.DisplayName))
        {
            return Result.Failure<VirtualRoomJoinSessionResponse>(
                VirtualRoomsErrors.JoinDisplayNameRequired
            );
        }

        var room = await ResolveRoomAsync(command.RoomIdOrSlug, cancellationToken);
        if (room is null)
        {
            return Result.Failure<VirtualRoomJoinSessionResponse>(VirtualRoomsErrors.RoomNotFound);
        }

        if (!room.IsActive)
        {
            return Result.Failure<VirtualRoomJoinSessionResponse>(VirtualRoomsErrors.RoomInactive);
        }

        var roleRes = await EvaluateRoleAsync(room, command, cancellationToken);
        if (roleRes.IsFailure)
        {
            return Result.Failure<VirtualRoomJoinSessionResponse>(roleRes.PrimaryError);
        }

        var sessionRes = await EnsureLiveSessionAsync(room, cancellationToken);
        return sessionRes.IsFailure
            ? Result.Failure<VirtualRoomJoinSessionResponse>(sessionRes.PrimaryError)
            : IssueJoinResponse(room, sessionRes.Value, command, roleRes.Value);
    }

    private Result<VirtualRoomJoinSessionResponse> IssueJoinResponse(
        VirtualRoom room,
        VirtualRoomSession session,
        MintVirtualRoomJoinTokenCommand command,
        VirtualParticipantRole role
    )
    {
        var identity = command.PersonId.HasValue
            ? $"person:{command.PersonId.Value:D}"
            : $"guest:{Guid.CreateVersion7(clock.UtcNow):N}";
        var ttl = TimeSpan.FromMinutes(Math.Clamp(command.TtlMinutes, 1, 120));

        var minted = providerSelector
            .ResolveActive()
            .MintAccessToken(
                new VirtualRoomTokenSpec(
                    room.LiveKitRoomName,
                    identity,
                    command.DisplayName.Trim(),
                    role,
                    ttl
                ),
                clock.UtcNow
            );

        return minted.IsFailure
            ? Result.Failure<VirtualRoomJoinSessionResponse>(minted.PrimaryError)
            : Result.Success(
                new VirtualRoomJoinSessionResponse(
                    room.Id.Value,
                    session.Id.Value,
                    room.LiveKitRoomName,
                    room.Name,
                    identity,
                    role,
                    minted.Value.WebSocketUrl,
                    minted.Value.AccessToken,
                    minted.Value.ExpiresAtUtc
                )
            );
    }

    private async Task<VirtualRoom?> ResolveRoomAsync(string? idOrSlug, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(idOrSlug))
        {
            return null;
        }

        var slugCandidate = idOrSlug.Trim().ToLowerInvariant();
        return Guid.TryParse(idOrSlug, out var parsed)
            ? await dbContext.VirtualRooms.FirstOrDefaultAsync(
                r => r.Id == VirtualRoomId.From(parsed),
                ct
            )
            : await dbContext.VirtualRooms.FirstOrDefaultAsync(r => r.Slug == slugCandidate, ct);
    }

    private async Task<Result<VirtualParticipantRole>> EvaluateRoleAsync(
        VirtualRoom room,
        MintVirtualRoomJoinTokenCommand command,
        CancellationToken ct
    )
    {
        if (
            command.PersonId.HasValue
            && room.HostModeratorPersonId.HasValue
            && command.PersonId.Value == room.HostModeratorPersonId.Value
        )
        {
            return Result.Success(VirtualParticipantRole.HostModerator);
        }

        if (command.Subject is null)
        {
            return Result.Success(ResolveStandardRole(room, command.RequestPresenterRole));
        }

        var subject = command.Subject;
        if (
            await authorizationService.IsAuthorizedAsync(
                room,
                AuthorizationActions.Administrate,
                subject,
                ct
            )
            || await authorizationService.IsAuthorizedAsync(
                room,
                AuthorizationActions.Edit,
                subject,
                ct
            )
        )
        {
            return Result.Success(VirtualParticipantRole.HostModerator);
        }

        var canView = await authorizationService.IsAuthorizedAsync(
            room,
            AuthorizationActions.View,
            subject,
            ct
        );
        return !canView
            ? Result.Failure<VirtualParticipantRole>(VirtualRoomsErrors.JoinAccessForbidden)
            : Result.Success(ResolveStandardRole(room, command.RequestPresenterRole));
    }

    private static VirtualParticipantRole ResolveStandardRole(
        VirtualRoom room,
        bool requestPresenter
    )
    {
        return room.IsWebinarBroadcast ? VirtualParticipantRole.Observer
            : requestPresenter ? VirtualParticipantRole.Presenter
            : VirtualParticipantRole.Participant;
    }

    private async Task<Result<VirtualRoomSession>> EnsureLiveSessionAsync(
        VirtualRoom room,
        CancellationToken ct
    )
    {
        var open = await dbContext
            .VirtualRoomSessions.Where(s =>
                s.VirtualRoomId == room.Id
                && (
                    s.Status == VirtualRoomSessionStatus.Live
                    || s.Status == VirtualRoomSessionStatus.Scheduled
                )
            )
            .OrderBy(static s => s.ScheduledStartUtc)
            .FirstOrDefaultAsync(ct);

        var now = clock.UtcNow;
        if (open is null)
        {
            open = VirtualRoomSession
                .Create(room, $"{room.Name} Live Session", now, now.AddHours(2))
                .Value;
            _ = dbContext.VirtualRoomSessions.Add(open);
        }

        if (open.Status != VirtualRoomSessionStatus.Live)
        {
            var prov = await providerSelector
                .ResolveActive()
                .EnsureRoomAsync(
                    new VirtualRoomProvisionSpec(
                        room.LiveKitRoomName,
                        room.MaxParticipants,
                        room.EmptyTimeoutSeconds
                    ),
                    ct
                );
            if (prov.IsFailure)
            {
                return Result.Failure<VirtualRoomSession>(prov.PrimaryError);
            }

            _ = open.MarkLive(prov.Value.LiveKitRoomSid, now);
            _ = await dbContext.SaveChangesAsync(ct);
        }

        return Result.Success(open);
    }
}
