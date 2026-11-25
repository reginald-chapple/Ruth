using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Identity.Contracts.Authentication;

/// <summary>Creates a new user account (administrator-provisioned after initial onboarding).</summary>
public sealed record CreateUserAccountCommand(
    string UserName,
    string Email,
    string DisplayName,
    string Password,
    bool IsConfirmed = true,
    IReadOnlyList<Guid>? RoleIds = null
) : ICommand<UserAccountSummaryResponse>;
