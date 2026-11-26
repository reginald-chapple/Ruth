using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Commerce.Contracts.Catalog;

/// <summary>Updates an existing punch pass or credit pack type.</summary>
/// <param name="PunchPassTypeId">Target punch pass type identifier.</param>
/// <param name="Spec">Updated specification.</param>
public sealed record UpdatePunchPassTypeCommand(Guid PunchPassTypeId, PunchPassTypeSpec Spec)
    : ICommand<PunchPassTypeResponse>;
