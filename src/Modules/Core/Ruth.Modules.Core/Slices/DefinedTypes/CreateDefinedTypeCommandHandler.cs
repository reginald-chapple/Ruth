using Microsoft.EntityFrameworkCore;
using Npgsql;
using Ruth.Modules.Core.Contracts.DefinedTypes;
using Ruth.Modules.Core.Domain;
using Ruth.Modules.Core.Infrastructure;
using Ruth.Modules.Core.Infrastructure.Caching;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Slices.DefinedTypes;

internal sealed class CreateDefinedTypeCommandHandler(CoreDbContext dbContext, ICoreReadCache cache)
    : ICommandHandler<CreateDefinedTypeCommand, DefinedTypeDetailResponse>
{
    public async Task<Result<DefinedTypeDetailResponse>> HandleAsync(
        CreateDefinedTypeCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var created = DefinedType.Create(
            command.Name,
            command.Description,
            command.HelpText,
            command.CategoryId.HasValue ? CategoryId.From(command.CategoryId.Value) : null,
            command.Order,
            isSystem: false,
            command.EnableSecurityOnValues,
            command.CategorizedValuesEnabled
        );
        if (created.IsFailure)
        {
            return Result.Failure<DefinedTypeDetailResponse>(created.Errors);
        }

        var entity = created.Value;
        _ = dbContext.DefinedTypes.Add(entity);
        try
        {
            _ = await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is PostgresException pg
                && string.Equals(
                    pg.SqlState,
                    PostgresErrorCodes.UniqueViolation,
                    StringComparison.Ordinal
                )
            )
        {
            dbContext.ChangeTracker.Clear();
            return CoreErrors.DefinedTypeAlreadyExists;
        }

        await cache.InvalidateBucketAsync(CoreReadCache.DefinedTypesBucket, cancellationToken);
        return DefinedTypeMapper.ToDetailResponse(entity);
    }
}
