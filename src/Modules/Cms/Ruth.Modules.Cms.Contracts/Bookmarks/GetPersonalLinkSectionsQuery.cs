using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Cms.Contracts.Bookmarks;

/// <summary>Query to retrieve shared and person-scoped staff bookmark sections.</summary>
public sealed record GetPersonalLinkSectionsQuery(Guid? PersonId = null, bool IncludeShared = true)
    : IQuery<IReadOnlyList<PersonalLinkSectionResponse>>;
