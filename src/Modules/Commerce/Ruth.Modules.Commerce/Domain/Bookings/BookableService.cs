using System.Text.RegularExpressions;
using Ruth.Modules.Commerce.Contracts.Bookings;
using Ruth.SharedKernel.Attributes;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;

namespace Ruth.Modules.Commerce.Domain.Bookings;

/// <summary>Bookable 1-on-1 service appointment, coaching/counseling session, intake, or multi-seat workshop.</summary>
public sealed partial class BookableService
    : AggregateRoot<BookableServiceId>,
        IAuditableEntity,
        IHasConcurrencyToken,
        ISecured,
        IHasAttributes
{
    public const string EntityTypeKeyName = "Ruth.Commerce.BookableService";

    private List<Guid> _assignedProviderPersonIds = [];
    private Dictionary<string, string?> _attributeValues = new(StringComparer.OrdinalIgnoreCase);

    private BookableService() { }

    private BookableService(BookableServiceId id)
        : base(id) { }

    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string? Summary { get; private set; }
    public string? Description { get; private set; }
    public BookingDeliveryMode DeliveryMode { get; private set; }
    public int DurationMinutes { get; private set; }
    public int SlotIncrementMinutes { get; private set; }
    public int SetupBufferMinutes { get; private set; }
    public int TeardownBufferMinutes { get; private set; }
    public int MaxParticipantsPerSlot { get; private set; } = 1;
    public int MinimumLeadTimeMinutes { get; private set; }
    public int MaximumAdvanceDays { get; private set; } = 60;
    public int CancellationNoticeMinutes { get; private set; }
    public string CurrencyCode { get; private set; } = "USD";
    public decimal StandardPrice { get; private set; }
    public bool IsFreeForMembers { get; private set; }
    public decimal? MemberOverridePrice { get; private set; }
    public Guid? FreeOrDiscountedMemberGroupId { get; private set; }
    public string? FreeOrDiscountedMemberTier { get; private set; }
    public bool AllowPunchPassRedemption { get; private set; }
    public int PunchPassCreditsRequired { get; private set; } = 1;
    public Guid? RevenueAccountId { get; private set; }
    public Guid? DefaultFinancialGatewayId { get; private set; }
    public bool AllowInvoiceBilling { get; private set; }
    public Guid? BranchId { get; private set; }
    public Guid? DefaultLocationId { get; private set; }
    public bool IsActive { get; private set; } = true;
    public bool IsPublic { get; private set; } = true;

    public IReadOnlyList<Guid> AssignedProviderPersonIds
    {
        get => _assignedProviderPersonIds;
        private set => _assignedProviderPersonIds = [.. value];
    }

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

    public static Result<BookableService> Create(BookableServiceSpec spec)
    {
        ArgumentNullException.ThrowIfNull(spec);
        var service = new BookableService(BookableServiceId.Create());
        var res = service.Update(spec);
        return res.IsFailure ? Result.Failure<BookableService>(res.Errors) : service;
    }

    public Result Update(BookableServiceSpec spec)
    {
        ArgumentNullException.ThrowIfNull(spec);

        var slug = spec.Slug?.Trim().ToLowerInvariant() ?? string.Empty;
        var currency = spec.CurrencyCode?.Trim().ToUpperInvariant() ?? string.Empty;
        if (!HasValidHeaderAndTiming(spec, slug, currency) || !HasValidPricing(spec))
        {
            return CommerceErrors.BookableServiceInvalid;
        }

        ApplySpecFields(spec, slug, currency);
        ApplyAttributes(spec.Attributes);
        return Result.Success();
    }

    public bool RepointPerson(Guid retiredPersonId, Guid survivingPersonId)
    {
        if (!_assignedProviderPersonIds.Contains(retiredPersonId))
        {
            return false;
        }

        _assignedProviderPersonIds =
        [
            .. _assignedProviderPersonIds
                .Select(id => id == retiredPersonId ? survivingPersonId : id)
                .Distinct(),
        ];
        return true;
    }

    private void ApplySpecFields(BookableServiceSpec spec, string slug, string currency)
    {
        Name = spec.Name.Trim();
        Slug = slug;
        Summary = TrimOptional(spec.Summary, 500);
        Description = TrimOptional(spec.Description, 4000);
        DeliveryMode = spec.DeliveryMode;
        DurationMinutes = spec.DurationMinutes;
        SlotIncrementMinutes = spec.SlotIncrementMinutes;
        SetupBufferMinutes = spec.SetupBufferMinutes;
        TeardownBufferMinutes = spec.TeardownBufferMinutes;
        MaxParticipantsPerSlot = spec.MaxParticipantsPerSlot;
        MinimumLeadTimeMinutes = spec.MinimumLeadTimeMinutes;
        MaximumAdvanceDays = spec.MaximumAdvanceDays;
        CancellationNoticeMinutes = spec.CancellationNoticeMinutes;
        CurrencyCode = currency;
        StandardPrice = decimal.Round(spec.StandardPrice, 2, MidpointRounding.AwayFromZero);
        IsFreeForMembers = spec.IsFreeForMembers;
        MemberOverridePrice = spec.MemberOverridePrice.HasValue
            ? decimal.Round(spec.MemberOverridePrice.Value, 2, MidpointRounding.AwayFromZero)
            : null;
        FreeOrDiscountedMemberGroupId = NormalizeGuid(spec.FreeOrDiscountedMemberGroupId);
        FreeOrDiscountedMemberTier = TrimOptional(spec.FreeOrDiscountedMemberTier, 80);
        AllowPunchPassRedemption = spec.AllowPunchPassRedemption;
        PunchPassCreditsRequired = spec.AllowPunchPassRedemption
            ? spec.PunchPassCreditsRequired
            : 1;
        RevenueAccountId = NormalizeGuid(spec.RevenueAccountId);
        DefaultFinancialGatewayId = NormalizeGuid(spec.DefaultFinancialGatewayId);
        AllowInvoiceBilling = spec.AllowInvoiceBilling;
        BranchId = NormalizeGuid(spec.BranchId);
        DefaultLocationId = NormalizeGuid(spec.DefaultLocationId);
        IsActive = spec.IsActive;
        IsPublic = spec.IsPublic;
        _assignedProviderPersonIds = spec.AssignedProviderPersonIds is null
            ? []
            : [.. spec.AssignedProviderPersonIds.Where(static g => g != Guid.Empty).Distinct()];
    }

    private static bool HasValidHeaderAndTiming(
        BookableServiceSpec spec,
        string slug,
        string currency
    )
    {
        return HasValidIdentity(spec.Name, slug, currency)
            && HasValidDurations(spec)
            && HasValidCapacityAndHorizon(spec);
    }

    private static bool HasValidIdentity(string? name, string slug, string currency)
    {
        return !string.IsNullOrWhiteSpace(name)
            && name.Trim().Length <= 200
            && SlugRegex.IsMatch(slug)
            && CurrencyRegex.IsMatch(currency);
    }

    private static bool HasValidDurations(BookableServiceSpec spec)
    {
        return spec.DurationMinutes is >= 5 and <= 1440
            && spec.SlotIncrementMinutes is >= 5 and <= 480
            && spec.SetupBufferMinutes is >= 0 and <= 720
            && spec.TeardownBufferMinutes is >= 0 and <= 720;
    }

    private static bool HasValidCapacityAndHorizon(BookableServiceSpec spec)
    {
        return spec.MaxParticipantsPerSlot is >= 1 and <= 1000
            && spec.MinimumLeadTimeMinutes >= 0
            && spec.MaximumAdvanceDays is >= 1 and <= 730
            && spec.CancellationNoticeMinutes >= 0;
    }

    private static bool HasValidPricing(BookableServiceSpec spec)
    {
        var price = decimal.Round(spec.StandardPrice, 2, MidpointRounding.AwayFromZero);
        var memberPriceValid =
            !spec.MemberOverridePrice.HasValue
            || (spec.MemberOverridePrice.Value >= 0m && spec.MemberOverridePrice.Value <= price);
        var revenueAccountOk = price == 0m || NormalizeGuid(spec.RevenueAccountId).HasValue;

        return price >= 0m
            && memberPriceValid
            && revenueAccountOk
            && (!spec.AllowPunchPassRedemption || spec.PunchPassCreditsRequired >= 1);
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
