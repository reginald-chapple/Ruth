using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Tenancy.Contracts.Tenants;
using Ruth.Modules.Tenancy.Domain;
using Ruth.Modules.Tenancy.Infrastructure;
using Ruth.SharedKernel.Tenancy;

namespace Ruth.Modules.Tenancy.Slices.Compliance;

internal static class TenantComplianceMapper
{
    public static async Task<TenantComplianceStatusResponse?> LoadStatusAsync(
        TenancyDbContext dbContext,
        TenantId tenantId,
        bool stripeMockMode,
        CancellationToken cancellationToken
    )
    {
        var tenant = await dbContext
            .Tenants.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == tenantId, cancellationToken);
        if (tenant is null)
        {
            return null;
        }

        var domains = await dbContext
            .TenantDomains.AsNoTracking()
            .Where(x => x.TargetTenantId == tenantId)
            .OrderByDescending(x => x.IsPrimary)
            .ThenBy(x => x.CreatedOnUtc)
            .ToListAsync(cancellationToken);

        return Map(tenant, domains, stripeMockMode);
    }

    public static TenantComplianceStatusResponse Map(
        Tenant tenant,
        IReadOnlyList<TenantDomain> domains,
        bool stripeMockMode
    )
    {
        return new TenantComplianceStatusResponse(
            tenant.Id.Value,
            tenant.Slug,
            tenant.LegalName,
            tenant.DisplayName,
            tenant.Ein,
            tenant.IrsSubsectionCode,
            tenant.StateCode,
            tenant.TimeZoneId,
            tenant.LifecycleStage,
            tenant.IrsStatus,
            tenant.IrsOfficialName,
            tenant.IrsDeductibilityCode,
            tenant.IrsVerifiedOnUtc,
            tenant.StripeConnectAccountId,
            tenant.StripeChargesEnabled,
            tenant.StripePayoutsEnabled,
            stripeMockMode,
            tenant.SubmittedForReviewOnUtc,
            tenant.ApprovedOnUtc,
            tenant.ApprovedByOperatorEmail,
            tenant.ReviewNotes,
            [.. domains.Select(MapDomain)]
        );
    }

    public static TenantDomainResponse MapDomain(TenantDomain domain)
    {
        return new TenantDomainResponse(
            domain.Id.Value,
            domain.Hostname,
            domain.IsCustomDomain,
            domain.IsVerified,
            domain.IsPrimary,
            domain.VerificationTxtName,
            domain.VerificationTxtValue,
            domain.VerifiedOnUtc
        );
    }
}
