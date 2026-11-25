using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Groups.Contracts.Groups;

/// <summary>Retrieves the hierarchical tree of Groups optionally filtered by GroupType, Branch, or archive state.</summary>
public sealed record GetGroupTreeQuery(
    Guid? GroupTypeId = null,
    Guid? BranchId = null,
    bool IncludeInactive = false,
    bool IncludeArchived = false,
    bool NavigationOnly = false
) : IQuery<IReadOnlyList<GroupTreeNodeResponse>>;
