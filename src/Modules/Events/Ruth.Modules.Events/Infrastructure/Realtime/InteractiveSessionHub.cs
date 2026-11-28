using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Ruth.Modules.Events.Contracts.InteractiveSessions;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Tenancy;

namespace Ruth.Modules.Events.Infrastructure.Realtime;

/// <summary>SignalR hub for real-time town-hall, classroom, and conference interactive sessions, polls, Q&amp;A, and stage displays.</summary>
[AllowAnonymous]
public sealed class InteractiveSessionHub(
    IQueryDispatcher queryDispatcher,
    ICommandDispatcher commandDispatcher
) : Hub
{
    private const string CapturedTenantStateItemKey = "Events.Hub.TenantExecutionState";

    public const string StageProjectionUpdatedMethod = "StageProjectionUpdated";

    public static string GetSessionGroupName(TenantId tenantId, Guid sessionId)
    {
        return tenantId.IsEmpty
            ? throw new TenantContextRequiredException(
                "Interactive session SignalR group requires an active tenant context."
            )
            : $"t:{tenantId}:interactive-session:{sessionId:D}";
    }

    public static string GetJoinCodeGroupName(TenantId tenantId, string joinCode)
    {
        return tenantId.IsEmpty
            ? throw new TenantContextRequiredException(
                "Interactive session SignalR group requires an active tenant context."
            )
            : $"t:{tenantId}:interactive-code:{joinCode.Trim().ToUpperInvariant()}";
    }

    public override Task OnConnectedAsync()
    {
        var current = AmbientTenantContext.CurrentState;
        if (current != TenantExecutionState.Unresolved)
        {
            Context.Items[CapturedTenantStateItemKey] = current;
        }

        return base.OnConnectedAsync();
    }

    public async Task<InteractiveStageProjectionResponse?> JoinSession(string sessionIdOrJoinCode)
    {
        if (string.IsNullOrWhiteSpace(sessionIdOrJoinCode))
        {
            return null;
        }

        using var scope = RestoreConnectionTenantScope();
        var tenantId = AmbientTenantContext.CurrentTenantId;
        if (tenantId.IsEmpty)
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
            GetSessionGroupName(tenantId, result.Value.SessionId),
            Context.ConnectionAborted
        );
        await Groups.AddToGroupAsync(
            Context.ConnectionId,
            GetJoinCodeGroupName(tenantId, result.Value.JoinCode),
            Context.ConnectionAborted
        );
        return result.Value;
    }

    public async Task LeaveSession(Guid sessionId, string? joinCode = null)
    {
        using var scope = RestoreConnectionTenantScope();
        var tenantId = AmbientTenantContext.CurrentTenantId;
        if (tenantId.IsEmpty)
        {
            return;
        }

        await Groups.RemoveFromGroupAsync(
            Context.ConnectionId,
            GetSessionGroupName(tenantId, sessionId),
            Context.ConnectionAborted
        );
        if (!string.IsNullOrWhiteSpace(joinCode))
        {
            await Groups.RemoveFromGroupAsync(
                Context.ConnectionId,
                GetJoinCodeGroupName(tenantId, joinCode),
                Context.ConnectionAborted
            );
        }
    }

    public async Task<InteractiveStageProjectionResponse?> SubmitAudienceResponse(
        SubmitInteractiveResponseCommand command
    )
    {
        ArgumentNullException.ThrowIfNull(command);
        using var scope = RestoreConnectionTenantScope();
        if (AmbientTenantContext.CurrentTenantId.IsEmpty)
        {
            return null;
        }

        var result = await commandDispatcher.SendAsync<
            SubmitInteractiveResponseCommand,
            InteractiveStageProjectionResponse
        >(command, Context.ConnectionAborted);
        return result.IsSuccess ? result.Value : null;
    }

    private AmbientTenantContext.TenantScope? RestoreConnectionTenantScope()
    {
        return
            AmbientTenantContext.CurrentState == TenantExecutionState.Unresolved
            && Context.Items.TryGetValue(CapturedTenantStateItemKey, out var raw)
            && raw is TenantExecutionState state
            && !state.TenantId.IsEmpty
            ? AmbientTenantContext.BeginScope(
                state.TenantId,
                state.TenantSlug,
                state.LifecycleStage
            )
            : null;
    }
}
