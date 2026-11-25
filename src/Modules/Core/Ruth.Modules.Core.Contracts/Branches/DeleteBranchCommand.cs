using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Contracts.Branches;

/// <summary>Deletes a non-system branch unless it is the last remaining branch.</summary>
public sealed record DeleteBranchCommand(Guid BranchId) : ICommand<bool>;
