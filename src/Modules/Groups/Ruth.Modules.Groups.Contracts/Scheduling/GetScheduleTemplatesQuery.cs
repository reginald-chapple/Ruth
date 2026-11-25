using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Groups.Contracts.Scheduling;

/// <summary>Lists GroupMemberScheduleTemplates optionally filtered by GroupType.</summary>
public sealed record GetScheduleTemplatesQuery(Guid? GroupTypeId = null)
    : IQuery<IReadOnlyList<GroupMemberScheduleTemplateResponse>>;
