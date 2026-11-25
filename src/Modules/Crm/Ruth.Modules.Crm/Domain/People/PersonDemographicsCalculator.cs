using System.Globalization;
using Ruth.Modules.Crm.Contracts.People;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Crm.Domain.People;

/// <summary>Pure domain service implementing birth-date, age, age-classification, and school-grade calculations.</summary>
public static class PersonDemographicsCalculator
{
    /// <summary>Default school grade transition month (June).</summary>
    public const int DefaultGradeTransitionMonth = 6;

    /// <summary>Default school grade transition day (1st).</summary>
    public const int DefaultGradeTransitionDay = 1;

    private static readonly (int MaxOffset, string Label)[] _gradeTable =
    [
        (0, "12th Grade"),
        (1, "11th Grade"),
        (2, "10th Grade"),
        (3, "9th Grade"),
        (4, "8th Grade"),
        (5, "7th Grade"),
        (6, "6th Grade"),
        (7, "5th Grade"),
        (8, "4th Grade"),
        (9, "3rd Grade"),
        (10, "2nd Grade"),
        (11, "1st Grade"),
        (12, "Kindergarten"),
        (13, "Pre-K"),
    ];

    /// <summary>Validates and builds an optional <see cref="DateOnly" /> from birth date parts.</summary>
    public static Result<DateOnly?> TryBuildBirthDate(
        int? birthYear,
        int? birthMonth,
        int? birthDay
    )
    {
        if (!birthMonth.HasValue || !birthDay.HasValue)
        {
            return birthYear is < 1 or > 9999
                ? CrmErrors.PersonBirthDateInvalid
                : Result.Success<DateOnly?>(value: null);
        }

        var effectiveYear = birthYear ?? 1;
        if (effectiveYear is < 1 or > 9999 || birthMonth.Value is < 1 or > 12)
        {
            return CrmErrors.PersonBirthDateInvalid;
        }

        var maxDay = DateTime.DaysInMonth(effectiveYear, birthMonth.Value);
        return birthDay.Value < 1 || birthDay.Value > maxDay
            ? (Result<DateOnly?>)CrmErrors.PersonBirthDateInvalid
            : Result.Success<DateOnly?>(
                new DateOnly(effectiveYear, birthMonth.Value, birthDay.Value)
            );
    }

    /// <summary>Calculates whole-year age (stopping at <paramref name="deceasedDate" /> when present).</summary>
    public static int? CalculateAge(
        int? birthYear,
        int? birthMonth,
        int? birthDay,
        DateOnly today,
        DateOnly? deceasedDate = null
    )
    {
        if (!birthYear.HasValue || !birthMonth.HasValue || !birthDay.HasValue)
        {
            return null;
        }

        var built = TryBuildBirthDate(birthYear, birthMonth, birthDay);
        if (built.IsFailure || !built.Value.HasValue)
        {
            return null;
        }

        var birth = built.Value.Value;
        var referenceDate = deceasedDate ?? today;
        var age = referenceDate.Year - birth.Year;
        if (birth > referenceDate.AddYears(-age))
        {
            age--;
        }

        return age;
    }

    /// <summary>Maps an age in years to the standard demographic <see cref="AgeBracket" />.</summary>
    public static AgeBracket CalculateAgeBracket(int? age)
    {
        return age switch
        {
            null or < 0 => AgeBracket.Unknown,
            <= 5 => AgeBracket.ZeroToFive,
            <= 12 => AgeBracket.SixToTwelve,
            <= 17 => AgeBracket.ThirteenToSeventeen,
            <= 24 => AgeBracket.EighteenToTwentyFour,
            <= 34 => AgeBracket.TwentyFiveToThirtyFour,
            <= 44 => AgeBracket.ThirtyFiveToFortyFour,
            <= 54 => AgeBracket.FortyFiveToFiftyFour,
            <= 64 => AgeBracket.FiftyFiveToSixtyFour,
            _ => AgeBracket.SixtyFiveOrOlder,
        };
    }

