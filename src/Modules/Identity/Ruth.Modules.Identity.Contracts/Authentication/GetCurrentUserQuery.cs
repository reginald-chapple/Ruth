using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Identity.Contracts.Authentication;

/// <summary>Retrieves profile and role details for an authenticated user account.</summary>
public sealed record GetCurrentUserQuery(Guid UserAccountId) : IQuery<AuthenticatedUserResponse>;
