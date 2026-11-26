using System.Globalization;
using Microsoft.AspNetCore.Components;
using Ruth.Modules.Engagement.Contracts.Pipelines;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Host.Components.Pages.Engagement;

/// <summary>Staff Kanban and Grid Coordinator Board for W5-S1 Engagement Pipelines and Opportunities.</summary>
public sealed partial class ConnectorBoardAndPipelines : ComponentBase
{
    private IReadOnlyList<PipelineTypeResponse> _pipelines = [];
    private ConnectorBoardResponse? _board;
    private Guid _selectedPipelineId;
    private bool _showGrid;

    private string _pipelineName = "Volunteer & Member Onboarding";
    private string _pipelineSlug = "volunteer-member-onboarding";
    private string _idleDaysInput = "7";

    private string _opportunityName = "Community Mentor Intake";
    private string _opportunitySlug = "community-mentor-intake";
    private string _opportunitySummary = "Join our mentorship & onboarding cohort.";

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

    private void ToggleViewMode()
    {
        _showGrid = !_showGrid;
    }

    private async Task OnPipelineChangedAsync(Guid pipelineId)
    {
        _selectedPipelineId = pipelineId;
        await LoadBoardAsync();
    }

    private async Task ReloadAsync()
    {
        var res = await QueryDispatcher.QueryAsync<
            GetPipelineTypesQuery,
            IReadOnlyList<PipelineTypeResponse>
        >(new GetPipelineTypesQuery());
        _pipelines = res.IsSuccess ? res.Value : [];
        if (_selectedPipelineId == Guid.Empty && _pipelines.Count > 0)
        {
            _selectedPipelineId = _pipelines[0].Id;
        }

        await LoadBoardAsync();
    }

    private async Task LoadBoardAsync()
    {
        if (_selectedPipelineId == Guid.Empty)
        {
            _board = null;
            return;
        }

        var boardRes = await QueryDispatcher.QueryAsync<
            GetConnectorBoardQuery,
            ConnectorBoardResponse
        >(new GetConnectorBoardQuery(_selectedPipelineId, IncludeClosed: true));
        _board = boardRes.IsSuccess ? boardRes.Value : null;
    }

    private async Task CreatePipelineAsync()
    {
        if (
            !int.TryParse(_idleDaysInput, CultureInfo.InvariantCulture, out var idleDays)
            || idleDays < 1
        )
        {
            idleDays = 7;
        }

        var spec = new PipelineTypeSpec(
            _pipelineName,
            _pipelineSlug,
            [
                new PipelineStageSpec("New Inquiry", 0, IsInitial: true),
                new PipelineStageSpec("Coordinator Intro", 1),
                new PipelineStageSpec("Orientation Scheduled", 2),
                new PipelineStageSpec("Connected", 3, IsTerminalCompleted: true),
            ],
            DaysUntilRequestIdle: idleDays,
            ActivityTypes:
            [
                new PipelineActivityTypeSpec("Phone Call", 0),
                new PipelineActivityTypeSpec("Intro Meeting", 1),
            ]
        );

        var res = await CommandDispatcher.SendAsync<
            CreatePipelineTypeCommand,
            PipelineTypeResponse
        >(new CreatePipelineTypeCommand(spec));
        if (res.IsFailure)
        {
            _isError = true;
            _statusMessage = res.PrimaryError.Description;
            return;
        }

        _selectedPipelineId = res.Value.Id;
        await ReloadAsync();
        _isError = false;
        _statusMessage = $"Created pipeline '{res.Value.Name}'.";
    }

    private async Task CreateOpportunityAsync()
    {
        if (_selectedPipelineId == Guid.Empty)
        {
            return;
        }

        var res = await CommandDispatcher.SendAsync<
            CreateEngagementOpportunityCommand,
            EngagementOpportunityResponse
        >(
            new CreateEngagementOpportunityCommand(
                new EngagementOpportunitySpec(
                    _selectedPipelineId,
                    _opportunityName,
                    _opportunitySlug,
                    _opportunitySummary
                )
            )
        );
        _isError = res.IsFailure;
        _statusMessage = res.IsSuccess
            ? $"Created opportunity '{res.Value.Name}'."
            : res.PrimaryError.Description;
        await LoadBoardAsync();
    }

    private async Task LogTouchAsync(Guid requestId)
    {
        var res = await CommandDispatcher.SendAsync<
            RecordEngagementActivityCommand,
            EngagementRequestResponse
        >(
            new RecordEngagementActivityCommand(
                requestId,
                ActivityTypeId: null,
                "Phone Call",
                Note: "Coordinator follow-up call"
            )
        );
        _isError = res.IsFailure;
        _statusMessage = res.IsSuccess ? "Logged coordinator call." : res.PrimaryError.Description;
        await LoadBoardAsync();
    }

    private async Task ConnectAsync(Guid requestId)
    {
        var res = await CommandDispatcher.SendAsync<
            ConnectEngagementRequestCommand,
            EngagementRequestResponse
        >(
            new ConnectEngagementRequestCommand(
                requestId,
                Note: "Completed onboarding via Connector Board"
            )
        );
        _isError = res.IsFailure;
        _statusMessage = res.IsSuccess ? "Marked request Connected." : res.PrimaryError.Description;
        await LoadBoardAsync();
    }
}
