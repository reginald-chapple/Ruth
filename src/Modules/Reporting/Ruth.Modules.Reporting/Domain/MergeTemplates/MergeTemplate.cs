using Ruth.Modules.Reporting.Contracts.MergeTemplates;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;

namespace Ruth.Modules.Reporting.Domain.MergeTemplates;

/// <summary>DOCX/HTML MergeTemplate aggregate backed by safe declarative merge tokens.</summary>
public sealed class MergeTemplate
    : AggregateRoot<MergeTemplateId>,
        IAuditableEntity,
        IHasConcurrencyToken,
        ISecured
{
    public const string EntityTypeKeyName = "Ruth.Reporting.MergeTemplate";

    private MergeTemplate() { }

    private MergeTemplate(MergeTemplateId id)
        : base(id) { }

    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public MergeTemplateFormat Format { get; private set; }
    public string TemplateBody { get; private set; } = string.Empty;
    public string? HeaderMarkup { get; private set; }
    public string? FooterMarkup { get; private set; }
    public Guid? CategoryId { get; private set; }
    public bool IsGlobal { get; private set; } = true;
    public Guid? OwnerPersonId { get; private set; }
    public bool IsActive { get; private set; } = true;

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

    public static Result<MergeTemplate> Create(
        string name,
        MergeTemplateFormat format,
        string templateBody,
        string? description = null,
        string? headerMarkup = null,
        string? footerMarkup = null,
        Guid? categoryId = null,
        bool isGlobal = true,
        Guid? ownerPersonId = null
    )
    {
        return string.IsNullOrWhiteSpace(name)
                ? Result.Failure<MergeTemplate>(
                    ReportingErrors.Validation(
                        "MergeTemplate.NameRequired",
                        "MergeTemplate name is required."
                    )
                )
            : string.IsNullOrWhiteSpace(templateBody)
                ? Result.Failure<MergeTemplate>(
                    ReportingErrors.Validation(
                        "MergeTemplate.BodyRequired",
                        "MergeTemplate body is required."
                    )
                )
            : Result.Success(
                new MergeTemplate(MergeTemplateId.Create())
                {
                    Name = name.Trim(),
                    Description = string.IsNullOrWhiteSpace(description)
                        ? null
                        : description.Trim(),
                    Format = format,
                    TemplateBody = templateBody,
                    HeaderMarkup = string.IsNullOrWhiteSpace(headerMarkup) ? null : headerMarkup,
                    FooterMarkup = string.IsNullOrWhiteSpace(footerMarkup) ? null : footerMarkup,
                    CategoryId = categoryId,
                    IsGlobal = isGlobal,
                    OwnerPersonId = ownerPersonId,
                    IsActive = true,
                }
            );
    }

    public void ReassignOwner(Guid survivingPersonId)
    {
        OwnerPersonId = survivingPersonId;
    }
}
