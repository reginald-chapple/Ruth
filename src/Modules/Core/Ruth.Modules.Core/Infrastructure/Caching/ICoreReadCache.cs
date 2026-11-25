namespace Ruth.Modules.Core.Infrastructure.Caching;

/// <summary>Redis-coordinated in-memory read cache for Core metadata (EntityTypes, DefinedTypes, Categories, Attributes).</summary>
public interface ICoreReadCache
{
    /// <summary>Gets or loads a cached metadata snapshot under <paramref name="bucket" />.</summary>
    Task<T> GetOrLoadAsync<T>(
        string bucket,
        Func<CancellationToken, Task<T>> loader,
        CancellationToken cancellationToken = default
    )
        where T : class;

    /// <summary>Invalidates a single cache bucket locally and across Redis nodes.</summary>
    Task InvalidateBucketAsync(string bucket, CancellationToken cancellationToken = default);

    /// <summary>Invalidates all Core cache buckets locally and across Redis nodes.</summary>
    Task InvalidateAllAsync(CancellationToken cancellationToken = default);
}
