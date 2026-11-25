using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Contracts.Files;

/// <summary>Gets binary file metadata by identifier.</summary>
public sealed record GetBinaryFileByIdQuery(Guid BinaryFileId) : IQuery<BinaryFileResponse>;
