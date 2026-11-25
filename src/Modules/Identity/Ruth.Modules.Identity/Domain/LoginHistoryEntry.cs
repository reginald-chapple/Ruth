using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Identity.Domain;

/// <summary>Immutable audit record of a user authentication attempt.</summary>
public sealed class LoginHistoryEntry : Entity<LoginHistoryEntryId>
{
    private LoginHistoryEntry() { }

    private LoginHistoryEntry(
        LoginHistoryEntryId id,
        UserAccountId? userAccountId,
        string userNameAttempted,
        string authMethod,
        string? providerKey,
        bool succeeded,
        string? failureReason,
        string? ipAddress,
        string? userAgent,
        DateTimeOffset occurredOnUtc
    )
        : base(id)
    {
        UserAccountId = userAccountId;
        UserNameAttempted = userNameAttempted;
        AuthMethod = authMethod;
        ProviderKey = providerKey;
        Succeeded = succeeded;
        FailureReason = failureReason;
        IpAddress = ipAddress;
        UserAgent = userAgent;
        OccurredOnUtc = occurredOnUtc.ToUniversalTime();
    }

    /// <summary>Gets the matched user account identifier when known.</summary>
    public UserAccountId? UserAccountId { get; private set; }

    /// <summary>Gets the username or email attempted.</summary>
    public string UserNameAttempted { get; private set; } = string.Empty;

    /// <summary>Gets the authentication method (Password, RefreshToken, Oidc, ApiKey).</summary>
    public string AuthMethod { get; private set; } = string.Empty;

    /// <summary>Gets the external provider key when authenticating via OIDC.</summary>
    public string? ProviderKey { get; private set; }

    /// <summary>Gets a value indicating whether the login attempt succeeded.</summary>
    public bool Succeeded { get; private set; }

    /// <summary>Gets the failure reason code when unsuccessful.</summary>
    public string? FailureReason { get; private set; }

    /// <summary>Gets the client IP address when available.</summary>
    public string? IpAddress { get; private set; }

    /// <summary>Gets the client User-Agent header when available.</summary>
    public string? UserAgent { get; private set; }

    /// <summary>Gets the UTC timestamp when the attempt occurred.</summary>
    public DateTimeOffset OccurredOnUtc { get; private set; }

    /// <summary>Creates a new login history audit entry.</summary>
    public static LoginHistoryEntry Record(
        UserAccountId? userAccountId,
        string userNameAttempted,
        string authMethod,
        bool succeeded,
        DateTimeOffset occurredOnUtc,
        string? providerKey = null,
        string? failureReason = null,
        string? ipAddress = null,
        string? userAgent = null
    )
    {
        var safeUser = Truncate(userNameAttempted, 256) ?? string.Empty;
        var safeMethod = Truncate(authMethod, 50) ?? "Password";
        return new LoginHistoryEntry(
            LoginHistoryEntryId.Create(),
            userAccountId,
            safeUser,
            safeMethod,
            Truncate(providerKey, 64),
            succeeded,
            Truncate(failureReason, 200),
            Truncate(ipAddress, 64),
            Truncate(userAgent, 512),
            occurredOnUtc
        );
    }

    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }
}
