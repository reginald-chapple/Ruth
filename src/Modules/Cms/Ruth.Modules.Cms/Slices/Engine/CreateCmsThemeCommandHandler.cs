using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Cms.Contracts.Engine;
using Ruth.Modules.Cms.Domain;
using Ruth.Modules.Cms.Domain.Engine;
using Ruth.Modules.Cms.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Cms.Slices.Engine;

internal sealed class CreateCmsThemeCommandHandler(CmsDbContext db)
    : ICommandHandler<CreateCmsThemeCommand, CmsThemeResponse>
{
    public async Task<Result<CmsThemeResponse>> HandleAsync(
        CreateCmsThemeCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var themeRes = CmsTheme.Create(
            command.Name,
            command.Slug,
            command.Description,
            command.CssCustomProperties,
            command.CustomCss,
            command.IsActive
        );
        if (themeRes.IsFailure)
        {
            return Result.Failure<CmsThemeResponse>(themeRes.PrimaryError);
        }

        var exists = await db
            .Themes.AsNoTracking()
            .AnyAsync(t => t.Slug == themeRes.Value.Slug, cancellationToken);
        if (exists)
        {
            return Result.Failure<CmsThemeResponse>(
                CmsErrors.Conflict(
                    "Theme.DuplicateSlug",
                    $"Theme slug '{themeRes.Value.Slug}' already exists."
                )
            );
        }

        _ = db.Themes.Add(themeRes.Value);
        _ = await db.SaveChangesAsync(cancellationToken);
        return Result.Success(CmsProjectionMapper.ToResponse(themeRes.Value));
    }
}
