using Ruth.Modules.Crm.Contracts.Households;
using Ruth.Modules.Crm.Contracts.People;
using Ruth.Modules.Crm.Contracts.Relationships;
using Ruth.Modules.Crm.Domain;
using Ruth.Modules.Crm.Domain.Households;
using Ruth.Modules.Crm.Domain.People;
using Ruth.Modules.Crm.Domain.Relationships;
using Xunit;

namespace Ruth.Modules.Crm.Tests.Domain;

public sealed class W2S2HouseholdsAndRelationshipsDomainTests
{
    private static readonly DateTimeOffset _nowUtc = new(2026, 11, 24, 18, 0, 0, TimeSpan.Zero);

    [Fact]
    public void HouseholdDomainService_ComputesSalutations_PrimaryContact_AndSpouse()
    {
        var partnerA = Person
            .Create(
                new PersonInput(
                    "Robert",
                    "Decker",
                    NickName: "Bob",
                    Gender: Gender.Male,
                    MaritalStatus: MaritalStatus.Married,
                    BirthYear: 1982,
                    BirthMonth: 3,
                    BirthDay: 10
                ),
                _nowUtc
            )
            .Value;
        var partnerB = Person
            .Create(
                new PersonInput(
                    "Elizabeth",
                    "Decker",
                    NickName: "Beth",
                    Gender: Gender.Female,
                    MaritalStatus: MaritalStatus.Married,
                    BirthYear: 1984,
                    BirthMonth: 7,
                    BirthDay: 22
                ),
                _nowUtc
            )
            .Value;
        var child = Person
            .Create(
                new PersonInput(
                    "Noah",
                    "Decker",
                    Gender: Gender.Male,
                    BirthYear: 2014,
                    BirthMonth: 1,
                    BirthDay: 5
                ),
                _nowUtc
            )
            .Value;

        var household = Household.Create("Decker Family").Value;
        _ = household.AddOrUpdateMember(
            partnerA.Id,
            HouseholdRole.Adult,
            0,
            isCombinedContributionMember: true
        );
        _ = household.AddOrUpdateMember(
            partnerB.Id,
            HouseholdRole.Adult,
            0,
            isCombinedContributionMember: true
        );
        _ = household.AddOrUpdateMember(
            child.Id,
            HouseholdRole.Child,
            0,
            isCombinedContributionMember: false
        );

        var map = new Dictionary<PersonId, Person>
        {
            [partnerA.Id] = partnerA,
            [partnerB.Id] = partnerB,
            [child.Id] = child,
        };

        Assert.Equal("Bob & Beth Decker", HouseholdDomainService.ComputeSalutation(household, map));
        Assert.Equal(
            "Bob, Beth & Noah Decker",
            HouseholdDomainService.ComputeSalutation(household, map, includeChildren: true)
        );
        Assert.Equal(
            "Robert & Elizabeth Decker",
            HouseholdDomainService.ComputeSalutation(household, map, useFormalNames: true)
        );
        Assert.Equal(partnerA.Id, HouseholdDomainService.ResolvePrimaryContactId(household, map));

        _ = household.AddOrUpdateMember(
            partnerB.Id,
            HouseholdRole.Adult,
            0,
            isCombinedContributionMember: true,
            isPrimaryContact: true
        );
        Assert.Equal(partnerB.Id, HouseholdDomainService.ResolvePrimaryContactId(household, map));
        Assert.Equal("Beth & Bob Decker", HouseholdDomainService.ComputeSalutation(household, map));

        var resolvedSpouse = HouseholdDomainService.ResolveSpouse(partnerA, [household], map);
        Assert.NotNull(resolvedSpouse);
        Assert.Equal(partnerB.Id, resolvedSpouse.Id);
    }

    [Fact]
    public void Household_SetLocation_DemotesExistingHomeAddressToPrevious()
    {
        var household = Household.Create("Smith Family").Value;
        var oldLoc = Guid.CreateVersion7();
        var newLoc = Guid.CreateVersion7();

        Assert.True(
            household
                .SetLocation(
                    oldLoc,
                    HouseholdLocationType.Home,
                    isMailingLocation: true,
                    isMappedLocation: true,
                    "123 Old St"
                )
                .IsSuccess
        );
        Assert.True(
            household
                .SetLocation(
                    newLoc,
                    HouseholdLocationType.Home,
                    isMailingLocation: true,
                    isMappedLocation: true,
                    "456 New Ave"
                )
                .IsSuccess
        );

        var oldEntry = household.Locations.Single(l => l.LocationId == oldLoc);
        var newEntry = household.Locations.Single(l => l.LocationId == newLoc);

        Assert.Equal(HouseholdLocationType.Previous, oldEntry.LocationType);
        Assert.False(oldEntry.IsMailingLocation);
        Assert.False(oldEntry.IsMappedLocation);
        Assert.Equal(HouseholdLocationType.Home, newEntry.LocationType);
        Assert.Equal(newLoc, HouseholdDomainService.ResolveHomeLocation(household)?.LocationId);
    }

    [Fact]
    public void KnownRelationship_CreatePair_LinksReciprocalInverses_AndRejectsSelfReference()
    {
        var a = PersonId.Create();
        var b = PersonId.Create();

        var selfResult = KnownRelationship.CreatePair(a, a, KnownRelationshipType.Friend);
        Assert.Equal(CrmErrors.RelationshipSelfReference, selfResult.PrimaryError);

        var pair = KnownRelationship.CreatePair(a, b, KnownRelationshipType.CanCheckIn, "Aunt");
        Assert.True(pair.IsSuccess);
        var (forward, inverse) = pair.Value;

        Assert.Equal(KnownRelationshipType.CanCheckIn, forward.RelationshipType);
        Assert.Equal(KnownRelationshipType.AllowCheckInBy, inverse.RelationshipType);
        Assert.Equal(inverse.Id, forward.InverseRelationshipId);
        Assert.Equal(forward.Id, inverse.InverseRelationshipId);
        Assert.Equal(
            KnownRelationshipType.Child,
            KnownRelationshipInverseMap.GetInverse(KnownRelationshipType.Parent)
        );
        Assert.Equal(
            KnownRelationshipType.Sibling,
            KnownRelationshipInverseMap.GetInverse(KnownRelationshipType.Sibling)
        );
    }

    [Fact]
    public void PeerNetworkScoreCalculator_AppliesMonthlyGrowthAndDecay()
    {
        var today = new DateOnly(2026, 11, 24);
        var start = new DateOnly(2026, 1, 24);

        var (grownScore, initialTrend) = PeerNetworkScoreCalculator.Calculate(
            RelationshipStrength.Close,
            start,
            endDate: null,
            today,
            rolePairMultiplier: 1.5m,
            isGrowthEnabled: true,
            previousScore: 15.0m
        );
        Assert.Equal(22.50m, grownScore);
        Assert.Equal(RelationshipTrend.Up, initialTrend);

        var (decayedScore, decayTrend) = PeerNetworkScoreCalculator.Calculate(
            RelationshipStrength.Close,
            start,
            endDate: new DateOnly(2026, 6, 24),
            today,
            rolePairMultiplier: 1.0m,
            isGrowthEnabled: true,
            previousScore: 10.0m
        );
        Assert.Equal(5.61m, decayedScore);
        Assert.Equal(RelationshipTrend.Down, decayTrend);

        var (zeroScore, zeroTrend) = PeerNetworkScoreCalculator.Calculate(
            RelationshipStrength.None,
            start,
            endDate: null,
            today
        );
        Assert.Equal(0m, zeroScore);
        Assert.Equal(RelationshipTrend.Level, zeroTrend);
    }
}
