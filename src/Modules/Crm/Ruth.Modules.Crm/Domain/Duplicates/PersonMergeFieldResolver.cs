using Ruth.Modules.Crm.Contracts.Duplicates;
using Ruth.Modules.Crm.Contracts.People;
using Ruth.Modules.Crm.Domain.People;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Crm.Domain.Duplicates;

/// <summary>Pure domain service enforcing merge protection guardrails and per-field merge resolution.</summary>
public static class PersonMergeFieldResolver
{
    /// <summary>Validates security guardrails before allowing two person records to be merged.</summary>
    public static Result ValidateMergeEligibility(
        Person surviving,
        Person retired,
        bool hasElevatedMergeAuthorization
    )
    {
        ArgumentNullException.ThrowIfNull(surviving);
        ArgumentNullException.ThrowIfNull(retired);

        if (surviving.Id == retired.Id)
        {
            return CrmErrors.MergeSamePerson;
        }

        if (surviving.IsMerged || retired.IsMerged)
        {
            return CrmErrors.PersonAlreadyMerged;
        }

        var maxProtection = (AccountProtectionProfile)
            Math.Max(
                (int)surviving.AccountProtectionProfile,
                (int)retired.AccountProtectionProfile
            );

        return
            maxProtection is AccountProtectionProfile.High or AccountProtectionProfile.Extreme
            && !hasElevatedMergeAuthorization
            ? (Result)CrmErrors.MergeProtectionForbidden
            : Result.Success();
    }

    /// <summary>Builds the merged <see cref="PersonInput" /> from surviving and retired person fields.</summary>
    public static PersonInput BuildMergedInput(
        Person surviving,
        Person retired,
        PersonMergeFieldSelections selections
    )
    {
        ArgumentNullException.ThrowIfNull(surviving);
        ArgumentNullException.ThrowIfNull(retired);
        ArgumentNullException.ThrowIfNull(selections);

        var birthSource =
            selections.BirthDate == PersonMergeFieldChoice.Retired ? retired : surviving;
        var emailSource = selections.Email == PersonMergeFieldChoice.Retired ? retired : surviving;
        var maritalSource =
            selections.MaritalStatus == PersonMergeFieldChoice.Retired ? retired : surviving;
        var maxProtection = ResolveMaxProtection(surviving, retired);
        return new PersonInput(
            Pick(selections.FirstName, surviving.FirstName, retired.FirstName),
            Pick(selections.LastName, surviving.LastName, retired.LastName),
            Pick(selections.NickName, surviving.NickName, retired.NickName),
            Pick(selections.MiddleName, surviving.MiddleName, retired.MiddleName),
            Pick(selections.Title, surviving.Title, retired.Title),
            Pick(selections.Suffix, surviving.Suffix, retired.Suffix),
            surviving.RecordType,
            surviving.RecordStatus,
            surviving.RecordStatusReasonValueId,
            Pick(selections.ConnectionStatus, surviving.ConnectionStatus, retired.ConnectionStatus),
            surviving.ConnectionStatusValueId,
            Pick(selections.Gender, surviving.Gender, retired.Gender),
            maritalSource.MaritalStatus,
            maritalSource.AnniversaryDate,
            birthSource.BirthYear,
            birthSource.BirthMonth,
            birthSource.BirthDay,
            Pick(selections.GraduationYear, surviving.GraduationYear, retired.GraduationYear),
            surviving.IsDeceased,
            surviving.DeceasedDate,
            surviving.IsLockedAsChild || retired.IsLockedAsChild,
            emailSource.Email,
            emailSource.IsEmailActive,
            emailSource.EmailNote,
            emailSource.EmailPreference,
            surviving.CommunicationPreference,
            maxProtection,
            Pick(selections.Photo, surviving.PhotoBinaryFileId, retired.PhotoBinaryFileId),
            Pick(selections.PrimaryBranch, surviving.PrimaryBranchId, retired.PrimaryBranchId)
        );
    }

    private static AccountProtectionProfile ResolveMaxProtection(Person surviving, Person retired)
    {
        return (AccountProtectionProfile)
            Math.Max(
                (int)surviving.AccountProtectionProfile,
                (int)retired.AccountProtectionProfile
            );
    }

    private static T Pick<T>(PersonMergeFieldChoice choice, T survivingValue, T retiredValue)
    {
        return choice == PersonMergeFieldChoice.Retired ? retiredValue : survivingValue;
    }
}
