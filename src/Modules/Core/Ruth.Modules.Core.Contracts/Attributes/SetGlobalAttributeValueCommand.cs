using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Contracts.Attributes;

/// <summary>Sets the stored value for a global attribute after validating it against its field type.</summary>
public sealed record SetGlobalAttributeValueCommand(Guid AttributeDefinitionId, string? Value)
    : ICommand<AttributeDefinitionResponse>;
