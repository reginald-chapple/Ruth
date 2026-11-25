using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Crm.Contracts.People;

/// <summary>Command to record a previous last name for a person.</summary>
public sealed record AddPersonPreviousNameCommand(Guid PersonId, string LastName)
    : ICommand<PersonDetailResponse>;
