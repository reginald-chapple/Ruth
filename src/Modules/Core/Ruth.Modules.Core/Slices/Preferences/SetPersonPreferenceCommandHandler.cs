using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Core.Contracts.Preferences;
using Ruth.Modules.Core.Domain;
using Ruth.Modules.Core.Domain.Collaboration;
using Ruth.Modules.Core.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Core.Slices.Preferences;

internal sealed class SetPersonPreferenceCommandHandler(CoreDbContext dbContext, IClock clock)
    : ICommandHandler<SetPersonPreferenceCommand, PersonPreferenceResponse>
{
    public async Task<Result<PersonPreferenceResponse>> HandleAsync(
        SetPersonPreferenceCommand command,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(command.Key) || command.Key.Trim().Length > 250)
        {
            return CoreCollaborationErrors.PreferenceKeyInvalid;
        }

        var normalizedKey = command.Key.Trim();
        EntityTypeDescriptorId? etId = command.EntityTypeId.HasValue
            ? EntityTypeDescriptorId.From(command.EntityTypeId.Value)
            : null;
        var nowUtc = clock.UtcNow;

        var existing = await dbContext.PersonPreferences.FirstOrDefaultAsync(
            p => p.OwnerKey == command.OwnerKey && p.Key == normalizedKey,
            cancellationToken
        );

        PersonPreference entity;
        if (existing is not null)
        {
            existing.UpdateValue(command.Value, command.IsEnduring, etId, command.EntityId, nowUtc);
            entity = existing;
        }
        else
        {
            var created = PersonPreference.Create(
                command.OwnerKey,
                normalizedKey,
                command.Value,
                etId,
                command.EntityId,
                command.IsEnduring,
                nowUtc
            );
            if (created.IsFailure)
            {
                return Result.Failure<PersonPreferenceResponse>(created.Errors);
            }

            entity = created.Value;
            _ = dbContext.PersonPreferences.Add(entity);
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(
            new PersonPreferenceResponse(
                entity.Id.Value,
                entity.OwnerKey,
                entity.Key,
                entity.EntityTypeId?.Value,
                entity.EntityId,
                entity.Value,
                entity.IsEnduring,
                entity.LastAccessedOnUtc
            )
        );
    }
}
