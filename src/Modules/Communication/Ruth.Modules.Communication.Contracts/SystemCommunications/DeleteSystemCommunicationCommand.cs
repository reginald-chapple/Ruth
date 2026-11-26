using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Communication.Contracts.SystemCommunications;

/// <summary>Deletes a non-system transactional communication template.</summary>
/// <param name="SystemCommunicationId">Target system communication identifier.</param>
public sealed record DeleteSystemCommunicationCommand(Guid SystemCommunicationId) : ICommand<bool>;
