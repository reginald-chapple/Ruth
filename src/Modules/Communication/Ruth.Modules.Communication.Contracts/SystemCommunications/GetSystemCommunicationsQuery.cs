using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Communication.Contracts.SystemCommunications;

/// <summary>Lists transactional system communication templates.</summary>
/// <param name="Category">Optional category filter.</param>
/// <param name="ActiveOnly">Whether to return only active templates.</param>
public sealed record GetSystemCommunicationsQuery(
    SystemCommunicationCategory? Category = null,
    bool ActiveOnly = false
) : IQuery<IReadOnlyList<SystemCommunicationResponse>>;
