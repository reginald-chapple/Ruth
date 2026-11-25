namespace Ruth.Modules.Groups.Contracts.Groups;

/// <summary>Tree node projection for the hierarchical GroupTreeView.</summary>
public sealed record GroupTreeNodeResponse(
    Guid Id,
    Guid GroupTypeId,
    string GroupTypeName,
    Guid? ParentGroupId,
    Guid? BranchId,
    string Name,
    bool IsActive,
    bool IsPublic,
    bool IsArchived,
    int Order,
    int ActiveMemberCount,
    int? GroupCapacity,
    IReadOnlyList<GroupTreeNodeResponse> Children
);
