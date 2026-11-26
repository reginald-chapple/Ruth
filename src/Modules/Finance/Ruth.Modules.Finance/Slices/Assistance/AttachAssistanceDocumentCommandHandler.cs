using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Finance.Contracts.Assistance;
using Ruth.Modules.Finance.Domain;
using Ruth.Modules.Finance.Domain.Assistance;
using Ruth.Modules.Finance.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Finance.Slices.Assistance;

internal sealed class AttachAssistanceDocumentCommandHandler(
    FinanceDbContext dbContext,
    IClock clock
) : ICommandHandler<AttachAssistanceDocumentCommand, AssistanceCaseResponse>
{
    public async Task<Result<AssistanceCaseResponse>> HandleAsync(
        AttachAssistanceDocumentCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var caseId = AssistanceCaseId.From(command.CaseId);
        var entity = await dbContext
            .AssistanceCases.Include(static c => c.Documents)
            .Include(static c => c.Disbursements)
            .FirstOrDefaultAsync(c => c.Id == caseId, cancellationToken);
        if (entity is null)
        {
            return FinanceErrors.AssistanceCaseNotFound;
        }

        var program = await dbContext
            .AssistancePrograms.AsNoTracking()
            .FirstAsync(p => p.Id == entity.ProgramId, cancellationToken);

        var added = entity.AttachDocument(
            command.Title,
            command.DocumentCategory,
            command.BinaryFileId,
            command.UploadedByPersonId,
            command.IsVerified,
            clock.UtcNow
        );
        if (added.IsFailure)
        {
            return Result.Failure<AssistanceCaseResponse>(added.Errors);
        }

        // Explicitly add child entity so EF Core tracks it as Added rather than Modified when PK is pre-assigned.
        _ = dbContext.AssistanceDocuments.Add(added.Value);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return AssistanceMapper.ToResponse(entity, program.Name, program.Category);
    }
}
