using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Cms.Domain.Bookmarks;

/// <summary>Personal or organization-shared staff bookmark bar section.</summary>
public sealed class PersonalLinkSection
    : AggregateRoot<PersonalLinkSectionId>,
        IAuditableEntity,
        IHasConcurrencyToken
{
    private readonly List<PersonalLink> _links = [];

    private PersonalLinkSection() { }

    private PersonalLinkSection(PersonalLinkSectionId id)
        : base(id) { }

    public string Name { get; private set; } = string.Empty;
    public Guid? PersonId { get; private set; }
    public bool IsShared { get; private set; }
    public int Order { get; private set; }

    public IReadOnlyCollection<PersonalLink> Links => _links.AsReadOnly();

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

    public static Result<PersonalLinkSection> Create(
        string name,
        Guid? personId = null,
        bool isShared = false,
        int order = 0
    )
    {
        return string.IsNullOrWhiteSpace(name)
                ? Result.Failure<PersonalLinkSection>(
                    CmsErrors.Validation(
                        "PersonalLinkSection.NameRequired",
                        "Bookmark section name is required."
                    )
                )
            : !isShared && personId is null
                ? Result.Failure<PersonalLinkSection>(
                    CmsErrors.Validation(
                        "PersonalLinkSection.OwnerRequired",
                        "Non-shared bookmark sections require a PersonId."
                    )
                )
            : Result.Success(
                new PersonalLinkSection(PersonalLinkSectionId.Create())
                {
                    Name = name.Trim(),
                    PersonId = isShared ? null : personId,
                    IsShared = isShared,
                    Order = order,
                }
            );
    }

    public Result<PersonalLink> AddLink(
        string name,
        string url,
        string? iconName = null,
        int order = 0
    )
    {
        var linkRes = PersonalLink.Create(Id, name, url, iconName, order);
        if (linkRes.IsFailure)
        {
            return linkRes;
        }

        _links.Add(linkRes.Value);
        return linkRes;
    }

    public bool RepointPerson(Guid retiredPersonId, Guid survivingPersonId)
    {
        if (PersonId != retiredPersonId)
        {
            return false;
        }

        PersonId = survivingPersonId;
        return true;
    }
}
