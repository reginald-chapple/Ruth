using Ruth.Modules.Crm.Contracts.BackgroundChecks;
using Ruth.Modules.Crm.Contracts.Households;
using Ruth.Modules.Crm.Contracts.People;
using Ruth.Modules.Crm.Contracts.Relationships;
using Ruth.Modules.Crm.Domain;
using Ruth.Modules.Crm.Domain.BackgroundChecks;
using Ruth.Modules.Crm.Domain.Duplicates;
using Ruth.Modules.Crm.Domain.People;
using Ruth.Modules.Crm.Domain.Relationships;
using Ruth.Modules.Crm.Domain.Signals;
using Xunit;

namespace Ruth.Modules.Crm.Tests.Domain;

public sealed class W2S4CrapCoverageClosureDomainTests
{
    [Theory]
    [InlineData("Xavier")]
    [InlineData("Alex")]
    [InlineData("George")]
    [InlineData("Ghoul")]
    [InlineData("Knight")]
    [InlineData("Sugar")]
    [InlineData("Schultz")]
    [InlineData("Caesar")]
    [InlineData("Czech")]
    [InlineData("Church")]
    [InlineData("Zebra")]
    [InlineData("Wright")]
    [InlineData("Philip")]
    [InlineData("Thomas")]
    [InlineData("Jorgen")]
    [InlineData("Vance")]
    [InlineData("Hanna")]
    [InlineData("Yvette")]
    [InlineData("McDonald")]
    [InlineData("Wanda")]
    [InlineData("Science")]
    [InlineData("Session")]
    public void DoubleMetaphoneEncoder_EncodesDiversePhoneticConsonants(string word)
    {
        var encoded = DoubleMetaphoneEncoder.Encode(word);
        Assert.False(string.IsNullOrWhiteSpace(encoded.PrimaryKey));
        Assert.True(encoded.PrimaryKey.Length <= 4);
    }

    [Theory]
    [InlineData(KnownRelationshipType.CanCheckIn, KnownRelationshipType.AllowCheckInBy)]
    [InlineData(KnownRelationshipType.AllowCheckInBy, KnownRelationshipType.CanCheckIn)]
    [InlineData(KnownRelationshipType.Parent, KnownRelationshipType.Child)]
    [InlineData(KnownRelationshipType.Child, KnownRelationshipType.Parent)]
    [InlineData(KnownRelationshipType.Grandparent, KnownRelationshipType.Grandchild)]
    [InlineData(KnownRelationshipType.Grandchild, KnownRelationshipType.Grandparent)]
    [InlineData(KnownRelationshipType.Mentor, KnownRelationshipType.Mentee)]
    [InlineData(KnownRelationshipType.Mentee, KnownRelationshipType.Mentor)]
    [InlineData(KnownRelationshipType.Employer, KnownRelationshipType.Employee)]
    [InlineData(KnownRelationshipType.Employee, KnownRelationshipType.Employer)]
    [InlineData(KnownRelationshipType.EmergencyContact, KnownRelationshipType.EmergencyContactFor)]
    [InlineData(KnownRelationshipType.EmergencyContactFor, KnownRelationshipType.EmergencyContact)]
    [InlineData(KnownRelationshipType.Sibling, KnownRelationshipType.Sibling)]
    [InlineData(KnownRelationshipType.Spouse, KnownRelationshipType.Spouse)]
    [InlineData(KnownRelationshipType.Friend, KnownRelationshipType.Friend)]
    public void KnownRelationshipInverseMap_MapsEveryRelationshipType(
        KnownRelationshipType input,
        KnownRelationshipType expectedInverse
    )
    {
        Assert.Equal(expectedInverse, KnownRelationshipInverseMap.GetInverse(input));
    }

    [Theory]
    [InlineData("+44 20 7946 0958", null, "44", "2079460958")]
    [InlineData("+442079460958", "44", "44", "2079460958")]
    [InlineData("+15551234567", null, "1", "5551234567")]
    [InlineData("+493012345678", null, "49", "3012345678")]
    [InlineData("555-1234", null, "1", "5551234")]
    public void PhoneNumberNormalizer_HandlesInternationalAndShortFormats(
        string raw,
        string? country,
        string expectedCc,
        string expectedDigits
    )
    {
        var result = PhoneNumberNormalizer.Normalize(raw, country);
        Assert.True(result.IsSuccess);
        Assert.Equal(expectedCc, result.Value.CountryCode);
        Assert.Equal(expectedDigits, result.Value.Digits);
    }

