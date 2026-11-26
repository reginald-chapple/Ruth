using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Ruth.Modules.Finance.Contracts.PaymentMethods;
using Ruth.Modules.Finance.Domain.PaymentMethods;
using Ruth.Modules.Finance.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Finance.Slices.PaymentMethods;

internal sealed class ResolvePersonByBankAccountQueryHandler(
    FinanceDbContext dbContext,
    IOptions<FinanceSecurityOptions> securityOptions
) : IQueryHandler<ResolvePersonByBankAccountQuery, BankAccountIdentifierResponse>
{
    public async Task<Result<BankAccountIdentifierResponse>> HandleAsync(
        ResolvePersonByBankAccountQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);

        var digest = BankAccountIdentifierHasher.ComputeDigest(
            query.RoutingNumber,
            query.AccountNumber,
            securityOptions.Value.GetBankAccountHmacKeyBytes()
        );
        if (digest.IsFailure)
        {
            return Result.Failure<BankAccountIdentifierResponse>(digest.Errors);
        }

        var matched = await dbContext
            .BankAccountIdentifiers.AsNoTracking()
            .FirstOrDefaultAsync(
                b => b.RoutingAndAccountHash == digest.Value.HashHex,
                cancellationToken
            );

        return matched is null
            ? DomainError.NotFound(
                "Finance.BankIdentifier.NotFound",
                "No constituent bank account identifier matched the supplied routing and account numbers."
            )
            : SavedPaymentMethodMapper.ToResponse(matched);
    }
}
