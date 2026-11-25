using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Identity.Domain;

/// <summary>Links an external OIDC identity subject to a Ruth <see cref="UserAccount" />.</summary>
public sealed class ExternalLoginLink : Entity<ExternalLoginLinkId>
{
    private ExternalLoginLink() { }

    private ExternalLoginLink(
        ExternalLoginLinkId id,
        UserAccountId userAccountId,
        string providerKey,
        string providerSubject,
        string? providerEmail,
        DateTimeOffset linkedOnUtc
    )
        : base(id)
    {
        UserAccountId = userAccountId;
        ProviderKey = providerKey;
        ProviderSubject = providerSubject;
        ProviderEmail = providerEmail;
        LinkedOnUtc = linkedOnUtc.ToUniversalTime();
    }

    /// <summary>Gets the Ruth user account identifier.</summary>
    public UserAccountId UserAccountId { get; private set; }

    /// <summary>Gets the configured provider key.</summary>
    public string ProviderKey { get; private set; } = string.Empty;

    /// <summary>Gets the immutable subject (sub) identifier from the external IdP.</summary>
    public string ProviderSubject { get; private set; } = string.Empty;

    /// <summary>Gets the email address returned by the external IdP when linked.</summary>
    public string? ProviderEmail { get; private set; }

    /// <summary>Gets the UTC timestamp when the link was established.</summary>
    public DateTimeOffset LinkedOnUtc { get; private set; }

    /// <summary>Creates a new external login link.</summary>
    public static ExternalLoginLink Create(
        UserAccountId userAccountId,
        string providerKey,
        string providerSubject,
        string? providerEmail,
        DateTimeOffset linkedOnUtc
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(providerSubject);

        return new ExternalLoginLink(
            ExternalLoginLinkId.Create(),
            userAccountId,
            providerKey.Trim().ToLowerInvariant(),
            providerSubject.Trim(),
            string.IsNullOrWhiteSpace(providerEmail) ? null : providerEmail.Trim(),
            linkedOnUtc
        );
    }
}
