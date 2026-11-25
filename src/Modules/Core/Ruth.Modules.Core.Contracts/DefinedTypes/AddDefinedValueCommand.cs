using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Contracts.DefinedTypes;

/// <summary>Adds a new DefinedValue item to an existing DefinedType.</summary>
public sealed record AddDefinedValueCommand(
    Guid DefinedTypeId,
    string Value,
    string? Description = null,
    Guid? CategoryId = null,
    int? Order = null,
    IReadOnlyDictionary<string, string?>? Attributes = null
) : ICommand<DefinedValueResponse>;
