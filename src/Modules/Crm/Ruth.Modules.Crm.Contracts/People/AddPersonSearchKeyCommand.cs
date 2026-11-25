using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Crm.Contracts.People;

/// <summary>Command to add an alternate search key (barcode, email, legacy ID) to a person.</summary>
public sealed record AddPersonSearchKeyCommand(
    Guid PersonId,
    PersonSearchKeyType KeyType,
    string SearchValue,
    bool IsValuePrivate = false,
    Guid? SearchTypeValueId = null
) : ICommand<PersonDetailResponse>;
