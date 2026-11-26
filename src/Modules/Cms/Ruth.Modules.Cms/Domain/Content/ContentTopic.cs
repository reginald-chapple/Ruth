using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Cms.Domain.Content;

/// <summary>Hierarchical editorial topic for categorizing CMS content channel items.</summary>
public sealed class ContentTopic
    : AggregateRoot<ContentTopicId>,
        IAuditableEntity,
        IHasConcurrencyToken
{
    private ContentTopic() { }

    private ContentTopic(ContentTopicId id)
        : base(id) { }

    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public ContentTopicId? ParentTopicId { get; private set; }
    public int Order { get; private set; }
    public bool IsActive { get; private set; } = true;

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

    public static Result<ContentTopic> Create(
        string name,
        string slug,
        string? description = null,
        ContentTopicId? parentTopicId = null,
        int order = 0
    )
    {
        var normalizedSlug = CmsTextNormalizer.NormalizeSlug(slug);
        return string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(normalizedSlug)
            ? Result.Failure<ContentTopic>(
                CmsErrors.Validation("ContentTopic.Invalid", "Topic name and slug are required.")
            )
            : Result.Success(
                new ContentTopic(ContentTopicId.Create())
                {
                    Name = name.Trim(),
                    Slug = normalizedSlug,
                    Description = string.IsNullOrWhiteSpace(description)
                        ? null
                        : description.Trim(),
                    ParentTopicId = parentTopicId,
                    Order = order,
                    IsActive = true,
                }
            );
    }
}
