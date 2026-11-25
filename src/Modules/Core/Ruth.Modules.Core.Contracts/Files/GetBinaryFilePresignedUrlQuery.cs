using Ruth.Modules.Core.Contracts.Storage;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Contracts.Files;

/// <summary>Generates a time-bounded presigned download URL for a stored binary file.</summary>
public sealed record GetBinaryFilePresignedUrlQuery(Guid BinaryFileId, int ExpiresInMinutes = 15)
    : IQuery<StoragePresignedUrl>;
