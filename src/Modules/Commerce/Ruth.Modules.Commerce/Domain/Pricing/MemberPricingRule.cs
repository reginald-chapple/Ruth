using Ruth.Modules.Commerce.Contracts.Catalog;
using Ruth.Modules.Commerce.Contracts.Pricing;
using Ruth.Modules.Commerce.Domain.Catalog;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;

namespace Ruth.Modules.Commerce.Domain.Pricing;

/// <summary>Automatic member tier or group discount rule evaluated during storefront checkout.</summary>
public sealed class MemberPricingRule
    : AggregateRoot<MemberPricingRuleId>,
        IAuditableEntity,
        IHasConcurrencyToken,
        ISecured
{
    public const string EntityTypeKeyName = "Ruth.Commerce.MemberPricingRule";

    private MemberPricingRule() { }

    private MemberPricingRule(MemberPricingRuleId id)
        : base(id) { }

    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public MemberDiscountKind DiscountKind { get; private set; }
    public decimal DiscountValue { get; private set; }
    public string? RequiredMembershipTier { get; private set; }
    public Guid? RequiredGroupId { get; private set; }
    public Guid? RequiredGroupTypeId { get; private set; }
    public ProductId? AppliesToProductId { get; private set; }
    public ProductKind? AppliesToProductKind { get; private set; }
    public int Priority { get; private set; } = 100;
    public DateOnly? StartDate { get; private set; }
    public DateOnly? EndDate { get; private set; }
    public bool IsActive { get; private set; } = true;

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

    public static Result<MemberPricingRule> Create(MemberPricingRuleSpec spec)
    {
        ArgumentNullException.ThrowIfNull(spec);
        var rule = new MemberPricingRule(MemberPricingRuleId.Create());
        var updated = rule.Update(spec);
        return updated.IsFailure ? Result.Failure<MemberPricingRule>(updated.Errors) : rule;
    }

    public Result Update(MemberPricingRuleSpec spec)
    {
        ArgumentNullException.ThrowIfNull(spec);

        var tier = string.IsNullOrWhiteSpace(spec.RequiredMembershipTier)
            ? null
            : spec.RequiredMembershipTier.Trim();
        var groupId = NormalizeGuid(spec.RequiredGroupId);
        var groupTypeId = NormalizeGuid(spec.RequiredGroupTypeId);

        if (!IsValidSpec(spec, tier, groupId, groupTypeId))
        {
            return CommerceErrors.MemberPricingRuleInvalid;
        }

        Name = spec.Name.Trim();
        Description = string.IsNullOrWhiteSpace(spec.Description) ? null : spec.Description.Trim();
        DiscountKind = spec.DiscountKind;
        DiscountValue = decimal.Round(spec.DiscountValue, 2, MidpointRounding.AwayFromZero);
        RequiredMembershipTier = tier;
        RequiredGroupId = groupId;
        RequiredGroupTypeId = groupTypeId;
        AppliesToProductId = NormalizeGuid(spec.AppliesToProductId) is { } pId
            ? ProductId.From(pId)
            : null;
        AppliesToProductKind = spec.AppliesToProductKind;
        Priority = spec.Priority;
        StartDate = spec.StartDate;
        EndDate = spec.EndDate;
        IsActive = spec.IsActive;
        return Result.Success();
    }

    public bool IsEligibleForLine(
        Product product,
        IReadOnlySet<string> buyerTiers,
        IReadOnlySet<Guid> buyerGroupIds,
        IReadOnlySet<Guid> buyerGroupTypeIds,
        DateOnly today
    )
    {
        if (!IsActive || !IsWithinDates(today) || !MatchesProductTarget(product))
        {
            return false;
        }

        var tierOk = RequiredMembershipTier is null || buyerTiers.Contains(RequiredMembershipTier);
        var groupOk = RequiredGroupId is null || buyerGroupIds.Contains(RequiredGroupId.Value);
        var groupTypeOk =
            RequiredGroupTypeId is null || buyerGroupTypeIds.Contains(RequiredGroupTypeId.Value);

        return tierOk && groupOk && groupTypeOk;
    }

    public decimal CalculateUnitDiscount(decimal listUnitPrice)
    {
        if (listUnitPrice <= 0m)
        {
            return 0m;
        }

        var raw = DiscountKind switch
        {
            MemberDiscountKind.Percentage => listUnitPrice * (DiscountValue / 100m),
            MemberDiscountKind.FixedAmountPerUnit => DiscountValue,
            _ => 0m,
        };
        var rounded = decimal.Round(raw, 2, MidpointRounding.AwayFromZero);
        return Math.Clamp(rounded, 0m, listUnitPrice);
    }

    private bool IsWithinDates(DateOnly today)
    {
        return (StartDate is null || today >= StartDate.Value)
            && (EndDate is null || today <= EndDate.Value);
    }

    private bool MatchesProductTarget(Product product)
    {
        return (AppliesToProductId is null || AppliesToProductId.Value == product.Id)
            && (AppliesToProductKind is null || AppliesToProductKind.Value == product.Kind);
    }

    private static bool IsValidSpec(
        MemberPricingRuleSpec spec,
        string? tier,
        Guid? groupId,
        Guid? groupTypeId
    )
    {
        var hasCriterion = tier is not null || groupId.HasValue || groupTypeId.HasValue;
        var datesValid =
            spec.StartDate is null || spec.EndDate is null || spec.EndDate >= spec.StartDate;

        return !string.IsNullOrWhiteSpace(spec.Name)
            && spec.Name.Trim().Length <= 160
            && hasCriterion
            && IsValidDiscountAmount(spec.DiscountKind, spec.DiscountValue)
            && datesValid;
    }

    private static bool IsValidDiscountAmount(MemberDiscountKind kind, decimal value)
    {
        return kind switch
        {
            MemberDiscountKind.Percentage => value is > 0m and <= 100m,
            MemberDiscountKind.FixedAmountPerUnit => value > 0m,
            _ => false,
        };
    }

    private static Guid? NormalizeGuid(Guid? value)
    {
        return value is { } g && g != Guid.Empty ? g : null;
    }
}
