using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Crm.Contracts.Badges;

/// <summary>Command to create a badge definition.</summary>
public sealed record CreateBadgeDefinitionCommand(
    string Name,
    string BadgeComponentKey,
    string? Description = null,
    bool IsActive = true,
    int Order = 0,
    IReadOnlyDictionary<string, string?>? Settings = null
) : ICommand<BadgeDefinitionResponse>;
