using Ruth.SharedKernel.Tenancy;

namespace Ruth.Modules.Groups.Domain.GroupTypes;

/// <summary>Allowed parent-to-child GroupType relationship entry.</summary>
public sealed class GroupTypeAllowedChild : ITenantScoped
{
    private GroupTypeAllowedChild() { }

    internal GroupTypeAllowedChild(GroupTypeId parentGroupTypeId, GroupTypeId childGroupTypeId)
    {
        ParentGroupTypeId = parentGroupTypeId;
        ChildGroupTypeId = childGroupTypeId;
    }

    /// <inheritdoc />
    public TenantId TenantId { get; private set; }

    /// <summary>Gets the parent GroupType identifier.</summary>
    public GroupTypeId ParentGroupTypeId { get; private set; }

    /// <summary>Gets the allowed child GroupType identifier.</summary>
    public GroupTypeId ChildGroupTypeId { get; private set; }

    /// <inheritdoc />
    public void AssignTenant(TenantId tenantId)
    {
        if (tenantId.IsEmpty)
        {
            throw new ArgumentException("Tenant identifier cannot be empty.", nameof(tenantId));
        }

        if (!TenantId.IsEmpty && TenantId != tenantId)
        {
            throw new CrossTenantAccessException(
                $"Cannot reassign GroupTypeAllowedChild ({ParentGroupTypeId}/{ChildGroupTypeId}) from tenant '{TenantId}' to '{tenantId}'."
            );
        }

        TenantId = tenantId;
    }
}
