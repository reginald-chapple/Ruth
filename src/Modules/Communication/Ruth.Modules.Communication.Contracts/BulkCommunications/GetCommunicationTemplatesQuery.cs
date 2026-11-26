using Ruth.Modules.Communication.Contracts.Transports;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Communication.Contracts.BulkCommunications;

/// <summary>Lists reusable bulk communication templates.</summary>
/// <param name="Channel">Optional communication channel filter.</param>
/// <param name="ActiveOnly">Whether to include only active templates.</param>
public sealed record GetCommunicationTemplatesQuery(
    CommunicationChannel? Channel = null,
    bool ActiveOnly = false
) : IQuery<IReadOnlyList<CommunicationTemplateResponse>>;
