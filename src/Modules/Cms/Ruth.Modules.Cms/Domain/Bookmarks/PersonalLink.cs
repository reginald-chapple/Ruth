using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Cms.Domain.Bookmarks;

/// <summary>Individual bookmark link inside a staff <see cref="PersonalLinkSection" />.</summary>
public sealed class PersonalLink : Entity<PersonalLinkId>
{
    private PersonalLink() { }

    private PersonalLink(
        PersonalLinkId id,
        PersonalLinkSectionId sectionId,
        string name,
        string url,
        string? iconName,
        int order
    )
        : base(id)
    {
        SectionId = sectionId;
        Name = name;
        Url = url;
        IconName = iconName;
        Order = order;
    }

    public PersonalLinkSectionId SectionId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Url { get; private set; } = string.Empty;
    public string? IconName { get; private set; }
    public int Order { get; private set; }

    public static Result<PersonalLink> Create(
        PersonalLinkSectionId sectionId,
        string name,
        string url,
        string? iconName = null,
        int order = 0
    )
    {
        return sectionId.IsEmpty || string.IsNullOrWhiteSpace(name)
                ? Result.Failure<PersonalLink>(
                    CmsErrors.Validation("PersonalLink.Invalid", "SectionId and Name are required.")
                )
            : !CmsTextNormalizer.IsSafeHttpOrRelativeUrl(url)
                ? Result.Failure<PersonalLink>(
                    CmsErrors.Validation(
                        "PersonalLink.UrlInvalid",
                        "Bookmark URL must be an http/https URL or root-relative path."
                    )
                )
            : Result.Success(
                new PersonalLink(
                    PersonalLinkId.Create(),
                    sectionId,
                    name.Trim(),
                    url.Trim(),
                    string.IsNullOrWhiteSpace(iconName) ? null : iconName.Trim(),
                    order
                )
            );
    }
}
