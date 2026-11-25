namespace Ruth.Modules.Core.Contracts.Storage;

/// <summary>Modular object storage provider contract (implemented by RustFS/S3 and test doubles).</summary>
public interface IStorageProvider
{
    /// <summary>Gets the canonical provider key (for example, rustfs).</summary>
    string ProviderKey { get; }

    /// <summary>Streams binary content to the configured bucket under <paramref name="objectKey" />.</summary>
    Task<StorageObjectMetadata> UploadAsync(
        string objectKey,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default
    );

    /// <summary>Opens a readable stream for <paramref name="objectKey" />, or null if not found.</summary>
    Task<Stream?> OpenReadAsync(string objectKey, CancellationToken cancellationToken = default);

    /// <summary>Returns whether an object exists at <paramref name="objectKey" />.</summary>
    Task<bool> ExistsAsync(string objectKey, CancellationToken cancellationToken = default);

    /// <summary>Deletes an object from storage if present.</summary>
    Task DeleteAsync(string objectKey, CancellationToken cancellationToken = default);

    /// <summary>Generates a time-bounded S3 presigned GET URL.</summary>
    Task<StoragePresignedUrl> CreatePresignedDownloadUrlAsync(
        string objectKey,
        TimeSpan expiresIn,
        CancellationToken cancellationToken = default
    );

    /// <summary>Generates a time-bounded S3 presigned PUT URL.</summary>
    Task<StoragePresignedUrl> CreatePresignedUploadUrlAsync(
        string objectKey,
        string contentType,
        TimeSpan expiresIn,
        CancellationToken cancellationToken = default
    );
}
