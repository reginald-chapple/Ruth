using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Contracts.DefinedTypes;

/// <summary>Updates an existing DefinedType's metadata.</summary>
public sealed record UpdateDefinedTypeCommand(
    Guid DefinedTypeId,
    string Name,
    string? Description,
    string? HelpText,
    Guid? CategoryId,
    int Order,
    bool IsActive,
    bool EnableSecurityOnValues,
    bool CategorizedValuesEnabled
) : ICommand<DefinedTypeDetailResponse>;
