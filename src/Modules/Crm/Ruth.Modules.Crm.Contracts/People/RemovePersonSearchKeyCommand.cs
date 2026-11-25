using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Crm.Contracts.People;

/// <summary>Command to remove a search key from a person.</summary>
public sealed record RemovePersonSearchKeyCommand(Guid PersonId, Guid SearchKeyId)
    : ICommand<PersonDetailResponse>;
