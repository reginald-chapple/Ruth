using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Finance.Domain.PaymentMethods;

/// <summary>Maps a keyed HMAC-SHA256 bank routing/account digest to a constituent Person for check-scanner and ACH matching.</summary>
public sealed class BankAccountIdentifier
    : AggregateRoot<BankAccountIdentifierId>,
        IAuditableEntity,
        IHasConcurrencyToken
{
    private BankAccountIdentifier() { }

    private BankAccountIdentifier(
        BankAccountIdentifierId id,
        Guid personId,
        string routingAndAccountHash,
        string maskedRoutingNumber,
        string maskedAccountNumber,
        string? bankName,
        DateTimeOffset verifiedOnUtc
    )
        : base(id)
    {
        PersonId = personId;
        RoutingAndAccountHash = routingAndAccountHash;
        MaskedRoutingNumber = maskedRoutingNumber;
        MaskedAccountNumber = maskedAccountNumber;
        BankName = bankName;
        VerifiedOnUtc = verifiedOnUtc.ToUniversalTime();
    }

    public Guid PersonId { get; private set; }
    public string RoutingAndAccountHash { get; private set; } = string.Empty;
    public string MaskedRoutingNumber { get; private set; } = string.Empty;
    public string MaskedAccountNumber { get; private set; } = string.Empty;
    public string? BankName { get; private set; }
    public DateTimeOffset VerifiedOnUtc { get; private set; }

    public DateTimeOffset CreatedOnUtc { get; private set; }
    public DateTimeOffset? ModifiedOnUtc { get; private set; }
    public Guid ConcurrencyStamp { get; private set; } = Guid.CreateVersion7();

    public void SetCreated(DateTimeOffset createdOnUtc)
    {
        CreatedOnUtc = createdOnUtc.ToUniversalTime();
    }

    public void SetModified(DateTimeOffset modifiedOnUtc)
    {
        ModifiedOnUtc = modifiedOnUtc.ToUniversalTime();
    }

    public void SetConcurrencyStamp(Guid newStamp)
    {
        ConcurrencyStamp = newStamp;
    }

    /// <summary>Creates a validated <see cref="BankAccountIdentifier" /> from plaintext routing/account inputs using a server secret pepper.</summary>
    public static Result<BankAccountIdentifier> Create(
        Guid personId,
        string routingNumber,
        string accountNumber,
        ReadOnlySpan<byte> secretKey,
        DateTimeOffset verifiedOnUtc,
        string? bankName = null
    )
    {
        if (personId == Guid.Empty)
        {
            return FinanceErrors.PaymentMethodPersonRequired;
        }

        var digest = BankAccountIdentifierHasher.ComputeDigest(
            routingNumber,
            accountNumber,
            secretKey
        );
        if (digest.IsFailure)
        {
            return Result.Failure<BankAccountIdentifier>(digest.Errors);
        }

        var trimmedBank = string.IsNullOrWhiteSpace(bankName) ? null : bankName.Trim();
        return new BankAccountIdentifier(
            BankAccountIdentifierId.Create(),
            personId,
            digest.Value.HashHex,
            digest.Value.MaskedRouting,
            digest.Value.MaskedAccount,
            trimmedBank is { Length: > 120 } ? trimmedBank[..120] : trimmedBank,
            verifiedOnUtc
        );
    }

    /// <summary>Updates owner or bank metadata when re-verifying an existing bank hash.</summary>
    public void Rebind(Guid personId, string? bankName, DateTimeOffset verifiedOnUtc)
    {
        if (personId != Guid.Empty)
        {
            PersonId = personId;
        }

        if (!string.IsNullOrWhiteSpace(bankName))
        {
            var trimmed = bankName.Trim();
            BankName = trimmed.Length <= 120 ? trimmed : trimmed[..120];
        }

        VerifiedOnUtc = verifiedOnUtc.ToUniversalTime();
    }

    /// <summary>Repoints <see cref="PersonId" /> when duplicate person records are merged.</summary>
    public bool RepointPerson(Guid retiredPersonId, Guid survivingPersonId)
    {
        if (PersonId != retiredPersonId || survivingPersonId == Guid.Empty)
        {
            return false;
        }

        PersonId = survivingPersonId;
        return true;
    }
}
