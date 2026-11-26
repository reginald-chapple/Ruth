using System.Globalization;
using Microsoft.AspNetCore.Components;
using Ruth.Modules.Lms.Contracts.Classes;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Host.Components.Pages.Lms;

public sealed partial class LmsFacilitatorGradingWorkbench : ComponentBase
{
    private IReadOnlyList<LearningActivityCompletionResponse> _queue = [];
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
            GetFacilitatorGradingQueueQuery,
            IReadOnlyList<LearningActivityCompletionResponse>
        >(new GetFacilitatorGradingQueueQuery());
        if (res.IsSuccess)
        {
            _queue = res.Value;
        }
    }

    private async Task AwardFullCreditAsync(LearningActivityCompletionResponse item)
    {
        var res = await CommandDispatcher.SendAsync<
            GradeLearningActivityCommand,
            LearningActivityCompletionResponse
        >(
            new GradeLearningActivityCommand(
                item.Id,
                item.PointsPossible,
                Guid.CreateVersion7(),
                "Approved by facilitator"
            )
        );
        _isError = res.IsFailure;
        _statusMessage = res.IsSuccess
            ? string.Create(
                CultureInfo.InvariantCulture,
                $"Graded '{item.ActivityName}' ({item.PointsPossible}/{item.PointsPossible})."
            )
            : res.PrimaryError.Description;
        if (res.IsSuccess)
        {
            await ReloadAsync();
        }
    }
}
