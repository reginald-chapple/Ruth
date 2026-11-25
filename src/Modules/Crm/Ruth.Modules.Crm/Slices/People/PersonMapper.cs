using Ruth.Modules.Crm.Contracts.People;
using Ruth.Modules.Crm.Domain.People;

namespace Ruth.Modules.Crm.Slices.People;

internal static class PersonMapper
{
    public static PersonDetailResponse ToDetail(Person person, DateOnly today)
    {
        var demo = ComputeDemographics(person, today);
        return BuildDetail(person, demo);
    }

    private static PersonDetailResponse BuildDetail(Person person, ComputedDemographics demo)
    {
        return new PersonDetailResponse(
            person.Id.Value,
            person.RecordType,
            person.RecordStatus,
            person.RecordStatusReasonValueId,
            person.RecordStatusLastModifiedOnUtc,
            person.ConnectionStatus,
            person.ConnectionStatusValueId,
            person.AccountProtectionProfile,
            person.Title,
            person.FirstName,
            person.NickName,
            person.MiddleName,
            person.LastName,
            person.Suffix,
            person.FullName,
            person.FullNameFormal,
            person.FullNameReversed,
            person.Initials,
            person.Gender,
            person.MaritalStatus,
            person.AnniversaryDate,
            person.BirthYear,
            person.BirthMonth,
            person.BirthDay,
            person.BirthDate,
            demo.Age,
            demo.AgeFormatted,
            demo.AgeBracket,
            person.AgeClassification,
            person.IsLockedAsChild,
            person.GraduationYear,
            demo.GradeOffset,
            demo.GradeFormatted,
            person.IsDeceased,
            person.DeceasedDate,
            person.Email,
            person.IsEmailActive,
            person.EmailNote,
            person.EmailPreference,
            person.CommunicationPreference,
            person.CanReceiveEmail(isBulk: true),
            person.PhotoBinaryFileId,
            person.PrimaryCampusId,
            person.PrimaryHouseholdId,
            person.GivingHouseholdId,
            person.TopSignalId,
            person.TopSignalColor,
            person.TopSignalIcon,
            person.ViewedCount,
            person.IsMerged,
            person.MergedIntoPersonId,
            MapPhones(person),
            MapPreviousNames(person),
            MapSearchKeys(person),
            CopyAttributes(person),
            person.CreatedOnUtc,
            person.ModifiedOnUtc
        );
    }

    private static Dictionary<string, string?> CopyAttributes(Person person)
    {
        return new Dictionary<string, string?>(person.Attributes, StringComparer.OrdinalIgnoreCase);
    }

    public static PersonSummaryResponse ToSummary(
        Person person,
        DateOnly today,
        string matchReason = "Directory"
    )
    {
        var demo = ComputeDemographics(person, today);
        var primaryPhone = person
            .PhoneNumbers.OrderBy(static p => (int)p.NumberType)
            .FirstOrDefault();

        return new PersonSummaryResponse(
            person.Id.Value,
            person.RecordType,
            person.RecordStatus,
            person.ConnectionStatus,
            person.FirstName,
            person.NickName,
            person.LastName,
            person.FullName,
            person.Initials,
            person.Gender,
            demo.Age,
            person.AgeClassification,
            demo.GradeFormatted,
            person.Email,
            primaryPhone?.ToString(),
            person.PhotoBinaryFileId,
            person.PrimaryCampusId,
            person.PrimaryHouseholdId,
            person.IsDeceased,
            person.TopSignalId,
            person.TopSignalColor,
            person.TopSignalIcon,
            matchReason
        );
    }

