using Microsoft.EntityFrameworkCore;
using Npgsql;
using Ruth.Modules.Core.Contracts.DefinedTypes;
using Ruth.Modules.Core.Domain;
using Ruth.Modules.Core.Infrastructure;
using Ruth.Modules.Core.Infrastructure.Caching;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Slices.DefinedTypes;

internal sealed class UpdateDefinedTypeCommandHandler(CoreDbContext dbContext, ICoreReadCache cache)
    : ICommandHandler<UpdateDefinedTypeCommand, DefinedTypeDetailResponse>
{
    public async Task<Result<DefinedTypeDetailResponse>> HandleAsync(
        UpdateDefinedTypeCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var id = DefinedTypeId.From(command.DefinedTypeId);
        var entity = await dbContext
            .DefinedTypes.Include(static dt => dt.Values)
            .FirstOrDefaultAsync(dt => dt.Id == id, cancellationToken);
        if (entity is null)
        {
            return CoreErrors.DefinedTypeNotFound;
        }

        var updated = entity.Update(
            command.Name,
            command.Description,
            command.HelpText,
            command.CategoryId.HasValue ? CategoryId.From(command.CategoryId.Value) : null,
            command.Order,
            command.IsActive,
            command.EnableSecurityOnValues,
            command.CategorizedValuesEnabled
        );
        if (updated.IsFailure)
        {
            return Result.Failure<DefinedTypeDetailResponse>(updated.Errors);
        }

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
