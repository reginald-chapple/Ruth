using Ruth.Modules.CheckIn.Contracts.Configurations;
using Ruth.Modules.CheckIn.Domain;
using Ruth.Modules.CheckIn.Domain.Configurations;
using Xunit;

namespace Ruth.Modules.CheckIn.Tests.Domain;

public sealed class W3S2CheckInConfigurationAndEligibilityDomainTests
{
    [Fact]
    public void StronglyTypedIds_AndConfigurationValidation_EnforceInvariants()
    {
        var configId = CheckInConfigurationId.Create();
        var areaId = CheckInAreaId.Create();
        var roomCellId = CheckInAreaLocationId.Create();
        Assert.Equal(7, configId.Value.Version);
        Assert.Equal(configId, CheckInConfigurationId.From(configId.Value));
        Assert.Equal(areaId, CheckInAreaId.From(areaId.Value));
        Assert.Equal(roomCellId, CheckInAreaLocationId.From(roomCellId.Value));

        var badPickup = CheckInConfiguration.Create(
            new CheckInConfigurationInput(
                "Youth Program",
                Description: null,
                CheckInProfileKind.YouthAndChildcare,
                BranchId: null,
                CheckInSearchMode.PhoneAndName,
                CheckInStartOffsetMinutes: 30,
                CheckInEndOffsetMinutes: 30,
                SecurityCodeAlphaLength: 1,
                SecurityCodeNumericLength: 1,
                PreventDuplicateCheckIn: true,
                AllowCheckout: true,
                AutoSelectDaysBack: 30,
                IsActive: true
            )
        );
        Assert.True(badPickup.IsFailure);
        Assert.Equal(CheckInErrors.SecurityCodeTooShortForPickup.Code, badPickup.PrimaryError.Code);

        var badWindow = CheckInConfiguration.Create(
            new CheckInConfigurationInput(
                "Facility Access",
                Description: "Bad\0Control",
                CheckInProfileKind.MemberFacilityAccess,
                BranchId: null,
                CheckInSearchMode.BarcodeAndPassOnly,
                CheckInStartOffsetMinutes: 10,
                CheckInEndOffsetMinutes: -20,
                SecurityCodeAlphaLength: 0,
                SecurityCodeNumericLength: 0,
                PreventDuplicateCheckIn: true,
                AllowCheckout: false,
                AutoSelectDaysBack: 60,
                IsActive: true
            )
        );
        Assert.True(badWindow.IsFailure);
        Assert.Equal(CheckInErrors.InvalidControlCharacters.Code, badWindow.PrimaryError.Code);
    }

