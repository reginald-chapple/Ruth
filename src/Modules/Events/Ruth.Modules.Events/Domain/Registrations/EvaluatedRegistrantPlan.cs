using Ruth.Modules.Events.Contracts.Registrations;

namespace Ruth.Modules.Events.Domain.Registrations;

/// <summary>Evaluated seat, dynamic field, fee, and pricing breakdown for a single prospective registrant.</summary>
public sealed record EvaluatedRegistrantPlan(
    Guid? PersonId,
    string FirstName,
    string LastName,
    string? Email,
    string? Phone,
    RegistrantStatus Status,
    int? WaitlistPosition,
    decimal BaseCost,
    decimal FeesTotal,
    decimal DiscountAmount,
    decimal NetCost,
    IReadOnlyDictionary<string, string?> FieldValues,
    IReadOnlyList<RegistrantFeeSelectionResponse> FeeSelections
);
