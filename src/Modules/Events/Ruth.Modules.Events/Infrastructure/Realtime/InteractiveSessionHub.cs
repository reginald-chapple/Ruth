using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Ruth.Modules.Events.Contracts.InteractiveSessions;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Events.Infrastructure.Realtime;

/// <summary>SignalR hub for real-time town-hall, classroom, and conference interactive sessions, polls, Q&amp;A, and stage displays.</summary>
[AllowAnonymous]
public sealed class InteractiveSessionHub(
    IQueryDispatcher queryDispatcher,
    ICommandDispatcher commandDispatcher
) : Hub
{
    public const string StageProjectionUpdatedMethod = "StageProjectionUpdated";

    public static string GetSessionGroupName(Guid sessionId)
    {
        return $"interactive-session:{sessionId:D}";
    }

    public static string GetJoinCodeGroupName(string joinCode)
    {
        return $"interactive-code:{joinCode.Trim().ToUpperInvariant()}";
    }

    public async Task<InteractiveStageProjectionResponse?> JoinSession(string sessionIdOrJoinCode)
    {
        if (string.IsNullOrWhiteSpace(sessionIdOrJoinCode))
        {
            return null;
        }

        var result = await queryDispatcher.QueryAsync<
            GetInteractiveSessionStageQuery,
            InteractiveStageProjectionResponse
        >(new GetInteractiveSessionStageQuery(sessionIdOrJoinCode), Context.ConnectionAborted);
        if (result.IsFailure)
        {
            return null;
        }

        await Groups.AddToGroupAsync(
            Context.ConnectionId,
            GetSessionGroupName(result.Value.SessionId),
            Context.ConnectionAborted
        );
        await Groups.AddToGroupAsync(
            Context.ConnectionId,
            GetJoinCodeGroupName(result.Value.JoinCode),
            Context.ConnectionAborted
        );
        return result.Value;
    }

    public async Task LeaveSession(Guid sessionId, string? joinCode = null)
    {
        await Groups.RemoveFromGroupAsync(
            Context.ConnectionId,
            GetSessionGroupName(sessionId),
            Context.ConnectionAborted
        );
        if (!string.IsNullOrWhiteSpace(joinCode))
        {
            await Groups.RemoveFromGroupAsync(
                Context.ConnectionId,
                GetJoinCodeGroupName(joinCode),
                Context.ConnectionAborted
            );
        }
    }

    public async Task<InteractiveStageProjectionResponse?> SubmitAudienceResponse(
        SubmitInteractiveResponseCommand command
    )
    {
        ArgumentNullException.ThrowIfNull(command);
        var result = await commandDispatcher.SendAsync<
            SubmitInteractiveResponseCommand,
            InteractiveStageProjectionResponse
        >(command, Context.ConnectionAborted);
        return result.IsSuccess ? result.Value : null;
    }
}
