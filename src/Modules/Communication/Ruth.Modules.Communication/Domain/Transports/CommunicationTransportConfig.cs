using System.Text.RegularExpressions;
using Ruth.Modules.Communication.Contracts.Transports;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;

namespace Ruth.Modules.Communication.Domain.Transports;

/// <summary>
/// Per-tenant communication transport credential and sender configuration aggregate.
/// Credentials are persisted exclusively via AES-256-GCM per-tenant AAD-bound envelope encryption.
/// </summary>
public sealed partial class CommunicationTransportConfig
    : AggregateRoot<CommunicationTransportConfigId>,
        IAuditableEntity,
        IHasConcurrencyToken,
        ISecured
{
    /// <summary>Canonical entity type key for ACL evaluation.</summary>
    public const string EntityTypeKeyName = "Ruth.Communication.CommunicationTransportConfig";

    private Dictionary<string, string> _credentials = new(StringComparer.OrdinalIgnoreCase);

    private CommunicationTransportConfig() { }

    private CommunicationTransportConfig(CommunicationTransportConfigId id)
        : base(id) { }

    /// <summary>Gets the canonical lowercase transport key (e.g., smtp, sendgrid, mailgun, twilio-sms).</summary>
    public string TransportKey { get; private set; } = string.Empty;

    /// <summary>Gets the communication channel served by this transport configuration.</summary>
    public CommunicationChannel Channel { get; private set; }

    /// <summary>Gets whether this tenant transport configuration is enabled.</summary>
    public bool IsEnabled { get; private set; } = true;

    /// <summary>Gets the optional default sender address or E.164 number override.</summary>
    public string? DefaultFromAddress { get; private set; }

    /// <summary>Gets the optional default sender display name override.</summary>
    public string? DefaultFromName { get; private set; }

    /// <summary>Gets the decrypted in-memory provider credentials map (envelope-encrypted at rest in PostgreSQL).</summary>
    public IReadOnlyDictionary<string, string> Credentials
    {
        get => _credentials;
        private set =>
            _credentials = new Dictionary<string, string>(value, StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public DateTimeOffset CreatedOnUtc { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset? ModifiedOnUtc { get; private set; }

    /// <inheritdoc />
    public Guid ConcurrencyStamp { get; private set; } = Guid.CreateVersion7();

    /// <inheritdoc />
    public string EntityTypeKey => EntityTypeKeyName;

    /// <inheritdoc />
    public Guid EntityId => Id.Value;

    /// <inheritdoc />
    public ISecured ParentAuthority => GlobalRestrictedDefaultSecured.Instance;

    /// <inheritdoc />
    public bool IsAllowedByDefault(string action)
    {
        return GlobalRestrictedDefaultSecured.Instance.IsAllowedByDefault(action);
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

    /// <summary>Creates a validated per-tenant communication transport configuration.</summary>
    public static Result<CommunicationTransportConfig> Create(
        string transportKey,
        CommunicationChannel channel,
        bool isEnabled,
        string? defaultFromAddress,
        string? defaultFromName,
        IReadOnlyDictionary<string, string>? credentials
    )
    {
        var normalizedKey = NormalizeTransportKey(transportKey);
        if (normalizedKey is null)
        {
            return DomainError.Validation(
                "Communication.TransportKeyInvalid",
                "TransportKey must be a valid lowercase identifier."
            );
        }

        var entity = new CommunicationTransportConfig(CommunicationTransportConfigId.Create())
        {
            TransportKey = normalizedKey,
            Channel = channel,
        };
        var updated = entity.Update(
            channel,
            isEnabled,
            defaultFromAddress,
            defaultFromName,
            credentials
        );
        return updated.IsFailure
            ? Result.Failure<CommunicationTransportConfig>(updated.Errors)
            : entity;
    }

    /// <summary>Updates mutable settings and credentials on this transport configuration.</summary>
    public Result Update(
        CommunicationChannel channel,
        bool isEnabled,
        string? defaultFromAddress,
        string? defaultFromName,
        IReadOnlyDictionary<string, string>? credentials
    )
    {
        if (
            (defaultFromAddress is not null && ContainsControlChars(defaultFromAddress))
            || (defaultFromName is not null && ContainsControlChars(defaultFromName))
        )
        {
            return DomainError.Validation(
                "Communication.TransportSenderInvalid",
                "Sender fields cannot contain CR/LF or control characters."
            );
        }

        Channel = channel;
        IsEnabled = isEnabled;
        DefaultFromAddress = string.IsNullOrWhiteSpace(defaultFromAddress)
            ? null
            : defaultFromAddress.Trim();
        DefaultFromName = string.IsNullOrWhiteSpace(defaultFromName)
            ? null
            : defaultFromName.Trim();
        ReplaceCredentials(credentials);
        return Result.Success();
    }

    private void ReplaceCredentials(IReadOnlyDictionary<string, string>? credentials)
    {
        _credentials.Clear();
        if (credentials is null)
        {
            return;
        }

        foreach (var pair in credentials)
        {
            if (!string.IsNullOrWhiteSpace(pair.Key) && !string.IsNullOrEmpty(pair.Value))
            {
                _credentials[pair.Key.Trim()] = pair.Value.Trim();
            }
        }
    }

    private static string? NormalizeTransportKey(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var normalized = raw.Trim().ToLowerInvariant();
        return TransportKeyRegex.IsMatch(normalized) ? normalized : null;
    }

    private static bool ContainsControlChars(string value)
    {
        foreach (var ch in value)
        {
            if (char.IsControl(ch))
            {
                return true;
            }
        }

        return false;
    }

    [GeneratedRegex(
        @"^[a-z0-9][a-z0-9_-]{1,63}$",
        RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
        matchTimeoutMilliseconds: 250
    )]
    private static partial Regex TransportKeyRegex { get; }
}
