using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Groups.Contracts.Groups;

/// <summary>Updates JSONB extensible attributes on a Group.</summary>
public sealed record UpdateGroupAttributesCommand(
    Guid GroupId,
    IReadOnlyDictionary<string, string?> Attributes
) : ICommand<GroupDetailResponse>;
