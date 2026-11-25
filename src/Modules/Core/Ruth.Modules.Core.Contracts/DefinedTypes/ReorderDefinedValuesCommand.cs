using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Contracts.DefinedTypes;

/// <summary>Reorders DefinedValue items inside a DefinedType according to the supplied ID sequence.</summary>
public sealed record ReorderDefinedValuesCommand(
    Guid DefinedTypeId,
    IReadOnlyList<Guid> OrderedValueIds
) : ICommand<IReadOnlyList<DefinedValueResponse>>;
