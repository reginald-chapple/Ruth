using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Commerce.Contracts.Bookings;
using Ruth.Modules.Commerce.Domain;
using Ruth.Modules.Commerce.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Commerce.Slices.Bookings;

internal sealed class DeleteAvailabilityRuleCommandHandler(CommerceDbContext dbContext)
    : ICommandHandler<DeleteAvailabilityRuleCommand>
{
    public async Task<Result> HandleAsync(
        DeleteAvailabilityRuleCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);
        var id = AvailabilityRuleId.From(command.RuleId);
        var rule = await dbContext.AvailabilityRules.FirstOrDefaultAsync(
            r => r.Id == id,
            cancellationToken
        );
        if (rule is null)
        {
            return CommerceErrors.AvailabilityRuleNotFound;
        }

        _ = dbContext.AvailabilityRules.Remove(rule);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
