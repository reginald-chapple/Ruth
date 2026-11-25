using Ruth.Modules.Crm.Contracts.People;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Crm.Domain.People;

internal static class PersonValidationHelper
{
    public sealed record ValidatedPersonFields(
        string FirstName,
        string NickName,
        string? MiddleName,
        string LastName,
        string? Title,
        string? Suffix,
        string? Email,
        string? EmailNote,
        DateOnly? BirthDate,
        DateOnly? AnniversaryDate,
        bool IsDeceased,
        DateOnly? DeceasedDate
    );

    public static Result<ValidatedPersonFields> Validate(PersonInput input)
    {
        var names = SanitizeNames(input);
        var nameCheck = ValidateNames(input.RecordType, names);
        if (nameCheck.IsFailure)
        {
            return Result.Failure<ValidatedPersonFields>(nameCheck.Errors);
        }

        var email = CrmTextPolicy.ValidateOptionalEmail(input.Email);
        var emailNote = CrmTextPolicy.ValidateOptionalText(
            input.EmailNote,
            CrmTextPolicy.EmailNoteMaxLength
        );
        var birthDate = PersonDemographicsCalculator.TryBuildBirthDate(
            input.BirthYear,
            input.BirthMonth,
            input.BirthDay
        );

        var combined = Result.Combine(email, emailNote, birthDate);
        if (combined.IsFailure)
        {
            return Result.Failure<ValidatedPersonFields>(combined.Errors);
        }

        var isDeceased = input.IsDeceased || input.DeceasedDate.HasValue;
        if (IsDeceasedBeforeBirth(isDeceased, input.DeceasedDate, input.BirthYear, birthDate.Value))
        {
            return CrmErrors.PersonDeceasedDateInvalid;
        }

        var effectiveNick = string.IsNullOrWhiteSpace(names.Nick) ? names.First : names.Nick;
        var effectiveAnniversary =
            input.MaritalStatus == MaritalStatus.Married ? input.AnniversaryDate : null;

        return Result.Success(
            new ValidatedPersonFields(
                names.First,
                effectiveNick,
                NullIfBlank(names.Middle),
                names.Last,
                NullIfBlank(names.Title),
                NullIfBlank(names.Suffix),
                email.Value,
                emailNote.Value,
                birthDate.Value,
                effectiveAnniversary,
                isDeceased,
                isDeceased ? input.DeceasedDate : null
            )
        );
    }

    private sealed record SanitizedNames(
        string First,
        string Last,
        string Nick,
        string Middle,
        string Title,
        string Suffix
    );

    private static SanitizedNames SanitizeNames(PersonInput input)
    {
        return new SanitizedNames(
            CrmTextPolicy.SanitizePersonName(input.FirstName),
            CrmTextPolicy.SanitizePersonName(input.LastName),
            CrmTextPolicy.SanitizePersonName(input.NickName),
            CrmTextPolicy.SanitizePersonName(input.MiddleName),
            CrmTextPolicy.SanitizePersonName(input.Title),
            CrmTextPolicy.SanitizePersonName(input.Suffix)
        );
    }

    private static Result ValidateNames(PersonRecordType recordType, SanitizedNames names)
    {
        if (recordType == PersonRecordType.Person && string.IsNullOrWhiteSpace(names.First))
        {
            return CrmErrors.PersonFirstNameRequired;
        }

        if (
            recordType is PersonRecordType.Person or PersonRecordType.Business
            && string.IsNullOrWhiteSpace(names.Last)
        )
        {
            return CrmErrors.PersonLastNameRequired;
        }

        var anyTooLong =
            names.First.Length > CrmTextPolicy.NamePartMaxLength
            || names.Last.Length > CrmTextPolicy.NamePartMaxLength
            || names.Nick.Length > CrmTextPolicy.NamePartMaxLength
            || names.Middle.Length > CrmTextPolicy.NamePartMaxLength
            || names.Title.Length > CrmTextPolicy.NamePartMaxLength
            || names.Suffix.Length > CrmTextPolicy.NamePartMaxLength;

        return anyTooLong ? CrmErrors.PersonNameTooLong : Result.Success();
    }

    private static bool IsDeceasedBeforeBirth(
        bool isDeceased,
        DateOnly? deceasedDate,
        int? birthYear,
        DateOnly? birthDate
    )
    {
        return isDeceased
            && deceasedDate.HasValue
            && birthDate.HasValue
            && birthYear.HasValue
            && deceasedDate.Value < birthDate.Value;
    }

    private static string? NullIfBlank(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}
