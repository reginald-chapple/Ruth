using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Workflow.Contracts.Forms;
using Ruth.Modules.Workflow.Domain;
using Ruth.Modules.Workflow.Domain.Forms;
using Ruth.Modules.Workflow.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Workflow.Slices.Forms;

internal sealed class CreateFormBuilderTemplateCommandHandler(WorkflowDbContext dbContext)
    : ICommandHandler<CreateFormBuilderTemplateCommand, FormBuilderTemplateResponse>
{
    public async Task<Result<FormBuilderTemplateResponse>> HandleAsync(
        CreateFormBuilderTemplateCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var created = FormBuilderTemplate.Create(command.Spec);
        if (created.IsFailure)
        {
            return Result.Failure<FormBuilderTemplateResponse>(created.PrimaryError);
        }

        var slugExists = await dbContext.FormBuilderTemplates.AnyAsync(
            t => t.Slug == created.Value.Slug,
            cancellationToken
        );
        if (slugExists)
        {
            return Result.Failure<FormBuilderTemplateResponse>(
                WorkflowErrors.FormTemplateSlugConflict
            );
        }

        _ = dbContext.FormBuilderTemplates.Add(created.Value);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(WorkflowProjectionMapper.MapFormTemplate(created.Value));
    }
}
