using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Contracts.Files;

/// <summary>Lists configured binary file types.</summary>
public sealed record GetBinaryFileTypesQuery(string? SearchTerm = null)
    : IQuery<IReadOnlyList<BinaryFileTypeResponse>>;
