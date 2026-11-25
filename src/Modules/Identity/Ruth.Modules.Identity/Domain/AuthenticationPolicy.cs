namespace Ruth.Modules.Identity.Domain;

/// <summary>Authentication security policy constants for lockout, token TTLs, and field lengths.</summary>
public static class AuthenticationPolicy
{
    /// <summary>Maximum consecutive failed logins within <see cref="FailedAttemptWindow" /> before lockout.</summary>
    public const int MaxFailedPasswordAttempts = 5;

    /// <summary>Rolling window for counting failed password attempts.</summary>
    public static readonly TimeSpan FailedAttemptWindow = TimeSpan.FromMinutes(15);

    /// <summary>Duration of automatic account lockout after exceeding failed attempts.</summary>
    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    /// <summary>Default lifetime of password reset tokens.</summary>
    public static readonly TimeSpan PasswordResetTokenLifetime = TimeSpan.FromHours(1);

    /// <summary>Default lifetime of account confirmation tokens.</summary>
    public static readonly TimeSpan AccountConfirmationTokenLifetime = TimeSpan.FromHours(24);

    /// <summary>Default lifetime of remote OIDC authentication sessions.</summary>
    public static readonly TimeSpan RemoteAuthSessionLifetime = TimeSpan.FromMinutes(10);

    /// <summary>Maximum length of an external provider key.</summary>
    public const int ProviderKeyMaxLength = 64;

    /// <summary>Maximum length of an external provider display name.</summary>
    public const int ProviderDisplayNameMaxLength = 150;

    /// <summary>Maximum length of an authority or callback URI.</summary>
    public const int UrlMaxLength = 500;
}
