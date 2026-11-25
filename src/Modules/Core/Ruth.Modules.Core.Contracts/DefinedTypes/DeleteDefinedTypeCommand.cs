using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Contracts.DefinedTypes;

/// <summary>Deletes a non-system DefinedType and its values.</summary>
public sealed record DeleteDefinedTypeCommand(Guid DefinedTypeId) : ICommand<bool>;
