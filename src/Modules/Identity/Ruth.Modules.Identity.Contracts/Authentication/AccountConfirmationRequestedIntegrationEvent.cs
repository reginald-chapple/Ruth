using Ruth.SharedKernel.Events;

namespace Ruth.Modules.Identity.Contracts.Authentication;

/// <summary>Outbox integration event published when an account email confirmation token is issued.</summary>
/// <param name="UserAccountId">The user account identifier.</param>
/// <param name="Email">The account email address.</param>
/// <param name="UserName">The account user name.</param>
/// <param name="ConfirmationToken">The issued opaque account confirmation token.</param>
/// <param name="ExpiresOnUtc">The UTC expiration timestamp of the confirmation token.</param>
/// <param name="OccurredOnUtc">The UTC timestamp when the token was issued.</param>
public sealed record AccountConfirmationRequestedIntegrationEvent(
    Guid UserAccountId,
    string Email,
    string UserName,
    string ConfirmationToken,
    DateTimeOffset ExpiresOnUtc,
    DateTimeOffset OccurredOnUtc
) : IntegrationEvent(OccurredOnUtc);
