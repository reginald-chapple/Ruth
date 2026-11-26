using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Engagement.Contracts.Touchpoints;

/// <summary>Command to add or update a ConstituentContact in a portfolio and ensure its next cadence Touchpoint.</summary>
/// <param name="Spec">ConstituentContact specification.</param>
public sealed record UpsertConstituentContactCommand(ConstituentContactSpec Spec)
    : ICommand<ConstituentContactResponse>;
