namespace Ruth.Modules.Crm.Contracts.People;

/// <summary>Compact person projection returned by search and directory listings.</summary>
public sealed record PersonSummaryResponse(
    Guid Id,
    PersonRecordType RecordType,
    PersonRecordStatus RecordStatus,
    PersonConnectionStatus ConnectionStatus,
    string FirstName,
    string NickName,
    string LastName,
    string FullName,
    string Initials,
    Gender Gender,
    int? Age,
    AgeClassification AgeClassification,
    string? GradeFormatted,
    string? Email,
    string? PrimaryPhoneFormatted,
    Guid? PhotoBinaryFileId,
    Guid? PrimaryCampusId,
    Guid? PrimaryHouseholdId,
    bool IsDeceased,
    Guid? TopSignalId,
    string? TopSignalColor,
    string? TopSignalIcon,
    string MatchReason
);
