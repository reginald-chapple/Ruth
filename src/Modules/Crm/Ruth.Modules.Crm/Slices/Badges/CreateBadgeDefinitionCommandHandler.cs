using Ruth.Modules.Crm.Contracts.Badges;
using Ruth.Modules.Crm.Domain.Badges;
using Ruth.Modules.Crm.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Crm.Slices.Badges;

internal sealed class CreateBadgeDefinitionCommandHandler(CrmDbContext dbContext)
    : ICommandHandler<CreateBadgeDefinitionCommand, BadgeDefinitionResponse>
{
    public async Task<Result<BadgeDefinitionResponse>> HandleAsync(
        CreateBadgeDefinitionCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var created = BadgeDefinition.Create(
            command.Name,
            command.BadgeComponentKey,
            command.Description,
            command.IsActive,
            command.Order,
            command.Settings
        );
        if (created.IsFailure)
        {
            return Result.Failure<BadgeDefinitionResponse>(created.Errors);
        }

        _ = dbContext.BadgeDefinitions.Add(created.Value);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(ToResponse(created.Value));
    }

    internal static BadgeDefinitionResponse ToResponse(BadgeDefinition b)
    {
        return new BadgeDefinitionResponse(
            b.Id.Value,
            b.Name,
            b.Description,
            b.BadgeComponentKey,
            b.TargetEntityTypeKey,
            b.IsActive,
            b.Order,
            new Dictionary<string, string?>(b.Settings, StringComparer.OrdinalIgnoreCase)
        );
    }
}
