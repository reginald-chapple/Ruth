using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Groups.Contracts.Groups;

/// <summary>Creates a GroupDemographicType for a GroupType.</summary>
public sealed record CreateGroupDemographicTypeCommand(
    Guid GroupTypeId,
    string Name,
    string ComponentKey,
    string? Description = null,
    bool IsAutomated = false,
    bool RunOnPersonUpdate = false
) : ICommand<GroupDemographicTypeResponse>;
