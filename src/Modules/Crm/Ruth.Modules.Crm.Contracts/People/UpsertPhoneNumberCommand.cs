using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Crm.Contracts.People;

/// <summary>Command to add or update a phone number on a person.</summary>
public sealed record UpsertPhoneNumberCommand(
    Guid PersonId,
    Guid? PhoneNumberId,
    PhoneNumberInputDto Phone
) : ICommand<PersonDetailResponse>;
