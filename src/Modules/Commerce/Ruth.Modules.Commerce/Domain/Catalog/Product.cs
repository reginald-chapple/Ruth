using System.Text.RegularExpressions;
using Ruth.Modules.Commerce.Contracts.Catalog;
using Ruth.SharedKernel.Attributes;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;

namespace Ruth.Modules.Commerce.Domain.Catalog;

/// <summary>Commerce catalog product aggregate owning purchasable SKU variants, fulfillment options, and IRS quid-pro-quo split settings.</summary>
public sealed partial class Product
    : AggregateRoot<ProductId>,
        IAuditableEntity,
        IHasConcurrencyToken,
        ISecured,
        IHasAttributes
{
    public const string EntityTypeKeyName = "Ruth.Commerce.Product";

    private readonly List<ProductVariant> _variants = [];
    private Dictionary<string, string?> _attributeValues = new(StringComparer.OrdinalIgnoreCase);

    private Product() { }

    private Product(ProductId id)
        : base(id) { }

    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string? Summary { get; private set; }
    public string? Description { get; private set; }
    public ProductKind Kind { get; private set; }
    public Guid? CategoryId { get; private set; }
    public Guid? BranchId { get; private set; }
    public Guid? ImageBinaryFileId { get; private set; }
    public string CurrencyCode { get; private set; } = "USD";
    public Guid RevenueAccountId { get; private set; }
    public bool IsQuidProQuo { get; private set; }
    public Guid? QuidProQuoCharitableAccountId { get; private set; }
    public bool IsTaxable { get; private set; }
    public decimal FlatTaxRatePercent { get; private set; }
    public bool AllowLocalPickup { get; private set; } = true;
    public bool AllowFlatRateShipping { get; private set; }
    public decimal FlatShippingFeeAmount { get; private set; }
    public Guid? DefaultFinancialGatewayId { get; private set; }
    public bool IsActive { get; private set; } = true;
    public bool IsPublic { get; private set; } = true;

    public IReadOnlyCollection<ProductVariant> Variants => _variants.AsReadOnly();

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

    public static Result<Product> Create(ProductSpec spec)
    {
        ArgumentNullException.ThrowIfNull(spec);
        var product = new Product(ProductId.Create());
        var res = product.Update(spec);
        return res.IsFailure ? Result.Failure<Product>(res.Errors) : product;
    }

    public Result Update(ProductSpec spec)
    {
        ArgumentNullException.ThrowIfNull(spec);

        var slug = spec.Slug?.Trim().ToLowerInvariant() ?? string.Empty;
        var currency = spec.CurrencyCode?.Trim().ToUpperInvariant() ?? string.Empty;
        if (!ValidateProductHeader(spec, slug, currency))
        {
            return CommerceErrors.ProductInvalid;
        }

        if (!ValidateQuidProQuoSettings(spec))
        {
            return CommerceErrors.QuidProQuoAccountInvalid;
        }

        var variantsRes = ReconcileVariants(spec.Kind, spec.IsQuidProQuo, spec.Variants);
        if (variantsRes.IsFailure)
        {
            return variantsRes;
        }

        ApplyScalarFields(spec, slug, currency);
        ApplyAttributes(spec.Attributes);
        return Result.Success();
    }

    private void ApplyScalarFields(ProductSpec spec, string slug, string currency)
    {
        Name = spec.Name.Trim();
        Slug = slug;
        Summary = TrimOptional(spec.Summary, 500);
        Description = TrimOptional(spec.Description, 4000);
        Kind = spec.Kind;
        CategoryId = NormalizeGuid(spec.CategoryId);
        BranchId = NormalizeGuid(spec.BranchId);
        ImageBinaryFileId = NormalizeGuid(spec.ImageBinaryFileId);
        CurrencyCode = currency;
        RevenueAccountId = spec.RevenueAccountId;
        IsQuidProQuo = spec.IsQuidProQuo;
        QuidProQuoCharitableAccountId = spec.IsQuidProQuo
            ? NormalizeGuid(spec.QuidProQuoCharitableAccountId)
            : null;
        IsTaxable = spec.IsTaxable;
        FlatTaxRatePercent = spec.IsTaxable
            ? decimal.Round(spec.FlatTaxRatePercent, 4, MidpointRounding.AwayFromZero)
            : 0m;
        AllowLocalPickup =
            spec.Kind == ProductKind.PhysicalPickupOrFlatRate && spec.AllowLocalPickup;
        AllowFlatRateShipping =
            spec.Kind == ProductKind.PhysicalPickupOrFlatRate && spec.AllowFlatRateShipping;
        FlatShippingFeeAmount = AllowFlatRateShipping
            ? decimal.Round(spec.FlatShippingFeeAmount, 2, MidpointRounding.AwayFromZero)
            : 0m;
        DefaultFinancialGatewayId = NormalizeGuid(spec.DefaultFinancialGatewayId);
        IsActive = spec.IsActive;
        IsPublic = spec.IsPublic;
    }

    private Result ReconcileVariants(
        ProductKind kind,
        bool isQuidProQuo,
        IReadOnlyList<ProductVariantSpec> specs
    )
    {
        if (specs is null || specs.Count == 0)
        {
            return CommerceErrors.ProductInvalid;
        }

        var seenSkus = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var updatedList = new List<ProductVariant>(specs.Count);

        foreach (var vSpec in specs)
        {
            var skuKey = vSpec.Sku?.Trim().ToUpperInvariant() ?? string.Empty;
            if (!seenSkus.Add(skuKey))
            {
                return CommerceErrors.ProductSlugOrSkuConflict;
            }

            var target = FindOrCreateVariant(vSpec);
            var applied = target.ApplySpec(kind, isQuidProQuo, vSpec);
            if (applied.IsFailure)
            {
                return applied;
            }

            updatedList.Add(target);
        }

        _variants.Clear();
        _variants.AddRange(updatedList);
        return Result.Success();
    }

    private ProductVariant FindOrCreateVariant(ProductVariantSpec spec)
    {
        if (spec.VariantId is { } existingId && existingId != Guid.Empty)
        {
            var found = _variants.Find(v => v.Id.Value == existingId);
            if (found is not null)
            {
                return found;
            }
        }

        return new ProductVariant(ProductVariantId.Create(), Id);
    }

    private static bool ValidateProductHeader(ProductSpec spec, string slug, string currency)
    {
        return HasValidProductIdentity(spec, slug, currency) && HasValidTaxAndFulfillment(spec);
    }

    private static bool HasValidProductIdentity(ProductSpec spec, string slug, string currency)
    {
        return !string.IsNullOrWhiteSpace(spec.Name)
            && spec.Name.Trim().Length <= 200
            && SlugRegex.IsMatch(slug)
            && CurrencyRegex.IsMatch(currency)
            && spec.RevenueAccountId != Guid.Empty;
    }

    private static bool HasValidTaxAndFulfillment(ProductSpec spec)
    {
        var physicalFulfillmentOk =
            spec.Kind != ProductKind.PhysicalPickupOrFlatRate
            || spec.AllowLocalPickup
            || spec.AllowFlatRateShipping;

        return spec.FlatTaxRatePercent is >= 0m and <= 100m
            && spec.FlatShippingFeeAmount >= 0m
            && physicalFulfillmentOk;
    }

    private static bool ValidateQuidProQuoSettings(ProductSpec spec)
    {
        return !spec.IsQuidProQuo
            || (
                spec.QuidProQuoCharitableAccountId is { } charId
                && charId != Guid.Empty
                && charId != spec.RevenueAccountId
            );
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
