using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;

namespace Ruth.Modules.Identity.Domain;

/// <summary>Configuration aggregate for an external OpenID Connect identity provider.</summary>
public sealed class ExternalAuthProviderConfig
    : AggregateRoot<ExternalAuthProviderConfigId>,
        IAuditableEntity,
        IHasConcurrencyToken
{
    private ExternalAuthProviderConfig() { }

    private ExternalAuthProviderConfig(
        ExternalAuthProviderConfigId id,
        string providerKey,
        string displayName,
        string authority,
        string clientId,
        string? clientSecret,
        string scopes,
        bool isEnabled,
        bool autoLinkByEmail,
        string callbackPath
    )
        : base(id)
    {
        ProviderKey = providerKey;
        DisplayName = displayName;
        Authority = authority;
        ClientId = clientId;
        ClientSecret = clientSecret;
        Scopes = scopes;
        IsEnabled = isEnabled;
        AutoLinkByEmail = autoLinkByEmail;
        CallbackPath = callbackPath;
    }

    /// <summary>Gets the unique lowercase provider key (e.g., microsoft, google).</summary>
    public string ProviderKey { get; private set; } = string.Empty;

    /// <summary>Gets the human-readable provider display name.</summary>
    public string DisplayName { get; private set; } = string.Empty;

    /// <summary>Gets the OIDC issuer/authority HTTPS URL.</summary>
    public string Authority { get; private set; } = string.Empty;

    /// <summary>Gets the OIDC client identifier.</summary>
    public string ClientId { get; private set; } = string.Empty;

    /// <summary>Gets the optional OIDC client secret for confidential clients.</summary>
    public string? ClientSecret { get; private set; }

    /// <summary>Gets the space-delimited OIDC scopes (default openid profile email).</summary>
    public string Scopes { get; private set; } = "openid profile email";

    /// <summary>Gets a value indicating whether this provider is active for sign-in.</summary>
    public bool IsEnabled { get; private set; }

    /// <summary>Gets a value indicating whether verified IdP emails can auto-link existing accounts.</summary>
    public bool AutoLinkByEmail { get; private set; }

    /// <summary>Gets the callback path on Ruth Host.</summary>
    public string CallbackPath { get; private set; } = string.Empty;

    /// <inheritdoc />
    public DateTimeOffset CreatedOnUtc { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset? ModifiedOnUtc { get; private set; }

    /// <inheritdoc />
    public Guid ConcurrencyStamp { get; private set; } = Guid.CreateVersion7();

    /// <summary>Creates a validated external OIDC provider configuration.</summary>
    public static Result<ExternalAuthProviderConfig> Create(
        string providerKey,
        string displayName,
        string authority,
        string clientId,
        string? clientSecret,
        string scopes,
        bool isEnabled,
        bool autoLinkByEmail,
        string callbackPath
    )
    {
        var validation = ValidateInputs(
            providerKey,
            displayName,
            authority,
            clientId,
            callbackPath
        );
        if (validation.IsFailure)
        {
            return Result.Failure<ExternalAuthProviderConfig>(validation.Errors);
        }

        var normalizedScopes = string.IsNullOrWhiteSpace(scopes)
            ? "openid profile email"
            : scopes.Trim();
        return new ExternalAuthProviderConfig(
            ExternalAuthProviderConfigId.Create(),
            providerKey.Trim().ToLowerInvariant(),
            displayName.Trim(),
            authority.Trim().TrimEnd('/'),
            clientId.Trim(),
            string.IsNullOrWhiteSpace(clientSecret) ? null : clientSecret.Trim(),
            normalizedScopes,
            isEnabled,
            autoLinkByEmail,
            callbackPath.Trim()
        );
    }

    /// <summary>Updates provider settings.</summary>
    public Result Update(
        string displayName,
        string authority,
        string clientId,
        string? clientSecret,
        string scopes,
        bool isEnabled,
        bool autoLinkByEmail,
        string callbackPath
    )
    {
        var validation = ValidateInputs(
            ProviderKey,
            displayName,
            authority,
            clientId,
            callbackPath
        );
        if (validation.IsFailure)
        {
            return validation;
        }

        DisplayName = displayName.Trim();
        Authority = authority.Trim().TrimEnd('/');
        ClientId = clientId.Trim();
        if (clientSecret is not null)
        {
            ClientSecret = string.IsNullOrWhiteSpace(clientSecret) ? null : clientSecret.Trim();
        }

        Scopes = string.IsNullOrWhiteSpace(scopes) ? "openid profile email" : scopes.Trim();
        IsEnabled = isEnabled;
        AutoLinkByEmail = autoLinkByEmail;
        CallbackPath = callbackPath.Trim();
        return Result.Success();
    }

    private static Result ValidateInputs(
        string providerKey,
        string displayName,
        string authority,
        string clientId,
        string callbackPath
    )
    {
        return
            string.IsNullOrWhiteSpace(providerKey)
            || providerKey.Trim().Length > AuthenticationPolicy.ProviderKeyMaxLength
            || string.IsNullOrWhiteSpace(displayName)
            || displayName.Trim().Length > AuthenticationPolicy.ProviderDisplayNameMaxLength
            || string.IsNullOrWhiteSpace(clientId)
            || !SafeRedirectValidator.IsSafeRegisteredRedirectUri(callbackPath)
            || !Uri.TryCreate(authority?.Trim(), UriKind.Absolute, out var authorityUri)
            || (
                !string.Equals(
                    authorityUri.Scheme,
                    Uri.UriSchemeHttps,
                    StringComparison.OrdinalIgnoreCase
                )
                && !string.Equals(
                    authorityUri.Scheme,
                    Uri.UriSchemeHttp,
                    StringComparison.OrdinalIgnoreCase
                )
            )
            ? (Result)IdentityErrors.ExternalProviderInvalid
            : Result.Success();
    }

    /// <inheritdoc />
    public void SetCreated(DateTimeOffset createdOnUtc)
    {
        CreatedOnUtc = createdOnUtc.ToUniversalTime();
    }

    /// <inheritdoc />
    public void SetModified(DateTimeOffset modifiedOnUtc)
    {
        ModifiedOnUtc = modifiedOnUtc.ToUniversalTime();
    }

    /// <inheritdoc />
    public void SetConcurrencyStamp(Guid newStamp)
    {
        ConcurrencyStamp = newStamp;
    }
}
