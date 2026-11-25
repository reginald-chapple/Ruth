using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Identity.Contracts.Authentication;

/// <summary>Retrieves recent authentication attempts, optionally filtered to a single user account.</summary>
public sealed record GetLoginHistoryQuery(Guid? UserAccountId = null, int MaxCount = 100)
    : IQuery<IReadOnlyList<LoginHistoryItemResponse>>;
