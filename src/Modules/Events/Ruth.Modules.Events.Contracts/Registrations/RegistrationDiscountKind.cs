namespace Ruth.Modules.Events.Contracts.Registrations;

/// <summary>Calculation kind for a promo or early-bird registration discount.</summary>
public enum RegistrationDiscountKind
{
    /// <summary>Percentage discount between 0 and 100.</summary>
    Percentage = 0,

    /// <summary>Fixed monetary discount per confirmed registrant.</summary>
    FixedAmountPerRegistrant = 1,
}
