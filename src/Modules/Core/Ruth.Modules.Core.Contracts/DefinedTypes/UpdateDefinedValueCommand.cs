using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Contracts.DefinedTypes;

/// <summary>Updates a DefinedValue item within a DefinedType.</summary>
public sealed record UpdateDefinedValueCommand(
    Guid DefinedTypeId,
    Guid DefinedValueId,
    string Value,
    string? Description,
    Guid? CategoryId,
    int Order,
    bool IsActive,
    IReadOnlyDictionary<string, string?>? Attributes = null
) : ICommand<DefinedValueResponse>;
