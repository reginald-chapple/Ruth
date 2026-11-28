namespace Ruth.SharedKernel.Security;

/// <summary>
/// Canonical named rate-limiting policy identifiers shared between endpoint registrations and the host pipeline.
/// </summary>
public static class RateLimitPolicyNames
{
    /// <summary>Strict brute-force throttling policy for login, token refresh, password recovery, OIDC, and signup routes.</summary>
    public const string AuthStrict = "ruth-auth-strict";

    /// <summary>Strict enumeration-resistance policy for Kiosk check-in search/session and public RSVP token routes.</summary>
    public const string KioskStrict = "ruth-kiosk-strict";
}
