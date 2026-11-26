using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Commerce.Contracts.Catalog;

/// <summary>Creates a new punch pass or credit pack type.</summary>
/// <param name="Spec">Punch pass type specification.</param>
public sealed record CreatePunchPassTypeCommand(PunchPassTypeSpec Spec)
    : ICommand<PunchPassTypeResponse>;
