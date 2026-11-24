namespace Ruth.Host.Configuration;

/// <summary>Root object storage configuration section.</summary>
public sealed class StorageOptions
{
    /// <summary>Configuration section path for storage settings.</summary>
    public const string SectionName = "Storage";

    /// <summary>Gets or sets the RustFS S3-compatible object storage configuration.</summary>
    public RustFsOptions RustFs { get; set; } = new();

    /// <summary>Gets or sets an optional flat storage endpoint fallback.</summary>
    public string? Endpoint { get; set; }

    /// <summary>Gets or sets an optional flat storage service URL fallback.</summary>
    public string? ServiceUrl { get; set; }

    /// <summary>Gets or sets an optional flat bucket name fallback.</summary>
    public string? BucketName { get; set; }
}
