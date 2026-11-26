namespace Ruth.Modules.Events.Contracts.Registrations;

/// <summary>Read projection for an individual RegistrationRegistrant.</summary>
public sealed record RegistrationRegistrantResponse(
    Guid Id,
    Guid RegistrationId,
    Guid? PersonId,
    string FirstName,
    string LastName,
    string? Email,
    string? Phone,
    RegistrantStatus Status,
    bool IsOnWaitlist,
    int? WaitlistPosition,
    decimal BaseCost,
    decimal FeesTotal,
    decimal DiscountAmount,
    decimal NetCost,
    Guid? PlacedGroupId,
    Guid? PlacedGroupMemberId,
    IReadOnlyDictionary<string, string?> FieldValues,
    IReadOnlyList<RegistrantFeeSelectionResponse> FeeSelections,
    DateTimeOffset CreatedOnUtc
);
