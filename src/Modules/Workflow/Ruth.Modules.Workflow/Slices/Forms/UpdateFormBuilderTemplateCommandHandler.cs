using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Workflow.Contracts.Forms;
using Ruth.Modules.Workflow.Domain;
using Ruth.Modules.Workflow.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Workflow.Slices.Forms;

internal sealed class UpdateFormBuilderTemplateCommandHandler(WorkflowDbContext dbContext)
    : ICommandHandler<UpdateFormBuilderTemplateCommand, FormBuilderTemplateResponse>
{
    public async Task<Result<FormBuilderTemplateResponse>> HandleAsync(
        UpdateFormBuilderTemplateCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var id = FormBuilderTemplateId.From(command.TemplateId);
        var entity = await dbContext.FormBuilderTemplates.FirstOrDefaultAsync(
            t => t.Id == id,
            cancellationToken
        );
        if (entity is null)
        {
            return Result.Failure<FormBuilderTemplateResponse>(WorkflowErrors.FormTemplateNotFound);
        }

        var res = entity.Update(command.Spec);
        if (res.IsFailure)
        {
            return Result.Failure<FormBuilderTemplateResponse>(res.PrimaryError);
        }

        var conflict = await dbContext.FormBuilderTemplates.AnyAsync(
            t => t.Id != id && t.Slug == entity.Slug,
            cancellationToken
        );
        if (conflict)
        {
            return Result.Failure<FormBuilderTemplateResponse>(
                WorkflowErrors.FormTemplateSlugConflict
            );
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(WorkflowProjectionMapper.MapFormTemplate(entity));
    }
}
