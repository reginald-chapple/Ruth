using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Identity.Contracts.Authentication;

/// <summary>Completes a password reset using a valid single-use reset token.</summary>
public sealed record ConfirmPasswordResetCommand(string Token, string NewPassword) : ICommand<bool>;
