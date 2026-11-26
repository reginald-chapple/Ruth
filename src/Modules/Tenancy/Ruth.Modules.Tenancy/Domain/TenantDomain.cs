using System.Security.Cryptography;
using Ruth.Modules.Tenancy.Contracts.Tenants;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Tenancy;

namespace Ruth.Modules.Tenancy.Domain;

/// <summary>Represents a sandbox subdomain or organization-owned custom domain routed to a <see cref="Tenant" />.</summary>
public sealed class TenantDomain : Entity<TenantDomainId>, IAuditableEntity
{
    private TenantDomain() { }

    private TenantDomain(
        TenantDomainId id,
        TenantId targetTenantId,
        string hostname,
        bool isCustomDomain,
        bool isVerified,
        bool isPrimary,
        bool makePrimaryWhenVerified,
        string? verificationTxtName,
        string? verificationTxtValue,
        DateTimeOffset createdOnUtc
    )
        : base(id)
    {
        TargetTenantId = targetTenantId;
        Hostname = hostname;
        IsCustomDomain = isCustomDomain;
        IsVerified = isVerified;
        IsPrimary = isPrimary;
        MakePrimaryWhenVerified = makePrimaryWhenVerified;
        VerificationTxtName = verificationTxtName;
        VerificationTxtValue = verificationTxtValue;
        CreatedOnUtc = createdOnUtc.ToUniversalTime();
        VerifiedOnUtc = isVerified ? CreatedOnUtc : null;
    }

    public TenantId TargetTenantId { get; private set; }

    public string Hostname { get; private set; } = string.Empty;

    public bool IsCustomDomain { get; private set; }

    public bool IsVerified { get; private set; }

    public bool IsPrimary { get; private set; }

    public bool MakePrimaryWhenVerified { get; private set; }

    public string? VerificationTxtName { get; private set; }

    public string? VerificationTxtValue { get; private set; }

    public DateTimeOffset? VerifiedOnUtc { get; private set; }

    public DateTimeOffset CreatedOnUtc { get; private set; }

    public DateTimeOffset? ModifiedOnUtc { get; private set; }

    public static TenantDomain CreateSandboxDomain(
        TenantId targetTenantId,
        string slug,
        string sandboxBaseDomain,
        DateTimeOffset nowUtc
    )
    {
        var hostname = $"{slug}.{sandboxBaseDomain}".ToLowerInvariant();
        return new TenantDomain(
            TenantDomainId.Create(),
            targetTenantId,
            hostname,
            isCustomDomain: false,
            isVerified: true,
            isPrimary: true,
            makePrimaryWhenVerified: false,
            verificationTxtName: null,
            verificationTxtValue: null,
            nowUtc
        );
    }

    public static Result<TenantDomain> CreateCustomDomain(
        TenantId targetTenantId,
        string? rawHostname,
        string reservedBaseDomain,
        bool makePrimaryWhenVerified,
        DateTimeOffset nowUtc
    )
    {
        var hostResult = TenantSlugPolicy.NormalizeAndValidateHostname(
            rawHostname,
            reservedBaseDomain
        );
        if (hostResult.IsFailure)
        {
            return Result.Failure<TenantDomain>(hostResult.Errors);
        }

        var token = $"domain-verify-{Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(16))}";
        var txtName = $"_org-verify.{hostResult.Value}";
        return new TenantDomain(
            TenantDomainId.Create(),
            targetTenantId,
            hostResult.Value,
            isCustomDomain: true,
            isVerified: false,
            isPrimary: false,
            makePrimaryWhenVerified,
            txtName,
            token,
            nowUtc
        );
    }

    public Result VerifyOwnership(string? observedToken, DateTimeOffset nowUtc)
    {
        if (IsVerified)
        {
            return Result.Success();
        }

        if (
            string.IsNullOrWhiteSpace(observedToken)
            || !string.Equals(observedToken.Trim(), VerificationTxtValue, StringComparison.Ordinal)
        )
        {
            return TenancyErrors.DomainVerificationMismatch;
        }

        IsVerified = true;
        VerifiedOnUtc = nowUtc.ToUniversalTime();
        return Result.Success();
    }

    public void SetPrimary(bool isPrimary)
    {
        IsPrimary = isPrimary;
    }

    public void SetCreated(DateTimeOffset createdOnUtc)
    {
        CreatedOnUtc = createdOnUtc.ToUniversalTime();
    }

    public void SetModified(DateTimeOffset modifiedOnUtc)
    {
        ModifiedOnUtc = modifiedOnUtc.ToUniversalTime();
    }
}
