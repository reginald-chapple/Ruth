using Ruth.Modules.Workflow.Contracts.Forms;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;

namespace Ruth.Modules.Workflow.Domain.Forms;

/// <summary>Reusable Form Builder template aggregate providing shared branding, person-entry settings, and confirmation copy.</summary>
public sealed class FormBuilderTemplate
    : AggregateRoot<FormBuilderTemplateId>,
        IAuditableEntity,
        IHasConcurrencyToken,
        ISecured
{
    public const string EntityTypeKeyName = "Ruth.Workflow.FormBuilderTemplate";

    private FormBuilderTemplate() { }

    private FormBuilderTemplate(FormBuilderTemplateId id)
        : base(id) { }

    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsActive { get; private set; } = true;
    public string? FormHeaderHtml { get; private set; }
    public string? FormFooterHtml { get; private set; }
    public string ConfirmationTitle { get; private set; } = "Submission Received";
    public string ConfirmationMessageTemplate { get; private set; } = string.Empty;
    public bool IncludePersonEntrySection { get; private set; } = true;
    public bool AllowNotes { get; private set; }
    public string? CompletionRedirectUrl { get; private set; }
    public Guid? LinkedWorkflowTypeId { get; private set; }

    public DateTimeOffset CreatedOnUtc { get; private set; }
    public DateTimeOffset? ModifiedOnUtc { get; private set; }
    public Guid ConcurrencyStamp { get; private set; } = Guid.CreateVersion7();

    public string EntityTypeKey => EntityTypeKeyName;
    public Guid EntityId => Id.Value;
    public ISecured ParentAuthority => GlobalDefaultSecured.Instance;

    public bool IsAllowedByDefault(string action)
    {
        return GlobalDefaultSecured.Instance.IsAllowedByDefault(action);
    }

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

    public static Result<FormBuilderTemplate> Create(FormBuilderTemplateSpec spec)
    {
        ArgumentNullException.ThrowIfNull(spec);
        var entity = new FormBuilderTemplate(FormBuilderTemplateId.Create());
        var res = entity.Apply(spec);
        return res.IsFailure
            ? Result.Failure<FormBuilderTemplate>(res.PrimaryError)
            : Result.Success(entity);
    }

    public Result Update(FormBuilderTemplateSpec spec)
    {
        ArgumentNullException.ThrowIfNull(spec);
        return Apply(spec);
    }

    private Result Apply(FormBuilderTemplateSpec spec)
    {
        if (
            string.IsNullOrWhiteSpace(spec.Name)
            || !WorkflowTextNormalizer.TryNormalizeSlug(spec.Slug, out var slug)
            || string.IsNullOrWhiteSpace(spec.ConfirmationTitle)
        )
        {
            return Result.Failure(WorkflowErrors.FormTemplateInvalid);
        }

        Name = spec.Name.Trim();
        Slug = slug;
        Description = WorkflowTextNormalizer.TrimOrNull(spec.Description);
        IsActive = spec.IsActive;
        FormHeaderHtml = WorkflowTextNormalizer.TrimOrNull(spec.FormHeaderHtml, 8000);
        FormFooterHtml = WorkflowTextNormalizer.TrimOrNull(spec.FormFooterHtml, 8000);
        ConfirmationTitle = spec.ConfirmationTitle.Trim();
        ConfirmationMessageTemplate = WorkflowTextNormalizer.TrimOrNull(
            spec.ConfirmationMessageTemplate,
            4000
        ) ?? "Thank you.";
        IncludePersonEntrySection = spec.IncludePersonEntrySection;
        AllowNotes = spec.AllowNotes;
        CompletionRedirectUrl = WorkflowTextNormalizer.TrimOrNull(spec.CompletionRedirectUrl, 500);
        LinkedWorkflowTypeId = spec.LinkedWorkflowTypeId;
        return Result.Success();
    }
}
