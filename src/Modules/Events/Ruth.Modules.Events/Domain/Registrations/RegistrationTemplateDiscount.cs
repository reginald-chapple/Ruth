using Ruth.Modules.Events.Contracts.Registrations;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Events.Domain.Registrations;

/// <summary>Promo code or automatic early-bird discount rule on a RegistrationTemplate.</summary>
public sealed class RegistrationTemplateDiscount : Entity<RegistrationTemplateDiscountId>
{
    private RegistrationTemplateDiscount() { }

    private RegistrationTemplateDiscount(
        RegistrationTemplateDiscountId id,
        RegistrationTemplateId templateId
    )
        : base(id)
    {
        TemplateId = templateId;
    }

    public RegistrationTemplateId TemplateId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public RegistrationDiscountKind DiscountKind { get; private set; }
    public decimal DiscountValue { get; private set; }
    public DateOnly? StartDate { get; private set; }
    public DateOnly? EndDate { get; private set; }
    public int? MaxRegistrants { get; private set; }
    public int? MinRegistrants { get; private set; }
    public bool IsAutoApplyEarlyBird { get; private set; }

    internal static Result<RegistrationTemplateDiscount> Create(
        RegistrationTemplateId templateId,
        RegistrationDiscountSpec spec
    )
    {
        ArgumentNullException.ThrowIfNull(spec);

        var validRange =
            spec.StartDate is null || spec.EndDate is null || spec.EndDate >= spec.StartDate;
        var validVal =
            spec.DiscountValue > 0m
            && (
                spec.DiscountKind != RegistrationDiscountKind.Percentage
                || spec.DiscountValue <= 100m
            );
        var invalid =
            string.IsNullOrWhiteSpace(spec.Code)
            || spec.Code.Trim().Length > 48
            || !validRange
            || !validVal
            || (spec.MaxRegistrants.HasValue && spec.MaxRegistrants.Value <= 0)
            || (spec.MinRegistrants.HasValue && spec.MinRegistrants.Value <= 0);

        return invalid
            ? EventsRegistrationErrors.TemplateInvalid
            : new RegistrationTemplateDiscount(RegistrationTemplateDiscountId.Create(), templateId)
            {
                Code = spec.Code.Trim().ToUpperInvariant(),
                DiscountKind = spec.DiscountKind,
                DiscountValue = decimal.Round(spec.DiscountValue, 2, MidpointRounding.AwayFromZero),
                StartDate = spec.StartDate,
                EndDate = spec.EndDate,
                MaxRegistrants = spec.MaxRegistrants,
                MinRegistrants = spec.MinRegistrants,
                IsAutoApplyEarlyBird = spec.IsAutoApplyEarlyBird,
            };
    }

    public bool IsEligibleOn(DateOnly today, int confirmedCount, int alreadyUsedRegistrantCount)
    {
        var inWindow =
            (StartDate is null || today >= StartDate.Value)
            && (EndDate is null || today <= EndDate.Value);
        var meetsMin = !MinRegistrants.HasValue || confirmedCount >= MinRegistrants.Value;
        var withinCap =
            !MaxRegistrants.HasValue
            || alreadyUsedRegistrantCount + confirmedCount <= MaxRegistrants.Value;
        return inWindow && meetsMin && withinCap;
    }
}
