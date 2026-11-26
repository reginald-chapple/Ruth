using System.Text.RegularExpressions;
using Ruth.Modules.Commerce.Contracts.Bookings;
using Ruth.SharedKernel.Attributes;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;

namespace Ruth.Modules.Commerce.Domain.Bookings;

/// <summary>Rentable physical facility room, court/field, studio, or pooled equipment item referencing a Core Location.</summary>
public sealed partial class RentableResource
    : AggregateRoot<RentableResourceId>,
        IAuditableEntity,
        IHasConcurrencyToken,
        ISecured,
        IHasAttributes
{
    public const string EntityTypeKeyName = "Ruth.Commerce.RentableResource";

    private Dictionary<string, string?> _attributeValues = new(StringComparer.OrdinalIgnoreCase);

    private RentableResource() { }

    private RentableResource(RentableResourceId id)
        : base(id) { }

    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public RentableResourceKind ResourceKind { get; private set; }
    public Guid CoreLocationId { get; private set; }
    public Guid? BranchId { get; private set; }
    public int QuantityAvailable { get; private set; } = 1;
    public int MinDurationMinutes { get; private set; }
    public int MaxDurationMinutes { get; private set; }
    public int SlotIncrementMinutes { get; private set; }
    public int SetupBufferMinutes { get; private set; }
    public int TeardownBufferMinutes { get; private set; }
    public int MinimumLeadTimeMinutes { get; private set; }
    public int MaximumAdvanceDays { get; private set; } = 90;
    public int CancellationNoticeMinutes { get; private set; }
    public string CurrencyCode { get; private set; } = "USD";
    public decimal HourlyRate { get; private set; }
    public decimal FlatCleaningOrSetupFee { get; private set; }
    public bool IsFreeForMembers { get; private set; }
    public decimal? MemberHourlyRate { get; private set; }
    public Guid? FreeOrDiscountedMemberGroupId { get; private set; }
    public string? FreeOrDiscountedMemberTier { get; private set; }
    public Guid? RevenueAccountId { get; private set; }
    public Guid? DefaultFinancialGatewayId { get; private set; }
    public bool AllowInvoiceBilling { get; private set; } = true;
    public bool IsActive { get; private set; } = true;
    public bool IsPublic { get; private set; } = true;

    public IReadOnlyDictionary<string, string?> AttributeValues
    {
        get => _attributeValues;
        private set =>
            _attributeValues = new Dictionary<string, string?>(
                value,
                StringComparer.OrdinalIgnoreCase
            );
    }

    public DateTimeOffset CreatedOnUtc { get; private set; }
    public DateTimeOffset? ModifiedOnUtc { get; private set; }
    public Guid ConcurrencyStamp { get; private set; } = Guid.CreateVersion7();

    public string EntityTypeKey => EntityTypeKeyName;
    public Guid EntityId => Id.Value;
    public ISecured ParentAuthority => GlobalDefaultSecured.Instance;

    public bool IsAllowedByDefault(string action)
    {
        return GlobalDefaultSecured.Instance.IsAllowedByDefault(action);
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

    public void SetAttributeValue(string key, string? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        _attributeValues[key.Trim()] = value;
    }

    public void RemoveAttributeValue(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        _ = _attributeValues.Remove(key.Trim());
    }

    public static Result<RentableResource> Create(RentableResourceSpec spec)
    {
        ArgumentNullException.ThrowIfNull(spec);
        var resource = new RentableResource(RentableResourceId.Create());
        var res = resource.Update(spec);
        return res.IsFailure ? Result.Failure<RentableResource>(res.Errors) : resource;
    }

    public Result Update(RentableResourceSpec spec)
    {
        ArgumentNullException.ThrowIfNull(spec);

        var slug = spec.Slug?.Trim().ToLowerInvariant() ?? string.Empty;
        var currency = spec.CurrencyCode?.Trim().ToUpperInvariant() ?? string.Empty;
        if (!HasValidHeaderAndTiming(spec, slug, currency) || !HasValidPricing(spec))
        {
            return CommerceErrors.RentableResourceInvalid;
        }

        ApplySpecFields(spec, slug, currency);
        ApplyAttributes(spec.Attributes);
        return Result.Success();
    }

    private void ApplySpecFields(RentableResourceSpec spec, string slug, string currency)
    {
        Name = spec.Name.Trim();
        Slug = slug;
        Description = TrimOptional(spec.Description, 4000);
        ResourceKind = spec.ResourceKind;
        CoreLocationId = spec.CoreLocationId;
        BranchId = NormalizeGuid(spec.BranchId);
        QuantityAvailable = spec.QuantityAvailable;
        MinDurationMinutes = spec.MinDurationMinutes;
        MaxDurationMinutes = spec.MaxDurationMinutes;
        SlotIncrementMinutes = spec.SlotIncrementMinutes;
        SetupBufferMinutes = spec.SetupBufferMinutes;
        TeardownBufferMinutes = spec.TeardownBufferMinutes;
        MinimumLeadTimeMinutes = spec.MinimumLeadTimeMinutes;
        MaximumAdvanceDays = spec.MaximumAdvanceDays;
        CancellationNoticeMinutes = spec.CancellationNoticeMinutes;
        CurrencyCode = currency;
        HourlyRate = decimal.Round(spec.HourlyRate, 2, MidpointRounding.AwayFromZero);
        FlatCleaningOrSetupFee = decimal.Round(
            spec.FlatCleaningOrSetupFee,
            2,
            MidpointRounding.AwayFromZero
        );
        IsFreeForMembers = spec.IsFreeForMembers;
        MemberHourlyRate = spec.MemberHourlyRate.HasValue
            ? decimal.Round(spec.MemberHourlyRate.Value, 2, MidpointRounding.AwayFromZero)
            : null;
        FreeOrDiscountedMemberGroupId = NormalizeGuid(spec.FreeOrDiscountedMemberGroupId);
        FreeOrDiscountedMemberTier = TrimOptional(spec.FreeOrDiscountedMemberTier, 80);
        RevenueAccountId = NormalizeGuid(spec.RevenueAccountId);
        DefaultFinancialGatewayId = NormalizeGuid(spec.DefaultFinancialGatewayId);
        AllowInvoiceBilling = spec.AllowInvoiceBilling;
        IsActive = spec.IsActive;
        IsPublic = spec.IsPublic;
    }

    private static bool HasValidHeaderAndTiming(
        RentableResourceSpec spec,
        string slug,
        string currency
    )
    {
        return HasValidIdentityAndLocation(spec, slug, currency)
            && HasValidDurationBounds(spec)
            && HasValidBuffersAndHorizon(spec);
    }

    private static bool HasValidIdentityAndLocation(
        RentableResourceSpec spec,
        string slug,
        string currency
    )
    {
        return !string.IsNullOrWhiteSpace(spec.Name)
            && spec.Name.Trim().Length <= 200
            && SlugRegex.IsMatch(slug)
            && CurrencyRegex.IsMatch(currency)
            && spec.CoreLocationId != Guid.Empty
            && spec.QuantityAvailable is >= 1 and <= 1000;
    }

    private static bool HasValidDurationBounds(RentableResourceSpec spec)
    {
        return spec.MinDurationMinutes is >= 15 and <= 1440
            && spec.MaxDurationMinutes >= spec.MinDurationMinutes
            && spec.MaxDurationMinutes <= 1440
            && spec.SlotIncrementMinutes is >= 5 and <= 480;
    }

    private static bool HasValidBuffersAndHorizon(RentableResourceSpec spec)
    {
        return spec.SetupBufferMinutes is >= 0 and <= 720
            && spec.TeardownBufferMinutes is >= 0 and <= 720
            && spec.MinimumLeadTimeMinutes >= 0
            && spec.MaximumAdvanceDays is >= 1 and <= 730
            && spec.CancellationNoticeMinutes >= 0;
    }

    private static bool HasValidPricing(RentableResourceSpec spec)
    {
        var hourly = decimal.Round(spec.HourlyRate, 2, MidpointRounding.AwayFromZero);
        var flat = decimal.Round(spec.FlatCleaningOrSetupFee, 2, MidpointRounding.AwayFromZero);
        var memberHourlyValid =
            !spec.MemberHourlyRate.HasValue
            || (spec.MemberHourlyRate.Value >= 0m && spec.MemberHourlyRate.Value <= hourly);
        var revenueAccountOk =
            (hourly == 0m && flat == 0m) || NormalizeGuid(spec.RevenueAccountId).HasValue;

        return hourly >= 0m && flat >= 0m && memberHourlyValid && revenueAccountOk;
    }

    private void ApplyAttributes(IReadOnlyDictionary<string, string?>? attributes)
    {
        if (attributes is null)
        {
            return;
        }

        _attributeValues.Clear();
        foreach (var (key, value) in attributes)
        {
            if (!string.IsNullOrWhiteSpace(key))
            {
                _attributeValues[key.Trim()] = value;
            }
        }
    }

    private static Guid? NormalizeGuid(Guid? value)
    {
        return value is { } g && g != Guid.Empty ? g : null;
    }

    private static string? TrimOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }

    [GeneratedRegex(
        @"^[a-z0-9][a-z0-9-]{1,95}$",
        RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
        matchTimeoutMilliseconds: 250
    )]
    private static partial Regex SlugRegex { get; }

    [GeneratedRegex(
        @"^[A-Z]{3}$",
        RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
        matchTimeoutMilliseconds: 250
    )]
    private static partial Regex CurrencyRegex { get; }
}
