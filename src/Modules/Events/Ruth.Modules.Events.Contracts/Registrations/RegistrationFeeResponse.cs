namespace Ruth.Modules.Events.Contracts.Registrations;

/// <summary>Read projection for a fee group on a RegistrationTemplate.</summary>
public sealed record RegistrationFeeResponse(
    Guid Id,
    string Name,
    RegistrationFeeType FeeType,
    bool IsRequired,
    bool AllowDiscount,
    int DisplayOrder,
    IReadOnlyList<RegistrationFeeItemResponse> Items
);
