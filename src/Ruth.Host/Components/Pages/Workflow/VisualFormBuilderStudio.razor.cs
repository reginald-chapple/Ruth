using System.Globalization;
using Microsoft.AspNetCore.Components;
using Ruth.Modules.Workflow.Contracts.Definitions;
using Ruth.Modules.Workflow.Contracts.Forms;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Host.Components.Pages.Workflow;

/// <summary>Interactive W5-S6 Visual Form Builder studio with conditional visibility rule authoring and live preview.</summary>
public sealed partial class VisualFormBuilderStudio : ComponentBase
{
    private readonly List<WorkflowActionFormFieldSpec> _draftFields =
        WorkflowFormStudioHelper.CreateDefaultFields();

    private IReadOnlyList<FormBuilderTemplateResponse> _templates = [];
    private List<ActionSelectionOption> _formCapableActions = [];
    private Guid _selectedActionTypeId;

    private string _templateName = "Assistance & Grant Intake";
    private string _templateSlug = "assistance-grant-intake";
    private string _confirmTitle = "Application Received";

    private string _newFieldKey = "GrantJustification";
    private string _newFieldLabel = "Grant Justification";
    private string _conditionSourceKey = "RequestKind";
    private string _conditionExpectedValue = "Grant";
    private string _sampleKindValue = "Grant";

    private FormVisibilityPreviewResponse? _previewResult;
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
        var tmplRes = await QueryDispatcher.QueryAsync<
            GetFormBuilderTemplatesQuery,
            IReadOnlyList<FormBuilderTemplateResponse>
        >(new GetFormBuilderTemplatesQuery(IncludeInactive: true));
        _templates = tmplRes.IsSuccess ? tmplRes.Value : [];

        var typesRes = await QueryDispatcher.QueryAsync<
            GetWorkflowTypesQuery,
            IReadOnlyList<WorkflowTypeResponse>
        >(new GetWorkflowTypesQuery(IncludeInactive: true));
        _formCapableActions = typesRes.IsSuccess
            ? WorkflowFormStudioHelper.ExtractFormCapableActions(typesRes.Value)
            : [];

        if (_selectedActionTypeId == Guid.Empty && _formCapableActions.Count > 0)
        {
            _selectedActionTypeId = _formCapableActions[0].ActionTypeId;
        }
    }

    private void AddDraftField()
    {
        WorkflowFormStudioHelper.AddOrReplaceDraftField(
            _draftFields,
            _newFieldKey,
            _newFieldLabel,
            _conditionSourceKey,
            _conditionExpectedValue
        );
    }

    private async Task PreviewVisibilityAsync()
    {
        var spec = WorkflowFormStudioHelper.BuildCurrentSpec(_draftFields, _templates);
        var res = await QueryDispatcher.QueryAsync<
            PreviewFormVisibilityQuery,
            FormVisibilityPreviewResponse
        >(
            new PreviewFormVisibilityQuery(
                spec,
                new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["RequestKind"] = _sampleKindValue,
                }
            )
        );
        _previewResult = res.IsSuccess ? res.Value : null;
    }

    private async Task SaveTemplateAsync()
    {
        var linkedWfId = _templates.Count == 0 ? null : _templates[0].LinkedWorkflowTypeId;
        var res = await CommandDispatcher.SendAsync<
            CreateFormBuilderTemplateCommand,
            FormBuilderTemplateResponse
        >(
            new CreateFormBuilderTemplateCommand(
                new FormBuilderTemplateSpec(
                    _templateName,
                    _templateSlug,
                    ConfirmationTitle: _confirmTitle,
                    LinkedWorkflowTypeId: linkedWfId
                )
            )
        );
        _isError = res.IsFailure;
        _statusMessage = res.IsSuccess
            ? $"Saved template '{res.Value.Name}'."
            : res.PrimaryError.Description;
        await ReloadAsync();
    }

    private async Task SaveActionFormAsync()
    {
        if (_selectedActionTypeId == Guid.Empty)
        {
            _isError = true;
            _statusMessage = "Create a WorkflowType with a User Entry Form action first.";
            return;
        }

        var spec = WorkflowFormStudioHelper.BuildCurrentSpec(_draftFields, _templates);
        var res = await CommandDispatcher.SendAsync<
            UpsertWorkflowActionFormCommand,
            WorkflowActionFormResponse
        >(new UpsertWorkflowActionFormCommand(_selectedActionTypeId, spec));
        _isError = res.IsFailure;
        _statusMessage = res.IsSuccess
            ? string.Create(
                CultureInfo.InvariantCulture,
                $"Saved interactive form ({res.Value.Fields.Count} fields)."
            )
            : res.PrimaryError.Description;
    }
}
