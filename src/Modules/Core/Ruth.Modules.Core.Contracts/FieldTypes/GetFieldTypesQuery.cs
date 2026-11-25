using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Contracts.FieldTypes;

/// <summary>Lists all registered modular field type plugins.</summary>
public sealed record GetFieldTypesQuery() : IQuery<IReadOnlyList<FieldTypeResponse>>;
