namespace Ruth.Modules.Core.Contracts.Files;

/// <summary>Document record attaching a permanent BinaryFile to a target entity.</summary>
public sealed record DocumentResponse(
    Guid Id,
    Guid DocumentTypeId,
    Guid EntityId,
    Guid BinaryFileId,
    string Name,
    string? PurposeKey,
    string? Description,
    bool IsSystem,
    DateTimeOffset CreatedOnUtc,
    DateTimeOffset? ModifiedOnUtc
);
