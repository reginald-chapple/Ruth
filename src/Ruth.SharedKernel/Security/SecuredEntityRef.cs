namespace Ruth.SharedKernel.Security;

/// <summary>Lightweight <see cref="ISecured" /> descriptor used for authorization checks without loading full aggregates.</summary>
public sealed record SecuredEntityRef(
    string EntityTypeKey,
    Guid EntityId,
    ISecured? ParentAuthority = null,
    ISecured? ParentAuthorityPre = null,
    bool RestrictedDefault = false
) : ISecured
{
    /// <inheritdoc />
    public bool IsAllowedByDefault(string action)
    {
        return !RestrictedDefault
            && (
                ParentAuthority is not null
                    ? ParentAuthority.IsAllowedByDefault(action)
                    : GlobalDefaultSecured.Instance.IsAllowedByDefault(action)
            );
    }
}
