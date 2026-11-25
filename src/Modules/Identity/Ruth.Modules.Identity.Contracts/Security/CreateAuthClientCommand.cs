using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Identity.Contracts.Security;

/// <summary>Registers a new OAuth/OIDC client.</summary>
public sealed record CreateAuthClientCommand(
    string Name,
    string ClientId,
    string? ClientSecret,
    string RedirectUri,
    string PostLogoutRedirectUri,
    string AllowedScopes,
    string AllowedClaims,
    bool AllowUserApiAccess,
    int ScopeApprovalExpirationDays = 365
) : ICommand<AuthClientResponse>;
