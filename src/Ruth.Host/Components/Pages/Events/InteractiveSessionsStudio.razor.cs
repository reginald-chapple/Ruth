using Microsoft.AspNetCore.Components;
using Ruth.Modules.Events.Contracts.InteractiveSessions;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Host.Components.Pages.Events;

/// <summary>Moderator studio and live stage preview for SignalR interactive sessions, polls, and Q&amp;A.</summary>
public sealed partial class InteractiveSessionsStudio : ComponentBase
{
    private List<InteractiveSessionDetailResponse> _sessions = [];
    private InteractiveStageProjectionResponse? _stageProjection;

    private string _sessionTitle = "Annual Strategy Town Hall";
    private string _pollTitle = "Priority Focus Poll";
    private string _pollPrompt = "Which initiative should we prioritize this quarter?";
    private string _pollOptionsCsv = "Youth Programs,Community Aid,Facility Expansion";

    private string? _statusMessage;
    private bool _isError;

    [Inject]
    private IQueryDispatcher QueryDispatcher { get; set; } = default!;

    [Inject]
    private ICommandDispatcher CommandDispatcher { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        await ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        var res = await QueryDispatcher.QueryAsync<
            GetInteractiveSessionsQuery,
            IReadOnlyList<InteractiveSessionDetailResponse>
        >(new GetInteractiveSessionsQuery());
        if (res.IsSuccess)
        {
            _sessions = [.. res.Value];
        }
    }

    private async Task CreateSessionAsync()
    {
        var options = _pollOptionsCsv
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToArray();

        var res = await CommandDispatcher.SendAsync<
            CreateInteractiveSessionCommand,
            InteractiveSessionDetailResponse
        >(
            new CreateInteractiveSessionCommand(
                _sessionTitle,
                InitialActions:
                [
                    new InteractiveActionSpec(
                        _pollTitle,
                        _pollPrompt,
                        InteractiveActionType.MultipleChoicePoll,
                        options
                    ),
                ]
            )
        );
        if (res.IsFailure)
        {
            _isError = true;
            _statusMessage = res.PrimaryError.Description;
            return;
        }

        _isError = false;
        _statusMessage = $"Created session {res.Value.Title} (Join Code: {res.Value.JoinCode}).";
        await ReloadAsync();
    }

    private async Task StartLiveAsync(Guid sessionId)
    {
        var res = await CommandDispatcher.SendAsync<
            StartInteractiveSessionOccurrenceCommand,
            InteractiveSessionDetailResponse
        >(new StartInteractiveSessionOccurrenceCommand(sessionId));
        if (res.IsFailure)
        {
            _isError = true;
            _statusMessage = res.PrimaryError.Description;
            return;
        }

        _isError = false;
        _statusMessage = $"Session {res.Value.JoinCode} is now LIVE.";
        await ReloadAsync();
        await LoadStageAsync(res.Value.JoinCode);
    }

    private async Task EndSessionAsync(Guid sessionId)
    {
        var res = await CommandDispatcher.SendAsync<
            EndInteractiveSessionCommand,
            InteractiveSessionDetailResponse
        >(new EndInteractiveSessionCommand(sessionId));
        if (res.IsFailure)
        {
            _isError = true;
            _statusMessage = res.PrimaryError.Description;
            return;
        }

        _isError = false;
        _statusMessage = "Session ended.";
        await ReloadAsync();
    }

    private async Task LoadStageAsync(string joinCode)
    {
        var res = await QueryDispatcher.QueryAsync<
            GetInteractiveSessionStageQuery,
            InteractiveStageProjectionResponse
        >(new GetInteractiveSessionStageQuery(joinCode));
        if (res.IsSuccess)
        {
            _stageProjection = res.Value;
        }
    }
}
