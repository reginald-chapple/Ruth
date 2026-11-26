using Ruth.SharedKernel.Events;

namespace Ruth.Modules.Identity.Contracts.Authentication;

/// <summary>Outbox integration event published when a password reset token is issued for an active user account.</summary>
/// <param name="UserAccountId">The user account identifier.</param>
/// <param name="Email">The account email address.</param>
/// <param name="UserName">The account user name.</param>
/// <param name="ResetToken">The issued opaque password reset token.</param>
/// <param name="ExpiresOnUtc">The UTC expiration timestamp of the reset token.</param>
/// <param name="OccurredOnUtc">The UTC timestamp when the token was issued.</param>
public sealed record PasswordResetRequestedIntegrationEvent(
    Guid UserAccountId,
    string Email,
    string UserName,
    string ResetToken,
    DateTimeOffset ExpiresOnUtc,
    DateTimeOffset OccurredOnUtc
) : IntegrationEvent(OccurredOnUtc);
