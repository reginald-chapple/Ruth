using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Finance.Contracts.Assistance;
using Ruth.Modules.Finance.Domain;
using Ruth.Modules.Finance.Domain.Assistance;
using Ruth.Modules.Finance.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Finance.Slices.Assistance;

internal sealed class GetAssistanceCaseByIdQueryHandler(FinanceDbContext dbContext)
    : IQueryHandler<GetAssistanceCaseByIdQuery, AssistanceCaseResponse>
{
    public async Task<Result<AssistanceCaseResponse>> HandleAsync(
        GetAssistanceCaseByIdQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var id = AssistanceCaseId.From(query.CaseId);
        var entity = await dbContext
            .AssistanceCases.AsNoTracking()
            .Include(static c => c.Documents)
            .Include(static c => c.Disbursements)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (entity is null)
        {
            return FinanceErrors.AssistanceCaseNotFound;
        }

        var program = await dbContext
            .AssistancePrograms.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == entity.ProgramId, cancellationToken);

        return AssistanceMapper.ToResponse(
            entity,
            program?.Name ?? "Program",
            program?.Category ?? AssistanceProgramCategory.CommunityAid
        );
    }
}
