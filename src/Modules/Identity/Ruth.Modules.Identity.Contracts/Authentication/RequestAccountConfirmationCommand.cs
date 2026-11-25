using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Identity.Contracts.Authentication;

/// <summary>Issues a single-use email/account confirmation token for a user account.</summary>
public sealed record RequestAccountConfirmationCommand(Guid UserAccountId)
    : ICommand<IssuedTokenResponse>;
