using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Finance.Contracts.Assistance;
using Ruth.Modules.Finance.Domain.Assistance;
using Ruth.Modules.Finance.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Finance.Slices.Assistance;

internal sealed class GetAssistanceCasesQueryHandler(FinanceDbContext dbContext)
    : IQueryHandler<GetAssistanceCasesQuery, IReadOnlyList<AssistanceCaseResponse>>
{
    public async Task<Result<IReadOnlyList<AssistanceCaseResponse>>> HandleAsync(
        GetAssistanceCasesQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var efQuery = dbContext
            .AssistanceCases.AsNoTracking()
            .Include(static c => c.Documents)
            .Include(static c => c.Disbursements)
            .AsQueryable();

        if (query.ProgramId.HasValue && query.ProgramId.Value != Guid.Empty)
        {
            var pId = AssistanceProgramId.From(query.ProgramId.Value);
            efQuery = efQuery.Where(c => c.ProgramId == pId);
        }

        if (query.ApplicantPersonId.HasValue && query.ApplicantPersonId.Value != Guid.Empty)
        {
            var appId = query.ApplicantPersonId.Value;
            efQuery = efQuery.Where(c => c.ApplicantPersonId == appId);
        }

        if (query.Status.HasValue)
        {
            var st = query.Status.Value;
            efQuery = efQuery.Where(c => c.Status == st);
        }

        if (query.BranchId.HasValue && query.BranchId.Value != Guid.Empty)
        {
            var br = query.BranchId.Value;
            efQuery = efQuery.Where(c => c.BranchId == br);
        }

        var limit = Math.Clamp(query.Limit, 1, 500);
        var cases = await efQuery
            .OrderByDescending(static c => c.SubmittedAtUtc)
            .Take(limit)
            .ToListAsync(cancellationToken);

        var programs = await dbContext
            .AssistancePrograms.AsNoTracking()
            .ToDictionaryAsync(
                static p => p.Id,
                static p => (p.Name, p.Category),
                cancellationToken
            );

        return cases
            .Select(c =>
            {
                var (name, category) = programs.GetValueOrDefault(
                    c.ProgramId,
                    ("Program", AssistanceProgramCategory.CommunityAid)
                );
                return AssistanceMapper.ToResponse(c, name, category);
            })
            .ToArray();
    }
}
