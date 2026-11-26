using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Ruth.Modules.Finance.Contracts.PaymentMethods;
using Ruth.Modules.Finance.Domain.PaymentMethods;
using Ruth.Modules.Finance.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Finance.Slices.PaymentMethods;

internal sealed class RegisterBankAccountIdentifierCommandHandler(
    FinanceDbContext dbContext,
    IOptions<FinanceSecurityOptions> securityOptions,
    IClock clock
) : ICommandHandler<RegisterBankAccountIdentifierCommand, BankAccountIdentifierResponse>
{
    public async Task<Result<BankAccountIdentifierResponse>> HandleAsync(
        RegisterBankAccountIdentifierCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var keyBytes = securityOptions.Value.GetBankAccountHmacKeyBytes();
        var candidate = BankAccountIdentifier.Create(
            command.PersonId,
            command.RoutingNumber,
            command.AccountNumber,
            keyBytes,
            clock.UtcNow,
            command.BankName
        );
        if (candidate.IsFailure)
        {
            return Result.Failure<BankAccountIdentifierResponse>(candidate.Errors);
        }

        var existing = await dbContext.BankAccountIdentifiers.FirstOrDefaultAsync(
            b => b.RoutingAndAccountHash == candidate.Value.RoutingAndAccountHash,
            cancellationToken
        );
        if (existing is not null)
        {
            existing.Rebind(command.PersonId, command.BankName, clock.UtcNow);
            _ = await dbContext.SaveChangesAsync(cancellationToken);
            return SavedPaymentMethodMapper.ToResponse(existing);
        }

        _ = dbContext.BankAccountIdentifiers.Add(candidate.Value);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return SavedPaymentMethodMapper.ToResponse(candidate.Value);
    }
}