    [Theory]
    [InlineData("")]
    [InlineData("123")]
    [InlineData("555-123-4567", "9999")]
    public void PhoneNumberNormalizer_RejectsInvalidInputs(string raw, string? country = null)
    {
        Assert.True(PhoneNumberNormalizer.Normalize(raw, country).IsFailure);
    }

    [Theory]
    [InlineData(null, AgeBracket.Unknown)]
    [InlineData(4, AgeBracket.ZeroToFive)]
    [InlineData(10, AgeBracket.SixToTwelve)]
    [InlineData(16, AgeBracket.ThirteenToSeventeen)]
    [InlineData(21, AgeBracket.EighteenToTwentyFour)]
    [InlineData(30, AgeBracket.TwentyFiveToThirtyFour)]
    [InlineData(40, AgeBracket.ThirtyFiveToFortyFour)]
    [InlineData(50, AgeBracket.FortyFiveToFiftyFour)]
    [InlineData(60, AgeBracket.FiftyFiveToSixtyFour)]
    [InlineData(72, AgeBracket.SixtyFiveOrOlder)]
    public void PersonDemographicsCalculator_CoversAllAgeBrackets(int? age, AgeBracket expected)
    {
        Assert.Equal(expected, PersonDemographicsCalculator.CalculateAgeBracket(age));
    }

    [Fact]
    public void DuplicatePhones_SignalTypeUpdate_AndBackgroundCheckStatuses_Work()
    {
        var p1 = new DuplicateCandidateProfile(
            PersonId.Create(),
            "Robert",
            "Bob",
            "Miller",
            Suffix: null,
            NormalizedEmail: null,
            Gender.Male,
            MaritalStatus.Married,
            new DateOnly(1985, 3, 10),
            PrimaryBranchId: null,
            Guid.CreateVersion7(),
            HouseholdRole.Adult,
            PrimaryHomeLocationId: null,
            ["1:5551112222:"],
            ["1:5553334444:"],
            "RPRT",
            "MLR",
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Robert", "Bob" }
        );
        var p2 = p1 with
        {
            PersonId = PersonId.Create(),
            PrimaryHouseholdId = Guid.CreateVersion7(),
        };
        var eval = PersonDuplicateScorer.Evaluate(p1, p2);
        Assert.True(eval.IsCandidateMatch);
        Assert.Contains("Phone", eval.ScoreDetail, StringComparison.Ordinal);

        var noExpandedEval = PersonDuplicateScorer.Evaluate(
            p1 with
            {
                ExpandedFirstNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            },
            p2 with
            {
                FirstName = "Bobby",
                NickName = "Bob",
                ExpandedFirstNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            }
        );
        Assert.True(noExpandedEval.IsCandidateMatch);

        var signal = SignalType.Create("VIP", "#0078D4").Value;
        Assert.True(signal.Update("Allergy", "#D13438", "warning", "Peanut", 1).IsSuccess);
        Assert.True(
            signal.Update("", "#D13438", signalIcon: null, description: null, order: 1).IsFailure
        );
        Assert.True(CrmTextPolicy.ContainsInvalidControlChars("bad\0name", allowNewLines: false));

        var bgCheck = BackgroundCheck.Create(
            BackgroundCheckId.Create(),
            PersonId.Create(),
            "checkr",
            "tasker_standard",
            "cand_x",
            BackgroundCheckStatus.Requested,
            "requested",
            DateTimeOffset.UtcNow
        );
        foreach (
            var status in new[]
            {
                BackgroundCheckStatus.InvitationSent,
                BackgroundCheckStatus.InvitationCompleted,
                BackgroundCheckStatus.ReportPending,
                BackgroundCheckStatus.ReportSuspended,
                BackgroundCheckStatus.ReportConsider,
            }
        )
        {
            bgCheck.ApplyWebhookEvent(
                new BackgroundCheckWebhookEvent(
                    Guid.CreateVersion7().ToString("D"),
                    "report.updated",
                    "cand_x",
                    "rep_x",
                    "tasker_standard",
                    status,
                    RecordFound: null,
                    Recommendation: string.Empty,
                    RawPayloadJson: "{}"
                ),
                DateTimeOffset.UtcNow
            );
            Assert.Equal(status, bgCheck.Status);
        }
    }
}
