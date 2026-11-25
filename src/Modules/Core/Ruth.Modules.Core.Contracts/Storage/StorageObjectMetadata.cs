namespace Ruth.Modules.Core.Contracts.Storage;

/// <summary>Metadata returned after storing an object in a storage provider.</summary>
public sealed record StorageObjectMetadata(
    string ObjectKey,
    long ContentLength,
    string ContentType,
    string? ETag = null
);
