using System.ComponentModel.DataAnnotations;

namespace Ruth.Providers.RustFsStorage;

/// <summary>Configuration options for the RustFS S3-compatible IStorageProvider.</summary>
public sealed class RustFsStorageOptions
{
    /// <summary>Primary configuration section path.</summary>
    public const string SectionName = "Storage:RustFs";

    /// <summary>Gets or sets the RustFS S3 HTTP endpoint.</summary>
    [Required]
    public string Endpoint { get; set; } = "http://localhost:9000";

    /// <summary>Gets or sets the S3 bucket name.</summary>
    [Required]
    public string BucketName { get; set; } = "ruth";

    /// <summary>Gets or sets the S3 access key.</summary>
    public string AccessKey { get; set; } = "rustfsadmin";

    /// <summary>Gets or sets the S3 secret key.</summary>
    public string SecretKey { get; set; } = "rustfsadmin";

    /// <summary>Gets or sets the AWS SigV4 region identifier.</summary>
    public string Region { get; set; } = "us-east-1";

    /// <summary>Gets or sets whether the provider automatically creates the configured bucket if missing.</summary>
    public bool AutoCreateBucket { get; set; } = true;
}
