using Microsoft.AspNetCore.Components;
using Ruth.Modules.Workflow.Contracts.Forms;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Host.Components.Pages.Workflow;

/// <summary>Staff W5-S6 MyTasks inbox and live conditional WorkflowEntry form workspace.</summary>
public sealed partial class WorkflowMyTasksAndEntry : ComponentBase
{
    private IReadOnlyList<WorkflowTaskItemResponse> _tasks = [];
    private WorkflowEntryFormViewResponse? _activeForm;
    private readonly Dictionary<string, string?> _draftValues = new(
        StringComparer.OrdinalIgnoreCase
    );

    private string? _statusMessage;
    private bool _isError;

    [Inject]
    private IQueryDispatcher QueryDispatcher { get; set; } = default!;

    [Inject]
    private ICommandDispatcher CommandDispatcher { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        await ReloadTasksAsync();
    }

    private async Task ReloadTasksAsync()
    {
        var res = await QueryDispatcher.QueryAsync<
            GetMyWorkflowTasksQuery,
            IReadOnlyList<WorkflowTaskItemResponse>
        >(new GetMyWorkflowTasksQuery(IncludeUnassigned: true));
        _tasks = res.IsSuccess ? res.Value : [];
    }

    private async Task OpenEntryFormAsync(Guid workflowId, Guid activityId)
    {
        _draftValues.Clear();
        var res = await QueryDispatcher.QueryAsync<
            GetWorkflowEntryFormQuery,
            WorkflowEntryFormViewResponse
        >(new GetWorkflowEntryFormQuery(workflowId, activityId));
        if (res.IsFailure)
        {
            _isError = true;
            _statusMessage = res.PrimaryError.Description;
            return;
        }

        _activeForm = res.Value;
        _isError = false;
    }

    private string GetDraftValue(string attributeKey, string? fallback)
    {
        return _draftValues.TryGetValue(attributeKey, out var val)
            ? val ?? string.Empty
            : fallback ?? string.Empty;
    }

    private async Task OnFieldEditedAsync(string attributeKey, string? newValue)
    {
        _draftValues[attributeKey] = newValue;
        if (_activeForm is null)
        {
            return;
        }

        var res = await QueryDispatcher.QueryAsync<
            GetWorkflowEntryFormQuery,
            WorkflowEntryFormViewResponse
        >(
            new GetWorkflowEntryFormQuery(
                _activeForm.WorkflowId,
                _activeForm.ActivityId,
                _draftValues
            )
        );
        if (res.IsSuccess)
        {
            _activeForm = res.Value;
        }
    }

    private async Task SubmitFormAsync(string buttonKey)
    {
        if (_activeForm is null)
        {
            return;
        }

        var res = await CommandDispatcher.SendAsync<
            SubmitWorkflowEntryFormCommand,
            WorkflowFormSubmissionResponse
        >(
            new SubmitWorkflowEntryFormCommand(
                _activeForm.WorkflowId,
                _activeForm.ActionId,
                buttonKey,
                new Dictionary<string, string?>(_draftValues, StringComparer.OrdinalIgnoreCase)
            )
        );

        _isError = res.IsFailure;
        _statusMessage = res.IsSuccess
            ? $"{res.Value.ConfirmationTitle}: {res.Value.ConfirmationMessage}"
            : res.PrimaryError.Description;

        if (res.IsSuccess)
        {
            _activeForm = null;
            _draftValues.Clear();
            await ReloadTasksAsync();
        }
    }
}
