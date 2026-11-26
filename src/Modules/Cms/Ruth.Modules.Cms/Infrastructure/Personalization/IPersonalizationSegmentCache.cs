namespace Ruth.Modules.Cms.Infrastructure.Personalization;

/// <summary>Redis bitset/set cache for fast personalization segment membership checks with in-memory fallback.</summary>
public interface IPersonalizationSegmentCache
{
    /// <summary>Replaces the cached member set and deterministic bitset for a segment.</summary>
    Task SetSegmentMembersAsync(
        string segmentKey,
        IReadOnlyCollection<Guid> personIds,
        CancellationToken cancellationToken = default
    );

    /// <summary>Checks whether a person belongs to a segment using Redis bitset/set or local fallback.</summary>
    Task<bool?> IsMemberAsync(
        string segmentKey,
        Guid personId,
        CancellationToken cancellationToken = default
    );
}
