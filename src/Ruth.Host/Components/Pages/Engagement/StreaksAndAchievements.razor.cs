using Microsoft.AspNetCore.Components;
using Ruth.Modules.Engagement.Contracts.Achievements;
using Ruth.Modules.Engagement.Contracts.Streaks;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Host.Components.Pages.Engagement;

/// <summary>Staff workspace for W5-S2 Span&lt;byte&gt; Streaks and Achievements.</summary>
public sealed partial class StreaksAndAchievements : ComponentBase
{
    private static readonly StreakCadence[] _cadences = [StreakCadence.Weekly, StreakCadence.Daily];

    private IReadOnlyList<StreakTypeResponse> _streakTypes = [];
    private IReadOnlyList<StreakResponse> _streaks = [];
    private IReadOnlyList<AchievementTypeResponse> _achievementTypes = [];
    private IReadOnlyList<AchievementAttemptResponse> _attempts = [];

    private string _streakName = "Weekly Community Participation";
    private string _streakCode = "WEEKLY_PARTICIPATION";
    private StreakCadence _cadence = StreakCadence.Weekly;
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
        var stRes = await QueryDispatcher.QueryAsync<
            GetStreakTypesQuery,
            IReadOnlyList<StreakTypeResponse>
        >(new GetStreakTypesQuery());
        _streakTypes = stRes.IsSuccess ? stRes.Value : [];

        var sRes = await QueryDispatcher.QueryAsync<GetStreaksQuery, IReadOnlyList<StreakResponse>>(
            new GetStreaksQuery()
        );
        _streaks = sRes.IsSuccess ? sRes.Value : [];

        var atRes = await QueryDispatcher.QueryAsync<
            GetAchievementTypesQuery,
            IReadOnlyList<AchievementTypeResponse>
        >(new GetAchievementTypesQuery());
        _achievementTypes = atRes.IsSuccess ? atRes.Value : [];

        var attRes = await QueryDispatcher.QueryAsync<
            GetAchievementAttemptsQuery,
            IReadOnlyList<AchievementAttemptResponse>
        >(new GetAchievementAttemptsQuery());
        _attempts = attRes.IsSuccess ? attRes.Value : [];
    }

    private async Task CreateStreakAndBadgeAsync()
    {
        var anchor = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-90);
        var stRes = await CommandDispatcher.SendAsync<CreateStreakTypeCommand, StreakTypeResponse>(
            new CreateStreakTypeCommand(
                new StreakTypeSpec(_streakName, _streakCode, _cadence, anchor)
            )
        );
        if (stRes.IsFailure)
        {
            _isError = true;
            _statusMessage = stRes.PrimaryError.Description;
            return;
        }

        _ = await CommandDispatcher.SendAsync<
            CreateAchievementTypeCommand,
            AchievementTypeResponse
        >(
            new CreateAchievementTypeCommand(
                new AchievementTypeSpec(
                    $"{_streakName} — 4 Streak Milestone",
                    $"{_streakCode}_4",
                    AchievementSourceKind.StreakCount,
                    4,
                    TargetStreakTypeId: stRes.Value.Id
                )
            )
        );

        await ReloadAsync();
        _isError = false;
        _statusMessage = $"Created streak '{stRes.Value.Name}' and linked 4-streak badge.";
    }
}
