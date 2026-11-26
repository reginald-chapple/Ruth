using System.Text.RegularExpressions;
using Ruth.Modules.Finance.Contracts.Gateways;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;

namespace Ruth.Modules.Finance.Domain.Gateways;

/// <summary>Financial payment gateway configuration aggregate.</summary>
public sealed partial class FinancialGateway
    : AggregateRoot<FinancialGatewayId>,
        IAuditableEntity,
        IHasConcurrencyToken,
        ISecured
{
    /// <summary>Canonical entity type key for ACL evaluation.</summary>
    public const string EntityTypeKeyName = "Ruth.Finance.FinancialGateway";

    private Dictionary<string, string> _settings = new(StringComparer.OrdinalIgnoreCase);

    private FinancialGateway() { }

    private FinancialGateway(FinancialGatewayId id)
        : base(id) { }

    public string Name { get; private set; } = string.Empty;
    public string ProviderKey { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool SupportsCards { get; private set; } = true;
    public bool SupportsAch { get; private set; } = true;
    public bool SupportsRecurring { get; private set; } = true;
    public bool IsTestMode { get; private set; }
    public bool IsActive { get; private set; } = true;
    public int BatchTimeOffsetHours { get; private set; }
    public string? PublicTokenizerKey { get; private set; }

    public IReadOnlyDictionary<string, string> Settings
    {
        get => _settings;
        private set =>
            _settings = new Dictionary<string, string>(value, StringComparer.OrdinalIgnoreCase);
    }

    public DateTimeOffset CreatedOnUtc { get; private set; }
    public DateTimeOffset? ModifiedOnUtc { get; private set; }
    public Guid ConcurrencyStamp { get; private set; } = Guid.CreateVersion7();

    public string EntityTypeKey => EntityTypeKeyName;
    public Guid EntityId => Id.Value;
    public ISecured ParentAuthority => GlobalRestrictedDefaultSecured.Instance;

    public bool IsAllowedByDefault(string action)
    {
        return GlobalRestrictedDefaultSecured.Instance.IsAllowedByDefault(action);
    }

    public void SetCreated(DateTimeOffset createdOnUtc)
    {
        CreatedOnUtc = createdOnUtc.ToUniversalTime();
    }

    public void SetModified(DateTimeOffset modifiedOnUtc)
    {
        ModifiedOnUtc = modifiedOnUtc.ToUniversalTime();
    }

    public void SetConcurrencyStamp(Guid newStamp)
    {
        ConcurrencyStamp = newStamp;
    }

    /// <summary>Creates a validated <see cref="FinancialGateway" /> configuration aggregate.</summary>
    public static Result<FinancialGateway> Create(FinancialGatewaySpec spec)
    {
        ArgumentNullException.ThrowIfNull(spec);
        var gateway = new FinancialGateway(FinancialGatewayId.Create());
        var updated = gateway.Update(spec);
        return updated.IsFailure ? Result.Failure<FinancialGateway>(updated.Errors) : gateway;
    }

    /// <summary>Validates and updates mutable fields on this gateway.</summary>
    public Result Update(FinancialGatewaySpec spec)
    {
        ArgumentNullException.ThrowIfNull(spec);

        var validation = ValidateGatewaySpec(spec);
        if (validation.IsFailure)
        {
            return validation;
        }

        Name = spec.Name.Trim();
        ProviderKey = spec.ProviderKey.Trim().ToLowerInvariant();
        Description = TrimOptional(spec.Description);
        SupportsCards = spec.SupportsCards;
        SupportsAch = spec.SupportsAch;
        SupportsRecurring = spec.SupportsRecurring;
        IsTestMode = spec.IsTestMode;
        IsActive = spec.IsActive;
        BatchTimeOffsetHours = spec.BatchTimeOffsetHours;
        PublicTokenizerKey = TrimOptional(spec.PublicTokenizerKey);
        ReplaceSettings(spec.Settings);

        return Result.Success();
    }

    private static Result ValidateGatewaySpec(FinancialGatewaySpec spec)
    {
        return IsInvalidName(spec.Name) ? FinanceErrors.GatewayNameInvalid
            : !IsValidProviderKey(spec.ProviderKey) ? FinanceErrors.GatewayProviderKeyInvalid
            : spec.BatchTimeOffsetHours is < -12 or > 14 ? FinanceErrors.GatewayBatchOffsetInvalid
            : Result.Success();
    }

    private static bool IsInvalidName(string? name)
    {
        return string.IsNullOrWhiteSpace(name) || name.Trim().Length > 120;
    }

    private static bool IsValidProviderKey(string? providerKey)
    {
        var normalized = providerKey?.Trim().ToLowerInvariant() ?? string.Empty;
        return ProviderKeyRegex.IsMatch(normalized);
    }

    private static string? TrimOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private void ReplaceSettings(IReadOnlyDictionary<string, string>? settings)
    {
        _settings.Clear();
        if (settings is null)
        {
            return;
        }

        foreach (var pair in settings)
        {
            if (!string.IsNullOrWhiteSpace(pair.Key) && pair.Value is not null)
            {
                _settings[pair.Key.Trim()] = pair.Value.Trim();
            }
        }
    }

    [GeneratedRegex(
        @"^[a-z0-9][a-z0-9_-]{1,63}$",
        RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
        matchTimeoutMilliseconds: 250
    )]
    private static partial Regex ProviderKeyRegex { get; }
}
