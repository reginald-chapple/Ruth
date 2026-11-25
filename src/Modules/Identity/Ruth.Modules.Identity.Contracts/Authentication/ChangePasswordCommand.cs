using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Identity.Contracts.Authentication;

/// <summary>Changes the password for an authenticated user account after verifying the current password.</summary>
public sealed record ChangePasswordCommand(
    Guid UserAccountId,
    string CurrentPassword,
    string NewPassword
) : ICommand<bool>;