    /// <summary>Formats age in years, months, or days.</summary>
    public static string FormatAge(
        int? birthYear,
        int? birthMonth,
        int? birthDay,
        DateOnly today,
        DateOnly? deceasedDate = null,
        bool condensed = false
    )
    {
        var built = TryBuildFullBirthDate(birthYear, birthMonth, birthDay);
        if (!built.HasValue)
        {
            return string.Empty;
        }

        var asOf = deceasedDate ?? today;
        if (built.Value > asOf)
        {
            return string.Empty;
        }

        var years = CalculateAge(birthYear, birthMonth, birthDay, today, deceasedDate) ?? 0;
        return years > 0
            ? FormatUnit(years, condensed ? "yr" : "year", condensed ? "yrs" : "years")
            : FormatSubYearAge(built.Value, asOf, condensed);
    }

    private static DateOnly? TryBuildFullBirthDate(int? birthYear, int? birthMonth, int? birthDay)
    {
        if (!birthYear.HasValue || !birthMonth.HasValue || !birthDay.HasValue)
        {
            return null;
        }

        var built = TryBuildBirthDate(birthYear, birthMonth, birthDay);
        return built.IsSuccess ? built.Value : null;
    }

    private static string FormatSubYearAge(DateOnly birth, DateOnly asOf, bool condensed)
    {
        var months = ((asOf.Year - birth.Year) * 12) + asOf.Month - birth.Month;
        if (asOf.Day < birth.Day)
        {
            months--;
        }

        if (months > 0)
        {
            return FormatUnit(months, condensed ? "mo" : "month", condensed ? "mos" : "months");
        }

        var days = Math.Max(0, asOf.DayNumber - birth.DayNumber);
        return FormatUnit(days, "day", "days");
    }

    /// <summary>Computes the upcoming high-school graduation year based on the grade transition date (default June 1).</summary>
    public static int GetCurrentGraduationYear(
        DateOnly today,
        int transitionMonth = DefaultGradeTransitionMonth,
        int transitionDay = DefaultGradeTransitionDay
    )
    {
        var transitionThisYear = new DateOnly(today.Year, transitionMonth, transitionDay);
        return transitionThisYear < today ? today.Year + 1 : today.Year;
    }

    /// <summary>Computes GradeOffset = GraduationYear - CurrentGraduationYear.</summary>
    public static int? CalculateGradeOffset(
        int? graduationYear,
        DateOnly today,
        int transitionMonth = DefaultGradeTransitionMonth,
        int transitionDay = DefaultGradeTransitionDay
    )
    {
        return !graduationYear.HasValue
            ? null
            : graduationYear.Value
                - GetCurrentGraduationYear(today, transitionMonth, transitionDay);
    }

    /// <summary>Calculates GraduationYear from a non-negative grade offset.</summary>
    public static int? GraduationYearFromGradeOffset(
        int? gradeOffset,
        DateOnly today,
        int transitionMonth = DefaultGradeTransitionMonth,
        int transitionDay = DefaultGradeTransitionDay
    )
    {
        return !gradeOffset.HasValue || gradeOffset.Value < 0
            ? null
            : GetCurrentGraduationYear(today, transitionMonth, transitionDay) + gradeOffset.Value;
    }

    /// <summary>Formats a grade offset into a human-friendly school grade name.</summary>
    public static string? FormatGrade(int? gradeOffset)
    {
        if (!gradeOffset.HasValue || gradeOffset.Value < 0)
        {
            return null;
        }

        foreach (var (maxOffset, label) in _gradeTable)
        {
            if (gradeOffset.Value <= maxOffset)
            {
                return label;
            }
        }

        return "Preschool";
    }

    /// <summary>Resolves Adult vs. Child classification using child-lock, age, and household role precedence.</summary>
    public static AgeClassification ResolveAgeClassification(
        int? age,
        bool isLockedAsChild,
        bool hasAdultHouseholdRole,
        bool hasChildHouseholdRole,
        AgeClassification previousClassification = AgeClassification.Unknown
    )
    {
        return isLockedAsChild ? AgeClassification.Child
            : (age.HasValue && age.Value >= 18) || hasAdultHouseholdRole ? AgeClassification.Adult
            : (age.HasValue && age.Value < 18) || hasChildHouseholdRole ? AgeClassification.Child
            : previousClassification != AgeClassification.Unknown ? previousClassification
            : AgeClassification.Unknown;
    }

    private static string FormatUnit(int value, string singular, string plural)
    {
        var label = value == 1 ? singular : plural;
        return string.Create(CultureInfo.InvariantCulture, $"{value} {label}");
    }
}
