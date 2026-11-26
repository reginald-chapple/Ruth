using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Engagement.Contracts.Pipelines;

/// <summary>Command to update an existing EngagementOpportunity.</summary>
/// <param name="OpportunityId">Opportunity identifier.</param>
/// <param name="Spec">Updated opportunity specification.</param>
public sealed record UpdateEngagementOpportunityCommand(
    Guid OpportunityId,
    EngagementOpportunitySpec Spec
) : ICommand<EngagementOpportunityResponse>;
