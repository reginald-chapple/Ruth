using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Engagement.Contracts.Touchpoints;

/// <summary>Command to evaluate active portfolios, refresh relationship health, and generate due/upcoming Touchpoints.</summary>
/// <param name="OwnerPersonId">Optional single owner Person filter.</param>
/// <param name="HorizonDays">Lookahead window in days (default 7).</param>
public sealed record GeneratePortfolioCadenceTouchpointsCommand(
    Guid? OwnerPersonId = null,
    int HorizonDays = 7
) : ICommand<TouchpointCadenceRunResponse>;
