using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Contracts.DefinedTypes;

/// <summary>Deletes a non-system DefinedValue item from a DefinedType.</summary>
public sealed record DeleteDefinedValueCommand(Guid DefinedTypeId, Guid DefinedValueId)
    : ICommand<bool>;
