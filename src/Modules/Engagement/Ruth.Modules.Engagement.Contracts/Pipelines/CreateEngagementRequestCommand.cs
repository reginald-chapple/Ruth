using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Engagement.Contracts.Pipelines;

/// <summary>Staff command to create an EngagementRequest for an existing CRM Person.</summary>
/// <param name="EngagementOpportunityId">Target EngagementOpportunity identifier.</param>
/// <param name="RequesterPersonId">Requester CRM Person identifier.</param>
/// <param name="BranchId">Optional Branch identifier.</param>
/// <param name="AssignedConnectorPersonId">Optional explicit coordinator Person identifier.</param>
/// <param name="PlacementGroupId">Optional pre-selected placement Group identifier.</param>
/// <param name="PlacementGroupRoleId">Optional pre-selected placement GroupRole identifier.</param>
/// <param name="FollowUpDate">Optional initial future follow-up date.</param>
/// <param name="Comments">Optional intake notes.</param>
public sealed record CreateEngagementRequestCommand(
    Guid EngagementOpportunityId,
    Guid RequesterPersonId,
    Guid? BranchId = null,
    Guid? AssignedConnectorPersonId = null,
    Guid? PlacementGroupId = null,
    Guid? PlacementGroupRoleId = null,
    DateOnly? FollowUpDate = null,
    string? Comments = null
) : ICommand<EngagementRequestResponse>;
