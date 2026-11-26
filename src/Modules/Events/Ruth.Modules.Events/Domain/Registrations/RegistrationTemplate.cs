using Ruth.Modules.Events.Contracts.Registrations;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;

namespace Ruth.Modules.Events.Domain.Registrations;

/// <summary>Registration template aggregate governing dynamic forms, fees, discounts, deposits, waitlists, and group placement.</summary>
public sealed class RegistrationTemplate
    : AggregateRoot<RegistrationTemplateId>,
        IAuditableEntity,
        IHasConcurrencyToken,
        ISecured
{
    public const string EntityTypeKeyName = "Ruth.Events.RegistrationTemplate";

    private readonly List<RegistrationTemplateForm> _forms = [];
    private readonly List<RegistrationTemplateFee> _fees = [];
    private readonly List<RegistrationTemplateDiscount> _discounts = [];
    private readonly List<RegistrationTemplatePlacement> _groupPlacements = [];

    private RegistrationTemplate() { }

    private RegistrationTemplate(RegistrationTemplateId id)
        : base(id) { }

    public string Name { get; private set; } = string.Empty;
    public Guid FinancialAccountId { get; private set; }
    public Guid? CategoryId { get; private set; }
    public Guid? DefaultFinancialGatewayId { get; private set; }
    public string CurrencyCode { get; private set; } = "USD";
    public decimal BaseCostPerRegistrant { get; private set; }
    public decimal MinimumInitialDepositPerRegistrant { get; private set; }
    public RegistrationPaymentModel PaymentModel { get; private set; }
    public bool WaitlistEnabled { get; private set; } = true;
    public int SeatHoldTtlMinutes { get; private set; } = 15;
    public int MaxRegistrantsPerSubmission { get; private set; } = 10;
    public bool IsActive { get; private set; } = true;

    public IReadOnlyCollection<RegistrationTemplateForm> Forms => _forms.AsReadOnly();
    public IReadOnlyCollection<RegistrationTemplateFee> Fees => _fees.AsReadOnly();
    public IReadOnlyCollection<RegistrationTemplateDiscount> Discounts => _discounts.AsReadOnly();
    public IReadOnlyCollection<RegistrationTemplatePlacement> GroupPlacements =>
        _groupPlacements.AsReadOnly();

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

    public static Result<RegistrationTemplate> Create(RegistrationTemplateSpec spec)
    {
        ArgumentNullException.ThrowIfNull(spec);
        var template = new RegistrationTemplate(RegistrationTemplateId.Create());
        var updated = template.Update(spec);
        return updated.IsFailure ? Result.Failure<RegistrationTemplate>(updated.Errors) : template;
    }

    public Result Update(RegistrationTemplateSpec spec)
    {
        ArgumentNullException.ThrowIfNull(spec);
        if (!IsValidCoreSpec(spec))
        {
            return EventsRegistrationErrors.TemplateInvalid;
        }

        var childrenResult = RebuildChildren(spec);
        if (childrenResult.IsFailure)
        {
            return childrenResult;
        }

        Name = spec.Name.Trim();
        FinancialAccountId = spec.FinancialAccountId;
        CategoryId = spec.CategoryId is { } cat && cat != Guid.Empty ? cat : null;
        DefaultFinancialGatewayId =
            spec.DefaultFinancialGatewayId is { } gw && gw != Guid.Empty ? gw : null;
        CurrencyCode = spec.CurrencyCode.Trim().ToUpperInvariant();
        BaseCostPerRegistrant = decimal.Round(
            spec.BaseCostPerRegistrant,
            2,
            MidpointRounding.AwayFromZero
        );
        MinimumInitialDepositPerRegistrant = decimal.Round(
            spec.MinimumInitialDepositPerRegistrant,
            2,
            MidpointRounding.AwayFromZero
        );
        PaymentModel = spec.PaymentModel;
        WaitlistEnabled = spec.WaitlistEnabled;
        SeatHoldTtlMinutes = spec.SeatHoldTtlMinutes;
        MaxRegistrantsPerSubmission = spec.MaxRegistrantsPerSubmission;
        IsActive = spec.IsActive;
        return Result.Success();
    }

    private static bool IsValidCoreSpec(RegistrationTemplateSpec spec)
    {
        return IsValidIdentityAndCurrency(spec) && IsValidPricingAndLimits(spec);
    }

    private static bool IsValidIdentityAndCurrency(RegistrationTemplateSpec spec)
    {
        var currency = spec.CurrencyCode?.Trim() ?? string.Empty;
        return !string.IsNullOrWhiteSpace(spec.Name)
            && spec.Name.Trim().Length <= 160
            && spec.FinancialAccountId != Guid.Empty
            && currency.Length == 3;
    }

    private static bool IsValidPricingAndLimits(RegistrationTemplateSpec spec)
    {
        return spec.BaseCostPerRegistrant >= 0m
            && spec.MinimumInitialDepositPerRegistrant >= 0m
            && spec.MinimumInitialDepositPerRegistrant <= spec.BaseCostPerRegistrant
            && spec.SeatHoldTtlMinutes is >= 1 and <= 120
            && spec.MaxRegistrantsPerSubmission is >= 1 and <= 100;
    }

    private Result RebuildChildren(RegistrationTemplateSpec spec)
    {
        var formsRes = BuildForms(spec.Forms);
        if (formsRes.IsFailure)
        {
            return formsRes;
        }

        var feesRes = BuildFees(spec.Fees);
        if (feesRes.IsFailure)
        {
            return feesRes;
        }

        var discRes = BuildDiscounts(spec.Discounts);
        if (discRes.IsFailure)
        {
            return discRes;
        }

        var placeRes = BuildPlacements(spec.GroupPlacements);
        if (placeRes.IsFailure)
        {
            return placeRes;
        }

        _forms.Clear();
        _forms.AddRange(formsRes.Value);
        _fees.Clear();
        _fees.AddRange(feesRes.Value);
        _discounts.Clear();
        _discounts.AddRange(discRes.Value);
        _groupPlacements.Clear();
        _groupPlacements.AddRange(placeRes.Value);
        return Result.Success();
    }

    private Result<List<RegistrationTemplateForm>> BuildForms(
        IReadOnlyList<RegistrationFormSpec>? specs
    )
    {
        var forms = new List<RegistrationTemplateForm>();
        if (specs is null)
        {
            return forms;
        }

        foreach (var formSpec in specs)
        {
            var f = RegistrationTemplateForm.Create(Id, formSpec);
            if (f.IsFailure)
            {
                return Result.Failure<List<RegistrationTemplateForm>>(f.Errors);
            }

            forms.Add(f.Value);
        }

        return forms;
    }

    private Result<List<RegistrationTemplateFee>> BuildFees(
        IReadOnlyList<RegistrationFeeSpec>? specs
    )
    {
        var fees = new List<RegistrationTemplateFee>();
        if (specs is null)
        {
            return fees;
        }

        foreach (var feeSpec in specs)
        {
            var fee = RegistrationTemplateFee.Create(Id, feeSpec);
            if (fee.IsFailure)
            {
                return Result.Failure<List<RegistrationTemplateFee>>(fee.Errors);
            }

            fees.Add(fee.Value);
        }

        return fees;
    }

    private Result<List<RegistrationTemplateDiscount>> BuildDiscounts(
        IReadOnlyList<RegistrationDiscountSpec>? specs
    )
    {
        var discounts = new List<RegistrationTemplateDiscount>();
        if (specs is null)
        {
            return discounts;
        }

        var codes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var discSpec in specs)
        {
            var disc = RegistrationTemplateDiscount.Create(Id, discSpec);
            if (disc.IsFailure || !codes.Add(disc.Value.Code))
            {
                return EventsRegistrationErrors.TemplateInvalid;
            }

            discounts.Add(disc.Value);
        }

        return discounts;
    }

    private Result<List<RegistrationTemplatePlacement>> BuildPlacements(
        IReadOnlyList<RegistrationGroupPlacementSpec>? specs
    )
    {
        var placements = new List<RegistrationTemplatePlacement>();
        if (specs is null)
        {
            return placements;
        }

        foreach (var placeSpec in specs)
        {
            var place = RegistrationTemplatePlacement.Create(Id, placeSpec);
            if (place.IsFailure)
            {
                return Result.Failure<List<RegistrationTemplatePlacement>>(place.Errors);
            }

            placements.Add(place.Value);
        }

        return placements;
    }
}
