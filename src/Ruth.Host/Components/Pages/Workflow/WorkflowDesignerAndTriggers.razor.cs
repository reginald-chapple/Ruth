using System.Globalization;
using Microsoft.AspNetCore.Components;
using Ruth.Modules.Workflow.Contracts.Actions;
using Ruth.Modules.Workflow.Contracts.Definitions;
using Ruth.Modules.Workflow.Contracts.Instances;
using Ruth.Modules.Workflow.Contracts.Triggers;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Host.Components.Pages.Workflow;

/// <summary>Staff W5-S5 WorkflowType state-machine designer, action catalog inspector, and automation trigger manager.</summary>
public sealed partial class WorkflowDesignerAndTriggers : ComponentBase
{
    private IReadOnlyList<WorkflowActionPluginDescriptor> _catalog = [];
    private IReadOnlyList<WorkflowTypeResponse> _workflowTypes = [];
    private IReadOnlyList<WorkflowTriggerResponse> _triggers = [];
    private IReadOnlyList<WorkflowInstanceResponse> _instances = [];
    private Guid _selectedTypeId;

    private string _typeName = "Member Intake & Review";
    private string _typeSlug = "member-intake-review";
    private string _prefix = "WF-";
    private string _workTerm = "Submission";

    private string _triggerName = "Auto-Intake on Support Request";
    private string _eventTypeKey = "engagement.support.submitted";

    private string? _statusMessage;
    private bool _isError;

    [Inject]
    private IQueryDispatcher QueryDispatcher { get; set; } = default!;

    [Inject]
    private ICommandDispatcher CommandDispatcher { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        await ReloadAllAsync();
    }

    private async Task OnTypeChangedAsync(Guid typeId)
    {
        _selectedTypeId = typeId;
        await ReloadInstancesAsync();
    }

    private async Task ReloadAllAsync()
    {
        var catRes = await QueryDispatcher.QueryAsync<
            GetWorkflowActionCatalogQuery,
            IReadOnlyList<WorkflowActionPluginDescriptor>
        >(new GetWorkflowActionCatalogQuery());
        _catalog = catRes.IsSuccess ? catRes.Value : [];

        var typesRes = await QueryDispatcher.QueryAsync<
            GetWorkflowTypesQuery,
            IReadOnlyList<WorkflowTypeResponse>
        >(new GetWorkflowTypesQuery(IncludeInactive: true));
        _workflowTypes = typesRes.IsSuccess ? typesRes.Value : [];
        if (_selectedTypeId == Guid.Empty && _workflowTypes.Count > 0)
        {
            _selectedTypeId = _workflowTypes[0].Id;
        }

        var trigRes = await QueryDispatcher.QueryAsync<
            GetWorkflowTriggersQuery,
            IReadOnlyList<WorkflowTriggerResponse>
        >(new GetWorkflowTriggersQuery(IncludeInactive: true));
        _triggers = trigRes.IsSuccess ? trigRes.Value : [];

        await ReloadInstancesAsync();
    }

    private async Task ReloadInstancesAsync()
    {
        var instRes = await QueryDispatcher.QueryAsync<
            GetWorkflowInstancesQuery,
            IReadOnlyList<WorkflowInstanceResponse>
        >(
            new GetWorkflowInstancesQuery(
                _selectedTypeId == Guid.Empty ? null : _selectedTypeId,
                Limit: 25
            )
        );
        _instances = instRes.IsSuccess ? instRes.Value : [];
    }

    private async Task CreateWorkflowTypeAsync()
    {
        var spec = WorkflowDesignerSpecFactory.BuildDefaultIntakeSpec(
            _typeName,
            _typeSlug,
            _workTerm,
            _prefix
        );
        var res = await CommandDispatcher.SendAsync<
            CreateWorkflowTypeCommand,
            WorkflowTypeResponse
        >(new CreateWorkflowTypeCommand(spec));
        _isError = res.IsFailure;
        _statusMessage = res.IsSuccess
            ? $"Created WorkflowType '{res.Value.Name}'."
            : res.PrimaryError.Description;
        if (res.IsSuccess)
        {
            _selectedTypeId = res.Value.Id;
            await ReloadAllAsync();
        }
    }

    private async Task CreateTriggerAsync()
    {
        if (_selectedTypeId == Guid.Empty)
        {
            return;
        }

        var res = await CommandDispatcher.SendAsync<
            CreateWorkflowTriggerCommand,
            WorkflowTriggerResponse
        >(
            new CreateWorkflowTriggerCommand(
                new WorkflowTriggerSpec(
                    _triggerName,
                    WorkflowTriggerKind.IntegrationEvent,
                    _eventTypeKey,
                    _selectedTypeId
                )
            )
        );
        _isError = res.IsFailure;
        _statusMessage = res.IsSuccess
            ? $"Created trigger '{res.Value.Name}'."
            : res.PrimaryError.Description;
        await ReloadAllAsync();
    }

    private async Task LaunchSelectedAsync()
    {
        if (_selectedTypeId == Guid.Empty)
        {
            return;
        }

        var res = await CommandDispatcher.SendAsync<
            LaunchWorkflowCommand,
            WorkflowInstanceResponse
        >(new LaunchWorkflowCommand(_selectedTypeId.ToString("D")));
        _isError = res.IsFailure;
        _statusMessage = res.IsSuccess
            ? $"Launched {res.Value.WorkflowNumber} ({res.Value.Status})."
            : res.PrimaryError.Description;
        await ReloadInstancesAsync();
    }

    private async Task ProcessPendingNowAsync()
    {
        var res = await CommandDispatcher.SendAsync<
            ProcessPendingWorkflowsCommand,
            WorkflowBatchProcessResponse
        >(new ProcessPendingWorkflowsCommand());
        _isError = res.IsFailure;
        _statusMessage = res.IsSuccess
            ? string.Create(
                CultureInfo.InvariantCulture,
                $"Evaluated {res.Value.EvaluatedCount} workflows (completed: {res.Value.CompletedCount})."
            )
            : res.PrimaryError.Description;
        await ReloadInstancesAsync();
    }
}
