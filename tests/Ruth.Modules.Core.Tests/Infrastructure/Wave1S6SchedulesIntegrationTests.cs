using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Core.Contracts.Categories;
using Ruth.Modules.Core.Contracts.EntityTypes;
using Ruth.Modules.Core.Contracts.Schedules;
using Ruth.Modules.Core.Domain.Schedules;
using Ruth.Modules.Core.Infrastructure;
using Ruth.Modules.Core.Tests.Fixtures;
using Ruth.SharedKernel.Messaging;
using Xunit;

namespace Ruth.Modules.Core.Tests.Infrastructure;

[Collection(CorePostgresFixtures.Name)]
public sealed class Wave1S6SchedulesIntegrationTests(CorePostgresContainerFixture postgresFixture)
{
    [Fact]
    public async Task Schedule_Materializes_ScheduleDates_And_Applies_Category_Exclusions()
    {
        var cs = await postgresFixture.CreateIsolatedDatabaseAsync();
        await using var provider = await CoreTestHostFactory.BuildAndMigrateAsync(cs);
        await using var scope = provider.CreateAsyncScope();
        var commands = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();
        var queries = scope.ServiceProvider.GetRequiredService<IQueryDispatcher>();
        var db = scope.ServiceProvider.GetRequiredService<CoreDbContext>();

        var nextSunday = ResolveNextSundayUtc();
        var (categoryId, scheduleId) = await CreateWeekendScheduleAsync(
            commands,
            queries,
            nextSunday
        );

        var sid = ScheduleId.From(scheduleId);
        var materializedBefore = await db.ScheduleDates.CountAsync(d => d.ScheduleId == sid);
        Assert.True(materializedBefore >= 100);

        await ExcludeDateAndAssertRemovedAsync(
            commands,
            queries,
            db,
            categoryId,
            scheduleId,
            nextSunday,
            materializedBefore
        );
    }

    private static DateTime ResolveNextSundayUtc()
    {
        var today = DateTime.UtcNow.Date;
        return today.AddDays(((int)DayOfWeek.Sunday - (int)today.DayOfWeek + 7) % 7);
    }

    private static async Task<(Guid CategoryId, Guid ScheduleId)> CreateWeekendScheduleAsync(
        ICommandDispatcher commands,
        IQueryDispatcher queries,
        DateTime nextSunday
    )
    {
        var scheduleEntityType = (
            await queries.QueryAsync<GetEntityTypeByKeyQuery, EntityTypeResponse>(
                new GetEntityTypeByKeyQuery(Schedule.EntityTypeKeyName)
            )
        ).Value;

        var category = (
            await commands.SendAsync<CreateCategoryCommand, CategoryResponse>(
                new CreateCategoryCommand(scheduleEntityType.Id, "Weekend Services")
            )
        ).Value;

        var ical = ICalendarParser.BuildSimpleICalendar(
            nextSunday.AddHours(9),
            TimeSpan.FromMinutes(75),
            rrule: "FREQ=WEEKLY;BYDAY=SU"
        );

        var schedule = (
            await commands.SendAsync<CreateScheduleCommand, ScheduleResponse>(
                new CreateScheduleCommand(
                    Name: "Sunday 9:00 AM",
                    ICalendarContent: ical,
                    CheckInStartOffsetMinutes: 60,
                    CheckInEndOffsetMinutes: 30,
                    CategoryId: category.Id,
                    TimeZoneId: "UTC"
                )
            )
        ).Value;

        return (category.Id, schedule.Id);
    }

    private static async Task ExcludeDateAndAssertRemovedAsync(
        ICommandDispatcher commands,
        IQueryDispatcher queries,
        CoreDbContext db,
        Guid categoryId,
        Guid scheduleId,
        DateTime nextSunday,
        int materializedBefore
    )
    {
        var excludeDate = DateOnly.FromDateTime(nextSunday);
        var exclusion = await commands.SendAsync<
            CreateScheduleCategoryExclusionCommand,
            ScheduleCategoryExclusionResponse
        >(
            new CreateScheduleCategoryExclusionCommand(
                categoryId,
                "Holiday Break",
                excludeDate,
                excludeDate
            )
        );
        Assert.True(exclusion.IsSuccess);

        var sid = ScheduleId.From(scheduleId);
        var materializedAfter = await db.ScheduleDates.CountAsync(d => d.ScheduleId == sid);
        Assert.Equal(materializedBefore - 1, materializedAfter);

        var excludedKey = (nextSunday.Year * 10000) + (nextSunday.Month * 100) + nextSunday.Day;
        var occurrences = (
            await queries.QueryAsync<
                GetScheduleOccurrencesQuery,
                IReadOnlyList<ScheduleOccurrenceDto>
            >(
                new GetScheduleOccurrencesQuery(
                    scheduleId,
                    new DateTimeOffset(nextSunday.AddDays(-1), TimeSpan.Zero),
                    new DateTimeOffset(nextSunday.AddDays(14), TimeSpan.Zero),
                    "UTC"
                )
            )
        ).Value;
        Assert.DoesNotContain(occurrences, o => o.StartDateKey == excludedKey);
    }
}
