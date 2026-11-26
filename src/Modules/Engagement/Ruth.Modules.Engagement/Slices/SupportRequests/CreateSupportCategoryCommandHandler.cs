using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Engagement.Contracts.SupportRequests;
using Ruth.Modules.Engagement.Domain;
using Ruth.Modules.Engagement.Domain.SupportRequests;
using Ruth.Modules.Engagement.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Engagement.Slices.SupportRequests;

internal sealed class CreateSupportCategoryCommandHandler(EngagementDbContext dbContext)
    : ICommandHandler<CreateSupportCategoryCommand, SupportCategoryResponse>
{
    public async Task<Result<SupportCategoryResponse>> HandleAsync(
        CreateSupportCategoryCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);
        var created = SupportCategory.Create(command.Spec);
        if (created.IsFailure)
        {
            return Result.Failure<SupportCategoryResponse>(created.Errors);
        }

        var conflict = await dbContext.SupportCategories.AnyAsync(
            c => c.Slug == created.Value.Slug,
            cancellationToken
        );
        if (conflict)
        {
            return EngagementErrors.SupportCategorySlugConflict;
        }

        _ = dbContext.SupportCategories.Add(created.Value);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return SupportProjectionMapper.ToResponse(created.Value);
    }
}
