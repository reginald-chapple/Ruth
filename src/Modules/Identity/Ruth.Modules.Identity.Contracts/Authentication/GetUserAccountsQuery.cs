using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Identity.Contracts.Authentication;

/// <summary>Queries user accounts optionally filtered by username, email, or display name.</summary>
public sealed record GetUserAccountsQuery(string? SearchTerm = null)
    : IQuery<IReadOnlyList<UserAccountSummaryResponse>>;
