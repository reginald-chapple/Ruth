using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Contracts.DefinedTypes;

/// <summary>Creates a new DefinedType lookup container.</summary>
public sealed record CreateDefinedTypeCommand(
    string Name,
    string? Description = null,
    string? HelpText = null,
    Guid? CategoryId = null,
    int Order = 0,
    bool EnableSecurityOnValues = false,
    bool CategorizedValuesEnabled = false
) : ICommand<DefinedTypeDetailResponse>;
