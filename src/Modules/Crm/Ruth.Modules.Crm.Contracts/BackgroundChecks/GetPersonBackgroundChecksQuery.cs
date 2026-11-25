using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Crm.Contracts.BackgroundChecks;

/// <summary>Query to list background checks for a person.</summary>
public sealed record GetPersonBackgroundChecksQuery(Guid PersonId)
    : IQuery<IReadOnlyList<BackgroundCheckResponse>>;
