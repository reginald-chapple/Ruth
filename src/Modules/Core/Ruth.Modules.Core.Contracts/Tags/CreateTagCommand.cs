using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Contracts.Tags;

/// <summary>Creates a personal (when <see cref="OwnerUserId" /> is set) or organizational tag.</summary>
public sealed record CreateTagCommand(
    string Name,
    Guid? EntityTypeId = null,
    string? Description = null,
    Guid? CategoryId = null,
    Guid? OwnerUserId = null,
    string? IconName = null,
    string? BackgroundColor = "#F4F5F7",
    int Order = 0
) : ICommand<TagResponse>;
