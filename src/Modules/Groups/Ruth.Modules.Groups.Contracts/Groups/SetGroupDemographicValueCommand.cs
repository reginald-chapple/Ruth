using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Groups.Contracts.Groups;

/// <summary>Sets or updates a GroupDemographicValue on a Group.</summary>
public sealed record SetGroupDemographicValueCommand(
    Guid GroupId,
    Guid GroupDemographicTypeId,
    string? Value,
    string? RelatedEntityTypeKey = null,
    Guid? RelatedEntityId = null
) : ICommand<GroupDemographicValueResponse>;
