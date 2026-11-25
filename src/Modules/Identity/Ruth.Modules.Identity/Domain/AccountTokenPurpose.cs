namespace Ruth.Modules.Identity.Domain;

/// <summary>Purpose for a time-limited single-use <see cref="AccountToken" />.</summary>
public enum AccountTokenPurpose
{
    /// <summary>Password reset confirmation token.</summary>
    PasswordReset = 1,

    /// <summary>Email/account confirmation token.</summary>
    AccountConfirmation = 2,
}
