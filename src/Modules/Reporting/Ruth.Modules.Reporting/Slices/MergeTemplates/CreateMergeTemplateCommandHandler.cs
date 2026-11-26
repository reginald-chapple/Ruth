using Ruth.Modules.Reporting.Contracts.MergeTemplates;
using Ruth.Modules.Reporting.Domain.MergeTemplates;
using Ruth.Modules.Reporting.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Reporting.Slices.MergeTemplates;

internal sealed class CreateMergeTemplateCommandHandler(ReportingDbContext db)
    : ICommandHandler<CreateMergeTemplateCommand, MergeTemplateResponse>
{
    public async Task<Result<MergeTemplateResponse>> HandleAsync(
        CreateMergeTemplateCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var created = MergeTemplate.Create(
            command.Name,
            command.Format,
            command.TemplateBody,
            command.Description,
            command.HeaderMarkup,
            command.FooterMarkup,
            command.CategoryId,
            command.IsGlobal,
            command.OwnerPersonId
        );
        if (created.IsFailure)
        {
            return Result.Failure<MergeTemplateResponse>(created.PrimaryError);
        }

        _ = db.MergeTemplates.Add(created.Value);
        _ = await db.SaveChangesAsync(cancellationToken);
        return Result.Success(ReportingDtoMapper.ToResponse(created.Value));
    }
}
