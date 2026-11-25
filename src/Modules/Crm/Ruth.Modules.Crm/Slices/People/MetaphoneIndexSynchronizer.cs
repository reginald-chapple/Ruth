using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Crm.Domain.People;
using Ruth.Modules.Crm.Infrastructure;

namespace Ruth.Modules.Crm.Slices.People;

internal static class MetaphoneIndexSynchronizer
{
    public static async Task EnsureIndexedAsync(
        CrmDbContext dbContext,
        Person person,
        CancellationToken cancellationToken
    )
    {
        var entries = person
            .GetMetaphoneTokens()
            .Select(MetaphoneEntry.TryCreate)
            .OfType<MetaphoneEntry>()
            .GroupBy(static m => m.Name, StringComparer.Ordinal)
            .Select(static g => g.First())
            .ToList();

        if (entries.Count == 0)
        {
            return;
        }

        var keys = entries.Select(static e => e.Name).ToArray();
        var existing = await dbContext
            .Metaphones.AsNoTracking()
            .Where(m => keys.Contains(m.Name))
            .Select(static m => m.Name)
            .ToListAsync(cancellationToken);

        var existingSet = new HashSet<string>(existing, StringComparer.Ordinal);
        foreach (var entry in entries)
        {
            if (
                !existingSet.Contains(entry.Name)
                && dbContext.Metaphones.Local.All(m =>
                    !string.Equals(m.Name, entry.Name, StringComparison.Ordinal)
                )
            )
            {
                _ = dbContext.Metaphones.Add(entry);
            }
        }
    }
}
