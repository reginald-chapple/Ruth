using Ruth.Modules.Events.Contracts.Registrations;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Events.Domain.Registrations;

/// <summary>Selectable option item within a RegistrationTemplateFee.</summary>
public sealed class RegistrationTemplateFeeItem : Entity<RegistrationTemplateFeeItemId>
{
    private RegistrationTemplateFeeItem() { }

    private RegistrationTemplateFeeItem(
        RegistrationTemplateFeeItemId id,
        RegistrationTemplateFeeId feeId
    )
        : base(id)
    {
        FeeId = feeId;
    }

    public RegistrationTemplateFeeId FeeId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Label { get; private set; } = string.Empty;
    public decimal UnitAmount { get; private set; }
    public int? MaxQuantityAvailable { get; private set; }
    public int DisplayOrder { get; private set; }

    internal static Result<RegistrationTemplateFeeItem> Create(
        RegistrationTemplateFeeId feeId,
        RegistrationFeeItemSpec spec
    )
    {
        ArgumentNullException.ThrowIfNull(spec);

        var invalid =
            string.IsNullOrWhiteSpace(spec.Code)
            || spec.Code.Trim().Length > 64
            || string.IsNullOrWhiteSpace(spec.Label)
            || spec.Label.Trim().Length > 160
            || spec.UnitAmount < 0m
            || (spec.MaxQuantityAvailable.HasValue && spec.MaxQuantityAvailable.Value <= 0);

        return invalid
            ? EventsRegistrationErrors.TemplateInvalid
            : new RegistrationTemplateFeeItem(RegistrationTemplateFeeItemId.Create(), feeId)
            {
                Code = spec.Code.Trim().ToUpperInvariant(),
                Label = spec.Label.Trim(),
                UnitAmount = decimal.Round(spec.UnitAmount, 2, MidpointRounding.AwayFromZero),
                MaxQuantityAvailable = spec.MaxQuantityAvailable,
                DisplayOrder = spec.DisplayOrder,
            };
    }
}
