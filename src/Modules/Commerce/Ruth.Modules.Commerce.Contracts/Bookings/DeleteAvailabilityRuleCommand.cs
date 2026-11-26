using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Commerce.Contracts.Bookings;

/// <summary>Deletes an availability or blackout rule.</summary>
/// <param name="RuleId">Rule identifier to remove.</param>
public sealed record DeleteAvailabilityRuleCommand(Guid RuleId) : ICommand;
