namespace Ruth.Modules.Identity.Domain;

/// <summary>Normalized identity profile returned by an external OpenID Connect provider.</summary>
public sealed record ExternalOidcUserInfo(
    string Subject,
    string? Email,
    bool EmailVerified,
    string? DisplayName
);
