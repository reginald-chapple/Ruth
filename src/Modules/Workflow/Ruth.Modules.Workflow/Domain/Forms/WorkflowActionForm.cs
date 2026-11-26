using Ruth.Modules.Workflow.Contracts.Forms;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Workflow.Domain.Forms;

/// <summary>Interactive form definition aggregate bound 1-to-1 with a WorkflowActionType.</summary>
public sealed class WorkflowActionForm
    : AggregateRoot<WorkflowActionFormId>,
        IAuditableEntity,
        IHasConcurrencyToken
{
    private readonly List<WorkflowActionFormSection> _sections = [];
    private readonly List<WorkflowActionFormField> _fields = [];
    private List<WorkflowFormButtonSpec> _buttons = [];

    private WorkflowActionForm() { }

    private WorkflowActionForm(WorkflowActionFormId id)
        : base(id) { }

    public WorkflowActionTypeId WorkflowActionTypeId { get; private set; }
    public FormBuilderTemplateId? FormBuilderTemplateId { get; private set; }
    public string? HeaderMarkup { get; private set; }
    public string? FooterMarkup { get; private set; }
    public bool AllowPersonEntry { get; private set; }
    public string? NotificationSystemCommunicationKey { get; private set; }

    public IReadOnlyList<WorkflowActionFormSection> Sections => _sections;
    public IReadOnlyList<WorkflowActionFormField> Fields => _fields;

    public IReadOnlyList<WorkflowFormButtonSpec> Buttons
    {
        get => _buttons;
        private set => _buttons = [.. value];
    }

    public DateTimeOffset CreatedOnUtc { get; private set; }
    public DateTimeOffset? ModifiedOnUtc { get; private set; }
    public Guid ConcurrencyStamp { get; private set; } = Guid.CreateVersion7();

    public void SetCreated(DateTimeOffset createdOnUtc)
    {
        CreatedOnUtc = createdOnUtc.ToUniversalTime();
    }

    public void SetModified(DateTimeOffset modifiedOnUtc)
    {
        ModifiedOnUtc = modifiedOnUtc.ToUniversalTime();
    }

    public void SetConcurrencyStamp(Guid newStamp)
    {
        ConcurrencyStamp = newStamp;
    }

    public static Result<WorkflowActionForm> Create(
        WorkflowActionTypeId actionTypeId,
        WorkflowActionFormSpec spec
    )
    {
        ArgumentNullException.ThrowIfNull(spec);
        if (actionTypeId.IsEmpty)
        {
            return Result.Failure<WorkflowActionForm>(WorkflowErrors.ActionFormInvalid);
        }

        var form = new WorkflowActionForm(WorkflowActionFormId.Create())
        {
            WorkflowActionTypeId = actionTypeId,
        };
        var res = form.Apply(spec);
        return res.IsFailure
            ? Result.Failure<WorkflowActionForm>(res.PrimaryError)
            : Result.Success(form);
    }

    public Result Update(WorkflowActionFormSpec spec)
    {
        ArgumentNullException.ThrowIfNull(spec);
        return Apply(spec);
    }

    private Result Apply(WorkflowActionFormSpec spec)
    {
        if (!HasNonEmptyCollections(spec))
        {
            return Result.Failure(WorkflowErrors.ActionFormInvalid);
        }

        var buttonsRes = ValidateButtons(spec.Buttons);
        if (buttonsRes.IsFailure)
        {
            return buttonsRes;
        }

        var sectionsRes = BuildSections(spec.Sections ?? []);
        if (sectionsRes.IsFailure)
        {
            return sectionsRes;
        }

        var sectionKeys = sectionsRes
            .Value.Select(static s => s.SectionKey)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var fieldsRes = BuildFields(spec.Fields, sectionKeys);
        if (fieldsRes.IsFailure)
        {
            return fieldsRes;
        }

        AssignMetadataAndCollections(spec, buttonsRes.Value, sectionsRes.Value, fieldsRes.Value);
        return Result.Success();
    }

    private static bool HasNonEmptyCollections(WorkflowActionFormSpec spec)
    {
        return spec.Fields is { Count: > 0 } && spec.Buttons is { Count: > 0 };
    }

    private void AssignMetadataAndCollections(
        WorkflowActionFormSpec spec,
        List<WorkflowFormButtonSpec> buttons,
        List<WorkflowActionFormSection> sections,
        List<WorkflowActionFormField> fields
    )
    {
        FormBuilderTemplateId = spec.FormBuilderTemplateId.HasValue
            ? Domain.FormBuilderTemplateId.From(spec.FormBuilderTemplateId.Value)
            : null;
        HeaderMarkup = WorkflowTextNormalizer.TrimOrNull(spec.HeaderMarkup, 8000);
        FooterMarkup = WorkflowTextNormalizer.TrimOrNull(spec.FooterMarkup, 8000);
        AllowPersonEntry = spec.AllowPersonEntry;
        NotificationSystemCommunicationKey = WorkflowTextNormalizer.TrimOrNull(
            spec.NotificationSystemCommunicationKey,
            160
        );

        _buttons = buttons;
        _sections.Clear();
        _sections.AddRange(sections);
        _fields.Clear();
        _fields.AddRange(fields);
    }

    private static Result<List<WorkflowFormButtonSpec>> ValidateButtons(
        IReadOnlyList<WorkflowFormButtonSpec> buttons
    )
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var normalized = new List<WorkflowFormButtonSpec>(buttons.Count);
        foreach (var btn in buttons.OrderBy(static b => b.Order))
        {
            if (
                !WorkflowTextNormalizer.TryNormalizeSlug(btn.ButtonKey, out var key)
                || string.IsNullOrWhiteSpace(btn.Label)
                || !seen.Add(key)
            )
            {
                return Result.Failure<List<WorkflowFormButtonSpec>>(
                    WorkflowErrors.ActionFormInvalid
                );
            }

            normalized.Add(btn with { ButtonKey = key, Label = btn.Label.Trim() });
        }

        return Result.Success(normalized);
    }

    private Result<List<WorkflowActionFormSection>> BuildSections(
        IReadOnlyList<WorkflowActionFormSectionSpec> specs
    )
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var list = new List<WorkflowActionFormSection>(specs.Count);
        foreach (var item in specs.OrderBy(static s => s.Order))
        {
            var created = WorkflowActionFormSection.Create(Id, item);
            if (created.IsFailure || !seen.Add(created.Value.SectionKey))
            {
                return Result.Failure<List<WorkflowActionFormSection>>(
                    WorkflowErrors.ActionFormInvalid
                );
            }

            list.Add(created.Value);
        }

        return Result.Success(list);
    }

    private Result<List<WorkflowActionFormField>> BuildFields(
        IReadOnlyList<WorkflowActionFormFieldSpec> specs,
        HashSet<string> validSections
    )
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var list = new List<WorkflowActionFormField>(specs.Count);
        foreach (var item in specs.OrderBy(static f => f.Order))
        {
            var created = WorkflowActionFormField.Create(Id, item);
            if (
                created.IsFailure
                || !seen.Add(created.Value.AttributeKey)
                || (
                    created.Value.SectionKey is not null
                    && !validSections.Contains(created.Value.SectionKey)
                )
            )
            {
                return Result.Failure<List<WorkflowActionFormField>>(
                    WorkflowErrors.ActionFormInvalid
                );
            }

            list.Add(created.Value);
        }

        return Result.Success(list);
    }
}
