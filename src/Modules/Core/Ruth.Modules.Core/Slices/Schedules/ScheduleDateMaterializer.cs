using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Core.Domain;
using Ruth.Modules.Core.Domain.Schedules;
using Ruth.Modules.Core.Infrastructure;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Core.Slices.Schedules;

internal static class ScheduleDateMaterializer
{
    public const int HorizonYears = 2;

    public static async Task SyncScheduleDatesAsync(
        CoreDbContext dbContext,
        Schedule schedule,
        IClock clock,
        string? timeZoneId,
        CancellationToken cancellationToken
    )
    {
        var existing = await dbContext
            .ScheduleDates.Where(d => d.ScheduleId == schedule.Id)
            .ToListAsync(cancellationToken);

        if (existing.Count > 0)
        {
            dbContext.ScheduleDates.RemoveRange(existing);
            _ = await dbContext.SaveChangesAsync(cancellationToken);
        }

        if (!schedule.IsActive)
        {
            return;
        }

        var exclusions = await LoadCategoryExclusionsAsync(
            dbContext,
            schedule.CategoryId,
            cancellationToken
        );
        var nowUtc = clock.UtcNow;
        var occurrences = schedule.GetOccurrences(
            nowUtc.AddYears(-HorizonYears),
            nowUtc.AddYears(HorizonYears),
            timeZoneId,
            exclusions
        );

        var distinctByStart = new Dictionary<DateTimeOffset, ScheduleDate>();
        foreach (var occ in occurrences)
        {
            _ = distinctByStart.TryAdd(
                occ.StartOnUtc,
                new ScheduleDate(schedule.Id, occ.StartOnUtc, occ.EndOnUtc, occ.StartDateKey)
            );
        }

        if (distinctByStart.Count > 0)
        {
            dbContext.ScheduleDates.AddRange(distinctByStart.Values);
            _ = await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public static async Task SyncCategorySchedulesAsync(
        CoreDbContext dbContext,
        CategoryId categoryId,
        IClock clock,
        CancellationToken cancellationToken
    )
    {
        var schedules = await dbContext
            .Schedules.Where(s => s.CategoryId == categoryId)
            .ToListAsync(cancellationToken);
        foreach (var schedule in schedules)
        {
            await SyncScheduleDatesAsync(
                dbContext,
                schedule,
                clock,
                timeZoneId: null,
                cancellationToken
            );
        }
    }

    public static async Task<
        IReadOnlyList<(DateTimeOffset StartUtc, DateTimeOffset EndUtc)>
    > LoadCategoryExclusionsAsync(
        CoreDbContext dbContext,
        CategoryId? categoryId,
        CancellationToken cancellationToken
    )
    {
        if (!categoryId.HasValue)
        {
            return [];
        }

        var cid = categoryId.Value;
        var rows = await dbContext
            .ScheduleCategoryExclusions.AsNoTracking()
            .Where(e => e.CategoryId == cid)
            .Select(static e => new { e.StartOnUtc, e.EndOnUtc })
            .ToListAsync(cancellationToken);

        return [.. rows.Select(static r => (r.StartOnUtc, r.EndOnUtc))];
    }
}
