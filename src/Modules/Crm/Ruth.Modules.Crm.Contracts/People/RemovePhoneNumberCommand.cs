using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Crm.Contracts.People;

/// <summary>Command to remove a phone number from a person.</summary>
public sealed record RemovePhoneNumberCommand(Guid PersonId, Guid PhoneNumberId)
    : ICommand<PersonDetailResponse>;
