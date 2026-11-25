using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Contracts.Files;

/// <summary>Lists binary files filtered by type or temporary flag.</summary>
public sealed record GetBinaryFilesQuery(Guid? BinaryFileTypeId = null, bool? IsTemporary = null)
    : IQuery<IReadOnlyList<BinaryFileResponse>>;
