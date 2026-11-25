using System.Collections.Frozen;

namespace Ruth.SharedKernel.Security;

/// <summary>Immutable identity snapshot evaluated against entity ACL rules.</summary>
public sealed record SecuritySubject
{
    private static readonly FrozenSet<Guid> _emptyRoles = FrozenSet.ToFrozenSet(
        Array.Empty<Guid>()
    );

    /// <summary>Gets an unauthenticated anonymous subject.</summary>
    public static SecuritySubject Anonymous { get; } = new(userAccountId: null, _emptyRoles);

    /// <summary>Initializes a new <see cref="SecuritySubject" />.</summary>
    public SecuritySubject(Guid? userAccountId, IEnumerable<Guid>? roleIds = null)
    {
        UserAccountId =
            userAccountId.HasValue && userAccountId.Value != Guid.Empty ? userAccountId : null;
        RoleIds = roleIds is null
            ? _emptyRoles
            : roleIds.Where(static id => id != Guid.Empty).ToFrozenSet();
    }

    /// <summary>Gets the authenticated user account identifier, or <see langword="null" /> when anonymous.</summary>
    public Guid? UserAccountId { get; }

    /// <summary>Gets the security role identifiers assigned to this subject.</summary>
    public IReadOnlySet<Guid> RoleIds { get; }

    /// <summary>Gets a value indicating whether the subject represents an authenticated user.</summary>
    public bool IsAuthenticated => UserAccountId.HasValue;
}
