using Ruth.Modules.Crm.Contracts.People;

namespace Ruth.Modules.Crm.Domain.People;

/// <summary>Validated input bag used to create or update a <see cref="Person" /> aggregate.</summary>
public sealed record PersonInput(
    string FirstName,
    string LastName,
    string? NickName = null,
    string? MiddleName = null,
    string? Title = null,
    string? Suffix = null,
    PersonRecordType RecordType = PersonRecordType.Person,
    PersonRecordStatus RecordStatus = PersonRecordStatus.Active,
    Guid? RecordStatusReasonValueId = null,
    PersonConnectionStatus ConnectionStatus = PersonConnectionStatus.Visitor,
    Guid? ConnectionStatusValueId = null,
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
    Guid? PrimaryCampusId = null
);