    public static PersonInput ToInput(CreatePersonCommand command)
    {
        return new PersonInput(
            command.FirstName,
            command.LastName,
            command.NickName,
            command.MiddleName,
            command.Title,
            command.Suffix,
            command.RecordType,
            command.RecordStatus,
            RecordStatusReasonValueId: null,
            command.ConnectionStatus,
            ConnectionStatusValueId: null,
            command.Gender,
            command.MaritalStatus,
            command.AnniversaryDate,
            command.BirthYear,
            command.BirthMonth,
            command.BirthDay,
            command.GraduationYear,
            command.IsDeceased,
            command.DeceasedDate,
            command.IsLockedAsChild,
            command.Email,
            command.IsEmailActive,
            command.EmailNote,
            command.EmailPreference,
            command.CommunicationPreference,
            command.AccountProtectionProfile,
            command.PhotoBinaryFileId,
            command.PrimaryCampusId
        );
    }

    public static PersonInput ToInput(UpdatePersonCommand command, PersonRecordType existingType)
    {
        return new PersonInput(
            command.FirstName,
            command.LastName,
            command.NickName,
            command.MiddleName,
            command.Title,
            command.Suffix,
            existingType,
            command.RecordStatus,
            command.RecordStatusReasonValueId,
            command.ConnectionStatus,
            command.ConnectionStatusValueId,
            command.Gender,
            command.MaritalStatus,
            command.AnniversaryDate,
            command.BirthYear,
            command.BirthMonth,
            command.BirthDay,
            command.GraduationYear,
            command.IsDeceased,
            command.DeceasedDate,
            command.IsLockedAsChild,
            command.Email,
            command.IsEmailActive,
            command.EmailNote,
            command.EmailPreference,
            command.CommunicationPreference,
            command.AccountProtectionProfile,
            command.PhotoBinaryFileId,
            command.PrimaryCampusId
        );
    }

    private readonly record struct ComputedDemographics(
        int? Age,
        string AgeFormatted,
        AgeBracket AgeBracket,
        int? GradeOffset,
        string? GradeFormatted
    );

    private static ComputedDemographics ComputeDemographics(Person person, DateOnly today)
    {
        var age = PersonDemographicsCalculator.CalculateAge(
            person.BirthYear,
            person.BirthMonth,
            person.BirthDay,
            today,
            person.DeceasedDate
        );
        var ageFormatted = PersonDemographicsCalculator.FormatAge(
            person.BirthYear,
            person.BirthMonth,
            person.BirthDay,
            today,
            person.DeceasedDate
        );
        var gradeOffset = PersonDemographicsCalculator.CalculateGradeOffset(
            person.GraduationYear,
            today
        );
        return new ComputedDemographics(
            age,
            ageFormatted,
            PersonDemographicsCalculator.CalculateAgeBracket(age),
            gradeOffset,
            PersonDemographicsCalculator.FormatGrade(gradeOffset)
        );
    }

    private static List<PhoneNumberResponse> MapPhones(Person person)
    {
        return
        [
            .. person.PhoneNumbers.Select(static phone => new PhoneNumberResponse(
                phone.Id.Value,
                phone.NumberType,
                phone.NumberTypeValueId,
                phone.CountryCode,
                phone.Number,
                phone.E164Number,
                phone.NumberFormatted,
                phone.Extension,
                phone.IsMessagingEnabled,
                phone.IsMessagingOptedOut,
                phone.MessagingOptedOutOnUtc,
                phone.IsUnlisted,
                phone.Description
            )),
        ];
    }

    private static List<PersonPreviousNameResponse> MapPreviousNames(Person person)
    {
        return
        [
            .. person.PreviousNames.Select(static n => new PersonPreviousNameResponse(
                n.Id.Value,
                n.LastName,
                n.CreatedOnUtc
            )),
        ];
    }

    private static List<PersonSearchKeyResponse> MapSearchKeys(Person person)
    {
        return
        [
            .. person.SearchKeys.Select(static k => new PersonSearchKeyResponse(
                k.Id.Value,
                k.KeyType,
                k.SearchTypeValueId,
                k.SearchValue,
                k.IsValuePrivate
            )),
        ];
    }
}
