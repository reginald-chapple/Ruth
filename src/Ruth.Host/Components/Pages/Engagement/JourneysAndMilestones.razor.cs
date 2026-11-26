using Microsoft.AspNetCore.Components;
using Ruth.Modules.Engagement.Contracts.Journeys;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Host.Components.Pages.Engagement;

/// <summary>Staff workspace for W5-S2 JourneyPrograms, MilestoneTypes, and JourneyCompletions.</summary>
public sealed partial class JourneysAndMilestones : ComponentBase
{
    private IReadOnlyList<JourneyProgramResponse> _programs = [];
    private IReadOnlyList<JourneyCompletionResponse> _completions = [];
    private string _programName = "Member Leadership Pathway";
    private string _programSlug = "member-leadership-pathway";
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
        var progRes = await QueryDispatcher.QueryAsync<
            GetJourneyProgramsQuery,
            IReadOnlyList<JourneyProgramResponse>
        >(new GetJourneyProgramsQuery());
        _programs = progRes.IsSuccess ? progRes.Value : [];

        var compRes = await QueryDispatcher.QueryAsync<
            GetJourneyCompletionsQuery,
            IReadOnlyList<JourneyCompletionResponse>
        >(new GetJourneyCompletionsQuery());
        _completions = compRes.IsSuccess ? compRes.Value : [];
    }

    private async Task CreateDefaultProgramAsync()
    {
        var spec = new JourneyProgramSpec(
            _programName,
            _programSlug,
            [
                new MilestoneStatusSpec(
                    "In Progress",
                    "#0F6CBD",
                    IsCompleteStatus: false,
                    Order: 0
                ),
                new MilestoneStatusSpec("Completed", "#107C10", IsCompleteStatus: true, Order: 1),
            ],
            [
                new MilestoneTypeSpec("Orientation Session", "ORIENTATION", 0),
                new MilestoneTypeSpec(
                    "Core Skills Workshop",
                    "SKILLS",
                    1,
                    PrerequisiteCodes: ["ORIENTATION"]
                ),
                new MilestoneTypeSpec("Team Shadowing", "SHADOW", 2, PrerequisiteCodes: ["SKILLS"]),
            ]
        );

        var res = await CommandDispatcher.SendAsync<
            CreateJourneyProgramCommand,
            JourneyProgramResponse
        >(new CreateJourneyProgramCommand(spec));
        _isError = res.IsFailure;
        _statusMessage = res.IsSuccess
            ? $"Created journey program '{res.Value.Name}'."
            : res.PrimaryError.Description;
        await ReloadAsync();
    }
}
