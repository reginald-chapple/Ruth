using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Events.Domain.Registrations;

/// <summary>Persisted fee item selection for an individual RegistrationRegistrant.</summary>
public sealed class RegistrationRegistrantFeeSelection
    : Entity<RegistrationRegistrantFeeSelectionId>
{
    private RegistrationRegistrantFeeSelection() { }

    internal RegistrationRegistrantFeeSelection(
        RegistrationRegistrantFeeSelectionId id,
        RegistrationRegistrantId registrantId,
        Guid feeId,
        Guid feeItemId,
        string label,
        int quantity,
        decimal unitAmount
    )
        : base(id)
    {
        RegistrantId = registrantId;
        FeeId = feeId;
        FeeItemId = feeItemId;
        Label = label;
        Quantity = quantity;
        UnitAmount = decimal.Round(unitAmount, 2, MidpointRounding.AwayFromZero);
        LineTotal = decimal.Round(unitAmount * quantity, 2, MidpointRounding.AwayFromZero);
    }

    public RegistrationRegistrantId RegistrantId { get; private set; }
    public Guid FeeId { get; private set; }
    public Guid FeeItemId { get; private set; }
    public string Label { get; private set; } = string.Empty;
    public int Quantity { get; private set; }
    public decimal UnitAmount { get; private set; }
    public decimal LineTotal { get; private set; }
}
