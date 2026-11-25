using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Groups.Contracts.Groups;

/// <summary>Retrieves a single Group with locations, demographics, and attributes.</summary>
public sealed record GetGroupByIdQuery(Guid GroupId) : IQuery<GroupDetailResponse>;
