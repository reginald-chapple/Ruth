using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Crm.Contracts.Signals;
using Ruth.Modules.Crm.Domain;
using Ruth.Modules.Crm.Domain.Signals;
using Ruth.Modules.Crm.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Crm.Slices.Signals;

internal sealed class CreateSignalTypeCommandHandler(CrmDbContext dbContext)
    : ICommandHandler<CreateSignalTypeCommand, SignalTypeResponse>
{
    public async Task<Result<SignalTypeResponse>> HandleAsync(
        CreateSignalTypeCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var created = SignalType.Create(
            command.Name,
            command.SignalColor,
            command.SignalIcon,
            command.Description,
            command.Order
        );
        if (created.IsFailure)
        {
            return Result.Failure<SignalTypeResponse>(created.Errors);
        }

        var normalized = created.Value.NormalizedName;
        var exists = await dbContext.SignalTypes.AnyAsync(
            s => s.NormalizedName == normalized,
            cancellationToken
        );
        if (exists)
        {
            return CrmErrors.SignalTypeDuplicate;
        }

        _ = dbContext.SignalTypes.Add(created.Value);
        _ = await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(ToResponse(created.Value));
    }

    internal static SignalTypeResponse ToResponse(SignalType type)
    {
        return new SignalTypeResponse(
            type.Id.Value,
            type.Name,
            type.Description,
            type.SignalColor,
            type.SignalIcon,
            type.Order
        );
    }
}
