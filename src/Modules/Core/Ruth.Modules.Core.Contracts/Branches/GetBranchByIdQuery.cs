using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Contracts.Branches;

/// <summary>Gets a single branch or site by identifier.</summary>
public sealed record GetBranchByIdQuery(Guid BranchId) : IQuery<BranchResponse>;
