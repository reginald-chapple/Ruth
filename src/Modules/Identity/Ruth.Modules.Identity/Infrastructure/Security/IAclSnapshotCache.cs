using Ruth.Modules.Identity.Domain;

namespace Ruth.Modules.Identity.Infrastructure.Security;

/// <summary>Provides lock-free cached access to the current <see cref="AclRuleSnapshot" />.</summary>
public interface IAclSnapshotCache
{
    /// <summary>Returns the current immutable <see cref="AclRuleSnapshot" />, reloading when invalidated.</summary>
    ValueTask<AclRuleSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default);

    /// <summary>Invalidates the cached snapshot locally and across Redis nodes.</summary>
    Task InvalidateAsync(CancellationToken cancellationToken = default);
}
