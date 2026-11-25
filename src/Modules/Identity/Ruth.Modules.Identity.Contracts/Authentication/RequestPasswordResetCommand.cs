using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Identity.Contracts.Authentication;

/// <summary>Issues a time-limited single-use password reset token for a matching user account.</summary>
public sealed record RequestPasswordResetCommand(string EmailOrUserName)
    : ICommand<IssuedTokenResponse>;
