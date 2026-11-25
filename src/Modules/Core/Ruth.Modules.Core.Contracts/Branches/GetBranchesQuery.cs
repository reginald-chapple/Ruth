using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Contracts.Branches;

/// <summary>Lists branches ordered by display order and name.</summary>
public sealed record GetBranchesQuery(bool ActiveOnly = false, string? SearchTerm = null)
    : IQuery<IReadOnlyList<BranchResponse>>;
