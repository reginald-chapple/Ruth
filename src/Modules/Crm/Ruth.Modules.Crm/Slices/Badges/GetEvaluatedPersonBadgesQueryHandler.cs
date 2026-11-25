using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Crm.Contracts.Assessments;
using Ruth.Modules.Crm.Contracts.Badges;
using Ruth.Modules.Crm.Domain;
using Ruth.Modules.Crm.Domain.Badges;
using Ruth.Modules.Crm.Domain.People;
using Ruth.Modules.Crm.Infrastructure;
using Ruth.Modules.Crm.Slices.People;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Crm.Slices.Badges;

internal sealed class GetEvaluatedPersonBadgesQueryHandler(
    CrmDbContext dbContext,
    IEnumerable<IPersonBadgeEvaluator> evaluators,
    IClock clock
) : IQueryHandler<GetEvaluatedPersonBadgesQuery, IReadOnlyList<EvaluatedPersonBadgeResponse>>
{
    private readonly Dictionary<string, IPersonBadgeEvaluator> _evaluators = evaluators
        .GroupBy(static e => e.ComponentKey, StringComparer.OrdinalIgnoreCase)
        .ToDictionary(static g => g.Key, static g => g.Last(), StringComparer.OrdinalIgnoreCase);

    public async Task<Result<IReadOnlyList<EvaluatedPersonBadgeResponse>>> HandleAsync(
        GetEvaluatedPersonBadgesQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var context = await BuildContextAsync(PersonId.From(query.PersonId), cancellationToken);
        if (context is null)
        {
            return CrmErrors.PersonNotFound;
        }

        var definitions = await dbContext
            .BadgeDefinitions.AsNoTracking()
            .Where(static b => b.IsActive)
            .OrderBy(static b => b.Order)
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<EvaluatedPersonBadgeResponse>>(
            EvaluateAll(definitions, context)
        );
    }

    private async Task<PersonBadgeEvaluationContext?> BuildContextAsync(
        PersonId personId,
        CancellationToken cancellationToken
    )
    {
        var person = await dbContext
            .People.AsNoTracking()
            .AsSplitQuery()
            .Include(static p => p.PhoneNumbers)
            .Include(static p => p.PreviousNames)
            .Include(static p => p.SearchKeys)
            .FirstOrDefaultAsync(p => p.Id == personId, cancellationToken);
        if (person is null)
        {
            return null;
        }

        var nowUtc = clock.UtcNow;
        var detail = PersonMapper.ToDetail(person, DateOnly.FromDateTime(nowUtc.UtcDateTime));
        var completed = await dbContext
            .Assessments.AsNoTracking()
            .Where(a => a.PersonId == personId && a.Status == AssessmentStatus.Complete)
            .OrderByDescending(static a => a.CompletedOnUtc)
            .Select(static a => a.SummaryText)
            .ToListAsync(cancellationToken);

        var latestCheck = await dbContext
            .BackgroundChecks.AsNoTracking()
            .Where(b => b.PersonId == personId)
            .OrderByDescending(static b => b.RequestedOnUtc)
            .Select(static b => new { b.Recommendation, b.RecordFound })
            .FirstOrDefaultAsync(cancellationToken);

        return new PersonBadgeEvaluationContext(
            detail,
            completed.Count,
            completed.FirstOrDefault(static s => !string.IsNullOrWhiteSpace(s)),
            latestCheck?.Recommendation,
            latestCheck?.RecordFound,
            nowUtc
        );
    }

    private List<EvaluatedPersonBadgeResponse> EvaluateAll(
        List<BadgeDefinition> definitions,
        PersonBadgeEvaluationContext context
    )
    {
        var rendered = new List<EvaluatedPersonBadgeResponse>(definitions.Count);
        foreach (var definition in definitions)
        {
            if (!_evaluators.TryGetValue(definition.BadgeComponentKey, out var evaluator))
            {
                continue;
            }

            var evaluated = evaluator.Evaluate(
                CreateBadgeDefinitionCommandHandler.ToResponse(definition),
                context
            );
            if (evaluated is not null)
            {
                rendered.Add(evaluated);
            }
        }

        return rendered;
    }
}
