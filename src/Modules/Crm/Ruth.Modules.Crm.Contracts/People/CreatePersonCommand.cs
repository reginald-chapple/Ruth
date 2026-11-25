using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Crm.Contracts.People;

/// <summary>Command to create a new person record in the CRM directory.</summary>
public sealed record CreatePersonCommand(
    string FirstName,
    string LastName,
    string? NickName = null,
    string? MiddleName = null,
    string? Title = null,
    string? Suffix = null,
    PersonRecordType RecordType = PersonRecordType.Person,
    PersonRecordStatus RecordStatus = PersonRecordStatus.Active,
    PersonConnectionStatus ConnectionStatus = PersonConnectionStatus.Visitor,
    Gender Gender = Gender.Unknown,
    MaritalStatus MaritalStatus = MaritalStatus.Unknown,
    DateOnly? AnniversaryDate = null,
    int? BirthYear = null,
    int? BirthMonth = null,
    int? BirthDay = null,
    int? GraduationYear = null,
    bool IsDeceased = false,
    DateOnly? DeceasedDate = null,
    bool IsLockedAsChild = false,
    string? Email = null,
    bool IsEmailActive = true,
    string? EmailNote = null,
    EmailPreference EmailPreference = EmailPreference.EmailAllowed,
    CommunicationPreference CommunicationPreference = CommunicationPreference.Email,
    AccountProtectionProfile AccountProtectionProfile = AccountProtectionProfile.Low,
    Guid? PhotoBinaryFileId = null,
    Guid? PrimaryBranchId = null,
    IReadOnlyList<PhoneNumberInputDto>? PhoneNumbers = null,
    IReadOnlyDictionary<string, string?>? Attributes = null
) : ICommand<PersonDetailResponse>;