    [Fact]
    public void EligibilityRule_ValidatesBounds_AndEvaluatorChecksAllCriteria()
    {
        Assert.True(
            CheckInEligibilityRule
                .Create(new CheckInEligibilityRuleDto(MinAgeYears: 12, MaxAgeYears: 8))
                .IsFailure
        );
        Assert.True(
            CheckInEligibilityRule
                .Create(
                    new CheckInEligibilityRuleDto(
                        MinBirthDate: new DateOnly(2020, 5, 1),
                        MaxBirthDate: new DateOnly(2019, 5, 1)
                    )
                )
                .IsFailure
        );
        Assert.True(
            CheckInEligibilityRule
                .Create(new CheckInEligibilityRuleDto(MinGrade: 6, MaxGrade: 3))
                .IsFailure
        );

        var config = CheckInConfiguration
            .Create(
                new CheckInConfigurationInput(
                    "Makerspace & Youth Hub",
                    Description: "Multi-program hub",
                    CheckInProfileKind.ClassesAndPrograms,
                    BranchId: Guid.CreateVersion7(),
                    CheckInSearchMode.PhoneAndName,
                    CheckInStartOffsetMinutes: 30,
                    CheckInEndOffsetMinutes: 30,
                    SecurityCodeAlphaLength: 2,
                    SecurityCodeNumericLength: 2,
                    PreventDuplicateCheckIn: true,
                    AllowCheckout: true,
                    AutoSelectDaysBack: 45,
                    IsActive: true
                )
            )
            .Value;

        var missingGroup = config.UpsertArea(
            new CheckInAreaSpec(
                "Pre-Enrolled Cohort",
                Description: null,
                LinkedGroupId: null,
                RoomBalancingStrategy.OrderedPriority,
                Order: 0,
                IsActive: true,
                PrintBadge: true,
                IssueSecurityPickupCode: false,
                new CheckInEligibilityRuleDto(
                    EnrollmentMode: EligibilityEnrollmentMode.PreEnrolledMembersOnly
                ),
                Rooms: null
            )
        );
        Assert.Equal(
            CheckInErrors.PreEnrolledRequiresLinkedGroup.Code,
            missingGroup.PrimaryError.Code
        );

        var groupId = Guid.CreateVersion7();
        var area = config
            .UpsertArea(
                new CheckInAreaSpec(
                    "Robotics Cohort",
                    Description: "Grades 4-8",
                    LinkedGroupId: groupId,
                    RoomBalancingStrategy.LowestCapacityRatio,
                    Order: 1,
                    IsActive: true,
                    PrintBadge: true,
                    IssueSecurityPickupCode: true,
                    new CheckInEligibilityRuleDto(
                        MinAgeYears: 9m,
                        MaxAgeYears: 14m,
                        MinBirthDate: new DateOnly(2012, 1, 1),
                        MaxBirthDate: new DateOnly(2017, 12, 31),
                        MinGrade: 4,
                        MaxGrade: 8,
                        AllowedMembershipTiers: ["Family", "Youth-Scholarship"],
                        RequiredSkillOrBadgeCodes: ["shop-safety"],
                        SupportsAccommodations: true,
                        RequiresAccommodationFlag: false,
                        EnrollmentMode: EligibilityEnrollmentMode.PreEnrolledMembersOnly
                    ),
                    Rooms: null
                )
            )
            .Value;

        var (matchingEligible, matchingReasons) = CheckInEligibilityEvaluator.Evaluate(
            area,
            new CheckInParticipantContextDto(
                AgeYears: 11.5m,
                BirthDate: new DateOnly(2015, 4, 10),
                Grade: 6,
                MembershipTier: "family",
                SkillAndBadgeCodes: ["SHOP-SAFETY", "first-aid"],
                NeedsAccommodations: false,
                EnrolledGroupIds: [groupId]
            )
        );
        Assert.True(matchingEligible);
        Assert.Empty(matchingReasons);

        var (failingEligible, failingReasons) = CheckInEligibilityEvaluator.Evaluate(
            area,
            new CheckInParticipantContextDto(
                AgeYears: 7.0m,
                BirthDate: new DateOnly(2019, 2, 1),
                Grade: 2,
                MembershipTier: "Basic",
                SkillAndBadgeCodes: [],
                EnrolledGroupIds: []
            )
        );
        Assert.False(failingEligible);
        Assert.Contains("NotPreEnrolledInGroup", failingReasons, StringComparer.Ordinal);
        Assert.Contains("AgeOutOfRange", failingReasons, StringComparer.Ordinal);
        Assert.Contains("BirthDateOutOfRange", failingReasons, StringComparer.Ordinal);
        Assert.Contains("GradeOutOfRange", failingReasons, StringComparer.Ordinal);
        Assert.Contains("MembershipTierNotAllowed", failingReasons, StringComparer.Ordinal);
        Assert.Contains("MissingRequiredSkillOrBadge", failingReasons, StringComparer.Ordinal);
    }

    [Fact]
    public void EligibilityEvaluator_HandlesAccommodationOnly_ShiftMode_AndMissingFields()
    {
        var shiftGroupId = Guid.CreateVersion7();
        var config = CheckInConfiguration
            .Create(
                new CheckInConfigurationInput(
                    "Sensory & Shift Profile",
                    Description: null,
                    CheckInProfileKind.VolunteerAndStaffShifts,
                    BranchId: null,
                    CheckInSearchMode.PhoneAndName,
                    30,
                    30,
                    2,
                    2,
                    PreventDuplicateCheckIn: true,
                    AllowCheckout: true,
                    30,
                    IsActive: true
                )
            )
            .Value;

        var sensoryArea = config
            .UpsertArea(
                new CheckInAreaSpec(
                    "Sensory Support Studio",
                    Description: null,
                    LinkedGroupId: shiftGroupId,
                    RoomBalancingStrategy.OrderedPriority,
                    0,
                    IsActive: true,
                    PrintBadge: true,
                    IssueSecurityPickupCode: false,
                    new CheckInEligibilityRuleDto(
                        MinAgeYears: 5m,
                        MinBirthDate: new DateOnly(2010, 1, 1),
                        MinGrade: 0,
                        RequiresAccommodationFlag: true,
                        EnrollmentMode: EligibilityEnrollmentMode.ScheduledShiftOnly
                    ),
                    Rooms: null
                )
            )
            .Value;

        var (missingEligible, missingReasons) = CheckInEligibilityEvaluator.Evaluate(
            sensoryArea,
            new CheckInParticipantContextDto(NeedsAccommodations: false)
        );
        Assert.False(missingEligible);
        Assert.Contains("NoConfirmedShiftAssignment", missingReasons, StringComparer.Ordinal);
        Assert.Contains("AgeMissing", missingReasons, StringComparer.Ordinal);
        Assert.Contains("BirthDateMissing", missingReasons, StringComparer.Ordinal);
        Assert.Contains("GradeMissing", missingReasons, StringComparer.Ordinal);
        Assert.Contains("AccommodationRequired", missingReasons, StringComparer.Ordinal);
    }
}
