using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.CheckIn.Contracts.Sessions;

/// <summary>Retrieves a check-in session receipt by its identifier.</summary>
/// <param name="SessionId">Session identifier.</param>
public sealed record GetCheckInSessionByIdQuery(Guid SessionId)
    : IQuery<CheckInSessionReceiptResponse>;
