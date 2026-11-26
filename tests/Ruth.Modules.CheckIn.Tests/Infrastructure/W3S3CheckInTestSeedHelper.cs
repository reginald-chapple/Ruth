using Ruth.Modules.Crm.Contracts.Households;
using Ruth.Modules.Crm.Contracts.People;
using Ruth.Modules.Groups.Contracts.Attendance;
using Ruth.Modules.Groups.Contracts.Groups;
using Ruth.Modules.Groups.Contracts.GroupTypes;
using Ruth.SharedKernel.Messaging;
using Xunit;

namespace Ruth.Modules.CheckIn.Tests.Infrastructure;

internal static class W3S3CheckInTestSeedHelper
{
    public static async Task<Guid> CreateLinkedYouthGroupAsync(ICommandDispatcher commands)
    {
        var gt = await commands.SendAsync<CreateGroupTypeCommand, GroupTypeDetailResponse>(
            new CreateGroupTypeCommand("Youth Check-In Cohorts", TakesAttendance: true)
        );
        Assert.True(gt.IsSuccess);

        var grp = await commands.SendAsync<CreateGroupCommand, GroupDetailResponse>(
            new CreateGroupCommand(gt.Value.Id, "Elementary STEM Cohort")
        );
        Assert.True(grp.IsSuccess);
        return grp.Value.Id;
    }

    public static async Task<(
        Guid HouseholdId,
        Guid ParentId,
        Guid ChildId
    )> SeedHouseholdInCrmAsync(ICommandDispatcher commands)
    {
        var parent = await commands.SendAsync<CreatePersonCommand, PersonDetailResponse>(
            new CreatePersonCommand(
                "Elena",
                "Navarro",
                BirthYear: 1988,
                BirthMonth: 4,
                BirthDay: 12,
                Email: "elena@example.org"
            )
        );
        Assert.True(parent.IsSuccess);

        _ = await commands.SendAsync<UpsertPhoneNumberCommand, PersonDetailResponse>(
            new UpsertPhoneNumberCommand(
                parent.Value.Id,
                PhoneNumberId: null,
                new PhoneNumberInputDto(PhoneNumberType.Mobile, "303-555-8241")
            )
        );
        _ = await commands.SendAsync<AddPersonSearchKeyCommand, PersonDetailResponse>(
            new AddPersonSearchKeyCommand(
                parent.Value.Id,
                PersonSearchKeyType.Barcode,
                "MEM-CARD-7788"
            )
        );

        var child = await commands.SendAsync<CreatePersonCommand, PersonDetailResponse>(
            new CreatePersonCommand(
                "Lucas",
                "Navarro",
                BirthYear: DateTimeOffset.UtcNow.Year - 9,
                BirthMonth: 1,
                BirthDay: 15
            )
        );
        Assert.True(child.IsSuccess);

        _ = await commands.SendAsync<UpdatePersonAttributesCommand, PersonDetailResponse>(
            new UpdatePersonAttributesCommand(
                child.Value.Id,
                new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["MembershipTier"] = "Family",
                    ["Allergies"] = "Peanuts",
                }
            )
        );

        var household = await commands.SendAsync<CreateHouseholdCommand, HouseholdResponse>(
            new CreateHouseholdCommand(
                "Navarro Household",
                Members:
                [
                    new HouseholdMemberInputDto(
                        parent.Value.Id,
                        HouseholdRole.Adult,
                        IsPrimaryContact: true
                    ),
                    new HouseholdMemberInputDto(child.Value.Id, HouseholdRole.Child),
                ]
            )
        );
        Assert.True(household.IsSuccess);
        return (household.Value.Id, parent.Value.Id, child.Value.Id);
    }

    public static async Task AssertGroupPresentCountAsync(
        IQueryDispatcher queries,
        Guid groupId,
        int expectedPresent
    )
    {
        var groupOccurrences = await queries.QueryAsync<
            GetGroupAttendanceOccurrencesQuery,
            IReadOnlyList<AttendanceOccurrenceSummaryResponse>
        >(new GetGroupAttendanceOccurrencesQuery(groupId));
        Assert.True(groupOccurrences.IsSuccess);
        Assert.Equal(expectedPresent, Assert.Single(groupOccurrences.Value).PresentCount);
    }
}
