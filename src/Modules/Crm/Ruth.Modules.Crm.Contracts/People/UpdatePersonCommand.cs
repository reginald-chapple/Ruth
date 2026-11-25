using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Crm.Contracts.People;

/// <summary>Command to update an existing person's core profile and demographics.</summary>
public sealed record UpdatePersonCommand(
    Guid PersonId,
    string FirstName,
    string LastName,
    string? NickName = null,
    string? MiddleName = null,
    string? Title = null,
    string? Suffix = null,
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
) : ICommand<PersonDetailResponse>;
