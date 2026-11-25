namespace Ruth.Modules.Groups.Domain.GroupTypes;

/// <summary>Allowed parent-to-child GroupType relationship entry.</summary>
public sealed class GroupTypeAllowedChild
{
    private GroupTypeAllowedChild() { }

    internal GroupTypeAllowedChild(GroupTypeId parentGroupTypeId, GroupTypeId childGroupTypeId)
    {
        ParentGroupTypeId = parentGroupTypeId;
        ChildGroupTypeId = childGroupTypeId;
    }

    /// <summary>Gets the parent GroupType identifier.</summary>
    public GroupTypeId ParentGroupTypeId { get; private set; }

    /// <summary>Gets the allowed child GroupType identifier.</summary>
    public GroupTypeId ChildGroupTypeId { get; private set; }
}
