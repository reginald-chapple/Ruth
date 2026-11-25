using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Crm.Contracts.Households;

/// <summary>Command to attach a physical Core Location to a household, optionally demoting prior same-type addresses to Previous.</summary>
public sealed record SetHouseholdLocationCommand(
    Guid HouseholdId,
    Guid LocationId,
    HouseholdLocationType LocationType = HouseholdLocationType.Home,
    bool IsMailingLocation = true,
    bool IsMappedLocation = true,
    string? AddressSummary = null,
    bool MoveExistingToPrevious = true
) : ICommand<HouseholdResponse>;
