namespace Ruth.Modules.Core.Contracts.Storage;

/// <summary>Time-bounded presigned URL for direct client download or upload.</summary>
public sealed record StoragePresignedUrl(Uri Url, DateTimeOffset ExpiresAtUtc, string HttpMethod);
