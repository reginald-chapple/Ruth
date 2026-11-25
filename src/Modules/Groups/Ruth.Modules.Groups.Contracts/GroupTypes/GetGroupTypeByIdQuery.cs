using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Groups.Contracts.GroupTypes;

/// <summary>Gets a single GroupType with its roles and allowed child types.</summary>
public sealed record GetGroupTypeByIdQuery(Guid GroupTypeId) : IQuery<GroupTypeDetailResponse>;
