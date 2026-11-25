namespace Ruth.Modules.Core.Contracts.Tags;

/// <summary>Personal or organizational tag definition.</summary>
public sealed record TagResponse(
    Guid Id,
    Guid SystemGuid,
    Guid? EntityTypeId,
    string Name,
    string? Description,
    Guid? CategoryId,
    Guid? OwnerUserId,
    bool IsPersonal,
    string? IconName,
    string BackgroundColor,
    int Order,
    bool IsActive,
    bool IsSystem,
    int ItemCount,
    DateTimeOffset CreatedOnUtc
);
