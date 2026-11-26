using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Commerce.Contracts.Bookings;

/// <summary>Creates an availability or blackout rule.</summary>
/// <param name="Spec">Rule specification.</param>
public sealed record CreateAvailabilityRuleCommand(AvailabilityRuleSpec Spec)
    : ICommand<AvailabilityRuleResponse>;
