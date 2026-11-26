using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Engagement.Contracts.Pipelines;

/// <summary>Command to create a new EngagementOpportunity.</summary>
/// <param name="Spec">Opportunity specification.</param>
public sealed record CreateEngagementOpportunityCommand(EngagementOpportunitySpec Spec)
    : ICommand<EngagementOpportunityResponse>;
