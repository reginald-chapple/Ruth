using Ruth.Modules.Crm.Contracts.People;

namespace Ruth.Modules.Crm.Domain.People;

/// <summary>Formats display names and initials for a <see cref="Person" /> across individual and organization records.</summary>
public static class PersonNameFormatter
{
    /// <summary>Formats standard display full name (NickName LastName [Suffix]).</summary>
    public static string FormatFullName(
        PersonRecordType recordType,
        string nickName,
        string lastName,
        string? suffix
    )
    {
        if (recordType == PersonRecordType.Nameless)
        {
            return "Nameless Person";
        }

        if (recordType == PersonRecordType.Business)
        {
            return lastName;
        }

        var core = string.IsNullOrWhiteSpace(nickName) ? lastName : $"{nickName} {lastName}".Trim();
        return string.IsNullOrWhiteSpace(suffix) ? core : $"{core} {suffix}";
    }

    /// <summary>Formats reversed full name (LastName [Suffix], NickName).</summary>
    public static string FormatFullNameReversed(
        PersonRecordType recordType,
        string nickName,
        string lastName,
        string? suffix
    )
    {
        if (recordType == PersonRecordType.Nameless)
        {
            return "Nameless Person";
        }

        if (recordType == PersonRecordType.Business)
        {
            return lastName;
        }

        var lastWithSuffix = string.IsNullOrWhiteSpace(suffix) ? lastName : $"{lastName} {suffix}";
        return string.IsNullOrWhiteSpace(nickName)
            ? lastWithSuffix
            : $"{lastWithSuffix}, {nickName}";
    }

    /// <summary>Formats formal full name ([Title] FirstName LastName [Suffix]).</summary>
    public static string FormatFullNameFormal(
        PersonRecordType recordType,
        string? title,
        string firstName,
        string lastName,
        string? suffix
    )
    {
        if (recordType == PersonRecordType.Nameless)
        {
            return "Nameless Person";
        }

        if (recordType == PersonRecordType.Business)
        {
            return lastName;
        }

        var parts = new[] { title, firstName, lastName, suffix }.Where(static p =>
            !string.IsNullOrWhiteSpace(p)
        );
        return string.Join(' ', parts);
    }

    /// <summary>Computes 1–2 uppercase initials from nickname and last name.</summary>
    public static string FormatInitials(string nickName, string lastName)
    {
        var first =
            nickName.Length > 0 ? char.ToUpperInvariant(nickName[0]).ToString() : string.Empty;
        var last =
            lastName.Length > 0 ? char.ToUpperInvariant(lastName[0]).ToString() : string.Empty;
        return $"{first}{last}";
    }
}
