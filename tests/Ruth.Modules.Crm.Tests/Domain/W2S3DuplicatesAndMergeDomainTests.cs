using Ruth.Modules.Crm.Contracts.Duplicates;
using Ruth.Modules.Crm.Contracts.Households;
using Ruth.Modules.Crm.Contracts.People;
using Ruth.Modules.Crm.Domain;
using Ruth.Modules.Crm.Domain.Duplicates;
using Ruth.Modules.Crm.Domain.People;
using Xunit;

namespace Ruth.Modules.Crm.Tests.Domain;

public sealed class W2S3DuplicatesAndMergeDomainTests
{
    private static readonly DateTimeOffset _nowUtc = new(2026, 11, 24, 18, 0, 0, TimeSpan.Zero);

    [Fact]
    public void PersonDuplicateScorer_ScoresMatchingProfiles_AndIgnoresSameHouseholdPhone()
    {
        var branchId = Guid.CreateVersion7();
        var homeLocId = Guid.CreateVersion7();

        var p1 = new DuplicateCandidateProfile(
            PersonId.Create(),
            "Robert",
            "Bob",
            "Decker",
            "Jr",
            "BOB@EXAMPLE.ORG",
            Gender.Male,
            MaritalStatus.Married,
            new DateOnly(1984, 4, 15),
            branchId,
            Guid.CreateVersion7(),
            HouseholdRole.Adult,
            homeLocId,
            ["1:6025550199:"],
            [],
            "RPRT",
            "TKR",
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Robert", "Bob" }
        );

        var p2 = p1 with
        {
            PersonId = PersonId.Create(),
            FirstName = "Bob",
            PrimaryHouseholdId = Guid.CreateVersion7(),
        };

        var eval = PersonDuplicateScorer.Evaluate(p1, p2);
        Assert.True(eval.IsCandidateMatch);
        Assert.True(eval.ConfidenceScore >= 80m);
        Assert.Contains("Email", eval.ScoreDetail, StringComparison.Ordinal);
        Assert.Contains("CellPhone", eval.ScoreDetail, StringComparison.Ordinal);
        Assert.Contains("Address", eval.ScoreDetail, StringComparison.Ordinal);
        Assert.Contains("Birthdate", eval.ScoreDetail, StringComparison.Ordinal);
        Assert.Contains("Branch", eval.ScoreDetail, StringComparison.Ordinal);

        var conflictingSuffix = p2 with
        {
            Suffix = "Sr",
            NormalizedEmail = null,
            CellPhoneKeys = [],
            PrimaryHomeLocationId = null,
            BirthDate = null,
            PrimaryBranchId = null,
        };
        Assert.False(PersonDuplicateScorer.Evaluate(p1, conflictingSuffix).IsCandidateMatch);
    }

    [Fact]
    public void PersonDuplicate_ResetsIgnoreUntilScoreChanges_WhenConfidenceDrifts()
    {
        var a = PersonId.Create();
        var b = PersonId.Create();
        var eval1 = new DuplicateScoreEvaluation(
            IsCandidateMatch: true,
            Score: 50,
            Capacity: 70,
            TotalCapacity: PersonDuplicateScorer.MaxPossibleWeight,
            ConfidenceScore: 71.9m,
            "Email|FirstName|LastName"
        );

        var dup = PersonDuplicate.Create(a, b, eval1, _nowUtc);
        Assert.True(dup.Person1Id.Value.CompareTo(dup.Person2Id.Value) <= 0);

        dup.SetReviewStatus(isConfirmedAsNotDuplicate: false, ignoreUntilScoreChanges: true);
        Assert.True(dup.IgnoreUntilScoreChanges);

        var eval2 = new DuplicateScoreEvaluation(
            IsCandidateMatch: true,
            Score: 70,
            Capacity: 80,
            TotalCapacity: PersonDuplicateScorer.MaxPossibleWeight,
            ConfidenceScore: 85.0m,
            "Email|FirstName|LastName|CellPhone"
        );
        dup.UpdateFromScan(eval2, _nowUtc.AddDays(1));
        Assert.False(dup.IgnoreUntilScoreChanges);
        Assert.Equal(85.0m, dup.ConfidenceScore);
    }

    [Fact]
    public void PersonMergeFieldResolver_EnforcesAccountProtectionGuardrails_AndFieldWinners()
    {
        var highProtected = Person
            .Create(
                new PersonInput(
                    "Director",
                    "Smith",
                    Email: "director@civic.org",
                    AccountProtectionProfile: AccountProtectionProfile.High
                ),
                _nowUtc
            )
            .Value;

        var duplicate = Person
            .Create(
                new PersonInput(
                    "John",
                    "Smith",
                    Email: "john.personal@example.org",
                    AccountProtectionProfile: AccountProtectionProfile.Low
                ),
                _nowUtc
            )
            .Value;

        var blocked = PersonMergeFieldResolver.ValidateMergeEligibility(
            highProtected,
            duplicate,
            hasElevatedMergeAuthorization: false
        );
        Assert.Equal(CrmErrors.MergeProtectionForbidden, blocked.PrimaryError);

        var allowed = PersonMergeFieldResolver.ValidateMergeEligibility(
            highProtected,
            duplicate,
            hasElevatedMergeAuthorization: true
        );
        Assert.True(allowed.IsSuccess);

        var mergedInput = PersonMergeFieldResolver.BuildMergedInput(
            highProtected,
            duplicate,
            new PersonMergeFieldSelections(
                FirstName: PersonMergeFieldChoice.Retired,
                Email: PersonMergeFieldChoice.Surviving
            )
        );
        Assert.Equal("John", mergedInput.FirstName);
        Assert.Equal("director@civic.org", mergedInput.Email);
        Assert.Equal(AccountProtectionProfile.High, mergedInput.AccountProtectionProfile);
    }
}
