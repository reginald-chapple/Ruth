using System.Globalization;
using Ruth.Modules.CheckIn.Contracts.WalletPasses;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.CheckIn.Domain.WalletPasses;

/// <summary>Issued smart wallet pass aggregate tracking serial number, hashed auth/claim tokens, scan token, and live dynamic fields.</summary>
public sealed class IssuedWalletPass
    : AggregateRoot<IssuedWalletPassId>,
        IAuditableEntity,
        IHasConcurrencyToken
{
    /// <summary>Canonical entity type registration key.</summary>
    public const string EntityTypeKeyName = "Ruth.CheckIn.IssuedWalletPass";

    private IssuedWalletPass() { }

    private IssuedWalletPass(
        IssuedWalletPassId id,
        WalletPassTemplateId templateId,
        Guid personId,
        string serialNumber,
        string passTypeIdentifier,
        string scanToken,
        string authTokenSalt,
        string authTokenHash,
        string claimTokenHash,
        WalletPassCardStyle cardStyle,
        WalletPassDynamicFieldsDto dynamicFields,
        DateTimeOffset issuedAtUtc,
        DateTimeOffset? expiresAtUtc
    )
        : base(id)
    {
        TemplateId = templateId;
        PersonId = personId;
        SerialNumber = serialNumber;
        PassTypeIdentifier = passTypeIdentifier;
        ScanToken = scanToken;
        AuthTokenSalt = authTokenSalt;
        AuthTokenHash = authTokenHash;
        ClaimTokenHash = claimTokenHash;
        CardStyle = cardStyle;
        DynamicFields = dynamicFields;
        Status = IssuedWalletPassStatus.Active;
        Revision = 1;
        IssuedAtUtc = issuedAtUtc;
        LastUpdatedAtUtc = issuedAtUtc;
        UpdatedSequence = issuedAtUtc.ToUnixTimeMilliseconds();
        ExpiresAtUtc = expiresAtUtc;
    }

    public WalletPassTemplateId TemplateId { get; private set; }
    public Guid PersonId { get; private set; }
    public string SerialNumber { get; private set; } = string.Empty;
    public string PassTypeIdentifier { get; private set; } = string.Empty;
    public string ScanToken { get; private set; } = string.Empty;
    public string AuthTokenSalt { get; private set; } = string.Empty;
    public string AuthTokenHash { get; private set; } = string.Empty;
    public string ClaimTokenHash { get; private set; } = string.Empty;
    public WalletPassCardStyle CardStyle { get; private set; }
    public IssuedWalletPassStatus Status { get; private set; }
    public int Revision { get; private set; }
    public long UpdatedSequence { get; private set; }
    public WalletPassDynamicFieldsDto DynamicFields { get; private set; } = new();
    public DateTimeOffset IssuedAtUtc { get; private set; }
    public DateTimeOffset LastUpdatedAtUtc { get; private set; }
    public DateTimeOffset? ExpiresAtUtc { get; private set; }
    public string? RevocationReason { get; private set; }

    /// <summary>Opaque monotonic tag returned in PassKit passesUpdatedSince responses.</summary>
    public string UpdatedTag => UpdatedSequence.ToString(CultureInfo.InvariantCulture);

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

    /// <summary>Issues a new active <see cref="IssuedWalletPass" /> together with its raw claim token.</summary>
    public static Result<(IssuedWalletPass Pass, string RawClaimToken)> Issue(
        WalletPassTemplate template,
        Guid personId,
        WalletPassDynamicFieldsDto? dynamicFields,
        string passKitHmacSecret,
        DateTimeOffset nowUtc,
        DateTimeOffset? explicitExpiresAtUtc = null
    )
    {
        ArgumentNullException.ThrowIfNull(template);
        if (!template.IsActive)
        {
            return Result.Failure<(IssuedWalletPass, string)>(WalletPassErrors.TemplateInactive);
        }

        if (personId == Guid.Empty)
        {
            return Result.Failure<(IssuedWalletPass, string)>(CheckInErrors.PersonRequired);
        }

        var normalizedFields = NormalizeDynamicFields(dynamicFields);
        if (normalizedFields.IsFailure)
        {
            return Result.Failure<(IssuedWalletPass, string)>(normalizedFields.Errors);
        }

        var id = IssuedWalletPassId.Create();
        var authSalt = WalletPassSecurityTokens.GenerateSaltHex();
        var rawAuthToken = WalletPassSecurityTokens.DeriveAppleAuthToken(
            passKitHmacSecret,
            id.Value,
            authSalt
        );
        var rawClaimToken = WalletPassSecurityTokens.GenerateClaimToken();
        var issuedUtc = TruncateToWholeSeconds(nowUtc.ToUniversalTime());
        var resolvedExpiry = explicitExpiresAtUtc?.ToUniversalTime()
            ?? (
                template.DefaultValidityDays.HasValue
                    ? issuedUtc.AddDays(template.DefaultValidityDays.Value)
                    : null
            );

        var pass = new IssuedWalletPass(
            id,
            template.Id,
            personId,
            WalletPassSecurityTokens.GenerateSerialNumber(),
            template.PassTypeIdentifier,
            WalletPassSecurityTokens.GenerateScanToken(),
            authSalt,
            WalletPassSecurityTokens.ComputeSha256Hex(rawAuthToken),
            WalletPassSecurityTokens.ComputeSha256Hex(rawClaimToken),
            template.CardStyle,
            normalizedFields.Value,
            issuedUtc,
            resolvedExpiry
        );
        return (pass, rawClaimToken);
    }

    /// <summary>Verifies an ApplePass token in constant time against <see cref="AuthTokenHash" />.</summary>
    public bool VerifyAppleAuthToken(string? rawApplePassToken)
    {
        return WalletPassSecurityTokens.ConstantTimeMatchesHash(rawApplePassToken, AuthTokenHash);
    }

    /// <summary>Validates that this pass is currently active and unexpired for Kiosk check-in scanning.</summary>
    public Result EnsureActiveForScan(DateTimeOffset nowUtc)
    {
        return
            Status != IssuedWalletPassStatus.Active
            || (ExpiresAtUtc.HasValue && ExpiresAtUtc.Value <= nowUtc.ToUniversalTime())
            ? WalletPassErrors.PassInactiveOrExpired
            : Result.Success();
    }

    /// <summary>Updates live dynamic fields, increments revision, and advances the PassKit sequence tag.</summary>
    public Result UpdateDynamicFields(
        WalletPassDynamicFieldsDto? fields,
        DateTimeOffset? expiresAtUtc,
        DateTimeOffset nowUtc
    )
    {
        if (Status == IssuedWalletPassStatus.Revoked)
        {
            return WalletPassErrors.PassInactiveOrExpired;
        }

        var normalized = NormalizeDynamicFields(fields);
        if (normalized.IsFailure)
        {
            return Result.Failure(normalized.Errors);
        }

        DynamicFields = normalized.Value;
        if (expiresAtUtc.HasValue)
        {
            ExpiresAtUtc = expiresAtUtc.Value.ToUniversalTime();
        }

        BumpRevision(nowUtc);
        return Result.Success();
    }

    /// <summary>Revokes the pass permanently and advances the PassKit sequence tag.</summary>
    public Result Revoke(string? reason, DateTimeOffset nowUtc)
    {
        Status = IssuedWalletPassStatus.Revoked;
        RevocationReason = string.IsNullOrWhiteSpace(reason) ? "Revoked" : reason.Trim();
        BumpRevision(nowUtc);
        return Result.Success();
    }

    /// <summary>Repoints holder person reference during a CRM person merge.</summary>
    public void RepointPerson(Guid retiredPersonId, Guid survivingPersonId)
    {
        if (PersonId == retiredPersonId)
        {
            PersonId = survivingPersonId;
        }
    }

    private void BumpRevision(DateTimeOffset nowUtc)
    {
        Revision++;
        var truncatedNow = TruncateToWholeSeconds(nowUtc.ToUniversalTime());
        LastUpdatedAtUtc =
            truncatedNow <= LastUpdatedAtUtc ? LastUpdatedAtUtc.AddSeconds(1) : truncatedNow;
        UpdatedSequence = Math.Max(LastUpdatedAtUtc.ToUnixTimeMilliseconds(), UpdatedSequence + 1);
    }

    private static DateTimeOffset TruncateToWholeSeconds(DateTimeOffset value)
    {
        return DateTimeOffset.FromUnixTimeSeconds(value.ToUnixTimeSeconds());
    }

    private static Result<WalletPassDynamicFieldsDto> NormalizeDynamicFields(
        WalletPassDynamicFieldsDto? input
    )
    {
        var dto = input ?? new WalletPassDynamicFieldsDto();
        if (dto.PunchOrCreditBalance is < 0)
        {
            return Result.Failure<WalletPassDynamicFieldsDto>(
                WalletPassErrors.DynamicFieldsInvalid
            );
        }

        var customResult = NormalizeCustomFields(dto.CustomFields);
        return customResult.IsFailure
            ? Result.Failure<WalletPassDynamicFieldsDto>(customResult.Errors)
            : new WalletPassDynamicFieldsDto(
                Clean(dto.HolderDisplayName),
                Clean(dto.MembershipTier),
                dto.PunchOrCreditBalance,
                Clean(dto.NextShiftOrBookingSummary),
                Clean(dto.EventTicketSummary),
                customResult.Value
            );
    }

    private static Result<IReadOnlyList<WalletPassCustomFieldDto>> NormalizeCustomFields(
        IReadOnlyList<WalletPassCustomFieldDto>? fields
    )
    {
        if (fields is null || fields.Count == 0)
        {
            return Result.Success<IReadOnlyList<WalletPassCustomFieldDto>>([]);
        }

        if (fields.Count > 12)
        {
            return Result.Failure<IReadOnlyList<WalletPassCustomFieldDto>>(
                WalletPassErrors.DynamicFieldsInvalid
            );
        }

        var custom = new List<WalletPassCustomFieldDto>(fields.Count);
        foreach (var field in fields)
        {
            if (!IsValidCustomField(field))
            {
                return Result.Failure<IReadOnlyList<WalletPassCustomFieldDto>>(
                    WalletPassErrors.DynamicFieldsInvalid
                );
            }

            custom.Add(
                new WalletPassCustomFieldDto(
                    field.Key.Trim(),
                    field.Label.Trim(),
                    field.Value.Trim()
                )
            );
        }

        return Result.Success<IReadOnlyList<WalletPassCustomFieldDto>>(custom);
    }

    private static bool IsValidCustomField(WalletPassCustomFieldDto field)
    {
        return !string.IsNullOrWhiteSpace(field.Key)
            && field.Key.Trim().Length <= 40
            && !string.IsNullOrWhiteSpace(field.Label)
            && !string.IsNullOrWhiteSpace(field.Value);
    }

    private static string? Clean(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
