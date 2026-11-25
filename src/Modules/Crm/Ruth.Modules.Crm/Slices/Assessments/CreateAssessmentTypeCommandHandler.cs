using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Crm.Contracts.Assessments;
using Ruth.Modules.Crm.Domain;
using Ruth.Modules.Crm.Domain.Assessments;
using Ruth.Modules.Crm.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Crm.Slices.Assessments;

internal sealed class CreateAssessmentTypeCommandHandler(CrmDbContext dbContext)
    : ICommandHandler<CreateAssessmentTypeCommand, AssessmentTypeResponse>
{
    public async Task<Result<AssessmentTypeResponse>> HandleAsync(
        CreateAssessmentTypeCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var created = AssessmentType.Create(
            command.Key,
            command.Title,
            command.AssessmentPath,
            command.Description,
            command.AssessmentResultsPath,
            command.RequiresRequest,
            command.MinimumDaysToRetake,
            command.ValidDurationDays,
            isSystem: false,
            command.IconName,
            command.BadgeColor
        );
        if (created.IsFailure)
        {
            return Result.Failure<AssessmentTypeResponse>(created.Errors);
        }

        var normalizedKey = created.Value.NormalizedKey;
        var exists = await dbContext.AssessmentTypes.AnyAsync(
            t => t.NormalizedKey == normalizedKey,
            cancellationToken
        );
        if (exists)
        {
            return CrmErrors.AssessmentTypeDuplicate;
        }

        _ = dbContext.AssessmentTypes.Add(created.Value);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(ToResponse(created.Value));
    }

    internal static AssessmentTypeResponse ToResponse(AssessmentType t)
    {
        return new AssessmentTypeResponse(
            t.Id.Value,
            t.Key,
            t.Title,
            t.Description,
            t.AssessmentPath,
            t.AssessmentResultsPath,
            t.IsActive,
            t.RequiresRequest,
            t.MinimumDaysToRetake,
            t.ValidDurationDays,
            t.IsSystem,
            t.IconName,
            t.BadgeColor
        );
    }
}
