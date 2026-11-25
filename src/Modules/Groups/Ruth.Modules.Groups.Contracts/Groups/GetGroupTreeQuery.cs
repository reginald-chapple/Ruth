using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Groups.Contracts.Groups;

/// <summary>Retrieves the hierarchical tree of Groups optionally filtered by GroupType, Campus, or archive state.</summary>
public sealed record GetGroupTreeQuery(
    Guid? GroupTypeId = null,
    Guid? CampusId = null,
    bool IncludeInactive = false,
    bool IncludeArchived = false,
    bool NavigationOnly = false
) : IQuery<IReadOnlyList<GroupTreeNodeResponse>>;
