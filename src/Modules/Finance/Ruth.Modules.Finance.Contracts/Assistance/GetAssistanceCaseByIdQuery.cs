using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Finance.Contracts.Assistance;

/// <summary>Query to retrieve a single assistance case by identifier.</summary>
public sealed record GetAssistanceCaseByIdQuery(Guid CaseId) : IQuery<AssistanceCaseResponse>;
