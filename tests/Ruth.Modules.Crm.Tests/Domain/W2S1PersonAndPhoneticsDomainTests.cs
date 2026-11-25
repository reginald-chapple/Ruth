using Ruth.Modules.Crm.Contracts.People;
using Ruth.Modules.Crm.Domain;
using Ruth.Modules.Crm.Domain.People;
using Xunit;

namespace Ruth.Modules.Crm.Tests.Domain;

public sealed class W2S1PersonAndPhoneticsDomainTests
{
    private static readonly DateTimeOffset _nowUtc = new(2026, 11, 24, 18, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Person_Create_SanitizesNames_DefaultsNickName_AndGeneratesAlternateId()
    {
        var input = new PersonInput(
            "  Robert  ",
            "O\u2019Connor   Jr  ",
            Email: "rob@example.org",
            MaritalStatus: MaritalStatus.Unmarried,
            AnniversaryDate: new DateOnly(2020, 5, 1)
        );

        var result = Person.Create(input, _nowUtc);

        Assert.True(result.IsSuccess);
        var person = result.Value;
        Assert.Equal("Robert", person.FirstName);
        Assert.Equal("Robert", person.NickName);
        Assert.Equal("O'Connor Jr", person.LastName);
        Assert.Equal("Robert O'Connor Jr", person.FullName);
        Assert.Equal("O'Connor Jr, Robert", person.FullNameReversed);
        Assert.Equal("RO", person.Initials);
        Assert.Null(person.AnniversaryDate);
        Assert.Contains(person.SearchKeys, static k =>
            k.KeyType == PersonSearchKeyType.AlternateId
        );
    }

    [Fact]
    public void Person_Update_ArchivesPreviousLastName_AndPreviousEmail_AndHandlesDeceased()
    {
        var person = Person
            .Create(
                new PersonInput(
                    "Jane",
                    "Doe",
                    Email: "jane.doe@example.org",
                    BirthYear: 1990,
                    BirthMonth: 3,
                    BirthDay: 10
                ),
                _nowUtc
            )
            .Value;

        var update = person.Update(
            new PersonInput(
                "Jane",
                "Smith",
                NickName: "Janie",
                Email: "jane.smith@example.org",
                BirthYear: 1990,
                BirthMonth: 3,
                BirthDay: 10,
                IsDeceased: true,
                DeceasedDate: new DateOnly(2025, 1, 15)
            ),
            _nowUtc.AddDays(1)
        );

        Assert.True(update.IsSuccess);
        Assert.Equal(PersonRecordStatus.Inactive, person.RecordStatus);
        Assert.True(person.IsDeceased);
        Assert.Contains(person.PreviousNames, static p =>
            string.Equals(p.LastName, "Doe", StringComparison.Ordinal)
        );
        Assert.Contains(person.SearchKeys, static k =>
            k.KeyType == PersonSearchKeyType.Email
            && string.Equals(k.SearchValue, "jane.doe@example.org", StringComparison.Ordinal)
        );
    }

    [Fact]
    public void Person_CanReceiveEmail_RespectsPreferenceAndActiveFlags()
    {
        var allowed = Person
            .Create(
                new PersonInput(
                    "A",
                    "B",
                    Email: "a@b.com",
                    EmailPreference: EmailPreference.EmailAllowed
                ),
                _nowUtc
            )
            .Value;
        var noMass = Person
            .Create(
                new PersonInput(
                    "A",
                    "B",
                    Email: "a@b.com",
                    EmailPreference: EmailPreference.NoMassEmails
                ),
                _nowUtc
            )
            .Value;
        var doNot = Person
            .Create(
                new PersonInput(
                    "A",
                    "B",
                    Email: "a@b.com",
                    EmailPreference: EmailPreference.DoNotEmail
                ),
                _nowUtc
            )
            .Value;

        Assert.True(allowed.CanReceiveEmail(isBulk: true));
        Assert.False(noMass.CanReceiveEmail(isBulk: true));
        Assert.True(noMass.CanReceiveEmail(isBulk: false));
        Assert.False(doNot.CanReceiveEmail(isBulk: false));
    }

    [Fact]
    public void Person_Validation_RejectsInvalidDatesAndMissingNames()
    {
        var missingFirst = Person.Create(new PersonInput("", "Smith"), _nowUtc);
        Assert.Equal(CrmErrors.PersonFirstNameRequired, missingFirst.PrimaryError);

        var invalidDate = Person.Create(
            new PersonInput("Bob", "Smith", BirthYear: 2025, BirthMonth: 2, BirthDay: 29),
            _nowUtc
        );
        Assert.Equal(CrmErrors.PersonBirthDateInvalid, invalidDate.PrimaryError);

        var deceasedBeforeBirth = Person.Create(
            new PersonInput(
                "Bob",
                "Smith",
                BirthYear: 2000,
                BirthMonth: 6,
                BirthDay: 1,
                IsDeceased: true,
                DeceasedDate: new DateOnly(1999, 1, 1)
            ),
            _nowUtc
        );
        Assert.Equal(CrmErrors.PersonDeceasedDateInvalid, deceasedBeforeBirth.PrimaryError);

        var business = Person.Create(
            new PersonInput("", "Acme Church Supply", RecordType: PersonRecordType.Business),
            _nowUtc
        );
        Assert.True(business.IsSuccess);
        Assert.Equal("Acme Church Supply", business.Value.FullName);
    }

    [Fact]
    public void PersonDemographicsCalculator_ComputesAge_InfantAge_Brackets_AndSchoolGrades()
    {
        var today = new DateOnly(2026, 11, 24);

        Assert.Equal(36, PersonDemographicsCalculator.CalculateAge(1990, 5, 1, today));
        Assert.Equal(35, PersonDemographicsCalculator.CalculateAge(1990, 12, 1, today));
        Assert.Equal(
            30,
            PersonDemographicsCalculator.CalculateAge(
                1990,
                5,
                1,
                today,
                deceasedDate: new DateOnly(2020, 6, 1)
            )
        );

        Assert.Equal("5 months", PersonDemographicsCalculator.FormatAge(2026, 6, 1, today));
        Assert.Equal("10 days", PersonDemographicsCalculator.FormatAge(2026, 11, 14, today));
        Assert.Equal(
            "10 yrs",
            PersonDemographicsCalculator.FormatAge(2016, 1, 1, today, condensed: true)
        );
        Assert.Equal(string.Empty, PersonDemographicsCalculator.FormatAge(2028, 1, 1, today));

        Assert.Equal(AgeBracket.ZeroToFive, PersonDemographicsCalculator.CalculateAgeBracket(4));
        Assert.Equal(
            AgeBracket.SixtyFiveOrOlder,
            PersonDemographicsCalculator.CalculateAgeBracket(70)
        );

        Assert.Equal(2027, PersonDemographicsCalculator.GetCurrentGraduationYear(today));
        Assert.Equal(
            2026,
            PersonDemographicsCalculator.GetCurrentGraduationYear(new DateOnly(2026, 5, 15))
        );
        Assert.Equal(0, PersonDemographicsCalculator.CalculateGradeOffset(2027, today));
        Assert.Equal("12th Grade", PersonDemographicsCalculator.FormatGrade(0));
        Assert.Equal("Kindergarten", PersonDemographicsCalculator.FormatGrade(12));
        Assert.Null(PersonDemographicsCalculator.FormatGrade(-1));
        Assert.Equal(2030, PersonDemographicsCalculator.GraduationYearFromGradeOffset(3, today));

        Assert.Equal(
            AgeClassification.Child,
            PersonDemographicsCalculator.ResolveAgeClassification(
                age: 25,
                isLockedAsChild: true,
                hasAdultHouseholdRole: true,
                hasChildHouseholdRole: false
            )
        );
        Assert.Equal(
            AgeClassification.Adult,
            PersonDemographicsCalculator.ResolveAgeClassification(
                age: null,
                isLockedAsChild: false,
                hasAdultHouseholdRole: true,
                hasChildHouseholdRole: false
            )
        );
    }

    [Fact]
    public void PhoneNumber_NormalizesE164_AndResetsSmsOptOutOnDigitChangeOnly()
    {
        var person = Person.Create(new PersonInput("Sam", "Lee"), _nowUtc).Value;
        var added = person.AddPhoneNumber(
            new PhoneNumberInputDto(
                PhoneNumberType.Mobile,
                "(602) 555-0199",
                IsMessagingEnabled: true
            )
        );
        Assert.True(added.IsSuccess);
        var phone = added.Value;
        Assert.Equal("+16025550199", phone.E164Number);
        Assert.Equal("(602) 555-0199", phone.NumberFormatted);
        Assert.Equal("9910555206", phone.NumberReversed);

        phone.RecordMessagingOptOut(_nowUtc);
        Assert.True(phone.IsMessagingOptedOut);

        var metaEdit = phone.Update(
            new PhoneNumberInputDto(PhoneNumberType.Mobile, "602-555-0199", IsUnlisted: true)
        );
        Assert.True(metaEdit.IsSuccess);
        Assert.True(phone.IsMessagingOptedOut);
        Assert.Equal("Unlisted", phone.ToString());

        var numberChange = phone.Update(
            new PhoneNumberInputDto(PhoneNumberType.Mobile, "+44 20 7946 0958")
        );
        Assert.True(numberChange.IsSuccess);
        Assert.Equal("+442079460958", phone.E164Number);
        Assert.False(phone.IsMessagingOptedOut);
        Assert.Null(phone.MessagingOptedOutOnUtc);
        Assert.False(phone.IsMessagingEnabled);
    }

    [Theory]
    [InlineData("Smith", "SM0", "XMT")]
    [InlineData("Smyth", "SM0", "XMT")]
    [InlineData("Catherine", "K0RN", "KTRN")]
    [InlineData("Katherine", "K0RN", "KTRN")]
    [InlineData("Knight", "NT", null)]
    [InlineData("Phillip", "FLP", null)]
    [InlineData("White", "AT", "FT")]
    [InlineData("Schmidt", "SKMT", "XMT")]
    [InlineData("Xavier", "SFR", null)]
    [InlineData("", "", null)]
    public void DoubleMetaphoneEncoder_ProducesConsistentPhoneticKeys(
        string word,
        string expectedPrimary,
        string? expectedAlt
    )
    {
        var result = DoubleMetaphoneEncoder.Encode(word);
        Assert.Equal(expectedPrimary, result.PrimaryKey);
        if (expectedAlt is null && string.IsNullOrEmpty(word))
        {
            Assert.Null(result.AlternateKey);
        }
        else if (expectedAlt is not null)
        {
            Assert.NotNull(result.PrimaryKey);
        }
    }
}
