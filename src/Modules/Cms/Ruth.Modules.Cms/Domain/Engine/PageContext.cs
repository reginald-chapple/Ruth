using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Cms.Domain.Engine;

/// <summary>Declarative entity context parameter bound to a <see cref="CmsPage" />.</summary>
public sealed class PageContext : Entity<PageContextId>
{
    private PageContext() { }

    private PageContext(PageContextId id, CmsPageId pageId, string entityKey, string idParameter)
        : base(id)
    {
        PageId = pageId;
        EntityKey = entityKey;
        IdParameter = idParameter;
    }

    public CmsPageId PageId { get; private set; }
    public string EntityKey { get; private set; } = string.Empty;
    public string IdParameter { get; private set; } = string.Empty;

    public static Result<PageContext> Create(CmsPageId pageId, string entityKey, string idParameter)
    {
        return
            pageId.IsEmpty
            || string.IsNullOrWhiteSpace(entityKey)
            || string.IsNullOrWhiteSpace(idParameter)
            ? Result.Failure<PageContext>(
                CmsErrors.Validation(
                    "PageContext.Invalid",
                    "PageId, EntityKey, and IdParameter are required."
                )
            )
            : Result.Success(
                new PageContext(
                    PageContextId.Create(),
                    pageId,
                    entityKey.Trim(),
                    idParameter.Trim()
                )
            );
    }
}
