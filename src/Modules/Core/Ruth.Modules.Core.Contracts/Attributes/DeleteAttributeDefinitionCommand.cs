using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Contracts.Attributes;

/// <summary>Deletes a non-system attribute definition.</summary>
public sealed record DeleteAttributeDefinitionCommand(Guid AttributeDefinitionId) : ICommand<bool>;
